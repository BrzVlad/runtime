// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Type discovery marker node for virtual dispatch dependency analysis.
    ///
    /// Created for each non-generic-definition, non-interface type encountered via
    /// TypeFixupSignature. Marked as InterestingForDynamicDependencyAnalysis so that
    /// ReadyToRunVirtualMethodDependenciesNode can scan for discovered types and
    /// resolve virtual/interface dispatch implementations on them.
    ///
    /// Also eagerly compiles the static constructor (.cctor) if present, since it is
    /// invoked implicitly by the runtime on first type access and has no explicit call
    /// site that would create a fixup dependency.
    /// </summary>
    public class InheritedVirtualMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public InheritedVirtualMethodsNode(TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => true;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            if (_type is not DefType defType)
                return null;

            DependencyList dependencies = null;

            // Static and instance constructors are invoked implicitly by the runtime
            // (e.g., .cctor on first type access, .ctor via Activator.CreateInstance,
            // default comparer creation, etc.) with no explicit call site that creates
            // a fixup. Eagerly compile them so that methods they call are transitively compiled.
            foreach (MethodDesc method in defType.GetMethods())
            {
                if (!method.IsConstructor && !method.IsStaticConstructor)
                    continue;

                MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                if (canonMethod.IsGenericMethodDefinition ||
                    !context.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
                    continue;

                try
                {
                    context.DetectGenericCycles(_type, canonMethod);
                    dependencies ??= new DependencyList();
                    dependencies.Add(context.CompiledMethodNode(canonMethod), "Constructor on discovered type");
                }
                catch (TypeSystemException) { }
            }

            return dependencies;
        }

        protected override string GetName(NodeFactory factory) => $"Inherited virtual methods on {_type}";
    }
}
