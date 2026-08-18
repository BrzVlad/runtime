// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public sealed class UnboxingStub : ILStubMethod, IPrefixMangledMethod
    {
        private readonly MethodDesc _targetMethod;
        private MethodSignature _signature;

        public UnboxingStub(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.OwningType.IsValueType);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);

            _targetMethod = targetMethod;
        }

        public MethodDesc TargetMethod => _targetMethod;

        public override Utf8Span Name => _targetMethod.Name;

        public override string DiagnosticName => "UNBOX_" + _targetMethod.DiagnosticName;

        public override TypeDesc OwningType => _targetMethod.OwningType;

        public override MethodSignature Signature => _signature ??= CreateSignature();

        public override TypeSystemContext Context => _targetMethod.Context;

        protected override int ClassCode => 0x5ec1a2b7;

        MethodDesc IPrefixMangledMethod.BaseMethod => _targetMethod;

        ReadOnlySpan<byte> IPrefixMangledMethod.Prefix => "Unbox"u8;

        protected override int ComputeHashCode() => _targetMethod.GetHashCode();

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_targetMethod, ((UnboxingStub)other)._targetMethod);
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "RawData"u8).GetField("Data"u8)));


            for (int i = 0; i < _targetMethod.Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(_targetMethod));
            codeStream.Emit(ILOpcode.ret);
            emitter.SetHasGeneratedTokens();

            return emitter.Link(this);
        }

        private MethodSignature CreateSignature()
        {
            MethodSignature targetSignature = _targetMethod.Signature;
            TypeDesc[] parameters = new TypeDesc[targetSignature.Length + 1];
            parameters[0] = Context.GetWellKnownType(WellKnownType.Object);

            for (int i = 0; i < targetSignature.Length; i++)
            {
                parameters[i + 1] = targetSignature[i];
            }

            return new MethodSignature(MethodSignatureFlags.Static, 0, targetSignature.ReturnType, parameters);
        }
    }
}
