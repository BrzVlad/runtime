// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Tracks usage of a generic virtual method and dynamically discovers implementations
    /// on types as they are added to the dependency graph. This is the R2R equivalent of
    /// NativeAOT's GVMDependenciesNode.
    ///
    /// When a GVM call site is encountered (e.g., IFoo.GetValue&lt;int&gt;()), this node is created
    /// for the canonical form of the method. As types implementing the interface or overriding
    /// the virtual method are discovered (via InheritedVirtualMethodsNode), this node resolves
    /// the GVM implementation on those types and adds compilation dependencies.
    ///
    /// We only analyze canonical forms of generic virtual methods to limit generic expansion,
    /// following the same approach as NativeAOT.
    /// </summary>
    public class ReadyToRunGVMDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ReadyToRunGVMDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.HasInstantiation);
            Debug.Assert(method.IsVirtual);

            _method = method;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override bool HasDynamicDependencies
        {
            get
            {
                TypeDesc methodOwningType = _method.OwningType;

                // If the method is on a sealed non-interface type or is final,
                // no further overrides are possible — nothing to discover dynamically.
                if (!methodOwningType.IsInterface &&
                    (methodOwningType.IsSealed() || _method.IsFinal))
                    return false;

                return true;
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            List<CombinedDependencyListEntry> dynamicDependencies = new List<CombinedDependencyListEntry>();

            TypeDesc methodOwningType = _method.OwningType;
            bool methodIsShared = _method.IsSharedByGenericInstantiations;
            TypeSystemContext context = _method.Context;

            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                DependencyNodeCore<NodeFactory> entry = markedNodes[i];

                // We scan for InheritedVirtualMethodsNode — these represent discovered types.
                if (entry is not InheritedVirtualMethodsNode typeNode)
                    continue;

                TypeDesc potentialOverrideType = typeNode.Type;
                if (potentialOverrideType is not DefType || potentialOverrideType.IsInterface)
                    continue;

                // If the GVM is canonical (shared), only process types that are already in
                // canonical form. Non-canonical types will have their own canonical form processed.
                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

                if (methodOwningType.IsInterface)
                {
                    ResolveInterfaceGVM(dynamicDependencies, factory, context, potentialOverrideType);
                }
                else
                {
                    ResolveClassGVM(dynamicDependencies, factory, context, potentialOverrideType);
                }
            }

            return dynamicDependencies;
        }

        private void ResolveInterfaceGVM(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            TypeSystemContext context,
            TypeDesc potentialOverrideType)
        {
            // Following NativeAOT's approach: resolve on the type definition using open
            // instantiation, then substitute. This correctly handles cases like:
            //   class Foo<T, U> : IFoo<T>, IFoo<U>, IFoo<string> { }
            // where a single canonical interface method could map to multiple implementations.
            TypeDesc potentialOverrideDefinition = potentialOverrideType.GetTypeDefinition();
            DefType[] potentialInterfaces = potentialOverrideType.RuntimeInterfaces;
            DefType[] potentialDefinitionInterfaces = potentialOverrideDefinition.RuntimeInterfaces;

            for (int interfaceIndex = 0; interfaceIndex < potentialInterfaces.Length; interfaceIndex++)
            {
                if (potentialInterfaces[interfaceIndex].ConvertToCanonForm(CanonicalFormKind.Specific) != _method.OwningType)
                    continue;

                MethodDesc interfaceMethod = _method.GetMethodDefinition();
                if (_method.OwningType.HasInstantiation)
                    interfaceMethod = context.GetMethodForInstantiatedType(
                        _method.GetTypicalMethodDefinition(), (InstantiatedType)potentialDefinitionInterfaces[interfaceIndex]);

                MethodDesc slotDecl = interfaceMethod.Signature.IsStatic
                    ? potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod)
                    : potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodTarget(interfaceMethod);

                if (slotDecl == null)
                {
                    var result = potentialOverrideDefinition.InstantiateAsOpen()
                        .ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, out slotDecl);
                    if (result != DefaultInterfaceMethodResolution.DefaultImplementation)
                        slotDecl = null;
                }

                if (slotDecl != null)
                {
                    // Create open method instantiation and substitute with the concrete arguments.
                    TypeDesc[] openInstantiation = new TypeDesc[_method.Instantiation.Length];
                    for (int instArg = 0; instArg < openInstantiation.Length; instArg++)
                        openInstantiation[instArg] = context.GetSignatureVariable(instArg, method: true);

                    MethodDesc implementingMethod = slotDecl.MakeInstantiatedMethod(openInstantiation)
                        .InstantiateSignature(potentialOverrideType.Instantiation, _method.Instantiation);

                    MethodDesc canonImpl = implementingMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                    AddMethodDependency(dynamicDependencies, factory, canonImpl);
                }
            }
        }

        private void ResolveClassGVM(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            TypeSystemContext context,
            TypeDesc potentialOverrideType)
        {
            // Walk up the type hierarchy to find where the canonical form matches _method's owning type.
            // This handles cases like:
            //   class Base<T> { virtual T M<U>(); }
            //   class Derived : Base<string> { override T M<U>(); }
            // We need to resolve Base<__Canon>.M<int> on Derived by first finding Base<string>.
            TypeDesc overrideTypeCur = potentialOverrideType;
            do
            {
                if (overrideTypeCur.ConvertToCanonForm(CanonicalFormKind.Specific) == _method.OwningType)
                    break;
                overrideTypeCur = overrideTypeCur.BaseType;
            }
            while (overrideTypeCur != null);

            if (overrideTypeCur == null)
                return;

            MethodDesc methodToResolve;
            if (_method.OwningType == overrideTypeCur)
            {
                methodToResolve = _method;
            }
            else
            {
                methodToResolve = context
                    .GetMethodForInstantiatedType(_method.GetTypicalMethodDefinition(), (InstantiatedType)overrideTypeCur)
                    .MakeInstantiatedMethod(_method.Instantiation);
            }

            MethodDesc targetMethod = potentialOverrideType.FindVirtualFunctionTargetMethodOnObjectType(methodToResolve);
            MethodDesc canonTarget = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

            if (canonTarget != _method)
            {
                AddMethodDependency(dynamicDependencies, factory, canonTarget);
            }
        }

        private void AddMethodDependency(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            MethodDesc canonMethod)
        {
            if (canonMethod.IsGenericMethodDefinition)
                return;

            if (!factory.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
                return;

            // Methods marked after phase 1 get empty code bodies, so skip.
            if (factory.CompilationCurrentPhase > 1)
                return;

            try
            {
                factory.DetectGenericCycles(_method, canonMethod);
                dynamicDependencies.Add(new CombinedDependencyListEntry(
                    factory.CompiledMethodNode(canonMethod), null, "GVM implementation on discovered type"));
            }
            catch (TypeSystemException) { }
        }

        protected override string GetName(NodeFactory factory) =>
            "__ReadyToRunGVMDependencies_" + factory.NameMangler.GetMangledMethodName(_method);
    }
}
