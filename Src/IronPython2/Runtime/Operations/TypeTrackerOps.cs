// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Collections;
using System.Runtime.CompilerServices;
using IronPython2.Runtime.Types;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;

namespace IronPython2.Runtime.Operations {
    public static class TypeTrackerOps {
        [SpecialName, PropertyMethod]
        public static IDictionary Get__dict__(CodeContext context, TypeTracker self) {
            return new DictProxy(DynamicHelpers.GetPythonTypeFromType(self.Type));
        }
    }
}
