// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;
using IronPython2.Runtime.Operations;

namespace IronPython2.Runtime.Types {
    public partial class OldInstance {
        #region Generated OldInstance Operators

        // *** BEGIN GENERATED CODE ***
        // generated by function: oldinstance_operators from: generate_ops.py

        [return: MaybeNotImplemented]
        public static object operator +([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__add__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__radd__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator +(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__radd__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceAdd(object other) {
            return InvokeOne(this, other, "__iadd__");
        }

        [return: MaybeNotImplemented]
        public static object operator -([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__sub__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rsub__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator -(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rsub__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceSubtract(object other) {
            return InvokeOne(this, other, "__isub__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object Power([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__pow__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rpow__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object Power(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rpow__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlacePower(object other) {
            return InvokeOne(this, other, "__ipow__");
        }

        [return: MaybeNotImplemented]
        public static object operator *([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__mul__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rmul__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator *(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rmul__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceMultiply(object other) {
            return InvokeOne(this, other, "__imul__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object FloorDivide([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__floordiv__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rfloordiv__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object FloorDivide(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rfloordiv__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceFloorDivide(object other) {
            return InvokeOne(this, other, "__ifloordiv__");
        }

        [return: MaybeNotImplemented]
        public static object operator /([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__div__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rdiv__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator /(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rdiv__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceDivide(object other) {
            return InvokeOne(this, other, "__idiv__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object TrueDivide([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__truediv__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rtruediv__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object TrueDivide(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rtruediv__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceTrueDivide(object other) {
            return InvokeOne(this, other, "__itruediv__");
        }

        [return: MaybeNotImplemented]
        public static object operator %([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__mod__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rmod__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator %(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rmod__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceMod(object other) {
            return InvokeOne(this, other, "__imod__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object LeftShift([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__lshift__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rlshift__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object LeftShift(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rlshift__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceLeftShift(object other) {
            return InvokeOne(this, other, "__ilshift__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object RightShift([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__rshift__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rrshift__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public static object RightShift(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rrshift__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceRightShift(object other) {
            return InvokeOne(this, other, "__irshift__");
        }

        [return: MaybeNotImplemented]
        public static object operator &([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__and__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rand__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator &(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rand__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceBitwiseAnd(object other) {
            return InvokeOne(this, other, "__iand__");
        }

        [return: MaybeNotImplemented]
        public static object operator |([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__or__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__ror__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator |(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__ror__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceBitwiseOr(object other) {
            return InvokeOne(this, other, "__ior__");
        }

        [return: MaybeNotImplemented]
        public static object operator ^([NotNull]OldInstance self, object other) {
            object res = InvokeOne(self, other, "__xor__");
            if (res != NotImplementedType.Value) return res;

            OldInstance otherOc = other as OldInstance;
            if (otherOc != null) {
                return InvokeOne(otherOc, self, "__rxor__");
            }
            return NotImplementedType.Value;
        }

        [return: MaybeNotImplemented]
        public static object operator ^(object other, [NotNull]OldInstance self) {
            return InvokeOne(self, other, "__rxor__");
        }

        [return: MaybeNotImplemented]
        [SpecialName]
        public object InPlaceExclusiveOr(object other) {
            return InvokeOne(this, other, "__ixor__");
        }


        // *** END GENERATED CODE ***

        #endregion

    }
}
