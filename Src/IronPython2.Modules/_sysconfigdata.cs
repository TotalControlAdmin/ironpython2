// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using IronPython2.Runtime;

[assembly: PythonModule("_sysconfigdata", typeof(IronPython2.Modules._sysconfigdata), PlatformsAttribute.PlatformFamily.Unix)]
namespace IronPython2.Modules {
    public static class _sysconfigdata
    {
        public static PythonDictionary build_time_vars = new PythonDictionary();
    }
}
