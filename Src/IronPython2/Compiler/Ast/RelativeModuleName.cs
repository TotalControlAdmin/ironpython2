﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Scripting;

namespace IronPython2.Compiler.Ast {
    public class RelativeModuleName : ModuleName {
        private readonly int _dotCount;

        public RelativeModuleName(string[] names, int dotCount)
            : base(names) {
            _dotCount = dotCount;
        }

        public int DotCount {
            get {
                return _dotCount;
            }
        }
    }
}
