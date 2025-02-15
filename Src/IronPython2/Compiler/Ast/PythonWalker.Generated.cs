// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace IronPython2.Compiler.Ast {
    /// <summary>
    /// PythonWalker class - The Python AST Walker (default result is true)
    /// </summary>
    public class PythonWalker {

        // This is generated by the scripts\generate_walker.py script.
        // That will scan all types that derive from the IronPython AST nodes and inject into here.

        #region Generated Python AST Walker

        // *** BEGIN GENERATED CODE ***
        // generated by function: gen_python_walker from: generate_walker.py

        // AndExpression
        public virtual bool Walk(AndExpression node) { return true; }
        public virtual void PostWalk(AndExpression node) { }

        // BackQuoteExpression
        public virtual bool Walk(BackQuoteExpression node) { return true; }
        public virtual void PostWalk(BackQuoteExpression node) { }

        // BinaryExpression
        public virtual bool Walk(BinaryExpression node) { return true; }
        public virtual void PostWalk(BinaryExpression node) { }

        // CallExpression
        public virtual bool Walk(CallExpression node) { return true; }
        public virtual void PostWalk(CallExpression node) { }

        // ConditionalExpression
        public virtual bool Walk(ConditionalExpression node) { return true; }
        public virtual void PostWalk(ConditionalExpression node) { }

        // ConstantExpression
        public virtual bool Walk(ConstantExpression node) { return true; }
        public virtual void PostWalk(ConstantExpression node) { }

        // DictionaryComprehension
        public virtual bool Walk(DictionaryComprehension node) { return true; }
        public virtual void PostWalk(DictionaryComprehension node) { }

        // DictionaryExpression
        public virtual bool Walk(DictionaryExpression node) { return true; }
        public virtual void PostWalk(DictionaryExpression node) { }

        // ErrorExpression
        public virtual bool Walk(ErrorExpression node) { return true; }
        public virtual void PostWalk(ErrorExpression node) { }

        // GeneratorExpression
        public virtual bool Walk(GeneratorExpression node) { return true; }
        public virtual void PostWalk(GeneratorExpression node) { }

        // IndexExpression
        public virtual bool Walk(IndexExpression node) { return true; }
        public virtual void PostWalk(IndexExpression node) { }

        // LambdaExpression
        public virtual bool Walk(LambdaExpression node) { return true; }
        public virtual void PostWalk(LambdaExpression node) { }

        // ListComprehension
        public virtual bool Walk(ListComprehension node) { return true; }
        public virtual void PostWalk(ListComprehension node) { }

        // ListExpression
        public virtual bool Walk(ListExpression node) { return true; }
        public virtual void PostWalk(ListExpression node) { }

        // MemberExpression
        public virtual bool Walk(MemberExpression node) { return true; }
        public virtual void PostWalk(MemberExpression node) { }

        // NameExpression
        public virtual bool Walk(NameExpression node) { return true; }
        public virtual void PostWalk(NameExpression node) { }

        // OrExpression
        public virtual bool Walk(OrExpression node) { return true; }
        public virtual void PostWalk(OrExpression node) { }

        // ParenthesisExpression
        public virtual bool Walk(ParenthesisExpression node) { return true; }
        public virtual void PostWalk(ParenthesisExpression node) { }

        // SetComprehension
        public virtual bool Walk(SetComprehension node) { return true; }
        public virtual void PostWalk(SetComprehension node) { }

        // SetExpression
        public virtual bool Walk(SetExpression node) { return true; }
        public virtual void PostWalk(SetExpression node) { }

        // SliceExpression
        public virtual bool Walk(SliceExpression node) { return true; }
        public virtual void PostWalk(SliceExpression node) { }

        // TupleExpression
        public virtual bool Walk(TupleExpression node) { return true; }
        public virtual void PostWalk(TupleExpression node) { }

        // UnaryExpression
        public virtual bool Walk(UnaryExpression node) { return true; }
        public virtual void PostWalk(UnaryExpression node) { }

        // YieldExpression
        public virtual bool Walk(YieldExpression node) { return true; }
        public virtual void PostWalk(YieldExpression node) { }

        // AssertStatement
        public virtual bool Walk(AssertStatement node) { return true; }
        public virtual void PostWalk(AssertStatement node) { }

        // AssignmentStatement
        public virtual bool Walk(AssignmentStatement node) { return true; }
        public virtual void PostWalk(AssignmentStatement node) { }

        // AugmentedAssignStatement
        public virtual bool Walk(AugmentedAssignStatement node) { return true; }
        public virtual void PostWalk(AugmentedAssignStatement node) { }

        // BreakStatement
        public virtual bool Walk(BreakStatement node) { return true; }
        public virtual void PostWalk(BreakStatement node) { }

        // ClassDefinition
        public virtual bool Walk(ClassDefinition node) { return true; }
        public virtual void PostWalk(ClassDefinition node) { }

        // ContinueStatement
        public virtual bool Walk(ContinueStatement node) { return true; }
        public virtual void PostWalk(ContinueStatement node) { }

        // DelStatement
        public virtual bool Walk(DelStatement node) { return true; }
        public virtual void PostWalk(DelStatement node) { }

        // EmptyStatement
        public virtual bool Walk(EmptyStatement node) { return true; }
        public virtual void PostWalk(EmptyStatement node) { }

        // ExecStatement
        public virtual bool Walk(ExecStatement node) { return true; }
        public virtual void PostWalk(ExecStatement node) { }

        // ExpressionStatement
        public virtual bool Walk(ExpressionStatement node) { return true; }
        public virtual void PostWalk(ExpressionStatement node) { }

        // ForStatement
        public virtual bool Walk(ForStatement node) { return true; }
        public virtual void PostWalk(ForStatement node) { }

        // FromImportStatement
        public virtual bool Walk(FromImportStatement node) { return true; }
        public virtual void PostWalk(FromImportStatement node) { }

        // FunctionDefinition
        public virtual bool Walk(FunctionDefinition node) { return true; }
        public virtual void PostWalk(FunctionDefinition node) { }

        // GlobalStatement
        public virtual bool Walk(GlobalStatement node) { return true; }
        public virtual void PostWalk(GlobalStatement node) { }

        // IfStatement
        public virtual bool Walk(IfStatement node) { return true; }
        public virtual void PostWalk(IfStatement node) { }

        // ImportStatement
        public virtual bool Walk(ImportStatement node) { return true; }
        public virtual void PostWalk(ImportStatement node) { }

        // PrintStatement
        public virtual bool Walk(PrintStatement node) { return true; }
        public virtual void PostWalk(PrintStatement node) { }

        // PythonAst
        public virtual bool Walk(PythonAst node) { return true; }
        public virtual void PostWalk(PythonAst node) { }

        // RaiseStatement
        public virtual bool Walk(RaiseStatement node) { return true; }
        public virtual void PostWalk(RaiseStatement node) { }

        // ReturnStatement
        public virtual bool Walk(ReturnStatement node) { return true; }
        public virtual void PostWalk(ReturnStatement node) { }

        // SuiteStatement
        public virtual bool Walk(SuiteStatement node) { return true; }
        public virtual void PostWalk(SuiteStatement node) { }

        // TryStatement
        public virtual bool Walk(TryStatement node) { return true; }
        public virtual void PostWalk(TryStatement node) { }

        // WhileStatement
        public virtual bool Walk(WhileStatement node) { return true; }
        public virtual void PostWalk(WhileStatement node) { }

        // WithStatement
        public virtual bool Walk(WithStatement node) { return true; }
        public virtual void PostWalk(WithStatement node) { }

        // Arg
        public virtual bool Walk(Arg node) { return true; }
        public virtual void PostWalk(Arg node) { }

        // ComprehensionFor
        public virtual bool Walk(ComprehensionFor node) { return true; }
        public virtual void PostWalk(ComprehensionFor node) { }

        // ComprehensionIf
        public virtual bool Walk(ComprehensionIf node) { return true; }
        public virtual void PostWalk(ComprehensionIf node) { }

        // DottedName
        public virtual bool Walk(DottedName node) { return true; }
        public virtual void PostWalk(DottedName node) { }

        // IfStatementTest
        public virtual bool Walk(IfStatementTest node) { return true; }
        public virtual void PostWalk(IfStatementTest node) { }

        // ModuleName
        public virtual bool Walk(ModuleName node) { return true; }
        public virtual void PostWalk(ModuleName node) { }

        // Parameter
        public virtual bool Walk(Parameter node) { return true; }
        public virtual void PostWalk(Parameter node) { }

        // RelativeModuleName
        public virtual bool Walk(RelativeModuleName node) { return true; }
        public virtual void PostWalk(RelativeModuleName node) { }

        // SublistParameter
        public virtual bool Walk(SublistParameter node) { return true; }
        public virtual void PostWalk(SublistParameter node) { }

        // TryStatementHandler
        public virtual bool Walk(TryStatementHandler node) { return true; }
        public virtual void PostWalk(TryStatementHandler node) { }

        // *** END GENERATED CODE ***

        #endregion
    }


    /// <summary>
    /// PythonWalkerNonRecursive class - The Python AST Walker (default result is false)
    /// </summary>
    public class PythonWalkerNonRecursive : PythonWalker {
        #region Generated Python AST Walker Nonrecursive

        // *** BEGIN GENERATED CODE ***
        // generated by function: gen_python_walker_nr from: generate_walker.py

        // AndExpression
        public override bool Walk(AndExpression node) { return false; }
        public override void PostWalk(AndExpression node) { }

        // BackQuoteExpression
        public override bool Walk(BackQuoteExpression node) { return false; }
        public override void PostWalk(BackQuoteExpression node) { }

        // BinaryExpression
        public override bool Walk(BinaryExpression node) { return false; }
        public override void PostWalk(BinaryExpression node) { }

        // CallExpression
        public override bool Walk(CallExpression node) { return false; }
        public override void PostWalk(CallExpression node) { }

        // ConditionalExpression
        public override bool Walk(ConditionalExpression node) { return false; }
        public override void PostWalk(ConditionalExpression node) { }

        // ConstantExpression
        public override bool Walk(ConstantExpression node) { return false; }
        public override void PostWalk(ConstantExpression node) { }

        // DictionaryComprehension
        public override bool Walk(DictionaryComprehension node) { return false; }
        public override void PostWalk(DictionaryComprehension node) { }

        // DictionaryExpression
        public override bool Walk(DictionaryExpression node) { return false; }
        public override void PostWalk(DictionaryExpression node) { }

        // ErrorExpression
        public override bool Walk(ErrorExpression node) { return false; }
        public override void PostWalk(ErrorExpression node) { }

        // GeneratorExpression
        public override bool Walk(GeneratorExpression node) { return false; }
        public override void PostWalk(GeneratorExpression node) { }

        // IndexExpression
        public override bool Walk(IndexExpression node) { return false; }
        public override void PostWalk(IndexExpression node) { }

        // LambdaExpression
        public override bool Walk(LambdaExpression node) { return false; }
        public override void PostWalk(LambdaExpression node) { }

        // ListComprehension
        public override bool Walk(ListComprehension node) { return false; }
        public override void PostWalk(ListComprehension node) { }

        // ListExpression
        public override bool Walk(ListExpression node) { return false; }
        public override void PostWalk(ListExpression node) { }

        // MemberExpression
        public override bool Walk(MemberExpression node) { return false; }
        public override void PostWalk(MemberExpression node) { }

        // NameExpression
        public override bool Walk(NameExpression node) { return false; }
        public override void PostWalk(NameExpression node) { }

        // OrExpression
        public override bool Walk(OrExpression node) { return false; }
        public override void PostWalk(OrExpression node) { }

        // ParenthesisExpression
        public override bool Walk(ParenthesisExpression node) { return false; }
        public override void PostWalk(ParenthesisExpression node) { }

        // SetComprehension
        public override bool Walk(SetComprehension node) { return false; }
        public override void PostWalk(SetComprehension node) { }

        // SetExpression
        public override bool Walk(SetExpression node) { return false; }
        public override void PostWalk(SetExpression node) { }

        // SliceExpression
        public override bool Walk(SliceExpression node) { return false; }
        public override void PostWalk(SliceExpression node) { }

        // TupleExpression
        public override bool Walk(TupleExpression node) { return false; }
        public override void PostWalk(TupleExpression node) { }

        // UnaryExpression
        public override bool Walk(UnaryExpression node) { return false; }
        public override void PostWalk(UnaryExpression node) { }

        // YieldExpression
        public override bool Walk(YieldExpression node) { return false; }
        public override void PostWalk(YieldExpression node) { }

        // AssertStatement
        public override bool Walk(AssertStatement node) { return false; }
        public override void PostWalk(AssertStatement node) { }

        // AssignmentStatement
        public override bool Walk(AssignmentStatement node) { return false; }
        public override void PostWalk(AssignmentStatement node) { }

        // AugmentedAssignStatement
        public override bool Walk(AugmentedAssignStatement node) { return false; }
        public override void PostWalk(AugmentedAssignStatement node) { }

        // BreakStatement
        public override bool Walk(BreakStatement node) { return false; }
        public override void PostWalk(BreakStatement node) { }

        // ClassDefinition
        public override bool Walk(ClassDefinition node) { return false; }
        public override void PostWalk(ClassDefinition node) { }

        // ContinueStatement
        public override bool Walk(ContinueStatement node) { return false; }
        public override void PostWalk(ContinueStatement node) { }

        // DelStatement
        public override bool Walk(DelStatement node) { return false; }
        public override void PostWalk(DelStatement node) { }

        // EmptyStatement
        public override bool Walk(EmptyStatement node) { return false; }
        public override void PostWalk(EmptyStatement node) { }

        // ExecStatement
        public override bool Walk(ExecStatement node) { return false; }
        public override void PostWalk(ExecStatement node) { }

        // ExpressionStatement
        public override bool Walk(ExpressionStatement node) { return false; }
        public override void PostWalk(ExpressionStatement node) { }

        // ForStatement
        public override bool Walk(ForStatement node) { return false; }
        public override void PostWalk(ForStatement node) { }

        // FromImportStatement
        public override bool Walk(FromImportStatement node) { return false; }
        public override void PostWalk(FromImportStatement node) { }

        // FunctionDefinition
        public override bool Walk(FunctionDefinition node) { return false; }
        public override void PostWalk(FunctionDefinition node) { }

        // GlobalStatement
        public override bool Walk(GlobalStatement node) { return false; }
        public override void PostWalk(GlobalStatement node) { }

        // IfStatement
        public override bool Walk(IfStatement node) { return false; }
        public override void PostWalk(IfStatement node) { }

        // ImportStatement
        public override bool Walk(ImportStatement node) { return false; }
        public override void PostWalk(ImportStatement node) { }

        // PrintStatement
        public override bool Walk(PrintStatement node) { return false; }
        public override void PostWalk(PrintStatement node) { }

        // PythonAst
        public override bool Walk(PythonAst node) { return false; }
        public override void PostWalk(PythonAst node) { }

        // RaiseStatement
        public override bool Walk(RaiseStatement node) { return false; }
        public override void PostWalk(RaiseStatement node) { }

        // ReturnStatement
        public override bool Walk(ReturnStatement node) { return false; }
        public override void PostWalk(ReturnStatement node) { }

        // SuiteStatement
        public override bool Walk(SuiteStatement node) { return false; }
        public override void PostWalk(SuiteStatement node) { }

        // TryStatement
        public override bool Walk(TryStatement node) { return false; }
        public override void PostWalk(TryStatement node) { }

        // WhileStatement
        public override bool Walk(WhileStatement node) { return false; }
        public override void PostWalk(WhileStatement node) { }

        // WithStatement
        public override bool Walk(WithStatement node) { return false; }
        public override void PostWalk(WithStatement node) { }

        // Arg
        public override bool Walk(Arg node) { return false; }
        public override void PostWalk(Arg node) { }

        // ComprehensionFor
        public override bool Walk(ComprehensionFor node) { return false; }
        public override void PostWalk(ComprehensionFor node) { }

        // ComprehensionIf
        public override bool Walk(ComprehensionIf node) { return false; }
        public override void PostWalk(ComprehensionIf node) { }

        // DottedName
        public override bool Walk(DottedName node) { return false; }
        public override void PostWalk(DottedName node) { }

        // IfStatementTest
        public override bool Walk(IfStatementTest node) { return false; }
        public override void PostWalk(IfStatementTest node) { }

        // ModuleName
        public override bool Walk(ModuleName node) { return false; }
        public override void PostWalk(ModuleName node) { }

        // Parameter
        public override bool Walk(Parameter node) { return false; }
        public override void PostWalk(Parameter node) { }

        // RelativeModuleName
        public override bool Walk(RelativeModuleName node) { return false; }
        public override void PostWalk(RelativeModuleName node) { }

        // SublistParameter
        public override bool Walk(SublistParameter node) { return false; }
        public override void PostWalk(SublistParameter node) { }

        // TryStatementHandler
        public override bool Walk(TryStatementHandler node) { return false; }
        public override void PostWalk(TryStatementHandler node) { }

        // *** END GENERATED CODE ***

        #endregion
    }
}
