// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

using IronPython2.Runtime;
using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;

using MSAst = System.Linq.Expressions;


namespace IronPython2.Compiler.Ast {
    using Ast = MSAst.Expression;

    public class TupleExpression : SequenceExpression {
        private bool _expandable;

        public TupleExpression(bool expandable, params Expression[] items)
            : base(items) {
            _expandable = expandable;
        }

        internal override string CheckAssign() {
            if (Items.Count == 0) {
                return "can't assign to ()";
            }
            for (int i = 0; i < Items.Count; i++) {
                Expression e = Items[i];
                if (e.CheckAssign() != null) {
                    // we don't return the same message here as CPython doesn't seem to either, 
                    // for example ((yield a), 2,3) = (2,3,4) gives a different error than
                    // a = yield 3 = yield 4.
                    return "can't assign to " + e.NodeName;
                }
            }
            return null;
        }

        internal override string CheckDelete() {
            if (Items.Count == 0)
                return "can't delete ()";
            return base.CheckDelete();
        }

        public override MSAst.Expression Reduce() {
            if (_expandable) {
                return Ast.NewArrayInit(
                    typeof(object),
                    ToObjectArray(Items)
                );
            }

            if (Items.Count == 0) {
                return Ast.Field(
                    null,
                    typeof(PythonOps).GetField(nameof(PythonOps.EmptyTuple))
                );
            }

            return Ast.Call(
                AstMethods.MakeTuple,
                Ast.NewArrayInit(
                    typeof(object),
                    ToObjectArray(Items)
                )
            );
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (Items != null) {
                    foreach (Expression e in Items) {
                        e.Walk(walker);
                    }
                }
            }
            walker.PostWalk(this);
        }

        public bool IsExpandable {
            get {
                return _expandable;
            }
        }

        internal override bool IsConstant {
            get {
                foreach (var item in Items) {
                    if (!item.IsConstant) {
                        return false;
                    }
                }
                return true;
            }
        }

        internal override object GetConstantValue() {
            if (Items.Count == 0) {
                return PythonTuple.EMPTY;
            }

            object[] items = new object[Items.Count];
            for (int i = 0; i < items.Length; i++) {
                items[i] = Items[i].GetConstantValue();
            }

            return PythonOps.MakeTuple(items);
        }
    }
}
