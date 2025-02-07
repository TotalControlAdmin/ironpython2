﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;

using Microsoft.Scripting;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;

using IronPython2.Runtime.Binding;
using IronPython2.Runtime.Operations;

namespace IronPython2.Runtime.Types {
    using Ast = Expression;
    using AstUtils = Microsoft.Scripting.Ast.Utils;

    public abstract class PythonCustomTracker : CustomTracker {
        public abstract PythonTypeSlot/*!*/ GetSlot();

        public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) {
            return new DynamicMetaObject(AstUtils.Constant(GetSlot(), typeof(PythonTypeSlot)), BindingRestrictions.Empty);
        }

        public override MemberTracker BindToInstance(DynamicMetaObject instance) {
            return new BoundMemberTracker(this, instance);
        }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value) {
            return SetBoundValue(resolverFactory, binder, type, value, new DynamicMetaObject(AstUtils.Constant(null), BindingRestrictions.Empty));
        }

        public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value, DynamicMetaObject errorSuggestion) {
            return base.SetValue(resolverFactory, binder, type, value, errorSuggestion);
        }

        protected override DynamicMetaObject GetBoundValue(OverloadResolverFactory factory, ActionBinder binder, Type instanceType, DynamicMetaObject instance) {
            return new DynamicMetaObject(
                Ast.Call(
                    typeof(PythonOps).GetMethod(nameof(PythonOps.SlotGetValue)),
                    ((PythonOverloadResolverFactory)factory)._codeContext,
                    AstUtils.Constant(GetSlot(), typeof(PythonTypeSlot)),
                    AstUtils.Convert(
                        instance.Expression,
                        typeof(object)
                    ),
                    AstUtils.Constant(DynamicHelpers.GetPythonTypeFromType(instanceType))
                ),
                BindingRestrictions.Empty
            );
        }

        protected override DynamicMetaObject SetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value, DynamicMetaObject instance) {
            return SetBoundValue(resolverFactory, binder, type, value, instance, null);
        }

        protected override DynamicMetaObject SetBoundValue(OverloadResolverFactory factory, ActionBinder binder, Type type, DynamicMetaObject value, DynamicMetaObject instance, DynamicMetaObject errorSuggestion) {
            return new DynamicMetaObject(
                Expression.Condition(
                    Ast.Call(
                        typeof(PythonOps).GetMethod(nameof(PythonOps.SlotTrySetValue)),
                        ((PythonOverloadResolverFactory)factory)._codeContext,
                        AstUtils.Constant(GetSlot(), typeof(PythonTypeSlot)),
                        AstUtils.Convert(
                            instance.Expression,
                            typeof(object)
                        ),
                        AstUtils.Constant(DynamicHelpers.GetPythonTypeFromType(type)),
                        value.Expression
                    ),
                    AstUtils.Convert(value.Expression, typeof(object)),
                    errorSuggestion != null ?
                        errorSuggestion.Expression :
                        Expression.Throw(
                            Expression.Call(
                                typeof(PythonOps).GetMethod(nameof(PythonOps.AttributeErrorForMissingAttribute), new Type[] { typeof(object), typeof(string) }),
                                instance.Expression,
                                Expression.Constant(Name)
                            ),
                            typeof(object)
                        )
                ),
                BindingRestrictions.Empty
            );
        }
    }

    /// <summary>
    /// Provides a CustomTracker which handles special fields which have custom
    /// behavior on get/set.
    /// </summary>
    class CustomAttributeTracker : PythonCustomTracker {
        private readonly PythonTypeSlot/*!*/ _slot;
        private readonly Type/*!*/ _declType;
        private readonly string/*!*/ _name;

        public CustomAttributeTracker(Type/*!*/ declaringType, string/*!*/ name, PythonTypeSlot/*!*/ slot) {
            Debug.Assert(slot != null);
            Debug.Assert(declaringType != null);
            Debug.Assert(name != null);

            _declType = declaringType;
            _name = name;
            _slot = slot;
        }

        public override DynamicMetaObject GetValue(OverloadResolverFactory factory, ActionBinder binder, Type instanceType) {
            return GetBoundValue(factory, binder, instanceType, new DynamicMetaObject(AstUtils.Constant(null), BindingRestrictions.Empty));
        }

        public override string Name {
            get { return _name; }
        }

        public override Type DeclaringType {
            get {
                return _declType;
            }
        }

        public override PythonTypeSlot/*!*/ GetSlot() {
            return _slot;
        }
    }

    class ClassMethodTracker : PythonCustomTracker {
        private MethodTracker/*!*/[]/*!*/ _trackers;
        
        public ClassMethodTracker(MemberGroup/*!*/ group) {
            List<MethodTracker> trackers = new List<MethodTracker>(group.Count);
            
            foreach (MethodTracker mt in group) {
                trackers.Add(mt);
            }

            _trackers = trackers.ToArray();
        }

        public override PythonTypeSlot GetSlot() {
            List<MethodBase> meths = new List<MethodBase>();
            foreach (MethodTracker mt in _trackers) {
                meths.Add(mt.Method);
            }

            return PythonTypeOps.GetFinalSlotForFunction(
                PythonTypeOps.GetBuiltinFunction(DeclaringType,
                        Name,
                        meths.ToArray()
                )
            );
        }

        public override DynamicMetaObject GetValue(OverloadResolverFactory factory, ActionBinder binder, Type instanceType) {
            return GetBoundValue(factory, binder, instanceType, new DynamicMetaObject(AstUtils.Constant(null), BindingRestrictions.Empty));
        }

        public override Type DeclaringType {
            get { return _trackers[0].DeclaringType; }
        }

        public override string Name {
            get { return _trackers[0].Name; }
        }
    }

    class OperatorTracker : PythonCustomTracker {
        private MethodTracker/*!*/[]/*!*/ _trackers;
        private bool _reversed;
        private string/*!*/ _name;
        private Type/*!*/ _declType;

        public OperatorTracker(Type/*!*/ declaringType, string/*!*/ name, bool reversed, params MethodTracker/*!*/[]/*!*/ members) {
            Debug.Assert(declaringType != null);
            Debug.Assert(members != null);
            Debug.Assert(name != null);
            Debug.Assert(members.Length > 0);

            _declType = declaringType;
            _reversed = reversed;
            _trackers = members;
            _name = name;
        }        
        
        public override PythonTypeSlot/*!*/ GetSlot() {
            List<MethodBase> meths = new List<MethodBase>();
            foreach (MethodTracker mt in _trackers) {
                meths.Add(mt.Method);
            }

            MethodBase[] methods = meths.ToArray();
            FunctionType ft = (PythonTypeOps.GetMethodFunctionType(DeclaringType, methods) | FunctionType.Method) & (~FunctionType.Function);
            if (_reversed) {
                ft |= FunctionType.ReversedOperator;
            } else {
                ft &= ~FunctionType.ReversedOperator;
            }

            // check if this operator is only availble after importing CLR (e.g. __getitem__ on functions)
            foreach (MethodInfo mi in methods) {
                if (!PythonHiddenAttribute.IsHidden(mi)) {
                    ft |= FunctionType.AlwaysVisible;
                    break;
                }
            }

            return PythonTypeOps.GetFinalSlotForFunction(PythonTypeOps.GetBuiltinFunction(DeclaringType, 
                        Name,
                        ft,
                        meths.ToArray()
                    ));
        }

        public override Type DeclaringType {
            get { return _declType; }
        }

        public override string Name {
            get { return _name; }
        }
    }
}
