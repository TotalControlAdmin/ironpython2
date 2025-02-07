// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

using IronPython2.Runtime.Binding;

using MSAst = System.Linq.Expressions;


namespace IronPython2.Compiler.Ast {

    public class ParenthesisExpression : Expression {
        private readonly Expression _expression;

        public ParenthesisExpression(Expression expression) {
            _expression = expression;
        }

        public Expression Expression {
            get { return _expression; }
        }

        public override MSAst.Expression Reduce() {
            return _expression;
        }

        internal override MSAst.Expression TransformSet(SourceSpan span, MSAst.Expression right, PythonOperationKind op) {
            return _expression.TransformSet(span, right, op);
        }

        internal override string CheckAssign() {
            return _expression.CheckAssign();
        }

        internal override string CheckAugmentedAssign() {
            return _expression.CheckAugmentedAssign();
        }

        internal override string CheckDelete() {
            return _expression.CheckDelete();
        }

        internal override MSAst.Expression TransformDelete() {
            return _expression.TransformDelete();
        }

        public override Type Type {
            get {
                return _expression.Type;
            }
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _expression?.Walk(walker);
            }
            walker.PostWalk(this);
        }

        internal override bool CanThrow {
            get {
                return _expression.CanThrow;
            }
        }
    }
}
