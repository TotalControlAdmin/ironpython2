using System;
using System.Collections.Generic;
using System.Text;

using MSAst = System.Linq.Expressions;


namespace IronPython2.Compiler.Ast {
    internal interface ILoopStatement {
        MSAst.LabelTarget BreakLabel {
            get;
            set;
        }

        MSAst.LabelTarget ContinueLabel {
            get;
            set;
        }

    }

}
