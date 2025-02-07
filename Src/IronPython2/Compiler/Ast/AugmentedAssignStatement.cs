// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System.Diagnostics;
using IronPython2.Runtime.Binding;

namespace IronPython2.Compiler.Ast {
    public class AugmentedAssignStatement : Statement {
        private readonly PythonOperator _op;
        private readonly Expression _left;
        private readonly Expression _right;

        public AugmentedAssignStatement(PythonOperator op, Expression left, Expression right) {
            _op = op;
            _left = left; 
            _right = right;
        }

        public PythonOperator Operator {
            get { return _op; }
        }

        public Expression Left {
            get { return _left; }
        }

        public Expression Right {
            get { return _right; }
        }

        public override MSAst.Expression Reduce() {
            return _left.TransformSet(Span, _right, PythonOperatorToAction(_op));
        }

        private static PythonOperationKind PythonOperatorToAction(PythonOperator op) {
            switch (op) {
                // Binary
                case PythonOperator.Add:
                    return PythonOperationKind.InPlaceAdd;
                case PythonOperator.Subtract:
                    return PythonOperationKind.InPlaceSubtract;
                case PythonOperator.Multiply:
                    return PythonOperationKind.InPlaceMultiply;
                case PythonOperator.Divide:
                    return PythonOperationKind.InPlaceDivide;
                case PythonOperator.TrueDivide:
                    return PythonOperationKind.InPlaceTrueDivide;
                case PythonOperator.Mod:
                    return PythonOperationKind.InPlaceMod;
                case PythonOperator.BitwiseAnd:
                    return PythonOperationKind.InPlaceBitwiseAnd;
                case PythonOperator.BitwiseOr:
                    return PythonOperationKind.InPlaceBitwiseOr;
                case PythonOperator.Xor:
                    return PythonOperationKind.InPlaceExclusiveOr;
                case PythonOperator.LeftShift:
                    return PythonOperationKind.InPlaceLeftShift;
                case PythonOperator.RightShift:
                    return PythonOperationKind.InPlaceRightShift;
                case PythonOperator.Power:
                    return PythonOperationKind.InPlacePower;
                case PythonOperator.FloorDivide:
                    return PythonOperationKind.InPlaceFloorDivide;
                default:
                    Debug.Assert(false, "Unexpected PythonOperator: " + op.ToString());
                    return PythonOperationKind.None;
            }
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _left?.Walk(walker);
                _right?.Walk(walker);
            }
            walker.PostWalk(this);
        }
    }
}
