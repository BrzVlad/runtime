// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Synthesized IL stub representing the unboxing entrypoint of a value type instance method.
    /// The stub receives the boxed instance as its first argument, adjusts it to a managed pointer
    /// into the box payload (skipping the MethodTable*), and forwards to the unboxed target method.
    ///
    /// This mirrors the VM's <c>CreateUnboxingILStubForValueTypeMethods</c>, but is a persistable
    /// method (unlike the transient <see cref="Internal.JitInterface.UnboxingMethodDesc"/>) so that
    /// crossgen2 can compile its body into the ReadyToRun image and the runtime can bind an unboxing
    /// stub MethodDesc directly to the precompiled code instead of generating an interpreted IL stub.
    /// It is the unboxing analog of <see cref="AsyncResumptionStub"/>.
    /// </summary>
    public partial class UnboxingStub : ILStubMethod
    {
        private readonly MethodDesc _targetMethod;
        private readonly TypeDesc _owningType;
        private MethodSignature _signature;

        public UnboxingStub(MethodDesc targetMethod, TypeDesc owningType)
        {
            Debug.Assert(targetMethod.OwningType.IsValueType);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);
            _targetMethod = targetMethod;
            _owningType = owningType;
        }

        public MethodDesc TargetMethod => _targetMethod;

        // Share the target's name so the version resilient hash code and the runtime name comparison
        // (which see the unboxing stub MethodDesc, which shares its target's name) agree.
        public override Utf8Span Name => _targetMethod.Name;

        public override string DiagnosticName => "UNBOX_" + _targetMethod.DiagnosticName;

        public override TypeDesc OwningType => _owningType;

        public override MethodSignature Signature => _signature ??= InitializeSignature();

        public override TypeSystemContext Context => _targetMethod.Context;

        /// <summary>
        /// The unboxing stub buckets with (and is looked up using) the same version resilient hash
        /// code as the unboxing stub MethodDesc at runtime, which is the target method's hash code.
        /// </summary>
        protected override int ComputeHashCode() => _targetMethod.GetHashCode();

        private MethodSignature InitializeSignature()
        {
            TypeDesc objectType = Context.GetWellKnownType(WellKnownType.Object);
            MethodSignature targetSignature = _targetMethod.Signature;

            // Static signature: the boxed 'this' is passed explicitly as the first parameter.
            // Physically this matches the instance calling convention of the target (this in arg0),
            // and reporting the boxed instance as a full object reference produces the correct GC
            // info for an unboxing stub (the boxed 'this' is a real object reference, not interior).
            TypeDesc[] parameters = new TypeDesc[targetSignature.Length + 1];
            parameters[0] = objectType;
            for (int i = 0; i < targetSignature.Length; i++)
                parameters[i + 1] = targetSignature[i];

            return new MethodSignature(MethodSignatureFlags.Static, 0, targetSignature.ReturnType, parameters);
        }

        public override MethodIL EmitIL()
        {
            ILEmitter ilEmitter = new ILEmitter();
            ILCodeStream ilStream = ilEmitter.NewCodeStream();

            // Load the boxed instance and adjust it to a managed pointer into the box payload.
            ilStream.EmitLdArg(0);
            ilStream.Emit(ILOpcode.unbox, ilEmitter.NewToken(_targetMethod.OwningType));

            // Forward the remaining arguments unchanged.
            for (int i = 0; i < _targetMethod.Signature.Length; i++)
                ilStream.EmitLdArg(i + 1);

            // Call the unboxed target directly (non-virtual), like the VM stub's calli to the target.
            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(_targetMethod));
            ilStream.Emit(ILOpcode.ret);
            ilEmitter.SetHasGeneratedTokens();

            return ilEmitter.Link(this);
        }
    }
}
