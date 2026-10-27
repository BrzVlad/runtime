// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.JitInterface;
using System.Diagnostics;

namespace ILCompiler
{
    /// <summary>
    /// Root supported hardware intrinsics and Vector128 fallbacks when target doesn't
    /// allow runtime code generation. Profiles for these targets will have to be obtained
    /// via the interpreter, which will not be able to observe some of these paths, due to
    /// missing hardware acceleration support.
    /// </summary>
    public class ReadyToRunHardwareIntrinsicRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            InstructionSetSupport specifiedInstructionSet = context.InstructionSetSupport;
            foreach (InstructionSet instructionSet in specifiedInstructionSet.SupportedFlags)
            {
                foreach (MetadataType hardwareIntrinsicType in InstructionSetParser.LookupPlatformIntrinsicTypes(context, instructionSet))
                {
                    foreach (MethodDesc method in hardwareIntrinsicType.GetMethods())
                    {
                        rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, "Supported hardware intrinsic method");
                    }
                }
            }

            // Here we will only root methods from the Vector128 types, missing any other methods that might get called from
            // them, both for simplicity and for reducing the size increase. Add some form of dependency expansion starting
            // from these rooted Vector128 methods, if this is proved to be needed.
            TypeDesc[] elementTypes =
            [
                context.GetWellKnownType(WellKnownType.SByte),
                context.GetWellKnownType(WellKnownType.Byte),
                context.GetWellKnownType(WellKnownType.Int16),
                context.GetWellKnownType(WellKnownType.UInt16),
                context.GetWellKnownType(WellKnownType.Int32),
                context.GetWellKnownType(WellKnownType.UInt32),
                context.GetWellKnownType(WellKnownType.Int64),
                context.GetWellKnownType(WellKnownType.UInt64),
                context.GetWellKnownType(WellKnownType.IntPtr),
                context.GetWellKnownType(WellKnownType.UIntPtr),
                context.GetWellKnownType(WellKnownType.Single),
                context.GetWellKnownType(WellKnownType.Double),
            ];
            MetadataType vector128 = context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector128"u8, throwIfNotFound: false);
            RootVector128Methods(vector128, elementTypes, rootProvider);
            MetadataType genericVector128 = context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector128`1"u8, throwIfNotFound: false);
            if (genericVector128 is not null)
            {
                foreach (TypeDesc elementType in elementTypes)
                    RootVector128Methods(genericVector128.MakeInstantiatedType(elementType), elementTypes, rootProvider);
            }
        }

        internal static bool IsVector128Method(MethodDesc method)
        {
            return method.OwningType.GetTypeDefinition() is MetadataType owningType &&
                owningType.Module == method.Context.SystemModule &&
                owningType.Namespace == "System.Runtime.Intrinsics"u8 &&
                (owningType.Name == "Vector128"u8 || owningType.Name == "Vector128`1"u8);
        }

        private void RootVector128Methods(MetadataType type, TypeDesc[] elementTypes, IRootingServiceProvider rootProvider)
        {
            if (type is null)
                return;

            foreach (MethodDesc method in type.GetMethods())
            {
                if (method.IsAbstract || method.IsInternalCall)
                    continue;

                // Avoid rooting Cartesian products of generic method arguments.
                if (method.Instantiation.Length > 1)
                    continue;

                if (method.HasInstantiation)
                {
                    foreach (TypeDesc elementType in elementTypes)
                        RootVector128Method(method.MakeInstantiatedMethod(new Instantiation([elementType])), rootProvider);
                }
                else
                    RootVector128Method(method, rootProvider);
            }
        }

        private void RootVector128Method(MethodDesc method, IRootingServiceProvider rootProvider)
        {
            if (!method.CheckConstraints() || CorInfoImpl.ShouldSkipCompilation(context.InstructionSetSupport, method))
                return;

            rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, reason: "Vector128 method");
        }
    }
}
