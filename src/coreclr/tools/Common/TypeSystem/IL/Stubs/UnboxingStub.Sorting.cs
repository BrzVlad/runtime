// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class UnboxingStub : ILStubMethod
    {
        protected override int ClassCode => 0x5ec1a2b7;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_targetMethod, ((UnboxingStub)other)._targetMethod);
        }
    }
}
