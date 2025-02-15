// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Runtime;

using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;
using System.Numerics;

namespace IronPython2.Runtime {
    [PythonType("buffer"), DontMapGetMemberNamesToDir]
    public sealed class PythonBuffer : ICodeFormattable, IDynamicMetaObjectProvider, IList<byte> {
        internal object _object;
        private int _offset;
        private readonly CodeContext/*!*/ _context;

        [Python3Warning("buffer() not supported in 3.x")]
        public PythonBuffer(CodeContext/*!*/ context, object @object, int offset=0, int size=-1) {
            PythonOps.Warn3k(context, "buffer() not supported in 3.x");
            if (!InitBufferObject(@object, offset, size)) {
                throw PythonOps.TypeError("expected buffer object");
            }
            _context = context;
        }

        private bool InitBufferObject(object o, int offset, int size) {
            if (offset < 0) {
                throw PythonOps.ValueError("offset must be zero or positive");
            } else if (size < -1) {
                //  -1 is the way to ask for the default size so we allow -1 as a size
                throw PythonOps.ValueError("size must be zero or positive");
            }

            //  we currently support only buffers, strings and arrays
            //  of primitives, strings, bytes, and bytearray objects.
            int length;
            if (o is PythonBuffer py) {
                o = py._object; // grab the internal object
                length = py.Size;
            } else if (o is string strobj) {
                length = strobj.Length;
            } else if (o is Bytes) {
                length = ((Bytes)o).Count;
            } else if (o is ByteArray) {
                length = ((ByteArray)o).Count;
            } else if (o is Array || o is IPythonArray) {
                if (o is Array arr) {
                    Type t = arr.GetType().GetElementType();
                    if (!t.IsPrimitive && t != typeof(string)) {
                        return false;
                    }
                    length = arr.Length;
                } else {
                    IPythonArray pa = (IPythonArray)o;
                    length = pa.Count;
                }
            } else if (o is IPythonBufferable) {
                length = ((IPythonBufferable)o).Size;
                _object = o;
            } else {
                return false;
            }

            // reset the size based on the given buffer's original size
            if (size >= (length - offset) || size == -1) {
                Size = length - offset;
            } else {
                Size = size;
            }

            _object = o;
            _offset = offset;

            return true;
        }

        public override string ToString() {
            object res = GetSelectedRange();
            if (res is Bytes) {
                return PythonOps.MakeString((Bytes)res);
            } else if (res is ByteArray) {
                return PythonOps.MakeString((ByteArray)res);
            } else if (res is IPythonBufferable) {
                return PythonOps.MakeString((IList<byte>)GetSelectedRange());
            } else if (res is byte[]) {
                return ((byte[])GetSelectedRange()).MakeString();
            }

            return res.ToString();
        }

        public int __cmp__([NotNull]PythonBuffer other) {
            if (Object.ReferenceEquals(this, other)) return 0;

            return PythonOps.Compare(ToString(), other.ToString());
        }

        [PythonHidden]
        public override bool Equals(object obj) {
            if (!(obj is PythonBuffer b)) {
                return false;
            }

            return __cmp__(b) == 0;
        }

        public override int GetHashCode() {
            return _object.GetHashCode() ^ _offset ^ (Size << 16 | (Size >> 16));
        }

        private Slice GetSlice() {
            object end = null;
            if (Size >= 0) {
                end = _offset + Size;
            }
            return new Slice(_offset, end);
        }

        public object __getslice__(object start, object stop) {
            return this[new Slice(start, stop)];
        }

        private static Exception ReadOnlyError() {
            return PythonOps.TypeError("buffer is read-only");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public object __setslice__(object start, object stop, object value) {
            throw ReadOnlyError();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void __delitem__(int index) {
            throw ReadOnlyError();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void __delslice__(object start, object stop) {
            throw ReadOnlyError();
        }

        public object this[object s] {
            [SpecialName]
            get {
                return PythonOps.GetIndex(_context, GetSelectedRange(), s);
            }
            [SpecialName]
            set {
                throw ReadOnlyError();
            }
        }

        private object GetSelectedRange() {
            if (_object is IPythonArray arr) {
                return arr.tostring();
            }

            if (_object is ByteArray bytearr) {
                return new Bytes((IList<byte>)bytearr[GetSlice()]);
            }

            if (_object is IPythonBufferable pyBuf) {
                return new Bytes(pyBuf.GetBytes(_offset, Size));
            }

            return PythonOps.GetIndex(_context, _object, GetSlice());
        }

        public static object operator +(PythonBuffer a, PythonBuffer b) {
            PythonContext context = a._context.LanguageContext;

            return context.Operation(
                PythonOperationKind.Add,
                PythonOps.GetIndex(a._context, a._object, a.GetSlice()),
                PythonOps.GetIndex(a._context, b._object, b.GetSlice())
            );
        }

        public static object operator +(PythonBuffer a, string b) {
            return a.ToString() + b;
        }

        public static object operator *(PythonBuffer b, int n) {
            PythonContext context = b._context.LanguageContext;

            return context.Operation(
                PythonOperationKind.Multiply,
                PythonOps.GetIndex(b._context, b._object, b.GetSlice()),
                n
            );
        }

        public static object operator *(int n, PythonBuffer b) {
            PythonContext context = b._context.LanguageContext;

            return context.Operation(
                PythonOperationKind.Multiply,
                PythonOps.GetIndex(b._context, b._object, b.GetSlice()),
                n
            );
        }

        public int __len__() {
            return Math.Max(Size, 0);
        }

        internal int Size { get; private set; }

        #region ICodeFormattable Members

        public string/*!*/ __repr__(CodeContext/*!*/ context) {
            return string.Format("<read-only buffer for 0x{0:X16}, size {1}, offset {2} at 0x{3:X16}>",
                PythonOps.Id(_object), Size, _offset, PythonOps.Id(this));
        }

        #endregion

        /// <summary>
        /// A DynamicMetaObject which is just used to support custom conversions to COM.
        /// </summary>
        class BufferMeta : DynamicMetaObject, IComConvertible {
            public BufferMeta(Expression expr, BindingRestrictions restrictions, object value)
                : base(expr, restrictions, value) {
            }

            #region IComConvertible Members

            DynamicMetaObject IComConvertible.GetComMetaObject() {
                return new DynamicMetaObject(
                    Expression.Call(
                        typeof(PythonOps).GetMethod(nameof(PythonOps.ConvertBufferToByteArray)),
                        Utils.Convert(
                            Expression,
                            typeof(PythonBuffer)
                        )
                    ),
                    BindingRestrictions.Empty
                );
            }

            #endregion
        }

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new BufferMeta(parameter, BindingRestrictions.Empty, this);
        }

        #endregion

        #region IList[System.Byte] implementation
        byte[] _objectByteCache = null;
        internal byte[] byteCache {
            get {
                return _objectByteCache ?? (_objectByteCache = PythonOps.ConvertBufferToByteArray(this));
            }
        }

        [PythonHidden]
        int IList<byte>.IndexOf(byte item) {
            for (int i = 0; i < byteCache.Length; ++i) {
                if (byteCache[i] == item)
                    return i;
            }

            return -1;
        }

        [PythonHidden]
        void IList<byte>.Insert(int index, byte item) {
            throw ReadOnlyError();
        }

        [PythonHidden]
        void IList<byte>.RemoveAt(int index) {
            throw ReadOnlyError();
        }

        byte IList<byte>.this[int index] {
            [PythonHidden]
            get {
                return byteCache[index];
            }

            [PythonHidden]
            set {
                throw ReadOnlyError();
            }
        }
        #endregion

        #region IEnumerable implementation
        [PythonHidden]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return byteCache.GetEnumerator();
        }
        #endregion

        #region IEnumerable[System.Byte] implementation
        [PythonHidden]
        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() {
            return ((IEnumerable<byte>)byteCache).GetEnumerator();
        }
        #endregion

        #region ICollection[System.Byte] implementation
        [PythonHidden]
        void ICollection<byte>.Add(byte item) {
            throw ReadOnlyError();
        }

        [PythonHidden]
        void ICollection<byte>.Clear() {
            throw ReadOnlyError();
        }

        [PythonHidden]
        bool ICollection<byte>.Contains(byte item) {
            return ((IList<byte>)this).IndexOf(item) != -1;
        }

        [PythonHidden]
        void ICollection<byte>.CopyTo(byte[] array, int arrayIndex) {
            byteCache.CopyTo(array, arrayIndex);
        }

        [PythonHidden]
        bool ICollection<byte>.Remove(byte item) {
            throw ReadOnlyError();
        }

        int ICollection<byte>.Count {
            [PythonHidden]
            get {
                return byteCache.Length;
            }
        }

        bool ICollection<byte>.IsReadOnly {
            [PythonHidden]
            get {
                return true;
            }
        }

        #endregion       
    }

    /// <summary>
    /// A marker interface so we can recognize and access sequence members on our array objects.
    /// </summary>
    internal interface IPythonArray : IList<object> {
        string tostring();
    }

    public interface IPythonBufferable {
        IntPtr UnsafeAddress {
            get;
        }

        int Size {
            get;
        }

        byte[] GetBytes(int offset, int length);
    }
}
