// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using IronPython2.Runtime.Exceptions;
using IronPython2.Runtime.Types;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using System.Numerics;

using SpecialNameAttribute = System.Runtime.CompilerServices.SpecialNameAttribute;

namespace IronPython2.Runtime.Operations {
    /// <summary>
    /// ExtensibleString is the base class that is used for types the user defines
    /// that derive from string.  It carries along with it the string's value and
    /// our converter recognizes it as a string.
    /// </summary>
    public class ExtensibleString : Extensible<string>, ICodeFormattable, IStructuralEquatable {
        public ExtensibleString() : base(String.Empty) { }
        public ExtensibleString(string self) : base(self) { }

        public override string ToString() {
            return Value;
        }

        #region ICodeFormattable Members

        public virtual string/*!*/ __repr__(CodeContext/*!*/ context) {
            return StringOps.Quote(Value);
        }

        #endregion        

        [return: MaybeNotImplemented]
        public object __eq__(object other) {
            if (other is string || other is ExtensibleString || other is Bytes) {
                return ScriptingRuntimeHelpers.BooleanToObject(EqualsWorker(other));
            }

            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public object __ne__(object other) {
            if (other is string || other is ExtensibleString || other is Bytes) {
                return ScriptingRuntimeHelpers.BooleanToObject(!EqualsWorker(other));
            }

            return NotImplementedType.Value;
        }

        #region IStructuralEquatable Members

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            if (comparer is PythonContext.PythonEqualityComparer) {
                return GetHashCode();
            }

            return ((IStructuralEquatable)PythonTuple.MakeTuple(Value.ToCharArray())).GetHashCode(comparer);
        }

        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer) {
            if (comparer is PythonContext.PythonEqualityComparer) {
                return EqualsWorker(other);
            }

            if (other is ExtensibleString es) return EqualsWorker(es.Value, comparer);
            if (other is string os) return EqualsWorker(os, comparer);
            if (other is Bytes tempBytes) return EqualsWorker(tempBytes.ToString(), comparer);

            return false;
        }

        private bool EqualsWorker(object other) {
            if (other == null) return false;

            if (other is ExtensibleString es) return Value == es.Value;
            if (other is string os) return Value == os;
            if (other is Bytes tempBytes) return Value == tempBytes.ToString();

            return false;
        }

        private bool EqualsWorker(string/*!*/ other, IEqualityComparer comparer) {
            Debug.Assert(other != null);

            if (Value.Length != other.Length) {
                return false;
            } else if (Value.Length == 0) {
                // 2 empty strings are equal
                return true;
            }

            for (int i = 0; i < Value.Length; i++) {
                if (!comparer.Equals(Value[i], other[i])) {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region ISequence Members

        public virtual object this[int index] {
            get { return ScriptingRuntimeHelpers.CharToString(Value[index]); }
        }

        public object this[Slice slice] {
            get { return StringOps.GetItem(Value, slice); }
        }

        public object __getslice__(int start, int stop) {
            return StringOps.__getslice__(Value, start, stop);
        }

        #endregion

        #region IPythonContainer Members

        public virtual int __len__() {
            return Value.Length;
        }

        public virtual bool __contains__(object value) {
            if (value is string) return Value.Contains((string)value);
            else if (value is ExtensibleString) return Value.Contains(((ExtensibleString)value).Value);
            else if (value is Bytes) return Value.Contains(value.ToString());

            throw PythonOps.TypeErrorForBadInstance("expected string, got {0}", value);
        }

        #endregion

    }

    [PythonHidden]
    public class StringBufferProtocol : IBufferProtocol {
        private string _wrapped;
        
        public StringBufferProtocol(string wrapped) {
            if(wrapped.Any(x => x > 255)) {
                throw PythonOps.TypeError("cannot make memory view because object does not have the buffer interface");
            }
            _wrapped = wrapped;
        }

        int IBufferProtocol.ItemCount => _wrapped.Length;

        string IBufferProtocol.Format => "B";

        BigInteger IBufferProtocol.ItemSize => 1;

        BigInteger IBufferProtocol.NumberDimensions => 1;

        bool IBufferProtocol.ReadOnly => true;

        PythonTuple IBufferProtocol.Strides => PythonTuple.MakeTuple(1);

        object IBufferProtocol.SubOffsets => null;

        Bytes IBufferProtocol.GetItem(int index) {
            lock (this) {
                return Bytes.Make(new byte[] { (byte)_wrapped[PythonOps.FixIndex(index, _wrapped.Length)] });
            }
        }

        IList<BigInteger> IBufferProtocol.GetShape(int start, int? end) {
            if (end != null) {
                return new[] { (BigInteger)end - start };
            }
            return new[] { (BigInteger)_wrapped.Length - start };
        }

        void IBufferProtocol.SetItem(int index, object value) {
            throw PythonOps.TypeError("cannot modify read-only memory");
        }

        void IBufferProtocol.SetSlice(Slice index, object value) {
            throw PythonOps.TypeError("cannot modify read-only memory");
        }

        Bytes IBufferProtocol.ToBytes(int start, int? end) {
            if (start == 0 && end == null) {
                return new Bytes(_wrapped.MakeByteArray());
            }

            return new Bytes(StringOps.GetItem(_wrapped, new Slice(start, end)).MakeByteArray());
        }

        List IBufferProtocol.ToList(int start, int? end) {
            string s = StringOps.GetItem(_wrapped, new Slice(start, end));
            if (String.IsNullOrEmpty(s)) {
                return new List();
            }

            return new List(s.MakeByteArray());
        }
    }

    /// <summary>
    /// StringOps is the static class that contains the methods defined on strings, i.e. 'abc'
    /// 
    /// Here we define all of the methods that a Python user would see when doing dir('abc').
    /// If the user is running in a CLS aware context they will also see all of the methods
    /// defined in the CLS System.String type.
    /// </summary>
    public static class StringOps {
        internal const int LowestUnicodeValue = 0x7f;

        internal static object FastNew(CodeContext/*!*/ context, object x) {
            if (x == null) {
                return "None";
            }

            if (x is string xstr) {
                return xstr;
            }

            // we don't invoke PythonOps.StringRepr here because we want to return the 
            // Extensible<string> directly back if that's what we received from __str__.
            object value = PythonContext.InvokeUnaryOperator(context, UnaryOperators.String, x);
            if (value is string || value is Extensible<string>) {
                return value;
            }

            throw PythonOps.TypeError("expected str, got {0} from __str__", DynamicHelpers.GetPythonType(value).Name);
        }

        internal static string FastNewUnicode(CodeContext context, object value, object encoding, object errors) {
            if (!(errors is string strErrors)) {
                throw PythonOps.TypeError("unicode() argument 3 must be string, not {0}", PythonTypeOps.GetName(errors));
            }

            if (value != null) {
                if (value is string strValue) {
                    return RawDecode(context, strValue, encoding, strErrors);
                }

                if (value is Extensible<string> es) {
                    return RawDecode(context, es.Value, encoding, strErrors);
                }

                if (value is Bytes bytes) {
                    return RawDecode(context, bytes.ToString(), encoding, strErrors);
                }

                if (value is PythonBuffer buffer) {
                    return RawDecode(context, buffer.ToString(), encoding, strErrors);
                }
            }

            throw PythonOps.TypeError("coercing to Unicode: need string or buffer, {0} found", PythonTypeOps.GetName(value));
        }

        internal static object FastNewUnicode(CodeContext context, object value, object encoding) {
            return FastNewUnicode(context, value, encoding, "strict");
        }

        internal static object FastNewUnicode(CodeContext context, object value) {
            if (value == null) {
                return "None";
            } else if (value is string) {
                return value;
            }

            object res;
            if (value is OldInstance oi &&
                (oi.TryGetBoundCustomMember(context, "__unicode__", out res) || oi.TryGetBoundCustomMember(context, "__str__", out res))) {
                res = context.LanguageContext.Call(context, res);
                if (res is string || res is Extensible<string>) {
                    return res;
                }
                throw PythonOps.TypeError("coercing to Unicode: expected string, got {0}", PythonTypeOps.GetName(value));
            }


            if (PythonTypeOps.TryInvokeUnaryOperator(context, value, "__unicode__", out res) ||
                PythonTypeOps.TryInvokeUnaryOperator(context, value, "__str__", out res)) {
                if (res is string || res is Extensible<string>) {
                    return res;
                }
                throw PythonOps.TypeError("coercing to Unicode: expected string, got {0}", PythonTypeOps.GetName(value));
            }

            return FastNewUnicode(context, value, context.LanguageContext.DefaultEncoding.WebName, "strict");
        }

        #region Python Constructors

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls) {
            if (cls == TypeCache.String) {
                return "";
            } else {
                return cls.CreateInstance(context);
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, object @object) {
            if (cls == TypeCache.String) {
                return FastNew(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, [NotNull]string @object) {
            if (cls == TypeCache.String) {
                return @object;
            } else {
                return cls.CreateInstance(context, @object);
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, [NotNull]ExtensibleString @object) {
            if (cls == TypeCache.String) {
                return FastNew(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, char @object) {
            if (cls == TypeCache.String) {
                return ScriptingRuntimeHelpers.CharToString(@object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, [NotNull]BigInteger @object) {
            if (cls == TypeCache.String) {
                return @object.ToString();
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, [NotNull]Extensible<BigInteger> @object) {
            if (cls == TypeCache.String) {
                return FastNew(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, int @object) {
            if (cls == TypeCache.String) {
                return @object.ToString();
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, bool @object) {
            if (cls == TypeCache.String) {
                return @object.ToString();
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, double @object) {
            if (cls == TypeCache.String) {
                return DoubleOps.__str__(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, Extensible<double> @object) {
            if (cls == TypeCache.String) {
                return FastNew(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls, float @object) {
            if (cls == TypeCache.String) {
                return SingleOps.__str__(context, @object);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @object));
            }
        }

        [StaticExtensionMethod]
        public static object __new__(CodeContext/*!*/ context, PythonType cls,
            object @string,
            [DefaultParameterValue(null)] string encoding,
            [DefaultParameterValue("strict")] string errors) {

            if (cls == TypeCache.String) {
                return FastNewUnicode(context, @string, encoding ?? context.LanguageContext.GetDefaultEncodingName(), errors);
            } else {
                return cls.CreateInstance(context, __new__(context, TypeCache.String, @string, encoding, errors));
            }
        }

        #endregion

        #region Python __ methods

        public static bool __contains__(string s, [BytesConversion]string item) {
            return s.Contains(item);
        }

        public static bool __contains__(string s, char item) {
            return s.IndexOf(item) != -1;
        }

        public static string __format__(CodeContext/*!*/ context, string self, [BytesConversion]string formatSpec) {
            return ObjectOps.__format__(context, self, formatSpec);
        }

        public static int __len__(string s) {
            return s.Length;
        }

        [SpecialName]
        public static string GetItem(string s, int index) {
            return ScriptingRuntimeHelpers.CharToString(s[PythonOps.FixIndex(index, s.Length)]);
        }

        [SpecialName]
        public static string GetItem(string s, object index) {
            return GetItem(s, Converter.ConvertToIndex(index));
        }

        [SpecialName]
        public static string GetItem(string s, Slice slice) {
            if (slice == null) throw PythonOps.TypeError("string indices must be slices or integers");
            int start, stop, step;
            slice.indices(s.Length, out start, out stop, out step);
            if (step == 1) {
                return stop > start ? s.Substring(start, stop - start) : String.Empty;
            } else {
                int index = 0;
                char[] newData;
                if (step > 0) {
                    if (start > stop) return String.Empty;

                    int icnt = (stop - start + step - 1) / step;
                    newData = new char[icnt];
                    for (int i = start; i < stop; i += step) {
                        newData[index++] = s[i];
                    }
                } else {
                    if (start < stop) return String.Empty;

                    int icnt = (stop - start + step + 1) / step;
                    newData = new char[icnt];
                    for (int i = start; i > stop; i += step) {
                        newData[index++] = s[i];
                    }
                }
                return new string(newData);
            }
        }

        public static string __getslice__(string self, int x, int y) {
            Slice.FixSliceArguments(self.Length, ref x, ref y);
            if (x >= y) return String.Empty;

            return self.Substring(x, y - x);
        }


        #endregion

        #region Public Python methods

        /// <summary>
        /// Returns a copy of this string converted to uppercase
        /// </summary>
        public static string capitalize(this string self) {
            if (self.Length == 0) return self;
            return Char.ToUpperInvariant(self[0]) + self.Substring(1).ToLowerInvariant();
        }

        //  default fillchar (padding char) is a space
        public static string center(this string self, int width) {
            return center(self, width, ' ');
        }

        public static string center(this string self, int width, char fillchar) {
            int spaces = width - self.Length;
            if (spaces <= 0) return self;

            StringBuilder ret = new StringBuilder(width);
            ret.Append(fillchar, spaces / 2);
            ret.Append(self);
            ret.Append(fillchar, (spaces + 1) / 2);
            return ret.ToString();
        }

        public static int count(this string self, [BytesConversion]string sub) {
            return count(self, sub, 0, self.Length);
        }

        public static int count(this string self, [BytesConversion]string sub, int start) {
            return count(self, sub, start, self.Length);
        }

        public static int count(this string self, [BytesConversion]string ssub, int start, int end) {
            if (ssub == null) throw PythonOps.TypeError("expected string for 'sub' argument, got NoneType");

            if (start > self.Length) {
                return 0;
            }

            start = PythonOps.FixSliceIndex(start, self.Length);
            end = PythonOps.FixSliceIndex(end, self.Length);

            if (ssub.Length == 0) {
                return Math.Max((end - start) + 1, 0);
            }

            int count = 0;
            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            while (true) {
                if (end <= start) break;
                int index = c.IndexOf(self, ssub, start, end - start, CompareOptions.Ordinal);
                if (index == -1) break;
                count++;
                start = index + ssub.Length;
            }
            return count;
        }

        public static string decode(CodeContext/*!*/ context, string s) {
            return decode(context, s, Missing.Value, "strict");
        }

        public static string decode(CodeContext/*!*/ context, string s, [Optional]object encoding, [DefaultParameterValue("strict")]string errors) {
            return RawDecode(context, s, encoding, errors);
        }

        public static string encode(CodeContext/*!*/ context, string s, [Optional]object encoding, [DefaultParameterValue("strict")]string errors) {
            return RawEncode(context, s, encoding, errors);
        }

        private static string CastString(object o) {
            if (o is string res) {
                return res;
            }

            return ((Extensible<string>)o).Value;
        }

        internal static string AsString(object o) {
            if (o is string res) {
                return res;
            }

            if (o is Extensible<string> es) {
                return es.Value;
            }

            return null;
        }

        public static bool endswith(this string self, object suffix) {
            TryStringOrTuple(suffix);
            if (suffix is PythonTuple)
                return endswith(self, (PythonTuple)suffix);
            else
                return endswith(self, CastString(suffix));
        }

        public static bool endswith(this string self, object suffix, int start) {
            TryStringOrTuple(suffix);
            if (suffix is PythonTuple)
                return endswith(self, (PythonTuple)suffix, start);
            else
                return endswith(self, CastString(suffix), start);
        }

        public static bool endswith(this string self, object suffix, int start, int end) {
            TryStringOrTuple(suffix);
            if (suffix is PythonTuple)
                return endswith(self, (PythonTuple)suffix, start, end);
            else
                return endswith(self, CastString(suffix), start, end);
        }

        public static string expandtabs(string self) {
            return expandtabs(self, 8);
        }

        public static string expandtabs(this string self, int tabsize) {
            StringBuilder ret = new StringBuilder(self.Length * 2);
            string v = self;
            int col = 0;
            for (int i = 0; i < v.Length; i++) {
                char ch = v[i];
                switch (ch) {
                    case '\n':
                    case '\r': col = 0; ret.Append(ch); break;
                    case '\t':
                        if (tabsize > 0) {
                            int tabs = tabsize - (col % tabsize);
                            int existingSize = ret.Capacity;
                            ret.Capacity = checked(existingSize + tabs);
                            ret.Append(' ', tabs);
                            col = 0;
                        }
                        break;
                    default:
                        col++;
                        ret.Append(ch);
                        break;
                }
            }
            return ret.ToString();
        }

        public static int find(this string self, [BytesConversion]string sub) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (sub.Length == 1) return self.IndexOf(sub[0]);

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            return c.IndexOf(self, sub, CompareOptions.Ordinal);
        }

        public static int find(this string self, [BytesConversion]string sub, int start) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            start = PythonOps.FixSliceIndex(start, self.Length);

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            return c.IndexOf(self, sub, start, CompareOptions.Ordinal);
        }

        public static int find(this string self, [BytesConversion]string sub, BigInteger start) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            return find(self, sub, (int)start);
        }

        public static int find(this string self, [BytesConversion]string sub, int start, int end) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            start = PythonOps.FixSliceIndex(start, self.Length);
            end = PythonOps.FixSliceIndex(end, self.Length);
            if (end < start) return -1;

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            return c.IndexOf(self, sub, start, end - start, CompareOptions.Ordinal);
        }

        public static int find(this string self, [BytesConversion]string sub, BigInteger start, BigInteger end) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            return find(self, sub, (int)start, (int)end);
        }

        public static int find(this string self, [BytesConversion]string sub, object start, [DefaultParameterValue(null)]object end) {
            return find(self, sub, CheckIndex(start, 0), CheckIndex(end, self.Length));
        }

        public static int index(this string self, [BytesConversion]string sub) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            return index(self, sub, 0, self.Length);
        }

        public static int index(this string self, [BytesConversion]string sub, int start) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            return index(self, sub, start, self.Length);
        }

        public static int index(this string self, [BytesConversion]string sub, int start, int end) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            int ret = find(self, sub, start, end);
            if (ret == -1) throw PythonOps.ValueError("substring {0} not found in {1}", sub, self);
            return ret;
        }

        public static int index(this string self, [BytesConversion]string sub, object start, [DefaultParameterValue(null)]object end) {
            return index(self, sub, CheckIndex(start, 0), CheckIndex(end, self.Length));
        }

        public static bool isalnum(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            for (int i = v.Length - 1; i >= 0; i--) {
                if (!Char.IsLetterOrDigit(v, i)) return false;
            }
            return true;
        }

        public static bool isalpha(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            for (int i = v.Length - 1; i >= 0; i--) {
                if (!Char.IsLetter(v, i)) return false;
            }
            return true;
        }

        public static bool isdigit(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            for (int i = v.Length - 1; i >= 0; i--) {
                // CPython considers the circled digits to be digits
                if (!Char.IsDigit(v, i) && (v[i] < '\u2460' || v[i] > '\u2468')) return false;
            }
            return true;
        }

        public static bool isspace(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            for (int i = v.Length - 1; i >= 0; i--) {
                if (!Char.IsWhiteSpace(v, i)) return false;
            }
            return true;
        }

        public static bool isdecimal(this string self) {
            return isnumeric(self);
        }

        public static bool isnumeric(this string self) {
            if (String.IsNullOrEmpty(self)) return false;

            foreach (char c in self) {
                if (!Char.IsDigit(c)) return false;
            }
            return true;
        }

        public static bool islower(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            bool hasLower = false;
            for (int i = v.Length - 1; i >= 0; i--) {
                if (!hasLower && Char.IsLower(v, i)) hasLower = true;
                if (Char.IsUpper(v, i)) return false;
            }
            return hasLower;
        }

        public static bool isupper(this string self) {
            if (self.Length == 0) return false;
            string v = self;
            bool hasUpper = false;
            for (int i = v.Length - 1; i >= 0; i--) {
                if (!hasUpper && Char.IsUpper(v, i)) hasUpper = true;
                if (Char.IsLower(v, i)) return false;
            }
            return hasUpper;
        }

        /// <summary>
        /// return true if self is a titlecased string and there is at least one
        /// character in self; also, uppercase characters may only follow uncased
        /// characters (e.g. whitespace) and lowercase characters only cased ones.
        /// return false otherwise.
        /// </summary>
        public static bool istitle(this string self) {
            if (self == null || self.Length == 0) return false;

            string v = self;
            bool prevCharCased = false, currCharCased = false, containsUpper = false;
            for (int i = 0; i < v.Length; i++) {
                if (Char.IsUpper(v, i) || CharUnicodeInfo.GetUnicodeCategory(v, i) == UnicodeCategory.TitlecaseLetter) {
                    containsUpper = true;
                    if (prevCharCased)
                        return false;
                    else
                        currCharCased = true;
                } else if (Char.IsLower(v, i))
                    if (!prevCharCased)
                        return false;
                    else
                        currCharCased = true;
                else
                    currCharCased = false;
                prevCharCased = currCharCased;
            }

            //  if we've gone through the whole string and haven't encountered any rule 
            //  violations but also haven't seen an Uppercased char, then this is not a 
            //  title e.g. '\n', all whitespace etc.
            return containsUpper;
        }

        public static bool isunicode(this string self) {
            return self.Any(c => c >= LowestUnicodeValue);
        }

        /// <summary>
        /// Return a string which is the concatenation of the strings 
        /// in the sequence seq. The separator between elements is the 
        /// string providing this method
        /// </summary>
        public static string join(this string self, object sequence) {
            IEnumerator seq = PythonOps.GetEnumerator(sequence);
            if (!seq.MoveNext()) return "";

            // check if we have just a sequence of just one value - if so just
            // return that value.
            object curVal = seq.Current;
            if (!seq.MoveNext()) return Converter.ConvertToString(curVal);

            StringBuilder ret = new StringBuilder();
            AppendJoin(curVal, 0, ret);

            int index = 1;
            do {
                ret.Append(self);

                AppendJoin(seq.Current, index, ret);

                index++;
            } while (seq.MoveNext());

            return ret.ToString();
        }

        public static string join(this string/*!*/ self, [NotNull]List/*!*/ sequence) {
            if (sequence.__len__() == 0) return String.Empty;

            lock (sequence) {
                if (sequence.__len__() == 1) {
                    return Converter.ConvertToString(sequence[0]);
                }

                StringBuilder ret = new StringBuilder();

                AppendJoin(sequence._data[0], 0, ret);
                for (int i = 1; i < sequence._size; i++) {
                    if (!String.IsNullOrEmpty(self)) {
                        ret.Append(self);
                    }
                    AppendJoin(sequence._data[i], i, ret);
                }

                return ret.ToString();
            }
        }

        public static string ljust(this string self, int width) {
            return ljust(self, width, ' ');
        }

        public static string ljust(this string self, int width, char fillchar) {
            if (width < 0) return self;
            int spaces = width - self.Length;
            if (spaces <= 0) return self;

            StringBuilder ret = new StringBuilder(width);
            ret.Append(self);
            ret.Append(fillchar, spaces);
            return ret.ToString();
        }

        // required for better match with cpython upper/lower
        private static CultureInfo CasingCultureInfo = new CultureInfo("en");

        public static string lower(this string self) {
            return CasingCultureInfo.TextInfo.ToLower(self);
        }

        internal static string ToLowerAsciiTriggered(this string self) {
            for (int i = 0; i < self.Length; i++) {
                if (self[i] >= 'A' && self[i] <= 'Z') {
                    return self.ToLowerInvariant();
                }
            }
            return self;
        }

        public static string lstrip(this string self) {
            return self.TrimStart();
        }

        public static string lstrip(this string self, [BytesConversion]string chars) {
            if (chars == null) return lstrip(self);
            return self.TrimStart(chars.ToCharArray());
        }

        [return: SequenceTypeInfo(typeof(string))]
        public static PythonTuple partition(this string self, [BytesConversion]string sep) {
            if (sep == null)
                throw PythonOps.TypeError("expected string, got NoneType");
            if (sep.Length == 0)
                throw PythonOps.ValueError("empty separator");

            object[] obj = new object[3] { "", "", "" };

            if (self.Length != 0) {
                int index = find(self, sep);
                if (index == -1) {
                    obj[0] = self;
                } else {
                    obj[0] = self.Substring(0, index);
                    obj[1] = sep;
                    obj[2] = self.Substring(index + sep.Length, self.Length - index - sep.Length);
                }
            }
            return new PythonTuple(obj);
        }

        public static string replace(this string self, [BytesConversion]string old, [BytesConversion]string @new,
            [DefaultParameterValue(-1)]int count) {

            if (old == null) {
                throw PythonOps.TypeError("expected a character buffer object"); // cpython message
            }
            if (old.Length == 0) return ReplaceEmpty(self, @new, count);

            string v = self;
            int replacements = StringOps.count(v, old);
            replacements = (count < 0 || count > replacements) ? replacements : count;
            int newLength = v.Length;
            newLength -= replacements * old.Length;
            newLength = checked(newLength + replacements * @new.Length);
            StringBuilder ret = new StringBuilder(newLength);

            int index;
            int start = 0;
            while (count != 0 && (index = v.IndexOf(old, start, StringComparison.Ordinal)) != -1) {
                ret.Append(v, start, index - start);
                ret.Append(@new);
                start = index + old.Length;
                count--;
            }
            ret.Append(v.Substring(start));

            return ret.ToString();
        }

        public static int rfind(this string self, [BytesConversion]string sub) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            return rfind(self, sub, 0, self.Length);
        }

        public static int rfind(this string self, [BytesConversion]string sub, int start) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            return rfind(self, sub, start, self.Length);
        }

        public static int rfind(this string self, [BytesConversion]string sub, BigInteger start) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            return rfind(self, sub, (int)start, self.Length);
        }

        public static int rfind(this string self, [BytesConversion]string sub, int start, int end) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;

            start = PythonOps.FixSliceIndex(start, self.Length);
            end = PythonOps.FixSliceIndex(end, self.Length);

            if (start > end) return -1;     // can't possibly match anything, not even an empty string
            if (sub.Length == 0) return end;    // match at the end
            if (end == 0) return -1;    // can't possibly find anything

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            return c.LastIndexOf(self, sub, end - 1, end - start, CompareOptions.Ordinal);
        }

        public static int rfind(this string self, [BytesConversion]string sub, BigInteger start, BigInteger end) {
            if (sub == null) throw PythonOps.TypeError("expected string, got NoneType");
            if (start > self.Length) return -1;
            return rfind(self, sub, (int)start, (int)end);
        }

        public static int rfind(this string self, [BytesConversion]string sub, object start, [DefaultParameterValue(null)]object end) {
            return rfind(self, sub, CheckIndex(start, 0), CheckIndex(end, self.Length));
        }

        public static int rindex(this string self, [BytesConversion]string sub) {
            return rindex(self, sub, 0, self.Length);
        }

        public static int rindex(this string self, [BytesConversion]string sub, int start) {
            return rindex(self, sub, start, self.Length);
        }

        public static int rindex(this string self, [BytesConversion]string sub, int start, int end) {
            int ret = rfind(self, sub, start, end);
            if (ret == -1) throw PythonOps.ValueError("substring {0} not found in {1}", sub, self);
            return ret;
        }

        public static int rindex(this string self, [BytesConversion]string sub, object start, [DefaultParameterValue(null)]object end) {
            return rindex(self, sub, CheckIndex(start, 0), CheckIndex(end, self.Length));
        }

        public static string rjust(this string self, int width) {
            return rjust(self, width, ' ');
        }

        public static string rjust(this string self, int width, char fillchar) {
            int spaces = width - self.Length;
            if (spaces <= 0) return self;

            StringBuilder ret = new StringBuilder(width);
            ret.Append(fillchar, spaces);
            ret.Append(self);
            return ret.ToString();
        }

        [return: SequenceTypeInfo(typeof(string))]
        public static PythonTuple rpartition(this string self, [BytesConversion]string sep) {
            if (sep == null)
                throw PythonOps.TypeError("expected string, got NoneType");
            if (sep.Length == 0)
                throw PythonOps.ValueError("empty separator");

            object[] obj = new object[3] { "", "", "" };
            if (self.Length != 0) {
                int index = rfind(self, sep);
                if (index == -1) {
                    obj[2] = self;
                } else {
                    obj[0] = self.Substring(0, index);
                    obj[1] = sep;
                    obj[2] = self.Substring(index + sep.Length, self.Length - index - sep.Length);
                }
            }
            return new PythonTuple(obj);
        }

        //  when no maxsplit arg is given then just use split
        public static List rsplit(this string self) {
            return SplitInternal(self, (char[])null, -1);
        }

        public static List rsplit(this string self, [BytesConversion]string sep) {
            return rsplit(self, sep, -1);
        }

        public static List rsplit(this string self, [BytesConversion]string sep, int maxsplit) {
            //  rsplit works like split but needs to split from the right;
            //  reverse the original string (and the sep), split, reverse 
            //  the split list and finally reverse each element of the list
            string reversed = Reverse(self);
            if (sep != null) sep = Reverse(sep);
            List temp = null, ret = null;
            temp = split(reversed, sep, maxsplit);
            temp.reverse();
            int resultlen = temp.__len__();
            if (resultlen != 0) {
                ret = new List(resultlen);
                foreach (string s in temp)
                    ret.AddNoLock(Reverse(s));
            } else {
                ret = temp;
            }
            return ret;
        }

        public static string rstrip(this string self) {
            return self.TrimEnd();
        }

        public static string rstrip(this string self, [BytesConversion]string chars) {
            if (chars == null) return rstrip(self);
            return self.TrimEnd(chars.ToCharArray());
        }

        public static List split(this string self) {
            return SplitInternal(self, (char[])null, -1);
        }

        public static List split(this string self, [BytesConversion]string sep) {
            return split(self, sep, -1);
        }

        public static List split(this string self, [BytesConversion]string sep, int maxsplit) {
            if (sep == null) {
                if (maxsplit == 0) {
                    // Corner case for CPython compatibility
                    List result = PythonOps.MakeEmptyList(1);
                    result.AddNoLock(self.TrimStart());
                    return result;

                } else {
                    return SplitInternal(self, (char[])null, maxsplit);
                }
            }

            if (sep.Length == 0) {
                throw PythonOps.ValueError("empty separator");
            } else if (sep.Length == 1) {
                return SplitInternal(self, new char[] { sep[0] }, maxsplit);
            } else {
                return SplitInternal(self, sep, maxsplit);
            }
        }

        public static List splitlines(this string self) {
            return splitlines(self, false);
        }

        public static List splitlines(this string self, bool keepends) {
            List ret = new List();
            int i, linestart;
            for (i = 0, linestart = 0; i < self.Length; i++) {
                if (self[i] == '\n' || self[i] == '\r' || self[i] == '\x2028') {
                    //  special case of "\r\n" as end of line marker
                    if (i < self.Length - 1 && self[i] == '\r' && self[i + 1] == '\n') {
                        if (keepends)
                            ret.AddNoLock(self.Substring(linestart, i - linestart + 2));
                        else
                            ret.AddNoLock(self.Substring(linestart, i - linestart));
                        linestart = i + 2;
                        i++;
                    } else { //'\r', '\n', or unicode new line as end of line marker
                        if (keepends)
                            ret.AddNoLock(self.Substring(linestart, i - linestart + 1));
                        else
                            ret.AddNoLock(self.Substring(linestart, i - linestart));
                        linestart = i + 1;
                    }
                }
            }
            //  the last line needs to be accounted for if it is not empty
            if (i - linestart != 0)
                ret.AddNoLock(self.Substring(linestart, i - linestart));
            return ret;
        }
        public static bool startswith(this string self, object prefix) {
            TryStringOrTuple(prefix);
            if (prefix is PythonTuple)
                return startswith(self, (PythonTuple)prefix);
            else
                return startswith(self, CastString(prefix));
        }

        public static bool startswith(this string self, object prefix, int start) {
            TryStringOrTuple(prefix);
            if (prefix is PythonTuple)
                return startswith(self, (PythonTuple)prefix, start);
            else
                return startswith(self, CastString(prefix), start);
        }

        public static bool startswith(this string self, object prefix, int start, int end) {
            TryStringOrTuple(prefix);
            if (prefix is PythonTuple)
                return startswith(self, (PythonTuple)prefix, start, end);
            else
                return startswith(self, CastString(prefix), start, end);
        }

        public static string strip(this string self) {
            return self.Trim();
        }

        public static string strip(this string self, [BytesConversion]string chars) {
            if (chars == null) return strip(self);
            return self.Trim(chars.ToCharArray());
        }

        public static string swapcase(this string self) {
            StringBuilder ret = new StringBuilder(self);
            for (int i = 0; i < ret.Length; i++) {
                char ch = ret[i];
                if (Char.IsUpper(ch)) ret[i] = Char.ToLowerInvariant(ch);
                else if (Char.IsLower(ch)) ret[i] = Char.ToUpperInvariant(ch);
            }
            return ret.ToString();
        }

        public static string title(this string self) {
            if (self == null || self.Length == 0) return self;

            char[] retchars = self.ToCharArray();
            bool prevCharCased = false;
            bool currCharCased = false;
            int i = 0;
            do {
                if (Char.IsUpper(retchars[i]) || Char.IsLower(retchars[i])) {
                    if (!prevCharCased)
                        retchars[i] = Char.ToUpperInvariant(retchars[i]);
                    else
                        retchars[i] = Char.ToLowerInvariant(retchars[i]);
                    currCharCased = true;
                } else {
                    currCharCased = false;
                }
                i++;
                prevCharCased = currCharCased;
            }
            while (i < retchars.Length);
            return new string(retchars);
        }

        //translate on a unicode string differs from that on an ascii
        //for unicode, the table argument is actually a dictionary with
        //character ordinals as keys and the replacement strings as values
        public static string translate(this string self, [NotNull]PythonDictionary table) {
            if (table == null || self.Length == 0) {
                return self;
            }

            StringBuilder ret = new StringBuilder();
            for (int i = 0, idx = 0; i < self.Length; i++) {
                idx = (int)self[i];
                if (table.__contains__(idx)) {
                    var mapped = table[idx];
                    if (mapped == null) {
                        continue;
                    }
                    if (mapped is int) {
                        var mappedInt = (int)mapped;
                        if (mappedInt > 0xFFFF) {
                            throw PythonOps.TypeError("character mapping must be in range(0x%lx)");
                        }
                        ret.Append((char)(int)mapped);
                    } else if (mapped is String) {
                        ret.Append(mapped);
                    } else {
                        throw PythonOps.TypeError("character mapping must return integer, None or unicode");
                    }
                } else {
                    ret.Append(self[i]);
                }
            }
            return ret.ToString();
        }

        public static string translate(this string self, [BytesConversion]string table) {
            return translate(self, table, (string)null);
        }

        public static string translate(this string self, [BytesConversion]string table, [BytesConversion]string deletechars) {
            if (table != null && table.Length != 256) {
                throw PythonOps.ValueError("translation table must be 256 characters long");
            } else if (self.Length == 0) {
                return self;
            }

            // List<char> is about 2/3rds as expensive as StringBuilder appending individual 
            // char's so we use that instead of a StringBuilder
            List<char> res = new List<char>();
            for (int i = 0; i < self.Length; i++) {
                if (deletechars == null || !deletechars.Contains(Char.ToString(self[i]))) {
                    if (table != null) {
                        int idx = (int)self[i];
                        if (idx >= 0 && idx < 256) {
                            res.Add(table[idx]);
                        }
                    } else {
                        res.Add(self[i]);
                    }
                }
            }
            return new String(res.ToArray());
        }

        public static string upper(this string self) {
            return CasingCultureInfo.TextInfo.ToUpper(self);
        }

        public static string zfill(this string self, int width) {
            int spaces = width - self.Length;
            if (spaces <= 0) return self;

            StringBuilder ret = new StringBuilder(width);
            if (self.Length > 0 && IsSign(self[0])) {
                ret.Append(self[0]);
                ret.Append('0', spaces);
                ret.Append(self.Substring(1));
            } else {
                ret.Append('0', spaces);
                ret.Append(self);
            }
            return ret.ToString();
        }

        /// <summary>
        /// Replaces each replacement field in the string with the provided arguments.
        /// 
        /// replacement_field =  "{" field_name ["!" conversion] [":" format_spec] "}"
        /// field_name        =  (identifier | integer) ("." identifier | "[" element_index "]")*
        /// 
        /// format_spec: [[fill]align][sign][#][0][width][,][.precision][type]
        /// 
        /// Conversion can be 'r' for repr or 's' for string.
        /// </summary>
        public static string/*!*/ format(CodeContext/*!*/ context, string format_string, [NotNull]params object[] args) {
            return NewStringFormatter.FormatString(
                context.LanguageContext,
                format_string,
                PythonTuple.MakeTuple(args),
                new PythonDictionary()
            );
        }

        /// <summary>
        /// Replaces each replacement field in the string with the provided arguments.
        /// 
        /// replacement_field =  "{" field_name ["!" conversion] [":" format_spec] "}"
        /// field_name        =  (identifier | integer) ("." identifier | "[" element_index "]")*
        /// 
        /// format_spec: [[fill]align][sign][#][0][width][.precision][type]
        /// 
        /// Conversion can be 'r' for repr or 's' for string.
        /// </summary>
        public static string/*!*/ format(CodeContext/*!*/ context, string format_string\u00F8, [ParamDictionary]IDictionary<object, object> kwargs\u00F8, params object[] args\u00F8) {
            return NewStringFormatter.FormatString(
                context.LanguageContext,
                format_string\u00F8,
                PythonTuple.MakeTuple(args\u00F8),
                kwargs\u00F8
            );
        }

        public static IEnumerable<PythonTuple>/*!*/ _formatter_parser(this string/*!*/ self) {
            return NewStringFormatter.GetFormatInfo(self);
        }

        public static PythonTuple/*!*/ _formatter_field_name_split(this string/*!*/ self) {
            return NewStringFormatter.GetFieldNameInfo(self);
        }

        #endregion

        #region operators
        [SpecialName]
        public static string Add([NotNull]string self, [NotNull]string other) {
            return self + other;
        }

        [SpecialName]
        public static string Add([NotNull]string self, char other) {
            return self + other;
        }

        [SpecialName]
        public static string Add(char self, [NotNull]string other) {
            return self + other;
        }

        [SpecialName]
        public static string Mod(CodeContext/*!*/ context, string self, object other) {
            return new StringFormatter(context, self, other).Format();
        }

        [SpecialName]
        [return: MaybeNotImplemented]
        public static object Mod(CodeContext/*!*/ context, object other, string self) {
            if (other is string str) {
                return new StringFormatter(context, str, self).Format();
            }

            if (other is Extensible<string> es) {
                return new StringFormatter(context, es.Value, self).Format();
            }

            return NotImplementedType.Value;
        }

        [SpecialName]
        public static string Multiply(string s, int count) {
            if (count <= 0) return String.Empty;
            if (count == 1) return s;

            long size = (long)s.Length * (long)count;
            if (size > Int32.MaxValue) throw PythonOps.OverflowError("repeated string is too long");

            int sz = s.Length;
            if (sz == 1) return new string(s[0], count);

            StringBuilder ret = new StringBuilder(sz * count);
            ret.Insert(0, s, count);
            // the above code is MUCH faster than the simple loop
            //for (int i=0; i < count; i++) ret.Append(s);
            return ret.ToString();
        }

        [SpecialName]
        public static string Multiply(int other, string self) {
            return Multiply(self, other);
        }

        [SpecialName]
        public static object Multiply(string self, [NotNull]Index count) {
            return PythonOps.MultiplySequence<string>(Multiply, self, count, true);
        }

        [SpecialName]
        public static object Multiply([NotNull]Index count, string self) {
            return PythonOps.MultiplySequence<string>(Multiply, self, count, false);
        }

        [SpecialName]
        public static object Multiply(string self, object count) {
            int index;
            if (Converter.TryConvertToIndex(count, out index)) {
                return Multiply(self, index);
            }

            throw PythonOps.TypeErrorForUnIndexableObject(count);
        }

        [SpecialName]
        public static object Multiply(object count, string self) {
            int index;
            if (Converter.TryConvertToIndex(count, out index)) {
                return Multiply(index, self);
            }

            throw PythonOps.TypeErrorForUnIndexableObject(count);
        }

        [SpecialName]
        public static bool GreaterThan(string x, string y) {
            return string.CompareOrdinal(x, y) > 0;
        }
        [SpecialName]
        public static bool LessThan(string x, string y) {
            return string.CompareOrdinal(x, y) < 0;
        }
        [SpecialName]
        public static bool LessThanOrEqual(string x, string y) {
            return string.CompareOrdinal(x, y) <= 0;
        }
        [SpecialName]
        public static bool GreaterThanOrEqual(string x, string y) {
            return string.CompareOrdinal(x, y) >= 0;
        }
        [SpecialName]
        public static bool Equals(string x, string y) {
            return string.Equals(x, y);
        }
        [SpecialName]
        public static bool NotEquals(string x, string y) {
            return !string.Equals(x, y);
        }

        #endregion

        [SpecialName, ImplicitConversionMethod]
        public static string ConvertFromChar(char c) {
            return ScriptingRuntimeHelpers.CharToString(c);
        }

        [SpecialName, ExplicitConversionMethod]
        public static char ConvertToChar(string s) {
            if (s.Length == 1) return s[0];
            throw PythonOps.TypeErrorForTypeMismatch("char", s);
        }

        [SpecialName, ImplicitConversionMethod]
        public static IEnumerable ConvertToIEnumerable(string s) {
            // make an enumerator that produces strings instead of chars
            return new PythonStringEnumerable(s);
        }

        internal static int Compare(string self, string obj) {
            int ret = string.CompareOrdinal(self, obj);
            return ret == 0 ? 0 : (ret < 0 ? -1 : +1);
        }

        public static object __getnewargs__(CodeContext/*!*/ context, string self) {
            if (!Object.ReferenceEquals(self, null)) {
                // Cast self to object to avoid exception caused by trying to access SystemState on DefaultContext
                return PythonTuple.MakeTuple(StringOps.__new__(context, TypeCache.String, (object)self));
            }
            throw PythonOps.TypeErrorForBadInstance("__getnewargs__ requires a 'str' object but received a '{0}'", self);
        }

        public static string __str__(string self) {
            return self;
        }

        public static Extensible<string> __str__(ExtensibleString self) {
            return self;
        }

        #region Internal implementation details

        internal static string Quote(string s) {

            bool isUnicode = false;
            StringBuilder b = new StringBuilder(s.Length + 5);
            char quote = '\'';
            if (s.IndexOf('\'') != -1 && s.IndexOf('\"') == -1) {
                quote = '\"';
            }
            b.Append(quote);
            b.Append(ReprEncode(s, quote, ref isUnicode));
            b.Append(quote);
            if (isUnicode) return "u" + b.ToString();
            return b.ToString();
        }

        internal static string ReprEncode(string s, ref bool isUnicode) {
            return ReprEncode(s, (char)0, ref isUnicode);
        }

        internal static bool TryGetEncoding(string name, out Encoding encoding) {
#if FEATURE_ENCODING
            name = NormalizeEncodingName(name);

            EncodingInfoWrapper encInfo;
            if (CodecsInfo.Codecs.TryGetValue(name, out encInfo)) {
                encoding = (Encoding)encInfo.GetEncoding().Clone();
                return true;
            }
#else
            switch (NormalizeEncodingName(name)) {
                case "us_ascii":
                case "ascii": encoding = PythonAsciiEncoding.Instance; return true;
                case "utf_8": encoding = (Encoding)new EncodingWrapper(Encoding.UTF8, new byte[0]).Clone(); return true;
                case "utf_16_le": encoding = (Encoding)new EncodingWrapper(Encoding.Unicode, new byte[0]).Clone(); return true;
                case "utf_16_be": encoding = (Encoding)new EncodingWrapper(Encoding.BigEndianUnicode, new byte[0]).Clone(); return true;
                case "utf_8_sig": encoding = Encoding.UTF8; return true;
            }
#endif
            encoding = null;
            return false;
        }

        internal static string RawUnicodeEscapeEncode(string s) {
            // in the common case we don't need to encode anything, so we
            // lazily create the StringBuilder only if necessary.
            StringBuilder b = null;
            for (int i = 0; i < s.Length; i++) {
                char ch = s[i];
                if (ch > 0xff) {
                    ReprInit(ref b, s, i);
                    b.AppendFormat("\\u{0:x4}", (int)ch);
                } else if (b != null) {
                    b.Append(ch);
                }
            }

            if (b == null) return s;
            return b.ToString();
        }


        #endregion

        #region Private implementation details

        private static int CheckIndex(object index, int defaultValue) {
            int res;

            if (index == null) {
                res = defaultValue;
            } else if (!Converter.TryConvertToIndex(index, out res)) {
                throw PythonOps.TypeError("slice indices must be integers or None or have an __index__ method");
            }

            return res;
        }

        private static void AppendJoin(object value, int index, StringBuilder sb) {
            string strVal;

            if ((strVal = value as string) != null) {
                sb.Append(strVal);
            } else if (Converter.TryConvertToString(value, out strVal) && strVal != null) {
                sb.Append(strVal);
            } else {
                throw PythonOps.TypeError("sequence item {0}: expected string, {1} found", index.ToString(), PythonOps.GetPythonTypeName(value));
            }
        }

        private static string ReplaceEmpty(string self, string @new, int count) {
            string v = self;

            if (count == 0) return v;
            else if (count < 0) count = v.Length + 1;
            else if (count > v.Length + 1) count = checked(v.Length + 1);

            int newLength = checked(v.Length + @new.Length * count);
            int max = Math.Min(v.Length, count);
            StringBuilder ret = new StringBuilder(newLength);
            for (int i = 0; i < max; i++) {
                ret.Append(@new);
                ret.Append(v[i]);
            }
            if (count > max) {
                ret.Append(@new);
            } else {
                ret.Append(v, max, v.Length - max);
            }

            return ret.ToString();
        }

        private static string Reverse(string s) {
            if (s.Length == 0 || s.Length == 1) return s;
            char[] rchars = new char[s.Length];
            for (int i = s.Length - 1, j = 0; i >= 0; i--, j++) {
                rchars[j] = s[i];
            }
            return new string(rchars);
        }

        internal static string ReprEncode(string s, char quote, ref bool isUnicode) {
            // in the common case we don't need to encode anything, so we
            // lazily create the StringBuilder only if necessary.
            StringBuilder b = null;
            for (int i = 0; i < s.Length; i++) {
                char ch = s[i];

                if (ch >= LowestUnicodeValue) isUnicode = true;
                switch (ch) {
                    case '\\': ReprInit(ref b, s, i); b.Append("\\\\"); break;
                    case '\t': ReprInit(ref b, s, i); b.Append("\\t"); break;
                    case '\n': ReprInit(ref b, s, i); b.Append("\\n"); break;
                    case '\r': ReprInit(ref b, s, i); b.Append("\\r"); break;
                    default:
                        if (quote != 0 && ch == quote) {
                            ReprInit(ref b, s, i);
                            b.Append('\\'); b.Append(ch);
                        } else if (ch < ' ' || (ch >= 0x7f && ch <= 0xff)) {
                            ReprInit(ref b, s, i);
                            b.AppendFormat("\\x{0:x2}", (int)ch);
                        } else if (ch > 0xff) {
                            ReprInit(ref b, s, i);
                            b.AppendFormat("\\u{0:x4}", (int)ch);
                        } else if (b != null) {
                            b.Append(ch);
                        }
                        break;
                }
            }

            if (b == null) return s;
            return b.ToString();
        }

        private static void ReprInit(ref StringBuilder sb, string s, int c) {
            if (sb != null) return;

            sb = new StringBuilder(s, 0, c, s.Length);
        }

        private static bool IsSign(char ch) {
            return ch == '+' || ch == '-';
        }

        internal static string GetEncodingName(Encoding encoding) {
#if FEATURE_ENCODING
            string name = null;

            // if we have a valid code page try and get a reasonable name.  The
            // web names / mail displays match tend to CPython's terse names
            if (encoding.CodePage != 0) {
#if !NETCOREAPP && !NETSTANDARD
                if (encoding.IsBrowserDisplay) {
                    name = encoding.WebName;
                }

                if (name == null && encoding.IsMailNewsDisplay) {
                    name = encoding.HeaderName;
                }
#endif

                // otherwise use a code page number which also matches CPython               
                if (name == null) {
                    name = "cp" + encoding.CodePage;
                }
            }

            if (name == null) {
                // otherwise just finally fall back to the human readable name
                name = encoding.EncodingName;
            }
#else
            // only has web names
            string name = encoding.WebName;
#endif

            return NormalizeEncodingName(name);
        }

        internal static string NormalizeEncodingName(string name) {
            if (name == null) {
                return null;
            }
            return name.ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        }

        private static string RawDecode(CodeContext/*!*/ context, string s, object encodingType, string errors) {
            PythonContext pc = context.LanguageContext;

            Encoding e = null;
            string encoding = encodingType as string;
            if (encoding == null) {
                e = encodingType as Encoding;
                if (e == null) {
                    if (encodingType == Missing.Value) {
                        encoding = pc.GetDefaultEncodingName();
                    } else {
                        throw PythonOps.TypeError("decode() expected string, got '{0}'", DynamicHelpers.GetPythonType(encodingType).Name);
                    }
                }
            }

            if (e == null) {
                string normalizedName = NormalizeEncodingName(encoding);
                if ("raw_unicode_escape" == normalizedName) {
                    return LiteralParser.ParseString(s, true, true);
                } else if ("unicode_escape" == normalizedName) {
                    return LiteralParser.ParseString(s, false, true);
                } else if ("string_escape" == normalizedName) {
                    return LiteralParser.ParseString(s, false, false);
                }
            }

            if (e != null || TryGetEncoding(encoding, out e)) {
                return DoDecode(context, s, errors, encoding, e);
            }

            // look for user-registered codecs
            PythonTuple codecTuple = PythonOps.LookupEncoding(context, encoding);
            if (codecTuple != null) {
                return UserDecodeOrEncode(codecTuple[1], s, errors);
            }

            throw PythonOps.LookupError("unknown encoding: {0}", encoding);
        }

#if FEATURE_ENCODING
        private static DecoderFallback ReplacementFallback = new DecoderReplacementFallback("\ufffd");
#endif

        internal static string DoDecode(CodeContext context, string s, string errors, string encoding, Encoding e) => DoDecode(context, s, errors, encoding, e, true, out _);

        internal static string DoDecode(CodeContext context, string s, string errors, string encoding, Encoding e, bool final, out int numBytes) {
            byte[] bytes = s.MakeByteArray();
            int start = GetStartingOffset(e, bytes);
            numBytes = bytes.Length - start;

#if FEATURE_ENCODING
            // CLR's encoder exceptions have a 1-1 mapping w/ Python's encoder exceptions
            // so we just clone the encoding & set the fallback to throw in strict mode.
            e = (Encoding)e.Clone();

            switch (errors) {
                case "backslashreplace":
                case "xmlcharrefreplace":
                case "strict": e.DecoderFallback = final ? DecoderFallback.ExceptionFallback : new ExceptionFallBack(numBytes, e is UTF8Encoding); break;
                case "replace": e.DecoderFallback = ReplacementFallback; break;
                case "ignore": e.DecoderFallback = new PythonDecoderFallback(encoding, s, null); break;
                default:
                    e.DecoderFallback = new PythonDecoderFallback(encoding, s, () => LightExceptions.CheckAndThrow(PythonOps.LookupEncodingError(context, errors)));
                    break;
            }
#endif

            string decoded = e.GetString(bytes, start, numBytes);

#if FEATURE_ENCODING
            if (e.DecoderFallback is ExceptionFallBack fallback) {
                byte[] badBytes = fallback.buffer.badBytes;
                if (badBytes != null) {
                    numBytes -= badBytes.Length;
                }
            }
#endif

            return decoded;
        }

        /// <summary>
        /// Gets the starting offset checking to see if the incoming bytes already include a preamble.
        /// </summary>
        private static int GetStartingOffset(Encoding e, byte[] bytes) {
            byte[] preamble = e.GetPreamble();

            if (preamble.Length != 0 && bytes.Length >= preamble.Length) {
                for (int i = 0; i < preamble.Length; i++) {
                    if (bytes[i] != preamble[i]) {
                        return 0;
                    }
                }

                return preamble.Length;
            }

            return 0;
        }

        private static string RawEncode(CodeContext/*!*/ context, string s, object encodingType, string errors) {
            string encoding = encodingType as string;
            Encoding e = null;
            if (encoding == null) {
                e = encodingType as Encoding;
                if (e == null) {
                    if (encodingType == Missing.Value) {
                        encoding = context.LanguageContext.GetDefaultEncodingName();
                    } else {
                        throw PythonOps.TypeError("encode() expected string, got '{0}'", DynamicHelpers.GetPythonType(encodingType).Name);
                    }
                }
            }

            if (e == null) {
                string normalizedName = NormalizeEncodingName(encoding);

                if ("raw_unicode_escape" == normalizedName) {
                    return RawUnicodeEscapeEncode(s);
                } else if ("unicode_escape" == normalizedName || "string_escape" == normalizedName) {
                    bool dummy = false;
                    return ReprEncode(s, '\'', ref dummy);
                }
            }

            if (e != null || TryGetEncoding(encoding, out e)) {
                return DoEncode(context, s, errors, encoding, e);
            }

            // look for user-registered codecs
            PythonTuple codecTuple = PythonOps.LookupEncoding(context, encoding);
            if (codecTuple != null) {
                return UserDecodeOrEncode(codecTuple[0], s, errors);
            }

            throw PythonOps.LookupError("unknown encoding: {0}", encoding);
        }

        internal static string DoEncode(CodeContext context, string s, string errors, string encoding, Encoding e) {
#if FEATURE_ENCODING
            // CLR's encoder exceptions have a 1-1 mapping w/ Python's encoder exceptions
            // so we just clone the encoding & set the fallback to throw in strict mode
            e = (Encoding)e.Clone();

            switch (errors) {
                case "strict": e.EncoderFallback = EncoderFallback.ExceptionFallback; break;
                case "replace": e.EncoderFallback = EncoderFallback.ReplacementFallback; break;
                case "backslashreplace": e.EncoderFallback = new BackslashEncoderReplaceFallback(); break;
                case "xmlcharrefreplace": e.EncoderFallback = new XmlCharRefEncoderReplaceFallback(); break;
                case "ignore": e.EncoderFallback = new PythonEncoderFallback(encoding, s, null); break;
                default:
                    e.EncoderFallback = new PythonEncoderFallback(encoding, s, () => LightExceptions.CheckAndThrow(PythonOps.LookupEncodingError(context, errors)));
                    break;
            }
#endif
            return PythonOps.MakeString(e.GetPreamble(), e.GetBytes(s));
        }

        private static string UserDecodeOrEncode(object function, string data, string errors) {
            object res;
            if (errors != null) {
                res = PythonCalls.Call(function, data, errors);
            } else {
                res = PythonCalls.Call(function, data);
            }

            string strRes = AsString(res);
            if (strRes != null) return strRes;

            // tuple is string, bytes used, we just want the string...
            if (!(res is PythonTuple t)) throw PythonOps.TypeErrorForBadInstance("expected tuple, but found {0}", res);

            return Converter.ConvertToString(t[0]);
        }

#if FEATURE_ENCODING
        static class CodecsInfo {
            public static readonly Dictionary<string, EncodingInfoWrapper> Codecs = MakeCodecsDict();

            private static Dictionary<string, EncodingInfoWrapper> MakeCodecsDict() {
                Dictionary<string, EncodingInfoWrapper> d = new Dictionary<string, EncodingInfoWrapper>();
#if NETCOREAPP || NETSTANDARD
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                // TODO: add more encodings
                d["cp1252"] = d["windows-1252"] = new EncodingInfoWrapper(Encoding.GetEncoding(1252));
                d["iso8859_15"] = d["iso_8859_15"] = d["latin9"] = d["l9"] = new EncodingInfoWrapper(Encoding.GetEncoding(28605));
#endif
                EncodingInfo[] encs = Encoding.GetEncodings();

                foreach (EncodingInfo enc in encs) {
                    string normalizedName = NormalizeEncodingName(enc.Name);

                    // setup well-known mappings, for everything else we'll store as lower case w/ _
                    // for the common types cp* are not actual Python aliases, but GetEncodingName may return them
                    switch (normalizedName) {
                        case "us_ascii":
                            d["cp" + enc.CodePage.ToString()] = d["us_ascii"] = d["us"] = d["ascii"] = d["646"] = new AsciiEncodingInfoWrapper();
                            continue;
                        case "iso_8859_1":
                            d["8859"] = d["latin_1"] = d["latin1"] = d["iso 8859_1"] = d["iso8859_1"] = d["cp819"] = d["819"] = d["latin"] = d["l1"] = enc;
                            break;
                        case "utf_7":
                            d["cp" + enc.CodePage.ToString()] = d["utf_7"] = d["u7"] = d["unicode-1-1-utf-7"] = enc;
                            break;
                        case "utf_8":
                            d["cp" + enc.CodePage.ToString()] = d["utf_8"] = d["utf8"] = d["u8"] = new EncodingInfoWrapper(enc, new byte[0]);
                            d["utf_8_sig"] = enc;
                            continue;
                        case "utf_16":
                            d["utf_16le"] = d["utf_16_le"] = new EncodingInfoWrapper(enc, new byte[0]);
                            d["cp" + enc.CodePage.ToString()] = d["utf_16"] = d["utf16"] = d["u16"] = enc;
                            continue;
                        case "utf_16be":
                            d["cp" + enc.CodePage.ToString()] = d["utf_16be"] = d["utf_16_be"] = new EncodingInfoWrapper(enc, new byte[0]);
                            continue;
                        case "utf_32":
                            d["utf_32le"] = d["utf_32_le"] = new EncodingInfoWrapper(enc, new byte[0]);
                            d["cp" + enc.CodePage.ToString()] = d["utf_32"] = d["utf32"] = d["u32"] = enc;
                            continue;
                        case "utf_32be":
                            d["cp" + enc.CodePage.ToString()] = d["utf_32be"] = d["utf_32_be"] = new EncodingInfoWrapper(enc, new byte[0]);
                            continue;
                    }

                    // publish under normalized name (all lower cases, -s replaced with _s)
                    d[normalizedName] = enc;
                    // publish under Windows code page as well...
                    try {
                        d["windows-" + enc.GetEncoding().WindowsCodePage.ToString()] = enc;
                    } catch (NotSupportedException) { } // Encoding.WindowsCodePage can throw NotSupportedException on .NET Core - https://github.com/IronLanguages/ironpython2/issues/731
                    // publish under code page number as well...
                    d["cp" + enc.CodePage.ToString()] = d[enc.CodePage.ToString()] = enc;
                }

                d["raw_unicode_escape"] = new EncodingInfoWrapper(new UnicodeEscapeEncoding(true));
                d["unicode_escape"] = new EncodingInfoWrapper(new UnicodeEscapeEncoding(false));


#if DEBUG
                // all codecs should be stored in lowercase because we only look up from lowercase strings
                foreach (KeyValuePair<string, EncodingInfoWrapper> kvp in d) {
                    Debug.Assert(kvp.Key.ToLowerInvariant() == kvp.Key);
                }
#endif
                return d;
            }
        }

        class EncodingInfoWrapper {
            private EncodingInfo _info;
            private Encoding _encoding;
            private byte[] _preamble;

            public EncodingInfoWrapper(Encoding enc) {
                _encoding = enc;
            }

            public EncodingInfoWrapper(EncodingInfo info, byte[] preamble = null) {
                _info = info;
                _preamble = preamble;
            }

            public virtual Encoding GetEncoding() {
                if (_encoding != null) return _encoding;

                if (_preamble == null) {
                    return _info.GetEncoding();
                }

                return new EncodingWrapper(_info.GetEncoding(), _preamble);
            }

            public static implicit operator EncodingInfoWrapper(EncodingInfo info) {
                return new EncodingInfoWrapper(info);
            }
        }

        class AsciiEncodingInfoWrapper : EncodingInfoWrapper {
            public AsciiEncodingInfoWrapper()
                : base((EncodingInfo)null) {
            }

            public override Encoding GetEncoding() {
                return PythonAsciiEncoding.Instance;
            }
        }
#endif

        class EncodingWrapper : Encoding {
            private byte[] _preamble;
            private Encoding _encoding;

            public EncodingWrapper(Encoding encoding, byte[] preamable) {
                _preamble = preamable;
                _encoding = encoding;
            }

            private void SetEncoderFallback() {
#if FEATURE_ENCODING
                _encoding.EncoderFallback = EncoderFallback;
#endif
            }

            private void SetDecoderFallback() {
#if FEATURE_ENCODING
                _encoding.DecoderFallback = DecoderFallback;
#endif
            }

            public override int GetByteCount(char[] chars, int index, int count) {
                SetEncoderFallback();
                return _encoding.GetByteCount(chars, index, count);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
                SetEncoderFallback();
                return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }

            public override int GetCharCount(byte[] bytes, int index, int count) {
                SetDecoderFallback();
                return _encoding.GetCharCount(bytes, index, count);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
                SetDecoderFallback();
                return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }

            public override int GetMaxByteCount(int charCount) {
                SetEncoderFallback();
                return _encoding.GetMaxByteCount(charCount);
            }

            public override int GetMaxCharCount(int byteCount) {
                SetDecoderFallback();
                return _encoding.GetMaxCharCount(byteCount);
            }

            public override byte[] GetPreamble() {
                return _preamble;
            }

            public override Encoder GetEncoder() {
                SetEncoderFallback();
                return _encoding.GetEncoder();
            }

            public override Decoder GetDecoder() {
                SetDecoderFallback();
                return _encoding.GetDecoder();
            }

#if FEATURE_ENCODING
            public override object Clone() {
                // need to call base.Clone to be marked as read/write
                EncodingWrapper res = (EncodingWrapper)base.Clone();
                res._encoding = (Encoding)_encoding.Clone();
                return res;
            }
#endif
        }

        private static List SplitEmptyString(bool separators) {
            List ret = PythonOps.MakeEmptyList(1);
            if (separators) {
                ret.AddNoLock(String.Empty);
            }
            return ret;
        }

        private static List SplitInternal(string self, char[] seps, int maxsplit) {
            if (String.IsNullOrEmpty(self)) {
                return SplitEmptyString(seps != null);
            } else {
                string[] r;
                //  If the optional second argument sep is absent or None, the words are separated 
                //  by arbitrary strings of whitespace characters (space, tab, newline, return, formfeed);

                r = self.Split(seps, (maxsplit < 0) ? Int32.MaxValue : maxsplit + 1,
                    (seps == null) ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);

                List ret = PythonOps.MakeEmptyList(r.Length);
                foreach (string s in r) ret.AddNoLock(s);
                return ret;
            }
        }

        private static List SplitInternal(string self, string separator, int maxsplit) {
            if (String.IsNullOrEmpty(self)) {
                return SplitEmptyString(separator != null);
            } else {
                string[] r = StringUtils.Split(self, separator, (maxsplit < 0) ? Int32.MaxValue : maxsplit + 1, StringSplitOptions.None);

                List ret = PythonOps.MakeEmptyList(r.Length);
                foreach (string s in r) ret.AddNoLock(s);
                return ret;
            }
        }

        private static void TryStringOrTuple(object prefix) {
            if (prefix == null) {
                throw PythonOps.TypeError("expected string or Tuple, got NoneType");
            }
            if (!(prefix is string) && !(prefix is PythonTuple) && !(prefix is Extensible<string>)) {
                throw PythonOps.TypeError("expected string or Tuple, got {0} Type", prefix.GetType());
            }
        }

        private static string GetString(object obj) {
            string ret = AsString(obj);
            if (ret == null) {
                throw PythonOps.TypeError("expected string, got {0}", DynamicHelpers.GetPythonType(obj).Name);
            }
            return ret;
        }

        public static bool endswith(string self, [BytesConversion]string suffix) {
            return self.EndsWith(suffix, StringComparison.Ordinal);
        }

        //  Indexing is 0-based. Need to deal with negative indices
        //  (which mean count backwards from end of sequence)
        //  +---+---+---+---+---+
        //  | a | b | c | d | e |
        //  +---+---+---+---+---+
        //    0   1   2   3   4
        //   -5  -4  -3  -2  -1

        public static bool endswith(string self, [BytesConversion]string suffix, int start) {
            int len = self.Length;
            if (start > len) return false;
            // map the negative indice to its positive counterpart
            if (start < 0) {
                start += len;
                if (start < 0) start = 0;
            }
            return self.Substring(start).EndsWith(suffix, StringComparison.Ordinal);
        }

        //  With optional start, test beginning at that position (the char at that index is
        //  included in the test). With optional end, stop comparing at that position (the
        //  char at that index is not included in the test)
        public static bool endswith(string self, [BytesConversion]string suffix, int start, int end) {
            int len = self.Length;
            if (start > len) return false;
            // map the negative indices to their positive counterparts
            else if (start < 0) {
                start += len;
                if (start < 0) start = 0;
            }
            if (end >= len) return self.Substring(start).EndsWith(suffix, StringComparison.Ordinal);
            else if (end < 0) {
                end += len;
                if (end < 0) return false;
            }
            if (end < start) return false;
            return self.Substring(start, end - start).EndsWith(suffix, StringComparison.Ordinal);
        }

        private static bool endswith(string self, PythonTuple suffix) {
            foreach (object obj in suffix) {
                if (self.EndsWith(GetString(obj), StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private static bool endswith(string self, PythonTuple suffix, int start) {
            foreach (object obj in suffix) {
                if (endswith(self, GetString(obj), start)) {
                    return true;
                }
            }
            return false;
        }

        private static bool endswith(string self, PythonTuple suffix, int start, int end) {
            foreach (object obj in suffix) {
                if (endswith(self, GetString(obj), start, end)) {
                    return true;
                }
            }
            return false;
        }

        public static bool startswith(string self, [BytesConversion]string prefix) {
            return self.StartsWith(prefix, StringComparison.Ordinal);
        }

        public static bool startswith(string self, [BytesConversion]string prefix, int start) {
            int len = self.Length;
            if (start > len) return false;
            if (start < 0) {
                start += len;
                if (start < 0) start = 0;
            }
            return self.Substring(start).StartsWith(prefix, StringComparison.Ordinal);
        }

        public static bool startswith(string self, [BytesConversion]string prefix, int start, int end) {
            int len = self.Length;
            if (start > len) return false;
            // map the negative indices to their positive counterparts
            else if (start < 0) {
                start += len;
                if (start < 0) start = 0;
            }
            if (end >= len) return self.Substring(start).StartsWith(prefix, StringComparison.Ordinal);
            else if (end < 0) {
                end += len;
                if (end < 0) return false;
            }
            if (end < start) return false;
            return self.Substring(start, end - start).StartsWith(prefix, StringComparison.Ordinal);
        }

        private static bool startswith(string self, PythonTuple prefix) {
            foreach (object obj in prefix) {
                if (self.StartsWith(GetString(obj), StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private static bool startswith(string self, PythonTuple prefix, int start) {
            foreach (object obj in prefix) {
                if (startswith(self, GetString(obj), start)) {
                    return true;
                }
            }
            return false;
        }

        private static bool startswith(string self, PythonTuple prefix, int start, int end) {
            foreach (object obj in prefix) {
                if (startswith(self, GetString(obj), start, end)) {
                    return true;
                }
            }
            return false;
        }

        // note: any changes in how this iterator works should also be applied in the
        //       optimized overloads of Builtins.map()
        [PythonType("str_iterator")]
        private class PythonStringEnumerable : IEnumerable, IEnumerator<string> {
            private readonly string/*!*/ _s;
            private int _index;

            public PythonStringEnumerable(string s) {
                Assert.NotNull(s);

                _index = -1;
                _s = s;
            }

            #region IEnumerable Members

            public IEnumerator GetEnumerator() {
                return this;
            }

            #endregion

            #region IEnumerator<string> Members

            public string Current {
                get {
                    if (_index < 0) {
                        throw PythonOps.SystemError("Enumeration has not started. Call MoveNext.");
                    } else if (_index >= _s.Length) {
                        throw PythonOps.SystemError("Enumeration already finished.");
                    }
                    return ScriptingRuntimeHelpers.CharToString(_s[_index]);
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose() { }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current {
                get {
                    return ((IEnumerator<string>)this).Current;
                }
            }

            public bool MoveNext() {
                if (_index >= _s.Length) {
                    return false;
                }
                _index++;
                return _index != _s.Length;
            }

            public void Reset() {
                _index = -1;
            }

            #endregion
        }

        internal static IEnumerable StringEnumerable(string str) {
            return new PythonStringEnumerable(str);
        }

        internal static IEnumerator<string> StringEnumerator(string str) {
            return new PythonStringEnumerable(str);
        }

        #endregion

        #region  Unicode Encode/Decode Fallback Support

#if FEATURE_ENCODING
        /// When encoding or decoding strings if an error occurs CPython supports several different
        /// behaviors, in addition it supports user-extensible behaviors as well.  For the default
        /// behavior we're ok - both of us support throwing and replacing.  For custom behaviors
        /// we define a single fallback for decoding and encoding that calls the python function to do
        /// the replacement.
        /// 
        /// When we do the replacement we call the provided handler w/ a UnicodeEncodeError or UnicodeDecodeError
        /// object which contains:
        ///         encoding    (string, the encoding the user requested)
        ///         end         (the end of the invalid characters)
        ///         object      (the original string being decoded)
        ///         reason      (the error, e.g. 'unexpected byte code', not sure of others)
        ///         start       (the start of the invalid sequence)
        ///         
        /// The decoder returns a tuple of (unicode, int) where unicode is the replacement string
        /// and int is an index where encoding should continue.

        private class PythonEncoderFallbackBuffer : EncoderFallbackBuffer {
            private object _function;
            private string _encoding, _strData;
            private string _buffer;
            private int _bufferIndex;

            public PythonEncoderFallbackBuffer(string encoding, string str, object callable) {
                _function = callable;
                _strData = str;
                this._encoding = encoding;
            }

            public override bool Fallback(char charUnknown, int index) {
                return DoPythonFallback(index, 1);
            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) {
                return DoPythonFallback(index, 2);
            }

            public override char GetNextChar() {
                if (_buffer == null || _bufferIndex >= _buffer.Length) return Char.MinValue;

                return _buffer[_bufferIndex++];
            }

            public override bool MovePrevious() {
                if (_bufferIndex > 0) {
                    _bufferIndex--;
                    return true;
                }
                return false;
            }

            public override int Remaining {
                get {
                    if (_buffer == null) return 0;
                    return _buffer.Length - _bufferIndex;
                }
            }

            public override void Reset() {
                _buffer = null;
                _bufferIndex = 0;
                base.Reset();
            }

            private bool DoPythonFallback(int index, int length) {
                if (_function != null) {
                    // create the exception object to hand to the user-function...
                    PythonExceptions._UnicodeEncodeError exObj = new PythonExceptions._UnicodeEncodeError();

                    exObj.__init__(_encoding, _strData, index, index + length, "unexpected code byte");

                    // call the user function...
                    object res = PythonCalls.Call(_function, exObj);

                    string replacement = PythonDecoderFallbackBuffer.CheckReplacementTuple(res, "encoding");

                    // finally process the user's request.
                    _buffer = replacement;
                    _bufferIndex = 0;
                    return true;
                }

                return false;
            }

        }

        class PythonEncoderFallback : EncoderFallback {
            private object _function;
            private string _str;
            private string _enc;
            private readonly Func<object> lookup;

            public PythonEncoderFallback(string encoding, string data, Func<object> lookup) {
                _str = data;
                _enc = encoding;
                this.lookup = lookup;
            }

            public override EncoderFallbackBuffer CreateFallbackBuffer() {
                if (_function == null && lookup != null) {
                    _function = lookup.Invoke();
                }
                return new PythonEncoderFallbackBuffer(_enc, _str, _function);
            }

            public override int MaxCharCount {
                get { return Int32.MaxValue; }
            }
        }

        private class PythonDecoderFallbackBuffer : DecoderFallbackBuffer {
            private object _function;
            private string _encoding, _strData;
            private string _buffer;
            private int _bufferIndex;

            public PythonDecoderFallbackBuffer(string encoding, string str, object callable) {
                this._encoding = encoding;
                this._strData = str;
                this._function = callable;
            }

            public override int Remaining {
                get {
                    if (_buffer == null) return 0;
                    return _buffer.Length - _bufferIndex;
                }
            }

            public override char GetNextChar() {
                if (_buffer == null || _bufferIndex >= _buffer.Length) return Char.MinValue;

                return _buffer[_bufferIndex++];
            }

            public override bool MovePrevious() {
                if (_bufferIndex > 0) {
                    _bufferIndex--;
                    return true;
                }
                return false;
            }

            public override void Reset() {
                _buffer = null;
                _bufferIndex = 0;
                base.Reset();
            }

            public override bool Fallback(byte[] bytesUnknown, int index) {
                if (_function != null) {
                    // create the exception object to hand to the user-function...
                    PythonExceptions._UnicodeDecodeError exObj = new PythonExceptions._UnicodeDecodeError();

                    exObj.__init__(_encoding, _strData, index, index + bytesUnknown.Length, "unexpected code byte");

                    // call the user function...
                    object res = PythonCalls.Call(_function, exObj);

                    string replacement = CheckReplacementTuple(res, "decoding");

                    // finally process the user's request.
                    _buffer = replacement;
                    _bufferIndex = 0;
                    return true;
                }

                return false;
            }

            internal static string CheckReplacementTuple(object res, string encodeOrDecode) {
                bool ok = true;
                string replacement = null;

                // verify the result is sane...
                if (res is PythonTuple tres && tres.__len__() == 2) {
                    if (!Converter.TryConvertToString(tres[0], out replacement)) ok = false;
                    if (ok) {
                        int bytesSkipped;
                        if (!Converter.TryConvertToInt32(tres[1], out bytesSkipped)) ok = false;
                    }
                } else {
                    ok = false;
                }

                if (!ok) throw PythonOps.TypeError("{1} error handler must return tuple containing (str, int), got {0}", PythonOps.GetPythonTypeName(res), encodeOrDecode);
                return replacement;
            }
        }

        class PythonDecoderFallback : DecoderFallback {
            private object function;
            private string str;
            private string enc;
            private readonly Func<object> lookup;

            public PythonDecoderFallback(string encoding, string data, Func<object> lookup) {
                str = data;
                enc = encoding;
                this.lookup = lookup;
            }

            public override DecoderFallbackBuffer CreateFallbackBuffer() {
                if (function == null && lookup != null) {
                    function = lookup.Invoke();
                }
                return new PythonDecoderFallbackBuffer(enc, str, function);
            }

            public override int MaxCharCount {
                get { throw new NotImplementedException(); }
            }
        }

        class BackslashEncoderReplaceFallback : EncoderFallback {
            class BackslashReplaceFallbackBuffer : EncoderFallbackBuffer {
                private List<char> _buffer = new List<char>();
                private int _index;

                public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) {
                    return false;
                }

                public override bool Fallback(char charUnknown, int index) {
                    _buffer.Add('\\');
                    int val = (int)charUnknown;
                    if (val > 0xFF) {
                        _buffer.Add('u');
                        AddCharacter(val >> 8);
                        AddCharacter(val & 0xFF);
                    } else {
                        _buffer.Add('x');
                        AddCharacter(charUnknown);
                    }
                    return true;
                }

                private void AddCharacter(int val) {
                    AddOneDigit(((val) & 0xF0) >> 4);
                    AddOneDigit(val & 0x0F);
                }

                private void AddOneDigit(int val) {
                    if (val > 9) {
                        _buffer.Add((char)('a' + val - 0x0A));
                    } else {
                        _buffer.Add((char)('0' + val));
                    }
                }

                public override char GetNextChar() {
                    if (_index == _buffer.Count) return Char.MinValue;

                    return _buffer[_index++];
                }

                public override bool MovePrevious() {
                    if (_index > 0) {
                        _index--;
                        return true;
                    }
                    return false;
                }

                public override int Remaining {
                    get { return _buffer.Count - _index; }
                }
            }

            public override EncoderFallbackBuffer CreateFallbackBuffer() {
                return new BackslashReplaceFallbackBuffer();
            }

            public override int MaxCharCount {
                get { throw new NotImplementedException(); }
            }
        }

        class XmlCharRefEncoderReplaceFallback : EncoderFallback {
            class XmlCharRefEncoderReplaceFallbackBuffer : EncoderFallbackBuffer {
                private List<char> _buffer = new List<char>();
                private int _index;

                public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) {
                    return false;
                }

                public override bool Fallback(char charUnknown, int index) {
                    _buffer.Add('&');
                    _buffer.Add('#');
                    int val = (int)charUnknown;
                    foreach (char c in val.ToString()) {
                        _buffer.Add(c);
                    }
                    _buffer.Add(';');
                    return true;
                }

                public override char GetNextChar() {
                    if (_index == _buffer.Count) return Char.MinValue;

                    return _buffer[_index++];
                }

                public override bool MovePrevious() {
                    if (_index > 0) {
                        _index--;
                        return true;
                    }
                    return false;
                }

                public override int Remaining {
                    get { return _buffer.Count - _index; }
                }
            }

            public override EncoderFallbackBuffer CreateFallbackBuffer() {
                return new XmlCharRefEncoderReplaceFallbackBuffer();
            }

            public override int MaxCharCount {
                get { throw new NotImplementedException(); }
            }
        }

        class UnicodeEscapeEncoding : Encoding {
            private bool _raw;

            public UnicodeEscapeEncoding(bool raw) {
                _raw = raw;
            }

            public override int GetByteCount(char[] chars, int index, int count) {
                return EscapeEncode(chars, index, count).Length;
            }

            private string EscapeEncode(char[] chars, int index, int count) {
                if (_raw) {
                    return RawUnicodeEscapeEncode(new string(chars, index, count));
                }

                bool dummy = false;
                return ReprEncode(new string(chars, index, count), ref dummy);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
                if (_raw) {
                    string res = RawUnicodeEscapeEncode(new string(chars, charIndex, charCount));
                    for (int i = 0; i < res.Length; i++) {
                        bytes[i + byteIndex] = _raw ? (byte)res[i] : (byte)chars[i];
                    }
                    return res.Length;
                } else {
                    for (int i = 0; i < charCount; i++) {
                        bytes[i + byteIndex] = (byte)chars[i + charIndex];
                    }
                    return charCount;
                }
            }

            public override int GetCharCount(byte[] bytes, int index, int count) {
                char[] tmpChars = new char[count];
                for (int i = 0; i < count; i++) {
                    tmpChars[i] = (char)bytes[i + index];
                }

                return LiteralParser.ParseString(tmpChars, 0, tmpChars.Length, _raw, true, false).Length;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
                char[] tmpChars = new char[byteCount];
                for (int i = 0; i < byteCount; i++) {
                    chars[i] = (char)bytes[i + byteIndex];
                }

                string res = LiteralParser.ParseString(tmpChars, 0, tmpChars.Length, _raw, true, false);
                for (int i = 0; i < res.Length; i++) {
                    chars[i + charIndex] = (char)res[i];
                }

                return res.Length;
            }

            public override int GetMaxByteCount(int charCount) {
                return charCount * 5;
            }

            public override int GetMaxCharCount(int byteCount) {
                return byteCount;
            }
        }

        class ExceptionFallBack : DecoderFallback {
            internal ExceptionFallbackBuffer buffer;

            #region check for Utf8 Fallback issue

            static bool isUtf8Bugged;

            private class TestUtf8DecoderFallBack : DecoderFallback {
                public override int MaxCharCount => 0;

                public override DecoderFallbackBuffer CreateFallbackBuffer() => new TestUtf8DecoderFallbackBuffer();
            }

            private class TestUtf8DecoderFallbackBuffer : DecoderFallbackBuffer {
                public override int Remaining => 0;

                public override bool Fallback(byte[] bytesUnknown, int index) {
                    if (index < 0) throw new Exception();
                    return false;
                }

                public override char GetNextChar() => (char)0;

                public override bool MovePrevious() => false;
            }

            static ExceptionFallBack() {
                var e = (Encoding)Encoding.UTF8.Clone();
                e.DecoderFallback = new TestUtf8DecoderFallBack();
                try { e.GetString(new byte[] { 255 }); } catch { isUtf8Bugged = true; }
            }

            #endregion

            public ExceptionFallBack(int length, bool isUtf8 = false) {
                buffer = isUtf8 && isUtf8Bugged ? new ExceptionFallbackBufferUtf8DotNet(length) : new ExceptionFallbackBuffer(length);
            }

            public override DecoderFallbackBuffer CreateFallbackBuffer() => buffer;

            public override int MaxCharCount => 0;
        }

        class ExceptionFallbackBuffer : DecoderFallbackBuffer {
            private readonly int length;
            internal byte[] badBytes;

            public ExceptionFallbackBuffer(int length) {
                this.length = length;
            }

            public override bool Fallback(byte[] bytesUnknown, int index) {
                if (index > 0 && index + bytesUnknown.Length != length) {
                    throw PythonOps.UnicodeDecodeError($"failed to decode bytes at index: {index}", bytesUnknown, index);
                }
                // just some bad bytes at the end
                badBytes = bytesUnknown;
                return false;
            }

            public override char GetNextChar() => (char)0;

            public override bool MovePrevious() => false;

            public override int Remaining => 0;
        }

        // This class can be removed as soon as workaround for utf8 encoding in .net is
        // no longer necessary.
        class ExceptionFallbackBufferUtf8DotNet : ExceptionFallbackBuffer {
            private bool ignoreNext = false;

            public ExceptionFallbackBufferUtf8DotNet(int length) : base(length) { }

            public override bool Fallback(byte[] bytesUnknown, int index) {
                // In case of dot net and utf-8 value of index does not conform to documentation provided by
                // Microsoft http://msdn.microsoft.com/en-us/library/bdftay9c%28v=vs.100%29.aspx
                // The value of index is mysteriously decreased by the size of bytesUnknown
                // Tested on Windows 7 64, .NET 4.0.30319.18408, all recommended patches as of 06.02.2014
                if (ignoreNext) {
                    // dot net sometimes calls second time after this method returns false
                    // if this is the case, do nothing
                    return false;
                }
                // adjust index
                index = index + bytesUnknown.Length;
                ignoreNext = true;
                return base.Fallback(bytesUnknown, index);
            }
        }

#endif

        #endregion

        public static string/*!*/ __repr__(string/*!*/ self) {
            return StringOps.Quote(self);
        }
    }
}
