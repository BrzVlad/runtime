// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Tracks usage of a virtual or interface method call and dynamically discovers
    /// implementations on types as they are added to the dependency graph.
    ///
    /// Created from VirtualEntry fixups in MethodFixupSignature. For each discovered
    /// concrete type (via InheritedVirtualMethodsNode as an "interesting" type marker),
    /// this node resolves the method implementation and adds it as a compilation dependency.
    ///
    /// Handles both GVM (generic virtual methods with method-level instantiation) and
    /// non-GVM virtual/interface dispatch. This is demand-driven: only implementations
    /// that are actually dispatched via virtual calls get compiled.
    ///
    /// Canonical forms are used to limit generic expansion — reference type instantiations
    /// collapse to __Canon, while value types keep their specific instantiation.
    /// </summary>
    public class ReadyToRunVirtualMethodDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ReadyToRunVirtualMethodDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
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
                // no overrides are possible — nothing to discover dynamically.
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

                // If the method is canonical (shared), only process types that are already in
                // canonical form. Non-canonical types will have their own canonical form processed.
                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

                if (methodOwningType.IsInterface)
                {
                    ResolveInterfaceMethod(dynamicDependencies, factory, context, potentialOverrideType);
                }
                else
                {
                    ResolveClassVirtualMethod(dynamicDependencies, factory, context, potentialOverrideType);
                }
            }

            return dynamicDependencies;
        }

        private void ResolveInterfaceMethod(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            TypeSystemContext context,
            TypeDesc potentialOverrideType)
        {
            // Resolve on the type definition using open instantiation, then substitute.
            // This correctly handles cases like:
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
                    MethodDesc implementingMethod;
                    if (_method.HasInstantiation)
                    {
                        // GVM: create open method instantiation and substitute with concrete arguments.
                        TypeDesc[] openInstantiation = new TypeDesc[_method.Instantiation.Length];
                        for (int instArg = 0; instArg < openInstantiation.Length; instArg++)
                            openInstantiation[instArg] = context.GetSignatureVariable(instArg, method: true);

                        implementingMethod = slotDecl.MakeInstantiatedMethod(openInstantiation)
                            .InstantiateSignature(potentialOverrideType.Instantiation, _method.Instantiation);
                    }
                    else
                    {
                        // Non-GVM: just substitute type-level arguments.
                        implementingMethod = slotDecl.InstantiateSignature(
                            potentialOverrideType.Instantiation, new Instantiation());
                    }

                    MethodDesc canonImpl = implementingMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                    AddMethodDependency(dynamicDependencies, factory, canonImpl);
                }
            }
        }

        private void ResolveClassVirtualMethod(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            TypeSystemContext context,
            TypeDesc potentialOverrideType)
        {
            // Walk up the type hierarchy to find where the canonical form matches.
            TypeDesc overrideTypeCur = potentialOverrideType;
            do
            {
                if (overrideTypeCur.ConvertToCanonForm(CanonicalFormKind.Specific) == _method.OwningType)
                    break;

                // Value type instantiations (e.g. SortedSet<int>) keep their specific form
                // under CanonicalFormKind.Specific, so they will never match a shared owning
                // type like SortedSet<__Canon>. Fall back to comparing type definitions so
                // that virtual dispatch is resolved for these instantiations as well.
                if (overrideTypeCur.HasInstantiation &&
                    _method.OwningType.HasInstantiation &&
                    overrideTypeCur.GetTypeDefinition() == _method.OwningType.GetTypeDefinition())
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
                MethodDesc typicalOnConcreteType = context
                    .GetMethodForInstantiatedType(_method.GetTypicalMethodDefinition(), (InstantiatedType)overrideTypeCur);

                methodToResolve = _method.HasInstantiation
                    ? typicalOnConcreteType.MakeInstantiatedMethod(_method.Instantiation)
                    : typicalOnConcreteType;
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
                    factory.CompiledMethodNode(canonMethod), null, "Virtual dispatch implementation on discovered type"));
            }
            catch (TypeSystemException) { }
        }

        protected override string GetName(NodeFactory factory) =>
            "__ReadyToRunVirtualMethodDependencies_" + factory.NameMangler.GetMangledMethodName(_method);
    }
}
