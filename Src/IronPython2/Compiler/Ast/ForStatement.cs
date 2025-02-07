// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MSAst = System.Linq.Expressions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Scripting;

using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace IronPython2.Compiler.Ast {
    using Ast = MSAst.Expression;

    public class ForStatement : Statement, ILoopStatement {
        private int _headerIndex;
        private readonly Expression _left;
        private Expression _list;
        private Statement _body;
        private readonly Statement _else;
        private MSAst.LabelTarget _break, _continue;

        public ForStatement(Expression left, Expression list, Statement body, Statement else_) {
            _left = left;
            _list = list;
            _body = body;
            _else = else_;
        }

        public int HeaderIndex {
            set { _headerIndex = value; }
        }

        public Expression Left {
            get { return _left; }
        }

        public Statement Body {
            get { return _body; }
            set { _body = value; }
        }

        public Expression List {
            get { return _list; }
            set { _list = value; }
        }

        public Statement Else {
            get { return _else; }
        }

        MSAst.LabelTarget ILoopStatement.BreakLabel {
            get {
                return _break;
            }
            set {
                _break = value;
            }
        }

        MSAst.LabelTarget ILoopStatement.ContinueLabel {
            get {
                return _continue;
            }
            set {
                _continue = value;
            }
        }

        public override MSAst.Expression Reduce() {
            // Temporary variable for the IEnumerator object
            MSAst.ParameterExpression enumerator = Ast.Variable(typeof(KeyValuePair<IEnumerator, IDisposable>), "foreach_enumerator");

            return Ast.Block(new[] { enumerator }, TransformFor(Parent, enumerator, _list, _left, _body, _else, Span, GlobalParent.IndexToLocation(_headerIndex), _break, _continue, true));
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _left?.Walk(walker);
                _list?.Walk(walker);
                _body?.Walk(walker);
                _else?.Walk(walker);
            }
            walker.PostWalk(this);
        }

        internal static MSAst.Expression TransformFor(ScopeStatement parent, MSAst.ParameterExpression enumerator,
                                                    Expression list, Expression left, MSAst.Expression body,
                                                    Statement else_, SourceSpan span, SourceLocation header,
                                                    MSAst.LabelTarget breakLabel, MSAst.LabelTarget continueLabel, bool isStatement) {
            // enumerator, isDisposable = Dynamic(GetEnumeratorBinder, list)
            MSAst.Expression init = Ast.Assign(
                    enumerator,
                    new PythonDynamicExpression1<KeyValuePair<IEnumerator, IDisposable>>(
                        Binders.UnaryOperationBinder(
                            parent.GlobalParent.PyContext,
                            PythonOperationKind.GetEnumeratorForIteration
                        ), 
                        parent.GlobalParent.CompilationMode, 
                        AstUtils.Convert(list, typeof(object))
                    )
                );

            // while enumerator.MoveNext():
            //    left = enumerator.Current
            //    body
            // else:
            //    else
            MSAst.Expression ls = AstUtils.Loop(
                    parent.GlobalParent.AddDebugInfo(
                        Ast.Call(
                            Ast.Property(
                                enumerator,
                                typeof(KeyValuePair<IEnumerator, IDisposable>).GetProperty("Key")
                            ),
                            typeof(IEnumerator).GetMethod("MoveNext")
                        ),
                        left.Span
                    ),
                    null,
                    Ast.Block(
                        left.TransformSet(
                            SourceSpan.None,
                            Ast.Call(
                                Ast.Property(
                                    enumerator,
                                    typeof(KeyValuePair<IEnumerator, IDisposable>).GetProperty("Key")
                                ),
                                typeof(IEnumerator).GetProperty("Current").GetGetMethod()
                            ),
                            PythonOperationKind.None
                        ),
                        body,
                        isStatement ? UpdateLineNumber(parent.GlobalParent.IndexToLocation(list.StartIndex).Line) : AstUtils.Empty(),
                        AstUtils.Empty()
                    ),
                    else_,
                    breakLabel,
                    continueLabel
            );

            return Ast.Block(
                init,
                Ast.TryFinally(
                    ls,
                    Ast.Block(
                        Ast.Call(AstMethods.ForLoopDispose, enumerator),
                        Ast.Assign(enumerator, Ast.New(typeof(KeyValuePair<IEnumerator, IDisposable>)))
                    )
                )
            );
        }

        internal override bool CanThrow {
            get {
                if (_left.CanThrow) {
                    return true;
                }

                if (_list.CanThrow) {
                    return true;
                }

                // most constants (int, float, long, etc...) will throw here
                if (_list is ConstantExpression ce) {
                    if (ce.Value is string) {
                        return false;
                    }
                    return true;
                }

                return false;
            }
        }
    }
}
