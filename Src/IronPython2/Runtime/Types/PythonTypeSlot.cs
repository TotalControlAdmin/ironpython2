// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System.Dynamic;
using System.Reflection;
using System.Runtime.InteropServices;

using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;

using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace IronPython2.Runtime.Types {
    using Ast = Expression;

    /// <summary>
    /// A TypeSlot is an item that gets stored in a type's dictionary.  Slots provide an 
    /// opportunity to customize access at runtime when a value is get or set from a dictionary.
    /// </summary>
    [PythonType]
    public class PythonTypeSlot {
        /// <summary>
        /// Gets the value stored in the slot for the given instance binding it to an instance if one is provided and
        /// the slot binds to instances.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        internal virtual bool TryGetValue(CodeContext context, object instance, PythonType owner, out object value) {
            value = null;
            return false;
        }

        /// <summary>
        /// Sets the value of the slot for the given instance.
        /// </summary>
        /// <returns>true if the value was set, false if it can't be set</returns>
        internal virtual bool TrySetValue(CodeContext context, object instance, PythonType owner, object value) {
            return false;
        }

        /// <summary>
        /// Deletes the value stored in the slot from the instance.
        /// </summary>
        /// <returns>true if the value was deleted, false if it can't be deleted</returns>
        internal virtual bool TryDeleteValue(CodeContext context, object instance, PythonType owner) {            
            return false;
        }

        internal virtual bool IsAlwaysVisible {
            get {
                return true;
            }
        }

        /// <summary>
        /// True if generating code for gets can result in more optimal accesses.
        /// </summary>
        internal virtual bool CanOptimizeGets {
            get {
                return false;
            }
        }

        /// <summary>
        /// Gets an expression which is used for accessing this slot.  If the slot lookup fails the error expression
        /// is used again.
        /// 
        /// The default implementation just calls the TryGetValue method.  Subtypes of PythonTypeSlot can override
        /// this and provide a more optimal implementation.
        /// </summary>
        internal virtual void MakeGetExpression(PythonBinder/*!*/ binder, Expression/*!*/ codeContext, DynamicMetaObject instance, DynamicMetaObject/*!*/ owner, ConditionalBuilder/*!*/ builder) {
            ParameterExpression tmp = Ast.Variable(typeof(object), "slotTmp");
            Expression call = Ast.Call(
                 typeof(PythonOps).GetMethod(nameof(PythonOps.SlotTryGetValue)),
                 codeContext,
                 AstUtils.Convert(AstUtils.WeakConstant(this), typeof(PythonTypeSlot)),
                 instance != null ? instance.Expression : AstUtils.Constant(null),
                 owner.Expression,
                 tmp
            );

            builder.AddVariable(tmp);
            if (!GetAlwaysSucceeds) {
                builder.AddCondition(
                    call,
                    tmp
                );
            } else {
                builder.FinishCondition(Ast.Block(call, tmp));
            }
        }
        
        /// <summary>
        /// True if TryGetValue will always succeed, false if it may fail.
        /// 
        /// This is used to optimize away error generation code.
        /// </summary>
        internal virtual bool GetAlwaysSucceeds {
            get {
                return false;
            }
        }

        internal virtual bool IsSetDescriptor(CodeContext context, PythonType owner) {
            return false;
        }

        public virtual object __get__(CodeContext/*!*/ context, object instance, [DefaultParameterValue(null)]object typeContext) {
            PythonType dt = typeContext as PythonType;

            object res;
            if (TryGetValue(context, instance, dt, out res))
                return res;

            throw PythonOps.AttributeErrorForMissingAttribute(dt == null ? "?" : dt.Name, "__get__");
        }
    }
}
