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

        foreach(var word in words)
        {
            count += BitOperations.PopCount(word);
        }

        // any remaining bytes that don't fit into a ulong?
        int remainderStart = words.Length * sizeof(ulong);

        for (int i = remainderStart; i < vector.Length; i++)
        {
            count += BitOperations.PopCount(vector[i]);
        }

        return count;
    }

    internal static void And(byte[] objectVector, byte[] sourceVector)
    {
        if(objectVector.Length != sourceVector.Length)
        {
            throw new PaleyEngineException("Bit vectors must be the same length for AND operation.");
        }

        AndVector256(objectVector, sourceVector);
    }

    internal static void Or(byte[] objectVector, byte[] sourceVector)
    {
        if (objectVector.Length != sourceVector.Length)
        {
            throw new PaleyEngineException("Bit vectors must be the same length for OR operation.");
        }

        OrVector256(objectVector, sourceVector);
    }

    internal static void Not(byte[] objectVector, byte[] sourceVector)
    {
        if (objectVector.Length != sourceVector.Length)
        {
            throw new PaleyEngineException("Bit vectors must be the same length for NOT operation.");
        }

        throw new NotImplementedException("NOT operation is not implemented yet.");
        //NotVector256(objectVector, sourceVector);
    }

    private static void AndUlong(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        Span<ulong> destWords =
            MemoryMarshal.Cast<byte, ulong>(destination);

        ReadOnlySpan<ulong> srcWords =
            MemoryMarshal.Cast<byte, ulong>(source);

        for (int i = 0; i < destWords.Length; i++)
        {
            destWords[i] &= srcWords[i];
        }

        int remainderStart =
            destWords.Length * sizeof(ulong);

        for (int i = remainderStart; i < destination.Length; i++)
        {
            destination[i] &= source[i];
        }
    }

    private static void AndVector256(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        if (!Avx2.IsSupported)
        {
            AndUlong(destination, source);
            return;
        }

        ref byte destRef =
            ref MemoryMarshal.GetReference(destination);

        ref byte srcRef =
            ref MemoryMarshal.GetReference(source);

        int width = Vector256<byte>.Count;
        int i = 0;

        for (; i <= destination.Length - width; i += width)
        {
            var left =
                Vector256.LoadUnsafe(ref destRef, (nuint)i);

            var right =
                Vector256.LoadUnsafe(ref srcRef, (nuint)i);

            Avx2.And(left, right)
                 .StoreUnsafe(ref destRef, (nuint)i);
        }

        for (; i < destination.Length; i++)
        {
            destination[i] &= source[i];
        }
    }

    public static void OrUlong(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        Span<ulong> destWords =
            MemoryMarshal.Cast<byte, ulong>(destination);

        ReadOnlySpan<ulong> srcWords =
            MemoryMarshal.Cast<byte, ulong>(source);

        for (int i = 0; i < destWords.Length; i++)
        {
            destWords[i] |= srcWords[i];
        }

        int remainderStart =
            destWords.Length * sizeof(ulong);

        for (int i = remainderStart; i < destination.Length; i++)
        {
            destination[i] |= source[i];
        }
    }

    public static void OrVector256(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        if (!Avx2.IsSupported)
        {
            OrUlong(destination, source);
            return;
        }

        ref byte destRef =
            ref MemoryMarshal.GetReference(destination);

        ref byte srcRef =
            ref MemoryMarshal.GetReference(source);

        int width = Vector256<byte>.Count;
        int i = 0;

        for (; i <= destination.Length - width; i += width)
        {
            var left =
                Vector256.LoadUnsafe(ref destRef, (nuint)i);

            var right =
                Vector256.LoadUnsafe(ref srcRef, (nuint)i);

            Avx2.Or(left, right)
                 .StoreUnsafe(ref destRef, (nuint)i);
        }

        for (; i < destination.Length; i++)
        {
            destination[i] |= source[i];
        }
    }
}
