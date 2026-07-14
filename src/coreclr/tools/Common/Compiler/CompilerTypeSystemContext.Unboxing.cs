// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        public UnboxingStub GetUnboxingStub(MethodDesc targetMethod, TypeDesc owningType)
        {
            return _unboxingStubHashtable.GetOrCreateValue(new UnboxingStubKey(targetMethod, owningType));
        }

        private readonly struct UnboxingStubKey : IEquatable<UnboxingStubKey>
        {
            public readonly MethodDesc TargetMethod;
            public readonly TypeDesc OwningType;

            public UnboxingStubKey(MethodDesc targetMethod, TypeDesc owningType)
            {
                TargetMethod = targetMethod;
                OwningType = owningType;
            }

            public bool Equals(UnboxingStubKey other)
                => TargetMethod == other.TargetMethod;

            public override bool Equals(object obj)
                => obj is UnboxingStubKey other && Equals(other);

            public override int GetHashCode()
                => TargetMethod.GetHashCode();
        }

        private sealed class UnboxingStubHashtable : LockFreeReaderHashtable<UnboxingStubKey, UnboxingStub>
        {
            protected override int GetKeyHashCode(UnboxingStubKey key) => key.GetHashCode();
            protected override int GetValueHashCode(UnboxingStub value) => value.TargetMethod.GetHashCode();
            protected override bool CompareKeyToValue(UnboxingStubKey key, UnboxingStub value)
                => key.TargetMethod == value.TargetMethod;
            protected override bool CompareValueToValue(UnboxingStub value1, UnboxingStub value2)
                => value1.TargetMethod == value2.TargetMethod;
            protected override UnboxingStub CreateValueFromKey(UnboxingStubKey key)
                => new UnboxingStub(key.TargetMethod, key.OwningType);
        }

        private UnboxingStubHashtable _unboxingStubHashtable = new UnboxingStubHashtable();
    }
}
