﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Frozen
{
    internal sealed partial class OrdinalStringFrozenSet_RightJustifiedSingleChar : OrdinalStringFrozenSet
    {
        internal OrdinalStringFrozenSet_RightJustifiedSingleChar(
            string[] entries,
            IEqualityComparer<string> comparer,
            int minimumLength,
            int maximumLengthDiff,
            int hashIndex)
            : base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, 1)
        {
        }

        // See comment in OrdinalStringFrozenSet for why these overrides exist. Do not remove.
        private protected override int FindItemIndex(string item) => base.FindItemIndex(item);

        private protected override int GetHashCode(string s) => s[s.Length + HashIndex];
        private protected override int GetHashCode(ReadOnlySpan<char> s) => s[s.Length + HashIndex];
    }
}
