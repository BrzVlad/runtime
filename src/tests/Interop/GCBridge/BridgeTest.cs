using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Java;
using Xunit;

////////////////////////
//JavaMarshal needs
//    - MarkCrossReferences callback which is a native function pointer inside bridge.cpp
//
//bridge.cpp needs
//    - Callback into managed to inform that bridge processing is finished

public unsafe class GCBridgeTests 
{
    [DllImport("GCBridgeNative")]
    private static extern int SimplePrint();

    [DllImport("GCBridgeNative")]
    private static extern delegate* unmanaged<nint, StronglyConnectedComponent*, nint, ComponentCrossReference*, void> GetMarkCrossReferencesFtn();

    [Fact]
    public static int TestEntryPoint()
    {
        JavaMarshal.Initialize(GetMarkCrossReferencesFtn());
        Object obj = new Object();

        JavaMarshal.CreateReferenceTrackingHandle(obj, IntPtr.Zero);

        GC.Collect();

        return SimplePrint();
    }
}
