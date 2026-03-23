// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Ensures that interface method implementations inherited from generic base types
    /// are compiled for a given type instantiation.
    ///
    /// When a generic type like Derived&lt;int&gt; implements an interface via a method defined
    /// on a base class (e.g., Base&lt;int&gt;.IFoo&lt;int&gt;.GetValue()), AllMethodsOnTypeNode only
    /// discovers methods defined directly on Derived&lt;T&gt;. This node fills the gap by walking
    /// the type's interface map, resolving each interface method to its actual implementation
    /// (which may live on a base type), and creating compilation dependencies for those methods.
    /// </summary>
    public class InterfaceMethodsOnTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public InterfaceMethodsOnTypeNode(TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            DependencyList dependencies = new DependencyList();

            DefType defType = _type.GetClosestDefType();

            foreach (DefType interfaceType in defType.RuntimeInterfaces)
            {
                foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                {
                    // Skip static virtual methods — they are resolved through a different mechanism
                    if (interfaceMethod.Signature.IsStatic)
                        continue;

                    // Resolve: which method actually implements this interface method?
                    // This walks the full type hierarchy (type → base → base.base → ...)
                    MethodDesc implMethod = defType.ResolveInterfaceMethodTarget(interfaceMethod);
                    if (implMethod == null)
                        continue;

                    // If the implementation lives on the same type, AllMethodsOnTypeNode already
                    // handles it — we only need to act on inherited implementations.
                    if (implMethod.OwningType.HasSameTypeDefinition(defType))
                        continue;

                    // Walk up the hierarchy to find the correctly-instantiated base type
                    // that owns this implementation (e.g., Base<int> not Base<T>).
                    MethodDesc targetMethod = implMethod;
                    TypeDesc implType = defType;
                    while (!implType.HasSameTypeDefinition(implMethod.OwningType))
                        implType = implType.BaseType;

                    if (!implType.IsTypeDefinition)
                        targetMethod = implType.Context.GetMethodForInstantiatedType(
                            implMethod.GetTypicalMethodDefinition(), (InstantiatedType)implType);

                    // Canonicalize — CompiledMethodNode requires canonical form
                    MethodDesc canonMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    if (!canonMethod.IsGenericMethodDefinition &&
                        context.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
                    {
                        try
                        {
                            context.DetectGenericCycles(_type, canonMethod);
                            dependencies.Add(context.CompiledMethodNode(canonMethod),
                                $"Interface method {interfaceMethod} implemented on base type for {_type}");
                        }
                        catch (TypeSystemException)
                        {
                        }
                    }
                }
            }

            return dependencies;
        }

        protected override string GetName(NodeFactory factory) => $"Interface methods on type {_type}";
    }
}
