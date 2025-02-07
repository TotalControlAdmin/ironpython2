﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Numerics;

namespace IronPython2.Runtime {
    public interface IBufferProtocol {
        Bytes GetItem(int index);
        void SetItem(int index, object value);
        void SetSlice(Slice index, object value);

        int ItemCount {
            get;
        }

        string Format {
            get;
        }

        BigInteger ItemSize {
            get;
        }

        BigInteger NumberDimensions {
            get;
        }

        bool ReadOnly {
            get;
        }

        IList<BigInteger> GetShape(int start, int ?end);

        PythonTuple Strides {
            get;
        }

        object SubOffsets {
            get;
        }

        Bytes ToBytes(int start, int? end);

        List ToList(int start, int? end);
    }
}
