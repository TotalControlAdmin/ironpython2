// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting;
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using IronPython2.Runtime.Binding;

using MSAst = System.Linq.Expressions;

using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace IronPython2.Compiler.Ast {
    using Ast = MSAst.Expression;

    public class TryStatement : Statement {
        private int _headerIndex;
        /// <summary>
        /// The statements under the try-block.
        /// </summary>
        private Statement _body;

        /// <summary>
        /// Array of except (catch) blocks associated with this try. NULL if there are no except blocks.
        /// </summary>
        private readonly TryStatementHandler[] _handlers;

        /// <summary>
        /// The body of the optional Else block for this try. NULL if there is no Else block.
        /// </summary>
        private Statement _else;

        /// <summary>
        /// The body of the optional finally associated with this try. NULL if there is no finally block.
        /// </summary>
        private Statement _finally;

        public TryStatement(Statement body, TryStatementHandler[] handlers, Statement else_, Statement finally_) {
            _body = body;
            _handlers = handlers;
            _else = else_;
            _finally = finally_;
        }

        public int HeaderIndex {
            set { _headerIndex = value; }
        }

        public Statement Body {
            get { return _body; }
        }

        public Statement Else {
            get { return _else; }
        }

        public Statement Finally {
            get { return _finally; }
        }

        public IList<TryStatementHandler> Handlers {
            get { return _handlers; }
        }

        public override MSAst.Expression Reduce() {
            // allocated all variables here so they won't be shared w/ other 
            // locals allocated during the body or except blocks.
            MSAst.ParameterExpression lineUpdated = null;
            MSAst.ParameterExpression runElse = null;

            if (_else != null || (_handlers != null && _handlers.Length > 0)) {
                lineUpdated = Ast.Variable(typeof(bool), "$lineUpdated_try");
                if (_else != null) {
                    runElse = Ast.Variable(typeof(bool), "run_else");
                }
            }

            // don't allocate locals below here...
            MSAst.Expression body = _body;
            MSAst.Expression @else = _else;
            MSAst.Expression @catch, result;
            MSAst.ParameterExpression exception;

            if (_handlers != null && _handlers.Length > 0) {
                exception = Ast.Variable(typeof(Exception), "$exception");
                @catch = TransformHandlers(exception);
            } else {
                exception = null;
                @catch = null;
            }

            // We have else clause, must generate guard around it
            if (@else != null) {
                Debug.Assert(@catch != null);

                //  run_else = true;
                //  try {
                //      try_body
                //  } catch ( ... ) {
                //      run_else = false;
                //      catch_body
                //  }
                //  if (run_else) {
                //      else_body
                //  }
                result =
                    Ast.Block(
                        Ast.Assign(runElse, AstUtils.Constant(true)),
                        // save existing line updated, we could choose to do this only for nested exception handlers.
                        PushLineUpdated(false, lineUpdated),
                        LightExceptions.RewriteExternal(
                            AstUtils.Try(
                                Parent.AddDebugInfo(AstUtils.Empty(), new SourceSpan(Span.Start, GlobalParent.IndexToLocation(_headerIndex))),
                                body,
                                AstUtils.Constant(null)
                            ).Catch(exception,
                                Ast.Assign(runElse, AstUtils.Constant(false)),
                                @catch,
                                // restore existing line updated after exception handler completes
                                PopLineUpdated(lineUpdated),
                                Ast.Assign(exception, Ast.Constant(null, typeof(Exception))),
                                AstUtils.Constant(null)
                            )
                        ),
                        AstUtils.IfThen(runElse,
                            @else
                        ),
                        AstUtils.Empty()
                    );

            } else if (@catch != null) {        // no "else" clause
                //  try {
                //      <try body>
                //  } catch (Exception e) {
                //      ... catch handling ...
                //  }
                //
                result = 
                    LightExceptions.RewriteExternal(
                        AstUtils.Try(
                            GlobalParent.AddDebugInfo(AstUtils.Empty(), new SourceSpan(Span.Start, GlobalParent.IndexToLocation(_headerIndex))),
                            // save existing line updated
                            PushLineUpdated(false, lineUpdated),
                            body,
                            AstUtils.Constant(null)
                        ).Catch(exception,
                            @catch,
                            // restore existing line updated after exception handler completes
                            PopLineUpdated(lineUpdated),
                            Ast.Call(AstMethods.ExceptionHandled, Parent.LocalContext),
                            Ast.Assign(exception, Ast.Constant(null, typeof(Exception))),
                            AstUtils.Constant(null)
                        )
                    );
            } else {
                result = body;
            }
            
            return Ast.Block(
                GetVariables(lineUpdated, runElse), 
                AddFinally(result),
                AstUtils.Default(typeof(void))
            );
        }

        private static ReadOnlyCollectionBuilder<MSAst.ParameterExpression> GetVariables(MSAst.ParameterExpression lineUpdated, MSAst.ParameterExpression runElse) {
            var paramList = new ReadOnlyCollectionBuilder<MSAst.ParameterExpression>();
            if(lineUpdated != null) {
                paramList.Add(lineUpdated);
            }
            if(runElse != null) {
                paramList.Add(runElse);
            }
            return paramList;
        }

        private MSAst.Expression AddFinally(MSAst.Expression/*!*/ body) {
            if (_finally != null) {                
                MSAst.ParameterExpression tryThrows = Ast.Variable(typeof(Exception), "$tryThrows");
                MSAst.ParameterExpression locException = Ast.Variable(typeof(Exception), "$localException");

                MSAst.Expression @finally = _finally;

                // lots is going on here.  We need to consider:
                //      1. Exceptions propagating out of try/except/finally.  Here we need to save the line #
                //          from the exception block and not save the # from the finally block later.
                //      2. Exceptions propagating out of the finally block.  Here we need to report the line number
                //          from the finally block and leave the existing stack traces cleared.
                //      3. Returning from the try block: Here we need to run the finally block and not update the
                //          line numbers.
                body = AstUtils.Try( // we use a fault to know when we have an exception and when control leaves normally (via
                    // either a return or the body completing successfully).
                    AstUtils.Try(
                        Parent.AddDebugInfo(AstUtils.Empty(), new SourceSpan(Span.Start, GlobalParent.IndexToLocation(_headerIndex))),
                        Ast.Assign(tryThrows, AstUtils.Constant(null, typeof(Exception))),
                        body,
                        AstUtils.Empty()
                    ).Catch(
                        locException,
                        Expression.Block(
                            Ast.Assign(tryThrows, locException),
                            Expression.Rethrow()
                        )
                    )
                ).FinallyWithJumps(
                    // if we had an exception save the line # that was last executing during the try
                    AstUtils.If(
                        Expression.NotEqual(tryThrows, Expression.Default(typeof(Exception))),
                        Parent.GetSaveLineNumberExpression(tryThrows, false)
                    ),

                    // clear the frames incase thae finally throws, and allow line number
                    // updates to proceed
                    UpdateLineUpdated(false),
                    
                    // run the finally code
                    @finally,

                    // if we took an exception in the try block we have saved the line number.  Otherwise
                    // we have no line number saved and will need to continue saving them if
                    // other exceptions are thrown.
                    AstUtils.If(
                        Expression.NotEqual(tryThrows, Expression.Default(typeof(Exception))),
                        UpdateLineUpdated(true)
                    )
                );
                body = Ast.Block(new[] { tryThrows }, body);
            }

            return body;
        }


        /// <summary>
        /// Transform multiple python except handlers for a try block into a single catch body.
        /// </summary>
        /// <param name="exception">The variable for the exception in the catch block.</param>
        /// <returns>Null if there are no except handlers. Else the statement to go inside the catch handler</returns>
        private MSAst.Expression TransformHandlers(MSAst.ParameterExpression exception) {
            Assert.NotEmpty(_handlers);

            MSAst.ParameterExpression extracted = Ast.Variable(typeof(object), "$extracted");

            var tests = new List<Microsoft.Scripting.Ast.IfStatementTest>(_handlers.Length);
            MSAst.ParameterExpression converted = null;
            MSAst.Expression catchAll = null;

            for (int index = 0; index < _handlers.Length; index++) {
                TryStatementHandler tsh = _handlers[index];

                if (tsh.Test != null) {
                    Microsoft.Scripting.Ast.IfStatementTest ist;

                    //  translating:
                    //      except Test ...
                    //
                    //  generate following AST for the Test (common part):
                    //      CheckException(exception, Test)
                    MSAst.Expression test =
                        Ast.Call(
                            AstMethods.CheckException,
                            Parent.LocalContext,
                            extracted,
                            AstUtils.Convert(tsh.Test, typeof(object))
                        );

                    if (tsh.Target != null) {
                        //  translating:
                        //      except Test, Target:
                        //          <body>
                        //  into:
                        //      if ((converted = CheckException(exception, Test)) != null) {
                        //          Target = converted;
                        //          traceback-header
                        //          <body>
                        //      }

                        if (converted == null) {
                            converted = Ast.Variable(typeof(object), "$converted");
                        }

                        ist = AstUtils.IfCondition(
                            Ast.NotEqual(
                                Ast.Assign(converted, test),
                                AstUtils.Constant(null)
                            ),
                            Ast.Block(
                                tsh.Target.TransformSet(SourceSpan.None, converted, PythonOperationKind.None),
                                GlobalParent.AddDebugInfo(
                                    GetTracebackHeader(
                                        this, 
                                        exception,
                                        tsh.Body
                                    ),
                                    new SourceSpan(GlobalParent.IndexToLocation(tsh.StartIndex), GlobalParent.IndexToLocation(tsh.HeaderIndex))
                                ),
                                AstUtils.Empty()
                            )
                        );
                    } else {
                        //  translating:
                        //      except Test:
                        //          <body>
                        //  into:
                        //      if (CheckException(exception, Test) != null) {
                        //          traceback-header
                        //          <body>
                        //      }
                        ist = AstUtils.IfCondition(
                            Ast.NotEqual(
                                test,
                                AstUtils.Constant(null)
                            ),
                            GlobalParent.AddDebugInfo(
                                GetTracebackHeader(
                                    this, 
                                    exception,
                                    tsh.Body
                                ),
                                new SourceSpan(GlobalParent.IndexToLocation(tsh.StartIndex), GlobalParent.IndexToLocation(tsh.HeaderIndex))
                            )
                        );
                    }

                    // Add the test to the if statement test cascade
                    tests.Add(ist);
                } else {
                    Debug.Assert(index == _handlers.Length - 1);
                    Debug.Assert(catchAll == null);

                    //  translating:
                    //      except:
                    //          <body>
                    //  into:
                    //  {
                    //          traceback-header
                    //          <body>
                    //  }

                    catchAll = GlobalParent.AddDebugInfo(
                        GetTracebackHeader(this, exception, tsh.Body),
                        new SourceSpan(GlobalParent.IndexToLocation(tsh.StartIndex), GlobalParent.IndexToLocation(tsh.HeaderIndex))
                    );
                }
            }

            MSAst.Expression body = null;

            if (tests.Count > 0) {
                // rethrow the exception if we have no catch-all block
                if (catchAll == null) {
                    catchAll = Ast.Block(
                        Parent.GetSaveLineNumberExpression(exception, true),
                        Ast.Throw(
                            Ast.Call(
                                typeof(ExceptionHelpers).GetMethod("UpdateForRethrow"),
                                exception
                            )
                        )
                    );
                }

                body = AstUtils.If(
                    tests.ToArray(),
                    catchAll
                );
            } else {
                Debug.Assert(catchAll != null);
                body = catchAll;
            }

            IList<MSAst.ParameterExpression> args;
            if (converted != null) {
                args = new ReadOnlyCollectionBuilder<MSAst.ParameterExpression> { converted,  extracted };
            } else {
                args = new ReadOnlyCollectionBuilder<MSAst.ParameterExpression> { extracted };
            }

            // Codegen becomes:
            //     extracted = PythonOps.SetCurrentException(exception)
            //      < dynamic exception analysis >
            return Ast.Block(
                args,
                Ast.Assign(
                    extracted,
                    Ast.Call(
                        AstMethods.SetCurrentException,
                        Parent.LocalContext,
                        exception
                    )
                ),
                body,
                Ast.Assign(extracted, Ast.Constant(null)),
                AstUtils.Empty()
            );
        }

        /// <summary>
        /// Surrounds the body of an except block w/ the appropriate code for maintaining the traceback.
        /// </summary>
        internal static MSAst.Expression GetTracebackHeader(Statement node, MSAst.ParameterExpression exception, MSAst.Expression body) {
            // we are about to enter a except block.  We need to emit the line number update so we track
            // the line that the exception was thrown from.  We then need to build exc_info() so that
            // it's available.  Finally we clear the list of dynamic stack frames because they've all
            // been associated with this exception.
            return Ast.Block(
                // pass false so if we take another exception we'll add it to the frame list
                node.Parent.GetSaveLineNumberExpression(exception, false),
                Ast.Call(
                    AstMethods.BuildExceptionInfo,
                    node.Parent.LocalContext,
                    exception
                ),
                body,
                AstUtils.Empty()
            );
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _body?.Walk(walker);
                if (_handlers != null) {
                    foreach (TryStatementHandler handler in _handlers) {
                        handler.Walk(walker);
                    }
                }

                _else?.Walk(walker);
                _finally?.Walk(walker);
            }
            walker.PostWalk(this);
        }
    }

    // A handler corresponds to the except block.
    public class TryStatementHandler : Node {
        private int _headerIndex;
        private readonly Expression _test, _target;
        private readonly Statement _body;

        public TryStatementHandler(Expression test, Expression target, Statement body) {
            _test = test;
            _target = target;
            _body = body;
        }

        public SourceLocation Header {
            get { return GlobalParent.IndexToLocation(_headerIndex); }
        }

        public int HeaderIndex {
            get { return _headerIndex; }
            set { _headerIndex = value; }
        }

        public Expression Test {
            get { return _test; }
        }

        public Expression Target {
            get { return _target; }
        }

        public Statement Body {
            get { return _body; }
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                _test?.Walk(walker);
                _target?.Walk(walker);
                _body?.Walk(walker);
            }
            walker.PostWalk(this);
        }
    }
}
