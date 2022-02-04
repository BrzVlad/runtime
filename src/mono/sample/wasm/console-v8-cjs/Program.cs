// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class Test
{
        public static int Method1 (int a)
        {
                Method2 (a);
                return 1;
        }

        public static int Method2 (int a)
        {
                Method3 (a);
                return 2;
        }

        public static int Method3 (int a)
        {
                Method4 (a);
                return 3;
        }

        public static int Method4 (int a)
        {
                StackTrace st = new StackTrace(true);


                Console.WriteLine ("HELLO {0}, Stack: {1}", a, st.ToString ());
                return 4;
        }

    public static async Task<int> Main(string[] args)
    {
        await Task.Delay(1);
        Console.WriteLine("Hello World!");
        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine($"args[{i}] = {args[i]}");
        }
        Method1 (20);
        return 0;
    }
}
