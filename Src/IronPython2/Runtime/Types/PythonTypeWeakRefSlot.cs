// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System;
using Microsoft.Scripting.Runtime;
using IronPython2.Runtime.Operations;

namespace IronPython2.Runtime.Types {
    [PythonType("getset_descriptor")]
    public sealed class PythonTypeWeakRefSlot : PythonTypeSlot, ICodeFormattable {
        PythonType _type;

        public PythonTypeWeakRefSlot(PythonType parent) {
            this._type = parent;
        }

        internal override bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            if (instance == null) {
                value = this;
                return true;
            }

            IWeakReferenceable reference;
            if (context.LanguageContext.TryConvertToWeakReferenceable(instance, out reference)) {
                WeakRefTracker tracker = reference.GetWeakRef();
                if (tracker == null || tracker.HandlerCount == 0) {
                    value = null;
                } else {
                    value = tracker.GetHandlerCallback(0);
                }
                return true;
            }

            value = null;
            return false;
        }

        internal override bool TrySetValue(CodeContext context, object instance, PythonType owner, object value) {
            IWeakReferenceable reference;
            if (context.LanguageContext.TryConvertToWeakReferenceable(instance, out reference)) {
                return reference.SetWeakRef(new WeakRefTracker(reference, value, instance));
            }
            return false;
        }

        internal override bool TryDeleteValue(CodeContext context, object instance, PythonType owner) {
            throw PythonOps.TypeError("__weakref__ attribute cannot be deleted");
        }
       
        public override string ToString() {
            return String.Format("<attribute '__weakref__' of '{0}' objects>", _type.Name);
        }

        public void __set__(CodeContext context, object instance, object value) {
            TrySetValue(context, instance, DynamicHelpers.GetPythonType(instance), value);
        }

        #region ICodeFormattable Members

        public string/*!*/ __repr__(CodeContext/*!*/ context) {
            return String.Format("<attribute '__weakref__' of {0} objects",
                PythonOps.Repr(context, _type));
        }

        #endregion
    }
}
