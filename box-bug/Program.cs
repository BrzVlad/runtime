using System;
using System.Runtime.CompilerServices;
using System.Threading;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type BoxAndGetType<T>(T val)
    {
        // Constrained call on T → CORINFO_BOX_THIS → ldloca + call.helper.p.ps
        return val.GetType();
    }

    static void Main()
    {
        var cts = new CancellationTokenSource();
        var gcThread = new Thread(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                GC.Collect();
                Thread.Sleep(1);
            }
        });
        gcThread.IsBackground = true;
        gcThread.Start();

        for (int iter = 0; iter < 10000; iter++)
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                Type t = BoxAndGetType(i);
                if (t != typeof(int))
                    throw new Exception($"Expected System.Int32, got {t}");
            }
            Console.Write(".");
        }

        cts.Cancel();
        gcThread.Join();
        Console.WriteLine("\nPASSED");
    }
}
