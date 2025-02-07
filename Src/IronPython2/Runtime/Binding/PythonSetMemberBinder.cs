﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;

namespace IronPython2.Runtime.Binding {
    using Ast = Expression;
    using System.Runtime.CompilerServices;

    class PythonSetMemberBinder : SetMemberBinder, IPythonSite, IExpressionSerializable {
        private readonly PythonContext/*!*/ _context;

        public PythonSetMemberBinder(PythonContext/*!*/ context, string/*!*/ name)
            : base(name, false) {
            _context = context;
        }

        public PythonSetMemberBinder(PythonContext/*!*/ context, string/*!*/ name, bool ignoreCase)
            : base(name, ignoreCase) {
            _context = context;
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject self, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            if (self.NeedsDeferral()) {
                return Defer(self, value);
            }
#if FEATURE_COM
            DynamicMetaObject com;
            if (Microsoft.Scripting.ComInterop.ComBinder.TryBindSetMember(this, self, BindingHelpers.GetComArgument(value), out com)) {
                return com;
            }
#endif
            return Context.Binder.SetMember(Name, self, value, errorSuggestion, new PythonOverloadResolverFactory(_context.Binder, AstUtils.Constant(Context.SharedContext)));
        }

        public override T BindDelegate<T>(CallSite<T> site, object[] args) {
            if (args[0] is IFastSettable fastSet) {
                T res = fastSet.MakeSetBinding<T>(site, this);
                if (res != null) {
                    return res;
                }
            }

            if (args[0] is IPythonObject ipo && !(ipo is IProxyObject)) {
                FastBindResult<T> res = UserTypeOps.MakeSetBinding<T>(Context.SharedContext, site, ipo, args[1], this);

                if (res.Target != null) {
                    PerfTrack.NoteEvent(PerfTrack.Categories.BindingFast, "IPythonObject");

                    if (res.ShouldCache) {
                        CacheTarget(res.Target);
                    }
                    return res.Target;
                }

                PerfTrack.NoteEvent(PerfTrack.Categories.BindingSlow, "IPythonObject Set");
            }

            return base.BindDelegate(site, args);
        }

        public PythonContext/*!*/ Context {
            get {
                return _context;
            }
        }

        public override int GetHashCode() {
            return base.GetHashCode() ^ _context.Binder.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is PythonSetMemberBinder ob)) {
                return false;
            }

            return ob._context.Binder == _context.Binder && base.Equals(obj);
        }

        public override string ToString() {
            return "Python SetMember " + Name;
        }

        #region IExpressionSerializable Members

        public Expression CreateExpression() {
            return Ast.Call(
                typeof(PythonOps).GetMethod(nameof(PythonOps.MakeSetAction)),
                BindingHelpers.CreateBinderStateExpression(),
                AstUtils.Constant(Name)
            );
        }

        #endregion

    }
}

