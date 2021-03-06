// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["MultiplySubtract.Vector64.Int32"] = MultiplySubtract_Vector64_Int32,
                ["MultiplySubtract.Vector64.SByte"] = MultiplySubtract_Vector64_SByte,
                ["MultiplySubtract.Vector64.UInt16"] = MultiplySubtract_Vector64_UInt16,
                ["MultiplySubtract.Vector64.UInt32"] = MultiplySubtract_Vector64_UInt32,
                ["MultiplySubtract.Vector128.Byte"] = MultiplySubtract_Vector128_Byte,
                ["MultiplySubtract.Vector128.Int16"] = MultiplySubtract_Vector128_Int16,
                ["MultiplySubtract.Vector128.Int32"] = MultiplySubtract_Vector128_Int32,
                ["MultiplySubtract.Vector128.SByte"] = MultiplySubtract_Vector128_SByte,
                ["MultiplySubtract.Vector128.UInt16"] = MultiplySubtract_Vector128_UInt16,
                ["MultiplySubtract.Vector128.UInt32"] = MultiplySubtract_Vector128_UInt32,
                ["MultiplySubtractBySelectedScalar.Vector64.Int16.Vector64.Int16.3"] = MultiplySubtractBySelectedScalar_Vector64_Int16_Vector64_Int16_3,
                ["MultiplySubtractBySelectedScalar.Vector64.Int16.Vector128.Int16.7"] = MultiplySubtractBySelectedScalar_Vector64_Int16_Vector128_Int16_7,
                ["MultiplySubtractBySelectedScalar.Vector64.Int32.Vector64.Int32.1"] = MultiplySubtractBySelectedScalar_Vector64_Int32_Vector64_Int32_1,
                ["MultiplySubtractBySelectedScalar.Vector64.Int32.Vector128.Int32.3"] = MultiplySubtractBySelectedScalar_Vector64_Int32_Vector128_Int32_3,
                ["MultiplySubtractBySelectedScalar.Vector64.UInt16.Vector64.UInt16.3"] = MultiplySubtractBySelectedScalar_Vector64_UInt16_Vector64_UInt16_3,
                ["MultiplySubtractBySelectedScalar.Vector64.UInt16.Vector128.UInt16.7"] = MultiplySubtractBySelectedScalar_Vector64_UInt16_Vector128_UInt16_7,
                ["MultiplySubtractBySelectedScalar.Vector64.UInt32.Vector64.UInt32.1"] = MultiplySubtractBySelectedScalar_Vector64_UInt32_Vector64_UInt32_1,
                ["MultiplySubtractBySelectedScalar.Vector64.UInt32.Vector128.UInt32.3"] = MultiplySubtractBySelectedScalar_Vector64_UInt32_Vector128_UInt32_3,
                ["MultiplySubtractBySelectedScalar.Vector128.Int16.Vector64.Int16.3"] = MultiplySubtractBySelectedScalar_Vector128_Int16_Vector64_Int16_3,
                ["MultiplySubtractBySelectedScalar.Vector128.Int16.Vector128.Int16.7"] = MultiplySubtractBySelectedScalar_Vector128_Int16_Vector128_Int16_7,
                ["MultiplySubtractBySelectedScalar.Vector128.Int32.Vector64.Int32.1"] = MultiplySubtractBySelectedScalar_Vector128_Int32_Vector64_Int32_1,
                ["MultiplySubtractBySelectedScalar.Vector128.Int32.Vector128.Int32.3"] = MultiplySubtractBySelectedScalar_Vector128_Int32_Vector128_Int32_3,
                ["MultiplySubtractBySelectedScalar.Vector128.UInt16.Vector64.UInt16.3"] = MultiplySubtractBySelectedScalar_Vector128_UInt16_Vector64_UInt16_3,
                ["MultiplySubtractBySelectedScalar.Vector128.UInt16.Vector128.UInt16.7"] = MultiplySubtractBySelectedScalar_Vector128_UInt16_Vector128_UInt16_7,
                ["MultiplySubtractBySelectedScalar.Vector128.UInt32.Vector64.UInt32.1"] = MultiplySubtractBySelectedScalar_Vector128_UInt32_Vector64_UInt32_1,
                ["MultiplySubtractBySelectedScalar.Vector128.UInt32.Vector128.UInt32.3"] = MultiplySubtractBySelectedScalar_Vector128_UInt32_Vector128_UInt32_3,
                ["MultiplySubtractByScalar.Vector64.Int16"] = MultiplySubtractByScalar_Vector64_Int16,
                ["MultiplySubtractByScalar.Vector64.Int32"] = MultiplySubtractByScalar_Vector64_Int32,
                ["MultiplySubtractByScalar.Vector64.UInt16"] = MultiplySubtractByScalar_Vector64_UInt16,
                ["MultiplySubtractByScalar.Vector64.UInt32"] = MultiplySubtractByScalar_Vector64_UInt32,
                ["MultiplySubtractByScalar.Vector128.Int16"] = MultiplySubtractByScalar_Vector128_Int16,
                ["MultiplySubtractByScalar.Vector128.Int32"] = MultiplySubtractByScalar_Vector128_Int32,
                ["MultiplySubtractByScalar.Vector128.UInt16"] = MultiplySubtractByScalar_Vector128_UInt16,
                ["MultiplySubtractByScalar.Vector128.UInt32"] = MultiplySubtractByScalar_Vector128_UInt32,
                ["MultiplyWideningLower.Vector64.Byte"] = MultiplyWideningLower_Vector64_Byte,
                ["MultiplyWideningLower.Vector64.Int16"] = MultiplyWideningLower_Vector64_Int16,
                ["MultiplyWideningLower.Vector64.Int32"] = MultiplyWideningLower_Vector64_Int32,
                ["MultiplyWideningLower.Vector64.SByte"] = MultiplyWideningLower_Vector64_SByte,
                ["MultiplyWideningLower.Vector64.UInt16"] = MultiplyWideningLower_Vector64_UInt16,
                ["MultiplyWideningLower.Vector64.UInt32"] = MultiplyWideningLower_Vector64_UInt32,
                ["MultiplyWideningLowerAndAdd.Vector64.Byte"] = MultiplyWideningLowerAndAdd_Vector64_Byte,
                ["MultiplyWideningLowerAndAdd.Vector64.Int16"] = MultiplyWideningLowerAndAdd_Vector64_Int16,
                ["MultiplyWideningLowerAndAdd.Vector64.Int32"] = MultiplyWideningLowerAndAdd_Vector64_Int32,
                ["MultiplyWideningLowerAndAdd.Vector64.SByte"] = MultiplyWideningLowerAndAdd_Vector64_SByte,
                ["MultiplyWideningLowerAndAdd.Vector64.UInt16"] = MultiplyWideningLowerAndAdd_Vector64_UInt16,
                ["MultiplyWideningLowerAndAdd.Vector64.UInt32"] = MultiplyWideningLowerAndAdd_Vector64_UInt32,
                ["MultiplyWideningLowerAndSubtract.Vector64.Byte"] = MultiplyWideningLowerAndSubtract_Vector64_Byte,
                ["MultiplyWideningLowerAndSubtract.Vector64.Int16"] = MultiplyWideningLowerAndSubtract_Vector64_Int16,
                ["MultiplyWideningLowerAndSubtract.Vector64.Int32"] = MultiplyWideningLowerAndSubtract_Vector64_Int32,
                ["MultiplyWideningLowerAndSubtract.Vector64.SByte"] = MultiplyWideningLowerAndSubtract_Vector64_SByte,
                ["MultiplyWideningLowerAndSubtract.Vector64.UInt16"] = MultiplyWideningLowerAndSubtract_Vector64_UInt16,
                ["MultiplyWideningLowerAndSubtract.Vector64.UInt32"] = MultiplyWideningLowerAndSubtract_Vector64_UInt32,
                ["MultiplyWideningUpper.Vector128.Byte"] = MultiplyWideningUpper_Vector128_Byte,
                ["MultiplyWideningUpper.Vector128.Int16"] = MultiplyWideningUpper_Vector128_Int16,
                ["MultiplyWideningUpper.Vector128.Int32"] = MultiplyWideningUpper_Vector128_Int32,
                ["MultiplyWideningUpper.Vector128.SByte"] = MultiplyWideningUpper_Vector128_SByte,
                ["MultiplyWideningUpper.Vector128.UInt16"] = MultiplyWideningUpper_Vector128_UInt16,
                ["MultiplyWideningUpper.Vector128.UInt32"] = MultiplyWideningUpper_Vector128_UInt32,
                ["MultiplyWideningUpperAndAdd.Vector128.Byte"] = MultiplyWideningUpperAndAdd_Vector128_Byte,
                ["MultiplyWideningUpperAndAdd.Vector128.Int16"] = MultiplyWideningUpperAndAdd_Vector128_Int16,
                ["MultiplyWideningUpperAndAdd.Vector128.Int32"] = MultiplyWideningUpperAndAdd_Vector128_Int32,
                ["MultiplyWideningUpperAndAdd.Vector128.SByte"] = MultiplyWideningUpperAndAdd_Vector128_SByte,
                ["MultiplyWideningUpperAndAdd.Vector128.UInt16"] = MultiplyWideningUpperAndAdd_Vector128_UInt16,
                ["MultiplyWideningUpperAndAdd.Vector128.UInt32"] = MultiplyWideningUpperAndAdd_Vector128_UInt32,
                ["MultiplyWideningUpperAndSubtract.Vector128.Byte"] = MultiplyWideningUpperAndSubtract_Vector128_Byte,
                ["MultiplyWideningUpperAndSubtract.Vector128.Int16"] = MultiplyWideningUpperAndSubtract_Vector128_Int16,
                ["MultiplyWideningUpperAndSubtract.Vector128.Int32"] = MultiplyWideningUpperAndSubtract_Vector128_Int32,
                ["MultiplyWideningUpperAndSubtract.Vector128.SByte"] = MultiplyWideningUpperAndSubtract_Vector128_SByte,
                ["MultiplyWideningUpperAndSubtract.Vector128.UInt16"] = MultiplyWideningUpperAndSubtract_Vector128_UInt16,
                ["MultiplyWideningUpperAndSubtract.Vector128.UInt32"] = MultiplyWideningUpperAndSubtract_Vector128_UInt32,
                ["Negate.Vector64.Int16"] = Negate_Vector64_Int16,
                ["Negate.Vector64.Int32"] = Negate_Vector64_Int32,
                ["Negate.Vector64.SByte"] = Negate_Vector64_SByte,
                ["Negate.Vector64.Single"] = Negate_Vector64_Single,
                ["Negate.Vector128.Int16"] = Negate_Vector128_Int16,
                ["Negate.Vector128.Int32"] = Negate_Vector128_Int32,
                ["Negate.Vector128.SByte"] = Negate_Vector128_SByte,
                ["Negate.Vector128.Single"] = Negate_Vector128_Single,
                ["NegateSaturate.Vector64.Int16"] = NegateSaturate_Vector64_Int16,
                ["NegateSaturate.Vector64.Int32"] = NegateSaturate_Vector64_Int32,
                ["NegateSaturate.Vector64.SByte"] = NegateSaturate_Vector64_SByte,
                ["NegateSaturate.Vector128.Int16"] = NegateSaturate_Vector128_Int16,
                ["NegateSaturate.Vector128.Int32"] = NegateSaturate_Vector128_Int32,
                ["NegateSaturate.Vector128.SByte"] = NegateSaturate_Vector128_SByte,
                ["NegateScalar.Vector64.Double"] = NegateScalar_Vector64_Double,
                ["NegateScalar.Vector64.Single"] = NegateScalar_Vector64_Single,
                ["Not.Vector64.Byte"] = Not_Vector64_Byte,
                ["Not.Vector64.Double"] = Not_Vector64_Double,
                ["Not.Vector64.Int16"] = Not_Vector64_Int16,
                ["Not.Vector64.Int32"] = Not_Vector64_Int32,
                ["Not.Vector64.Int64"] = Not_Vector64_Int64,
                ["Not.Vector64.SByte"] = Not_Vector64_SByte,
                ["Not.Vector64.Single"] = Not_Vector64_Single,
                ["Not.Vector64.UInt16"] = Not_Vector64_UInt16,
                ["Not.Vector64.UInt32"] = Not_Vector64_UInt32,
                ["Not.Vector64.UInt64"] = Not_Vector64_UInt64,
                ["Not.Vector128.Byte"] = Not_Vector128_Byte,
                ["Not.Vector128.Double"] = Not_Vector128_Double,
                ["Not.Vector128.Int16"] = Not_Vector128_Int16,
                ["Not.Vector128.Int32"] = Not_Vector128_Int32,
            };
        }
    }
}
