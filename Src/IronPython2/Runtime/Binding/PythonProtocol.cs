﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

using IronPython2.Runtime.Operations;
using IronPython2.Runtime.Types;

namespace IronPython2.Runtime.Binding {
    using Ast = Expression;
    using AstUtils = Microsoft.Scripting.Ast.Utils;

    /// <summary>
    /// Provides binding logic which is implemented to follow various Python protocols.  This includes
    /// things such as calling __call__ to perform calls, calling __nonzero__/__len__ to convert to
    /// bool, calling __add__/__radd__ to do addition, etc...  
    /// 
    /// This logic gets shared between both the IDynamicMetaObjectProvider implementation for Python objects as well
    /// as the Python sites.  This ensures the logic we follow for our builtin types and user defined
    /// types is identical and properly conforming to the various protocols.
    /// </summary>
    static partial class PythonProtocol {

        #region Conversions

        /// <summary>
        /// Gets a MetaObject which converts the provided object to a bool using __nonzero__ or __len__
        /// protocol methods.  This code is shared between both our fallback for a site and our MetaObject
        /// for user defined objects.
        /// </summary>
        internal static DynamicMetaObject ConvertToBool(DynamicMetaObjectBinder/*!*/ conversion, DynamicMetaObject/*!*/ self) {
            Assert.NotNull(conversion, self);

            SlotOrFunction sf = SlotOrFunction.GetSlotOrFunction(
                PythonContext.GetPythonContext(conversion),
                "__nonzero__",
                self);

            if (sf.Success) {
                if (sf.Target.Expression.Type != typeof(bool)) {
                    return new DynamicMetaObject(
                        Ast.Call(
                            typeof(PythonOps).GetMethod(nameof(PythonOps.ThrowingConvertToNonZero)),
                            sf.Target.Expression
                        ),
                        sf.Target.Restrictions
                    );
                }

                return sf.Target;
            }

            sf = SlotOrFunction.GetSlotOrFunction(
                PythonContext.GetPythonContext(conversion),
                "__len__",
                self);

            if (sf.Success) {
                return new DynamicMetaObject(
                    GetConvertByLengthBody(
                        PythonContext.GetPythonContext(conversion),
                        sf.Target.Expression
                    ),
                    sf.Target.Restrictions
                );
            }

            return null;
        }

        /// <summary>
        /// Used for conversions to bool
        /// </summary>
        private static Expression/*!*/ GetConvertByLengthBody(PythonContext/*!*/ state, Expression/*!*/ call) {
            Assert.NotNull(state, call);

            Expression callAsInt = call;
            if (call.Type != typeof(int)) {
                callAsInt = DynamicExpression.Dynamic(
                    state.Convert(typeof(int), ConversionResultKind.ExplicitCast),
                    typeof(int),
                    call
                );
            }

            return Ast.NotEqual(callAsInt, AstUtils.Constant(0));
        }

        #endregion

        #region Calls

        internal static DynamicMetaObject Call(DynamicMetaObjectBinder/*!*/ call, DynamicMetaObject target, DynamicMetaObject/*!*/[]/*!*/ args) {
            Assert.NotNull(call, args);
            Assert.NotNullItems(args);

            if (target.NeedsDeferral()) {
                return call.Defer(ArrayUtils.Insert(target, args));
            }

            foreach (DynamicMetaObject mo in args) {
                if (mo.NeedsDeferral()) {
                    RestrictTypes(args);

                    return call.Defer(
                        ArrayUtils.Insert(target, args)
                    );
                }
            }

            DynamicMetaObject self = target.Restrict(target.GetLimitType());

            ValidationInfo valInfo = BindingHelpers.GetValidationInfo(target);
            PythonType pt = DynamicHelpers.GetPythonType(target.Value);
            PythonContext pyContext = PythonContext.GetPythonContext(call);

            // look for __call__, if it's present dispatch to it.  Otherwise fall back to the
            // default binder
            PythonTypeSlot callSlot;
            if (!typeof(Delegate).IsAssignableFrom(target.GetLimitType()) &&
                pt.TryResolveSlot(pyContext.SharedContext, "__call__", out callSlot)) {
                ConditionalBuilder cb = new ConditionalBuilder(call);
                Expression body;

                callSlot.MakeGetExpression(
                    pyContext.Binder,
                    PythonContext.GetCodeContext(call),
                    self,
                    GetPythonType(self),
                    cb
                );
                
                if (!cb.IsFinal) {
                    cb.FinishCondition(GetCallError(call, self));
                }

                Expression[] callArgs = ArrayUtils.Insert(
                    PythonContext.GetCodeContext(call),
                    cb.GetMetaObject().Expression, 
                    DynamicUtils.GetExpressions(args)
                );

                body = DynamicExpression.Dynamic(
                    PythonContext.GetPythonContext(call).Invoke(
                        BindingHelpers.GetCallSignature(call)
                    ),
                    typeof(object),
                    callArgs
                );

                body = Ast.TryFinally(
                    Ast.Block(
                        Ast.Call(typeof(PythonOps).GetMethod(nameof(PythonOps.FunctionPushFrame)), Ast.Constant(pyContext)),                        
                        body
                    ),
                    Ast.Call(typeof(PythonOps).GetMethod(nameof(PythonOps.FunctionPopFrame)))
                );

                return BindingHelpers.AddDynamicTestAndDefer(
                    call,
                    new DynamicMetaObject(body, self.Restrictions.Merge(BindingRestrictions.Combine(args))),
                    args,
                    valInfo
                );
            }

            return null;
        }

        private static DynamicMetaObject/*!*/ GetPythonType(DynamicMetaObject/*!*/ self) {
            Assert.NotNull(self);

            PythonType pt = DynamicHelpers.GetPythonType(self.Value);
            if (pt.IsSystemType) {
                return new DynamicMetaObject(
                    AstUtils.Constant(pt),
                    BindingRestrictions.Empty,
                    pt
                );
            }

            return new DynamicMetaObject(
                Ast.Property(
                    Ast.Convert(self.Expression, typeof(IPythonObject)),
                    PythonTypeInfo._IPythonObject.PythonType
                ),
                BindingRestrictions.Empty,
                pt
            );
        }

        private static Expression/*!*/ GetCallError(DynamicMetaObjectBinder binder, DynamicMetaObject/*!*/ self) {
            Assert.NotNull(self);

            return binder.Throw(
                Ast.Call(
                    typeof(PythonOps).GetMethod(nameof(PythonOps.UncallableError)),
                    AstUtils.Convert(self.Expression, typeof(object))
                )
            );
        }
        
        #endregion
    }
}
