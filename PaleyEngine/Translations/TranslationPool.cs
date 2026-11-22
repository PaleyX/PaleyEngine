using System;
using System.Collections.Generic;
using System.Linq;

namespace PaleyEngine.Translations;

/// <summary>
/// The buffer pool for the large arrays used to return the translated URNs
/// in reality there should only ever be one buffer at most in the pool
/// but by reusing it instead of creating and recreating huge byte arrays
/// it will take some pressure off the Large Object Heap and the GC
/// </summary>
internal sealed class TranslationPool
{
    private readonly List<byte[]> _Buffers;

    private TranslationPool()
    {
        _Buffers = new List<byte[]>();
    }

    // Singleton - the only way to access the pool
    private static TranslationPool _Instance;
    internal static TranslationPool Instance
    {
        get
        {
            return _Instance ?? (_Instance = new TranslationPool());
        }
    }

    /// <summary>
    /// Return a byte array of the specified size, either from the pool or by creating a new one
    /// if it is taken from the pool it is removed from the pool
    /// </summary>
    /// <param name="size">the length of the desired byte array</param>
    /// <returns>a byte array</returns>
    internal byte[] CheckOut(long size)
    {
        for (int i = 0; i < _Buffers.Count; ++i)
        {
            if (_Buffers[i].Length == size)
            {
                var buffer = _Buffers[i];
                _Buffers.RemoveAt(i);

                return buffer;
            }
        }

        return new byte[size];
    }

    /// <summary>
    /// Return a byte array to the pool (usually used in conjunction with CheckOut)
    /// </summary>
    /// <param name="buffer">the byte array to return</param>
    internal void CheckIn(byte[] buffer)
    {
        if (_Buffers.Any(item => item.Length == buffer.Length))
        {
            return;
        }

        Array.Clear(buffer, 0, buffer.Length);

        _Buffers.Add(buffer);
    }

    /// <summary>
    /// Remove all entries in the pool
    /// </summary>
    internal void Clear()
    {
        _Buffers.Clear();
    }
}
