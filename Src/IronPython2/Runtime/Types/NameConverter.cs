// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using System.Text;
using IronPython2.Runtime;
using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Types;

namespace IronPython2.Runtime.Types {
    /// <summary>
    /// Contains helper methods for converting C# names into Python names.
    /// </summary>
    public static class NameConverter {
        public static NameType TryGetName(PythonType dt, MethodInfo mi, out string name) {
            name = mi.Name;

            return GetNameFromMethod(dt, mi, NameType.Method, ref name);
        }
       
        public static NameType TryGetName(PythonType dt, EventInfo ei, MethodInfo eventMethod, out string name) {
            name = ei.Name;
            NameType res = dt.IsPythonType ? NameType.PythonEvent : NameType.Event;

            return GetNameFromMethod(dt, eventMethod, res, ref name);
        }

        public static NameType TryGetName(PythonType dt, PropertyInfo pi, MethodInfo prop, out string name) {
            if (PythonHiddenAttribute.IsHidden(pi)) {
                name = null;
                return NameType.None;
            }

            name = pi.Name;

            return GetNameFromMethod(dt, prop, NameType.Property, ref name);
        }

        public static string GetTypeName(Type t) {
            if (t.IsArray) {
                return "Array[" + GetTypeName(t.GetElementType()) + "]";
            }

            string name = PythonBinder.GetTypeNameInternal(t);

            if (name != t.Name) {
                return name;
            }

            int backtickIndex;
            if ((backtickIndex = name.IndexOf(ReflectionUtils.GenericArityDelimiter)) != -1) {
                name = name.Substring(0, backtickIndex);
                Type[] typeOf = t.GetGenericArguments();
                StringBuilder sb = new StringBuilder(name);
                sb.Append('[');
                bool first = true;
                foreach (Type tof in typeOf) {
                    if (first) first = false; else sb.Append(", ");
                    sb.Append(GetTypeName(tof));
                }
                sb.Append(']');
                name = sb.ToString();                
            }
            return name;
        }

        internal static NameType GetNameFromMethod(PythonType dt, MethodInfo mi, NameType res, ref string name) {
            string namePrefix = null;

            if (mi.IsPrivate || (mi.IsAssembly && !mi.IsFamilyOrAssembly)) {
                // allow explicitly implemented interface
                if (!(mi.IsPrivate && mi.IsFinal && mi.IsHideBySig && mi.IsVirtual)) {
                    // mangle protectes to private
                    namePrefix = "_" + dt.Name + "__";
                } else {
                    // explicitly implemented interface

                    // drop the namespace, leave the interface name, and replace 
                    // the dot with an underscore.  Eg System.IConvertible.ToBoolean
                    // becomes IConvertible_ToBoolean
                    int lastDot = name.LastIndexOf('.');
                    if (lastDot != -1) {
                        name = name.Substring(lastDot + 1);
                    }
                }
            }

            if (mi.IsDefined(typeof(ClassMethodAttribute), false)) {
                res |= NameType.ClassMember;
            }

            if (namePrefix != null) name = namePrefix + name;

            if (mi.DeclaringType.IsDefined(typeof(PythonTypeAttribute), false) ||
                !mi.DeclaringType.IsAssignableFrom(dt.UnderlyingSystemType)) {
                // extension types are all python names
                res |= NameType.Python;
            }

            if (mi.IsDefined(typeof(PropertyMethodAttribute), false)) {
                res = (res & ~NameType.BaseTypeMask) | NameType.Property;
            }

            return res;
        }
    }
}
