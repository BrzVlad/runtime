// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Ensures that virtual and interface method implementations inherited from generic
    /// base types are compiled for a given type instantiation.
    ///
    /// AllMethodsOnTypeNode only discovers methods defined directly on a type. When a
    /// type like Derived&lt;int&gt; inherits interface or virtual method implementations from
    /// Base&lt;int&gt;, those inherited methods are missed. This node fills the gap by:
    ///   1. Walking the type's interface map and resolving each interface method to its
    ///      actual implementation (which may be on a base type).
    ///   2. Enumerating all virtual slots and resolving each to its final override
    ///      (which may be on a base type).
    /// Only inherited implementations are included — methods defined on the type itself
    /// are already handled by AllMethodsOnTypeNode.
    /// </summary>
    public class InheritedVirtualMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public InheritedVirtualMethodsNode(TypeDesc type)
        {
            _type = type;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            DependencyList dependencies = new DependencyList();
            if (_type is not DefType)
                return dependencies;
            DefType defType = _type.GetClosestDefType();

            // 1. Interface method implementations inherited from base types
            foreach (DefType interfaceType in defType.RuntimeInterfaces)
            {
                foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                {
                    if (interfaceMethod.Signature.IsStatic)
                        continue;

                    MethodDesc implMethod = defType.ResolveInterfaceMethodTarget(interfaceMethod);
                    if (implMethod == null)
                        continue;

                    // AllMethodsOnTypeNode already handles methods defined on this type
                    if (implMethod.OwningType.HasSameTypeDefinition(defType))
                        continue;

                    AddInheritedMethod(dependencies, context, defType, implMethod);
                }
            }

            // 2. Virtual method overrides inherited from base types
            foreach (MethodDesc virtualSlot in defType.EnumAllVirtualSlots())
            {
                if (virtualSlot.HasInstantiation)
                    continue;

                MethodDesc implMethod = defType.FindVirtualFunctionTargetMethodOnObjectType(virtualSlot);
                if (implMethod == null)
                    continue;

                // AllMethodsOnTypeNode already handles methods defined on this type
                if (implMethod.OwningType.HasSameTypeDefinition(defType))
                    continue;

                AddInheritedMethod(dependencies, context, defType, implMethod);
            }

            return dependencies;
        }

        private void AddInheritedMethod(DependencyList dependencies, NodeFactory context, DefType defType, MethodDesc implMethod)
        {
            // Walk up to the correctly-instantiated base type owning the implementation
            MethodDesc targetMethod = implMethod;
            TypeDesc implType = defType;
            while (!implType.HasSameTypeDefinition(implMethod.OwningType))
                implType = implType.BaseType;

            if (!implType.HasInstantiation)
                return;

            if (!implType.IsTypeDefinition)
                targetMethod = implType.Context.GetMethodForInstantiatedType(
                    implMethod.GetTypicalMethodDefinition(), (InstantiatedType)implType);

            MethodDesc canonMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

            if (!canonMethod.IsGenericMethodDefinition &&
                context.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
            {
                try
                {
                    context.DetectGenericCycles(_type, canonMethod);
                    dependencies.Add(context.CompiledMethodNode(canonMethod),
                        $"Inherited virtual/interface method on {_type}");
                }
                catch (TypeSystemException)
                {
                }
            }
        }

        protected override string GetName(NodeFactory factory) => $"Inherited virtual methods on {_type}";
    }
}
