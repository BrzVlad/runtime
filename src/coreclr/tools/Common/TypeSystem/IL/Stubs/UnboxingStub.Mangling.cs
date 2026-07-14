// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class UnboxingStub : ILStubMethod, IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod => _targetMethod;

        ReadOnlySpan<byte> IPrefixMangledMethod.Prefix => "Unbox"u8;
    }
}
