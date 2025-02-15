// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using IronPython2.Runtime;
using IronPython2.Runtime.Exceptions;
using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using System.Numerics;

[assembly: PythonModule("marshal", typeof(IronPython2.Modules.PythonMarshal))]
namespace IronPython2.Modules {
    public static class PythonMarshal {
        public const string __doc__ = "Provides functions for serializing and deserializing primitive data types.";

        #region Public marshal APIs
        public static void dump(object value, PythonFile/*!*/ file) {
            dump(value, file, version);
        }

        public static void dump(object value, PythonFile/*!*/ file, int version) {
            if (file == null) throw PythonOps.TypeError("expected file, found None");

            file.write(dumps(value, version));
        }

        public static object load(PythonFile/*!*/ file) {
            if (file == null) throw PythonOps.TypeError("expected file, found None");

            return MarshalOps.GetObject(FileEnumerator (file));
        }

        public static object dumps(object value) {
            return dumps(value, version);
        }

        public static string dumps(object value, int version) {
            byte[] bytes = MarshalOps.GetBytes(value, version);
            StringBuilder sb = new StringBuilder(bytes.Length);
            for (int i = 0; i < bytes.Length; i++) {
                sb.Append((char)bytes[i]);
            }
            return sb.ToString();
        }

        public static object loads(string @string) {
            return MarshalOps.GetObject(StringEnumerator(@string));
        }

        public const int version = 2;
        #endregion

        #region Implementation details

        private static IEnumerator<byte> FileEnumerator(PythonFile/*!*/ file) {
            for (; ; ) {
                string data = file.read(1);
                if (data.Length == 0) {
                    yield break;
                }

                yield return (byte)data[0];
            }
        }

        private static IEnumerator<byte> StringEnumerator(string/*!*/ str) {
            for (int i = 0; i < str.Length; i++) {
                yield return (byte)str[i];
            }
        }

        #endregion
    }
}
