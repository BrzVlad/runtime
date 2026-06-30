// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Discovery node for the generic collection interface methods implemented by arrays.
    ///
    /// Arrays implement the generic collection interfaces (IEnumerable&lt;T&gt;, ICollection&lt;T&gt;,
    /// IList&lt;T&gt;, IReadOnlyCollection&lt;T&gt;, IReadOnlyList&lt;T&gt;). At runtime, a call such as
    /// ((ICollection&lt;int&gt;)intArray).Count is dispatched to SZArrayHelper.get_Count&lt;int&gt;
    /// (see GetActualImplementationForArrayGenericIListOrIReadOnlyListMethod in vm/array.cpp).
    /// SZArrayHelper is simply a class with one generic method per interface method, named identically
    /// to the interface method it implements.
    ///
    /// The regular non-GVM virtual method discovery (InheritedVirtualMethodsNode) only runs for
    /// DefTypes, and crossgen2 (unlike NativeAOT, which models these on the DefType Array&lt;T&gt;) does
    /// not model the array's collection methods on any DefType. So these implementations would otherwise
    /// never be discovered. For value-type element arrays the SZArrayHelper instantiation is
    /// non-canonical and must be compiled explicitly, otherwise it is interpreted at runtime.
    ///
    /// This node has conditional static dependencies: the resolved SZArrayHelper implementation is
    /// compiled only when the corresponding interface slot is actually used (via VirtualMethodUseNode).
    /// This keeps us from compiling methods that are never dispatched on arrays (e.g. the always-throwing
    /// Add/Insert/Remove/RemoveAt/Clear helpers).
    /// </summary>
    public class ArrayInterfaceMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ArrayType _arrayType;

        public ArrayInterfaceMethodsNode(ArrayType arrayType)
        {
            Debug.Assert(arrayType.IsSzArray);
            _arrayType = arrayType;
        }

        public ArrayType ArrayType => _arrayType;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            List<CombinedDependencyListEntry> result = new List<CombinedDependencyListEntry>();

            MetadataType szArrayHelper = factory.TypeSystemContext.SystemModule.GetType("System"u8, "SZArrayHelper"u8, throwIfNotFound: false);
            if (szArrayHelper == null)
                return result;

            TypeDesc elementType = _arrayType.ElementType;

            foreach (DefType interfaceType in _arrayType.RuntimeInterfaces)
            {
                // Only the generic collection interfaces are dispatched through SZArrayHelper.
                if (!interfaceType.HasInstantiation)
                    continue;

                foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                {
                    // GVMs are handled by GVMDependenciesNode, static virtual methods at the call site.
                    if (interfaceMethod.HasInstantiation || interfaceMethod.Signature.IsStatic)
                        continue;

                    // SZArrayHelper provides one generic method per interface method, with a matching name.
                    MethodDesc helperMethodDef = szArrayHelper.GetMethod(interfaceMethod.Name, null);
                    if (helperMethodDef == null)
                        continue;

                    MethodDesc helperMethod = factory.TypeSystemContext.GetInstantiatedMethod(helperMethodDef, new Instantiation(elementType));
                    MethodDesc canonHelperMethod = helperMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    if (!factory.CompilationModuleGroup.ContainsMethodBody(canonHelperMethod, false))
                        continue;

                    // No generic cycle detection is needed here: the SZArrayHelper helpers only ever
                    // reference their type parameter T at the same depth (T[], IndexOf<T>,
                    // SZGenericArrayEnumerator<T>, ...). They never bind T to a strictly deeper
                    // expression involving themselves, so their definitions can never form a flagged
                    // generic cycle and cannot cause unbounded instantiation growth.
                    result.Add(new CombinedDependencyListEntry(
                        factory.CompiledMethodNode(canonHelperMethod),
                        factory.VirtualMethodUse(interfaceMethod),
                        "Array generic interface method implemented by SZArrayHelper"));
                }
            }

            return result;
        }

        protected override string GetName(NodeFactory factory) => $"Array interface methods on {_arrayType}";
    }
}
