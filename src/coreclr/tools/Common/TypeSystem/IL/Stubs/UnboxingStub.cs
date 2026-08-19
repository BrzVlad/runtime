// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal sealed class BoxedValueType : MetadataType, INonEmittableType, IPrefixMangledType
    {
        public BoxedValueType(MetadataType valueType)
        {
            Debug.Assert(valueType.IsTypeDefinition);
            Debug.Assert(valueType.IsValueType);

            ValueTypeRepresented = valueType;
        }

        public MetadataType ValueTypeRepresented { get; }

        public override ModuleDesc Module => ValueTypeRepresented.Module;
        public override Utf8Span Name => "Boxed_"u8.Append(ValueTypeRepresented.Name);
        public override Utf8Span Namespace => ValueTypeRepresented.Namespace;
        public override string DiagnosticName => "Boxed_" + ValueTypeRepresented.DiagnosticName;
        public override string DiagnosticNamespace => ValueTypeRepresented.DiagnosticNamespace;
        public override Instantiation Instantiation => ValueTypeRepresented.Instantiation;
        public override PInvokeStringFormat PInvokeStringFormat => PInvokeStringFormat.AutoClass;
        public override bool IsExplicitLayout => false;
        public override bool IsSequentialLayout => true;
        public override bool IsExtendedLayout => false;
        public override bool IsAutoLayout => false;
        public override bool IsBeforeFieldInit => false;
        public override MetadataType BaseType => (MetadataType)Context.GetWellKnownType(WellKnownType.Object);
        public override bool IsSealed => true;
        public override bool IsAbstract => false;
        public override MetadataType ContainingType => null;
        public override DefType[] ExplicitlyImplementedInterfaces => Array.Empty<DefType>();
        public override TypeSystemContext Context => ValueTypeRepresented.Context;

        TypeDesc IPrefixMangledType.BaseType => ValueTypeRepresented;
        ReadOnlySpan<byte> IPrefixMangledType.Prefix => "Boxed"u8;

        public override ClassLayoutMetadata GetClassLayout() => default;
        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        public override IEnumerable<MetadataType> GetNestedTypes() => Array.Empty<MetadataType>();
        public override MetadataType GetNestedType(Utf8Span name) => null;
        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => Array.Empty<MethodImplRecord>();
        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(Utf8Span name) => Array.Empty<MethodImplRecord>();
        public override FieldDesc GetField(Utf8Span name) => null;
        public override IEnumerable<FieldDesc> GetFields() => Array.Empty<FieldDesc>();
        public override int GetHashCode() => ValueTypeRepresented.GetHashCode();

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = TypeFlags.HasFinalizerComputed | TypeFlags.AttributeCacheComputed;

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;
            }

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= TypeFlags.Class;
            }

            return flags;
        }

        protected override int ClassCode => 0x3F4D7A44;

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(ValueTypeRepresented, ((BoxedValueType)other).ValueTypeRepresented);
        }
    }

    public sealed class UnboxingStub : ILStubMethod, IPrefixMangledMethod
    {
        private readonly MethodDesc _targetMethod;
        private readonly TypeDesc _owningType;

        public UnboxingStub(MethodDesc targetMethod, TypeDesc owningType)
        {
            Debug.Assert(targetMethod.OwningType.IsValueType);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);
            Debug.Assert(!owningType.IsValueType);

            _targetMethod = targetMethod;
            _owningType = owningType;
        }

        public MethodDesc TargetMethod => _targetMethod;

        public override Utf8Span Name => _targetMethod.Name;

        public override string DiagnosticName => "UNBOX_" + _targetMethod.DiagnosticName;

        public override TypeDesc OwningType => _owningType;

        public override MethodSignature Signature => _targetMethod.Signature;

        public override TypeSystemContext Context => _targetMethod.Context;

        public override bool IsCanonicalMethod(CanonicalFormKind policy) => OwningType.IsCanonicalSubtype(policy);

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

            FieldDesc rawDataField = Context.SystemModule
                .GetKnownType("System.Runtime.CompilerServices"u8, "RawData"u8)
                .GetKnownField("Data"u8);
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(rawDataField));

            if (_targetMethod.IsSharedByGenericInstantiations)
            {
                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(rawDataField));
                codeStream.EmitLdc(Context.Target.PointerSize);
                codeStream.Emit(ILOpcode.sub);
                codeStream.Emit(ILOpcode.ldind_i);
                codeStream.Emit(
                    ILOpcode.call,
                    emitter.NewToken(Context.GetCoreLibEntryPoint(
                        "System.Runtime.CompilerServices"u8,
                        "RuntimeHelpers"u8,
                        "SetNextCallGenericContext"u8,
                        null)));
            }

            for (int i = 0; i < _targetMethod.Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(_targetMethod));
            codeStream.Emit(ILOpcode.ret);
            emitter.SetHasGeneratedTokens();

            return emitter.Link(this);
        }
    }
}
