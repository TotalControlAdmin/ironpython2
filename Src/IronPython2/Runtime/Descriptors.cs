// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;

namespace IronPython2.Runtime {
    [PythonType]
    public class staticmethod : PythonTypeSlot {
        internal object _func;

        public staticmethod(object func) {
            this._func = func;
        }

        internal override bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            value = __get__(instance, PythonOps.ToPythonType(owner));
            return true;
        }

        internal override bool GetAlwaysSucceeds {
            get {
                return true;
            }
        }

        public object __func__ {
            get {
                return _func;
            }
        }

        #region IDescriptor Members

        public object __get__(object instance) { return __get__(instance, null); }

        public object __get__(object instance, object owner) {
            return _func;
        }

        #endregion
    }

    [PythonType]
    public class classmethod : PythonTypeSlot {
        internal object func;

        public classmethod(CodeContext/*!*/ context, object func) {
            if (!PythonOps.IsCallable(context, func))
                throw PythonOps.TypeError("{0} object is not callable", PythonTypeOps.GetName(func));
            this.func = func;
        }

        internal override bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            value = __get__(instance, PythonOps.ToPythonType(owner));
            return true;
        }

        internal override bool GetAlwaysSucceeds {
            get {
                return true;
            }
        }

        public object __func__ {
            get {
                return func;
            }
        }

        #region IDescriptor Members

        public object __get__(object instance) { return __get__(instance, null); }

        public object __get__(object instance, object owner) {
            if (owner == null) {
                if (instance == null) throw PythonOps.TypeError("__get__(None, None) is invalid");
                owner = DynamicHelpers.GetPythonType(instance);
            }
            return new Method(func, owner, DynamicHelpers.GetPythonType(owner));
        }

        #endregion
    }

    [PythonType("property")]
    public class PythonProperty : PythonTypeDataSlot {
        private object _fget, _fset, _fdel, _doc;

        public PythonProperty() {
        }

        public PythonProperty(params object[] args) {
        }

        public PythonProperty(
            [ParamDictionary]IDictionary<object, object> dict, params object[] args) {
        }

        public void __init__([DefaultParameterValue(null)]object fget,
                        [DefaultParameterValue(null)]object fset,
                        [DefaultParameterValue(null)]object fdel,
                        [DefaultParameterValue(null)]object doc) {
            _fget = fget; _fset = fset; _fdel = fdel; _doc = doc;
            if (GetType() != typeof(PythonProperty) && _fget is PythonFunction) {
                // http://bugs.python.org/issue5890
                PythonDictionary dict = UserTypeOps.GetDictionary((IPythonObject)this);
                if (dict == null) {
                    throw PythonOps.AttributeError("{0} object has no __doc__ attribute", PythonTypeOps.GetName(this));
                }

                dict["__doc__"] = ((PythonFunction)_fget).__doc__;
            }
        }

        internal override bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            value = __get__(context, instance, PythonOps.ToPythonType(owner));
            return true;
        }

        internal override bool GetAlwaysSucceeds {
            get {
                return true;
            }
        }

        internal override bool TrySetValue(CodeContext context, object instance, PythonType owner, object value) {
            if (instance == null) {
                return false;
            }
            __set__(context, instance, value);
            return true;
        }

        internal override bool TryDeleteValue(CodeContext context, object instance, PythonType owner) {
            if (instance == null) {
                return false;
            } 
            __delete__(context, instance);
            return true;
        }

        [SpecialName, PropertyMethod, WrapperDescriptor]
        public static object Get__doc__(CodeContext context, PythonProperty self) {
            if (self._doc == null && PythonOps.HasAttr(context, self._fget, "__doc__")) {
                return PythonOps.GetBoundAttr(context, self._fget, "__doc__");
            } else if (self._doc == null) {
                System.Console.WriteLine("No attribute __doc__");
            }
            return self._doc;
        }

        [SpecialName, PropertyMethod, WrapperDescriptor]
        public static void Set__doc__(PythonProperty self, object value) {
            throw PythonOps.TypeError("readonly attribute");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public object fdel {
            get { return _fdel; }
            set {
                throw PythonOps.TypeError("readonly attribute");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public object fset {
            get { return _fset; }
            set {
                throw PythonOps.TypeError("readonly attribute");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public object fget {
            get { return _fget; }
            set {
                throw PythonOps.TypeError("readonly attribute");
            }
        }

        public override object __get__(CodeContext/*!*/ context, object instance, [DefaultParameterValue(null)]object owner) {
            if (instance == null) {
                return this;
            } else if (fget != null) {
                var site = context.LanguageContext.PropertyGetSite;

                return site.Target(site, context, fget, instance);
            }
            throw PythonOps.UnreadableProperty();
        }

        public override void __set__(CodeContext/*!*/ context, object instance, object value) {
            if (fset != null) {
                var site = context.LanguageContext.PropertySetSite;

                site.Target(site, context, fset, instance, value);
            } else {
                throw PythonOps.UnsetableProperty();
            }
        }

        public override void __delete__(CodeContext/*!*/ context, object instance) {
            if (fdel != null) {
                var site = context.LanguageContext.PropertyDeleteSite;

                site.Target(site, context, fdel, instance);
            } else {
                throw PythonOps.UndeletableProperty();
            }
        }

        public PythonProperty getter(object fget) {
            PythonProperty res = new PythonProperty();
            res.__init__(fget, _fset, _fdel, _doc);
            return res;
        }

        public PythonProperty setter(object fset) {
            PythonProperty res = new PythonProperty();
            res.__init__(_fget, fset, _fdel, _doc);
            return res;
        }

        public PythonProperty deleter(object fdel) {
            PythonProperty res = new PythonProperty();
            res.__init__(_fget, _fset, fdel, _doc);
            return res;
        }
    }

}
