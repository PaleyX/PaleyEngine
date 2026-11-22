using PaleyEngine.Database;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PaleyEngine.Translations;

internal static class UrnTranslator
{
    /// <summary>
    /// Number of source bytes processed in a single block.
    ///
    /// Each source byte requires 8 translation Int32 values.
    /// A block of 10,000 bytes therefore requires 80,000
    /// translation entries.
    /// </summary>
    private const int SourceBlockBytes = 10_000;

    internal static byte[] BinaryPermToTemp(
        string sourcePath,
        string translationPath,
        long length)
    {
        return Translate(
            sourcePath,
            translationPath,
            length);
    }

    internal static void BinaryTempToPerm(
        string sourcePath,
        string objectPath,
        string translationPath,
        long length)
    {
        byte[] buffer = Translate(
            sourcePath,
            translationPath,
            length);

        try
        {
            Dump(buffer, objectPath);
        }
        finally
        {
            TranslationPool.Instance.CheckIn(buffer);
        }
    }

    private static byte[] Translate(
        string sourcePath,
        string translationPath,
        long length)
    {
        //
        // One destination bit is required for each translated urn.
        // Translation files contain Int32 urn identifiers.
        //
        long objectLength =
            (long)Math.Ceiling(
                ((double)length / 4d) / 8d);

        //
        // Existing bitmap code assumes 8-byte alignment.
        //
        while ((objectLength % 8) != 0)
        {
            objectLength++;
        }

        byte[] objectBuffer =
            TranslationPool.Instance.CheckOut(
                objectLength);

        byte[] sourceBuffer =
            new byte[SourceBlockBytes];

        //
        // Eight translation entries for every source byte.
        //
        int[] translationBuffer =
            new int[SourceBlockBytes * 8];

        using FileStream sourceStream = new(
            sourcePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using FileStream translationStream = new(
            translationPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        //
        // Must be outside the loop.
        // The original implementation accumulated skipped
        // zero bytes across block boundaries.
        //
        long seekTo = 0;

        while (true)
        {
            int sourceRead =
                sourceStream.Read(
                    sourceBuffer,
                    0,
                    sourceBuffer.Length);

            if (sourceRead == 0)
            {
                break;
            }

            long sourcePointer = 0;
            int elements = sourceRead;

            //
            // Skip leading zero bytes.
            //
            while (true)
            {
                if (sourceBuffer[sourcePointer] != 0)
                {
                    break;
                }

                seekTo++;
                elements--;

                if (elements == 0)
                {
                    break;
                }

                sourcePointer++;
            }

            if (elements == 0)
            {
                if (sourceRead < SourceBlockBytes)
                {
                    break;
                }

                continue;
            }

            if (seekTo > 0)
            {
                translationStream.Seek(
                    seekTo * 8 * sizeof(int),
                    SeekOrigin.Current);
            }

            seekTo = 0;

            long todo = elements * 8;

            //
            // Read directly into the Int32 buffer.
            //
            Span<byte> translationBytes =
                MemoryMarshal.AsBytes(
                    translationBuffer.AsSpan());

            int requiredBytes =
                (int)(todo * sizeof(int));

            ReadUpTo(
                translationStream,
                translationBytes[..requiredBytes]);

            //
            // Preserve original behaviour exactly.
            //
            int read =
                requiredBytes / sizeof(int);

            elements =
                Math.Min(
                    read,
                    elements);

            //
            // Process each source byte.
            //
            for (long i = 0; i < elements; i++)
            {
                byte sourceByte =
                    sourceBuffer[sourcePointer++];

                if (sourceByte == 0)
                {
                    continue;
                }

                long translationOffset =
                    i * 8;

                //
                // Preserve original bit-processing logic.
                //
                for (int bit = 0; bit <= 7; bit++)
                {
                    if ((sourceByte & (1 << bit)) == 0)
                    {
                        continue;
                    }

                    int translatedUrn =
                        translationBuffer[
                            translationOffset + bit];

                    if (translatedUrn < 0)
                    {
                        throw new PaleyEngineException(
                            "Translation buffer error");
                    }

                    long byteOffset =
                        translatedUrn / 8;

                    int bitOffset =
                        translatedUrn % 8;

                    if (byteOffset < objectBuffer.Length)
                    {
                        objectBuffer[byteOffset] |=
                            (byte)(1 << bitOffset);
                    }
                }
            }

            if (sourceRead < SourceBlockBytes)
            {
                break;
            }
        }

        return objectBuffer;
    }

    /// <summary>
    /// Reads until the requested span is full or EOF is reached.
    /// The original implementation ignored short reads so this
    /// method preserves that behaviour.
    /// </summary>
    private static int ReadUpTo(
        Stream stream,
        Span<byte> buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read =
                stream.Read(
                    buffer[total..]);

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    /// <summary>
    /// Writes the translated bitmap to disk.
    /// </summary>
    private static void Dump(
        byte[] buffer,
        string pathname)
    {
        using FileStream stream = new(
            pathname,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        stream.Write(buffer);
    }
}