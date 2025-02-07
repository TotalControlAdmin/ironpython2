﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Reflection;

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

using IronPython2.Runtime.Operations;

namespace IronPython2.Runtime.Binding {
    using Ast = Expression;

    class PythonDeleteIndexBinder : DeleteIndexBinder, IPythonSite, IExpressionSerializable {
        private readonly PythonContext/*!*/ _context;

        public PythonDeleteIndexBinder(PythonContext/*!*/ context, int argCount)
            : base(new CallInfo(argCount)) {
            _context = context;
        }

        public override DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion) {
            return PythonProtocol.Index(this, PythonIndexType.DeleteItem, ArrayUtils.Insert(target, indexes),errorSuggestion);
        }

        public override int GetHashCode() {
            return base.GetHashCode() ^ _context.Binder.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is PythonDeleteIndexBinder ob)) {
                return false;
            }

            return ob._context.Binder == _context.Binder && base.Equals(obj);
        }

        #region IPythonSite Members

        public PythonContext/*!*/ Context {
            get { return _context; }
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression/*!*/ CreateExpression() {
            return Ast.Call(
                typeof(PythonOps).GetMethod(nameof(PythonOps.MakeDeleteIndexAction)),
                BindingHelpers.CreateBinderStateExpression(),
                AstUtils.Constant(CallInfo.ArgumentCount)
            );
        }

        #endregion
    }
}
