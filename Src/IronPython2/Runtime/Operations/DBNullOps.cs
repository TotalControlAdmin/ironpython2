﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
#if FEATURE_DBNULL
using System;

namespace IronPython2.Runtime.Operations {
    public static class DBNullOps {
        public static bool __nonzero__(DBNull value) {
            return false;
        }
    }
}
#endif
