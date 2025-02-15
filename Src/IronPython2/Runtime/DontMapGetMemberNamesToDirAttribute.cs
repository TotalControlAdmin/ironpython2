﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace IronPython2.Runtime {
    /// <summary>
    /// Marks a type so that IronPython will not expose types which have GetMemberNames
    /// as having a __dir__ method.
    /// 
    /// Also suppresses __dir__ on something which implements IDynamicMetaObjectProvider
    /// but is not an IPythonObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    sealed class DontMapGetMemberNamesToDirAttribute : Attribute {
    }
}
