namespace PaleyEngine.Translations;

/// <summary>
/// The buffer pool for the large arrays used to return the translated URNs
/// in reality there should only ever be one buffer at most in the pool
/// but by reusing it instead of creating and recreating huge byte arrays
/// it will take some pressure off the Large Object Heap and the GC
/// </summary>
internal sealed class TranslationPool
{
    private readonly List<byte[]> _buffers;

    private TranslationPool()
    {
        _buffers = [];
    }

    // Singleton - the only way to access the pool
    private static TranslationPool _Instance;
    internal static TranslationPool Instance
    {
        get
        {
            return _Instance ??= new TranslationPool();
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
        for (int i = 0; i < _buffers.Count; ++i)
        {
            if (_buffers[i].Length == size)
            {
                var buffer = _buffers[i];
                _buffers.RemoveAt(i);

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
        if (_buffers.Any(item => item.Length == buffer.Length))
        {
            return;
        }

        Array.Clear(buffer, 0, buffer.Length);

        _buffers.Add(buffer);
    }

    /// <summary>
    /// Remove all entries in the pool
    /// </summary>
    internal void Clear()
    {
        _buffers.Clear();
    }
}
