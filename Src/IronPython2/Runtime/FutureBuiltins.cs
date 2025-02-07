// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Scripting;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using IronPython2.Compiler;
using IronPython2.Runtime;
using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Exceptions;
using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;

using System.Numerics;

[assembly: PythonModule("future_builtins", typeof(FutureBuiltins))]
namespace IronPython2.Runtime {
    public static partial class FutureBuiltins {
        public const string __doc__ = "Provides access to built-ins which will be defined differently in Python 3.0.";

        [SpecialName]
        public static void PerformModuleReload(PythonContext context, PythonDictionary dict) {
            if (Importer.ImportModule(context.SharedContext, context.SharedContext.GlobalDict, "itertools", false, -1) is PythonModule scope) {
                dict["map"] = scope.__dict__["imap"];
                dict["filter"] = scope.__dict__["ifilter"];
                dict["zip"] = scope.__dict__["izip"];
            }
        }
        
        public static string ascii(CodeContext/*!*/ context, object @object) {
            return PythonOps.Repr(context, @object);
        }

        public static string hex(CodeContext/*!*/ context, object number) {
            if (number is int) {
                return Int32Ops.__hex__((int)number);
            } else if (number is BigInteger) {
                BigInteger x = (BigInteger)number;
                if (x < 0) {
                    return "-0x" + (-x).ToString(16).ToLower();
                } else {
                    return "0x" + x.ToString(16).ToLower();
                }
            }

            object value;
            if (PythonTypeOps.TryInvokeUnaryOperator(context,
                number,
                "__index__",
                out value)) {
                if (!(value is int) && !(value is BigInteger))
                    throw PythonOps.TypeError("index returned non-(int, long), got '{0}'", PythonTypeOps.GetName(value));

                return hex(context, value);
            }
            throw PythonOps.TypeError("hex() argument cannot be interpreted as an index");
        }
        
        public static string oct(CodeContext context, object number) {
            if (number is int) {
                number = (BigInteger)(int)number;
            }
            if (number is BigInteger) {
                BigInteger x = (BigInteger)number;
                if (x == 0) {
                    return "0o0";
                } else if (x > 0) {
                    return "0o" + x.ToString(8);
                } else {
                    return "-0o" + (-x).ToString(8);
                }
            }

            object value;
            if (PythonTypeOps.TryInvokeUnaryOperator(context,
                number,
                "__index__",
                out value)) {
                if (!(value is int) && !(value is BigInteger))
                    throw PythonOps.TypeError("index returned non-(int, long), got '{0}'", PythonTypeOps.GetName(value));

                return oct(context, value);
            }
            throw PythonOps.TypeError("oct() argument cannot be interpreted as an index");
        }
    }
}
