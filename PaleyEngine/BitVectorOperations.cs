using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PaleyEngine;

internal static class BitVectorOperations
{
    internal static long Tally(byte[] vector)
    {
        long count = 0;

        ReadOnlySpan<ulong> words =
            MemoryMarshal.Cast<byte, ulong>(vector.AsSpan());

        for (int i = 0; i < words.Length; i++)
        {
            count += BitOperations.PopCount(words[i]);
        }

        // any remaining bytes that don't fit into a ulong?
        int remainderStart = words.Length * sizeof(ulong);

        for (int i = remainderStart; i < vector.Length; i++)
        {
            count += BitOperations.PopCount(vector[i]);
        }

        return count;
    }

}
