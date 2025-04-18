using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Java;
using Xunit;

public class Bridge
{
    public List<object> Links;

    public unsafe Bridge()
    {
        Links = new List<object>();
        IntPtr *pContext = (IntPtr*)NativeMemory.Alloc(((nuint)sizeof(void*)));
        GCHandle handle = JavaMarshal.CreateReferenceTrackingHandle(this, (nint)pContext);

        *pContext = GCHandle.ToIntPtr(handle);
    }
}

public class NonBridge
{
    public object Link;
}

public class NonBridge2 : NonBridge
{
    public object Link2;
}

public unsafe class GCBridgeTests 
{
    [DllImport("GCBridgeNative")]
    private static extern delegate* unmanaged<nint, StronglyConnectedComponent*, nint, ComponentCrossReference*, void> GetMarkCrossReferencesFtn();

    [DllImport("GCBridgeNative")]
    private static extern void SetBridgeProcessingFinishCallback(delegate* unmanaged<nint, StronglyConnectedComponent*, nint, ComponentCrossReference*, void> callback);

    static bool releaseHandles;
    static nint expectedSccsLen, expectedCcrsLen;

    [UnmanagedCallersOnly]
    internal static unsafe void BridgeProcessingFinishCallback(nint sccsLen, StronglyConnectedComponent* sccs, nint ccrsLen, ComponentCrossReference* ccrs)
    {
        Console.WriteLine("Bridge processing finish SCCs {0}, CCRs {1}", sccsLen, ccrsLen);
        if (expectedSccsLen != sccsLen || expectedCcrsLen != ccrsLen)
        {
            Console.WriteLine("Expected SCCs {0}, CCRs {1}", expectedSccsLen, expectedCcrsLen);
            Environment.Exit(1);
        }

        if (releaseHandles)
        {
            for (int i = 0; i < sccsLen; i++)
            {
                for (int j = 0; j < sccs[i].Count; j++)
                {
                    IntPtr *pContext = (IntPtr*)sccs[i].Context[j];
                    GCHandle handle = GCHandle.FromIntPtr(*pContext);
                    handle.Free();

                    NativeMemory.Free(pContext);
                }
            }
        }

        JavaMarshal.ReleaseMarkCrossReferenceResources(
            new Span<StronglyConnectedComponent>(sccs, (int)sccsLen),
            new Span<ComponentCrossReference>(ccrs, (int)ccrsLen));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        JavaMarshal.Initialize(GetMarkCrossReferencesFtn());
        SetBridgeProcessingFinishCallback(&BridgeProcessingFinishCallback);

        return RunGraphTest(NestedCycles, 2, 1);
    }

    static bool CheckWeakRefs(List<WeakReference> weakRefs, bool expectedAlive)
    {
        foreach (WeakReference weakRef in weakRefs)
        {
            if (expectedAlive)
            {
                if (weakRef.Target == null)
                    return false;
                if (!weakRef.IsAlive)
                    return false;
            }
            else
            {
                if (weakRef.Target != null)
                    return false;
                if (weakRef.IsAlive)
                    return false;
            }
        }
        return true;
    }

    static int RunGraphTest(Func<List<WeakReference>> buildGraph, nint expectedSCCs, nint expectedCCRs)
    {
        if (!GC.TryStartNoGCRegion(100000))
            return 10;
        Console.WriteLine("Start test {0}", buildGraph.Method.Name);
        List<WeakReference> weakRefs = buildGraph();

        Console.WriteLine(" First GC");
        releaseHandles = false;
        expectedSccsLen = expectedSCCs;
        expectedCcrsLen = expectedCCRs;
        GC.EndNoGCRegion();
        GC.Collect ();

        if (!GC.TryStartNoGCRegion(100000))
            return 11;
        Thread.Sleep (500);

        if (!CheckWeakRefs(weakRefs, true))
            return 12;

        Console.WriteLine(" Second GC");
        releaseHandles = true;
        GC.EndNoGCRegion();
        GC.Collect ();

        // This should wait for bridge processing to finish (which also includes nulling of weakrefs)
        if (!CheckWeakRefs(weakRefs, false))
            return 13;

        if (!GC.TryStartNoGCRegion(100000))
            return 14;
        Thread.Sleep (500);
        Console.WriteLine(" Third GC");
        expectedSccsLen = 0;
        expectedCcrsLen = 0;
        GC.EndNoGCRegion();
        GC.Collect ();

        Thread.Sleep (500);
        Console.WriteLine("Finished test {0}", buildGraph.Method.Name);
        return 100;
    }

    // Simulates a graph with two nested cycles that is produces by
    // the async state machine when `async Task M()` method gets its
    // continuation rooted by an Action held by RunnableImplementor
    // (ie. the task continuation is hooked through the SynchronizationContext
    // implentation and rooted only by Android bridge objects).
    static List<WeakReference> NestedCycles()
    {
        Bridge runnableImplementor = new Bridge();
        Bridge byteArrayOutputStream = new Bridge();

        List<WeakReference> weakRefs = new List<WeakReference>();
        weakRefs.Add(new WeakReference(runnableImplementor));
        weakRefs.Add(new WeakReference(byteArrayOutputStream));

        NonBridge2 action = new NonBridge2();
        NonBridge displayClass = new NonBridge();
        NonBridge2 asyncStateMachineBox = new NonBridge2();
        NonBridge2 asyncStreamWriter = new NonBridge2();

        runnableImplementor.Links.Add(action);
        action.Link = displayClass;
        action.Link2 = asyncStateMachineBox;
        displayClass.Link = action;
        asyncStateMachineBox.Link = asyncStreamWriter;
        asyncStateMachineBox.Link2 = action;
        asyncStreamWriter.Link = byteArrayOutputStream;
        asyncStreamWriter.Link2 = asyncStateMachineBox;

        return weakRefs;
    }
}
