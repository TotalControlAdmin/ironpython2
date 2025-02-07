// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System;
using System.Dynamic;
using IronPython2.Runtime.Binding;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace IronPython2.Compiler.Ast {
    using Ast = MSAst.Expression;

    public class MemberExpression : Expression {
        private readonly Expression _target;
        private readonly string _name;

        public MemberExpression(Expression target, string name) {
            _target = target;
            _name = name;
        }

        public Expression Target {
            get { return _target; }
        }

        public string Name {
            get { return _name; }
        }

        public override string ToString() {
            return base.ToString() + ":" + _name;
        }

        public override MSAst.Expression Reduce() {
            return GlobalParent.Get(
                _name,
                _target
            );
        }

        internal override MSAst.Expression TransformSet(SourceSpan span, MSAst.Expression right, PythonOperationKind op) {
            if (op == PythonOperationKind.None) {
                return GlobalParent.AddDebugInfoAndVoid(
                    GlobalParent.Set(
                        _name,
                        _target,
                        right
                    ),
                    span
                );
            } else {
                MSAst.ParameterExpression temp = Ast.Variable(typeof(object), "inplace");
                return GlobalParent.AddDebugInfo(
                    Ast.Block(
                        new[] { temp },
                        Ast.Assign(temp, _target),
                        SetMemberOperator(right, op, temp),
                        AstUtils.Empty()
                    ),
                    Span.Start,
                    span.End
                );
            }
        }

        internal override string CheckAssign() {
            if (string.Compare(_name, "None") == 0) {
                return "cannot assign to None";
            }
            return null;
        }

        internal override string CheckDelete() {
            return null;
        }

        private MSAst.Expression SetMemberOperator(MSAst.Expression right, PythonOperationKind op, MSAst.ParameterExpression temp) {
            return GlobalParent.Set(
                _name,
                temp,
                GlobalParent.Operation(
                    typeof(object),
                    op,
                    GlobalParent.Get(
                        _name,
                        temp
                    ),
                    right
                )
            );
        }

        internal override MSAst.Expression TransformDelete() {
            return GlobalParent.Delete(
                typeof(void),
                _name,
                _target
            );
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _target?.Walk(walker);
            }
            walker.PostWalk(this);
        }
    }
}
