// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Arrays implement the generic collection interfaces (IEnumerable<T>, ICollection<T>,
// IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>). Dispatching such an interface call
// on an array resolves to a matching System.SZArrayHelper<T> method at runtime (see
// GetActualImplementationForArrayGenericIListOrIReadOnlyListMethod in vm/array.cpp).
//
// For a value-type element type defined in this assembly, the SZArrayHelper instantiation
// is non-canonical and must be compiled into this image (via ArrayInterfaceMethodsNode) or
// it would be interpreted at runtime. Discovery is conditional per interface slot: only the
// helper whose interface slot is actually used gets compiled. Each scenario below uses a
// distinct element struct and exercises exactly one interface slot so the discovery of each
// helper can be validated independently.

struct CountElement { public int Value; }
struct EnumElement { public int Value; }
struct ItemElement { public int Value; }
struct ContainsElement { public int Value; }
struct IndexOfElement { public int Value; }
struct CopyToElement { public int Value; }

static class ArrayInterfaceTests
{
    // ICollection<T>.Count -> SZArrayHelper.get_Count<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int UseCount()
    {
        ICollection<CountElement> c = new CountElement[3];
        return c.Count;
    }

    // IEnumerable<T>.GetEnumerator -> SZArrayHelper.GetEnumerator<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int UseGetEnumerator()
    {
        IEnumerable<EnumElement> e = new EnumElement[3];
        int sum = 0;
        foreach (EnumElement item in e)
        {
            sum += item.Value;
        }
        return sum;
    }

    // IList<T>.get_Item -> SZArrayHelper.get_Item<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ItemElement UseItem()
    {
        IList<ItemElement> l = new ItemElement[3];
        return l[0];
    }

    // ICollection<T>.Contains -> SZArrayHelper.Contains<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool UseContains(ContainsElement value)
    {
        ICollection<ContainsElement> c = new ContainsElement[3];
        return c.Contains(value);
    }

    // IList<T>.IndexOf -> SZArrayHelper.IndexOf<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int UseIndexOf(IndexOfElement value)
    {
        IList<IndexOfElement> l = new IndexOfElement[3];
        return l.IndexOf(value);
    }

    // ICollection<T>.CopyTo -> SZArrayHelper.CopyTo<T>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void UseCopyTo(CopyToElement[] destination)
    {
        ICollection<CopyToElement> c = new CopyToElement[3];
        c.CopyTo(destination, 0);
    }

    static void Run()
    {
        Console.WriteLine(UseCount());
        Console.WriteLine(UseGetEnumerator());
        Console.WriteLine(UseItem().Value);
        Console.WriteLine(UseContains(default));
        Console.WriteLine(UseIndexOf(default));
        UseCopyTo(new CopyToElement[3]);
    }
}
