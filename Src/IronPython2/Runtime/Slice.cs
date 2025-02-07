// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;
using IronPython2.Runtime.Exceptions;

namespace IronPython2.Runtime {
    [PythonType("slice")]
    public sealed class Slice : ICodeFormattable, IComparable, ISlice {
        private readonly object _start, _stop, _step;

        public Slice(object stop) : this(null, stop, null) { }

        public Slice(object start, object stop) : this(start, stop, null) { }

        public Slice(object start, object stop, object step) {
            _start = start;
            _stop = stop;
            _step = step;
        }

        #region Python Public API Surface

        public object start {
            get { return _start; }
        }

        public object stop {
            get { return _stop; }
        }

        public object step {
            get { return _step; }
        }

        public int __cmp__(Slice obj) {
            return PythonOps.CompareArrays(new object[] { _start, _stop, _step }, 3,
                new object[] { obj._start, obj._stop, obj._step }, 3);
        }

        public void indices(int len, out int ostart, out int ostop, out int ostep) {
            PythonOps.FixSlice(len, _start, _stop, _step, out ostart, out ostop, out ostep);
        }

        public void indices(object len, out int ostart, out int ostop, out int ostep) {
            PythonOps.FixSlice(Converter.ConvertToIndex(len), _start, _stop, _step, out ostart, out ostop, out ostep);
        }

        public PythonTuple __reduce__() {
            return PythonTuple.MakeTuple(
                DynamicHelpers.GetPythonTypeFromType(typeof(Slice)),
                PythonTuple.MakeTuple(
                    _start,
                    _stop,
                    _step
                )
            );
        }

        #endregion

        #region IComparable Members

        int IComparable.CompareTo(object obj) {
            if (!(obj is Slice other)) throw new ValueErrorException("expected slice");
            return __cmp__(other);
        }

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public int __hash__() {
            throw PythonOps.TypeErrorForUnhashableType("slice");
        }

        #region ISlice Members

        object ISlice.Start {
            get { return start; }
        }

        object ISlice.Stop {
            get { return stop; }
        }

        object ISlice.Step {
            get { return step; }
        }

        #endregion

        #region ICodeFormattable Members

        public string/*!*/ __repr__(CodeContext/*!*/ context) {
            return string.Format("slice({0}, {1}, {2})", PythonOps.Repr(context, _start), PythonOps.Repr(context, _stop), PythonOps.Repr(context, _step));
        }

        #endregion
        
        #region Internal Implementation details

        internal static void FixSliceArguments(int size, ref int start, ref int stop) {
            start = start < 0 ? 0 : start > size ? size : start;
            stop = stop < 0 ? 0 : stop > size ? size : stop;
        }

        internal static void FixSliceArguments(long size, ref long start, ref long stop) {
            start = start < 0 ? 0 : start > size ? size : start;
            stop = stop < 0 ? 0 : stop > size ? size : stop;
        }

        internal delegate void SliceAssign(int index, object value);

        internal void DoSliceAssign(SliceAssign assign, int size, object value) {
            int ostart, ostop, ostep;
            indices(size, out ostart, out ostop, out ostep);
            DoSliceAssign(assign, ostart, ostop, ostep, value);
        }

        private static void DoSliceAssign(SliceAssign assign, int start, int stop, int step, object value) {
            stop = step > 0 ? Math.Max(stop, start) : Math.Min(stop, start);
            int n = Math.Max(0, (step > 0 ? (stop - start + step - 1) : (stop - start + step + 1)) / step);
            // fast paths, if we know the size then we can
            // do this quickly.
            if (value is IList) {
                ListSliceAssign(assign, start, n, step, value as IList);
            } else {
                OtherSliceAssign(assign, start, stop, step, value);
            }
        }

        private static void ListSliceAssign(SliceAssign assign, int start, int n, int step, IList lst) {
            if (lst.Count < n) throw PythonOps.ValueError("too few items in the enumerator. need {0} have {1}", n, lst.Count);
            else if (lst.Count != n) throw PythonOps.ValueError("too many items in the enumerator need {0} have {1}", n, lst.Count);

            for (int i = 0, index = start; i < n; i++, index += step) {
                assign(index, lst[i]);
            }
        }

        private static void OtherSliceAssign(SliceAssign assign, int start, int stop, int step, object value) {
            // get enumerable data into a list, and then
            // do the slice.
            IEnumerator enumerator = PythonOps.GetEnumerator(value);
            List sliceData = new List();
            while (enumerator.MoveNext()) sliceData.AddNoLock(enumerator.Current);

            DoSliceAssign(assign, start, stop, step, sliceData);
        }

        #endregion
    }
}
