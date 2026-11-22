using PaleyEngine.Database;
using System;
using System.Diagnostics;
using System.IO;

namespace PaleyEngine.Translations;

internal static class LegacyUrnTranslator 
{
    internal static byte[] BinaryPermToTemp(string sourcePath, string transPath, long length)
    {
        return Translate(sourcePath, transPath, length);
        //Dump(buffer, objectPath);
    }

    internal static void BinaryTempToPerm(string sourcePath, string objectPath, string transPath, long length)
    {
        byte[] buffer = Translate(sourcePath, transPath, length);
        Dump(buffer, objectPath);
        TranslationPool.Instance.CheckIn(buffer);
    }

    private static byte[] Translate(string sourcePath, string transPath, long length)
    {
        const int DB32_BYTES = 10000;

        // The object length we require is the max length of the 
        // object file divided by length of UInt32
        // then by number of bits in a byte, 1k is added for the hell of it
        // the max length of the object file is the length of corresponding
        // translation file so for Temp2Perm it would be the Perm2Temp length

        // BUGFIX: 06-01-2014 
        long bufferLength = (long)Math.Ceiling(((double)length / 4d) / 8d);
        //long bufferLength = ((length / 4) / 8); // +1024

        while ((bufferLength % 8) > 0) // do we need this now? Yes we do - because of C++ optimisations - we can use it with Int32 and Int64 sections (see OrBuffer)
            ++bufferLength;
        // 16-06-2005 - removed the 1024 addition and just increased 
        //              the bufflen until it was on an 8 boundary
        //              adding 1024 meant that with a small FileLen 
        //              the buffer could be bigger than the FileLen
        //				which caused an array out of bounds error in
        //              BitVector.OrFile

        // allocate memory for buffers
        //var objectBuffer = new byte[bufferLength];
        var objectBuffer = TranslationPool.Instance.CheckOut(bufferLength);

        var translationBuffer = new Int32[DB32_BYTES * 8];

        using (FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
                          transStream = new(transPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int overflows = 0;

            using (var sourceReader = new BinaryReader(sourceStream))
            //transReader = new BinaryReader(transStream))
            {
                long seekTo = 0;

                // read through the source file, for each set bit seek 
                // on the translation file and output the new bit file
                for (; ; )
                {
                    byte[] sourceBuffer = sourceReader.ReadBytes(DB32_BYTES);
                    if (sourceBuffer.Length <= 0)
                        break;

                    long sourcePointer = 0;
                    int elements = sourceBuffer.Length;

                    // skip leading zeros
                    for (; ; )
                    {
                        if (sourceBuffer[sourcePointer] != 0)
                            break;

                        ++seekTo;
                        --elements;

                        if (elements == 0)
                            break;

                        ++sourcePointer;
                    }

                    if (elements == 0)
                    {
                        if (sourceBuffer.Length < DB32_BYTES)
                            break;

                        continue;
                    }

                    if (seekTo > 0)
                    {
                        //Debug.Assert(System.Runtime.InteropServices.Marshal.SizeOf(transBuff[0]) == 4);

                        // 8 bits in a byte
                        // 4 bytes in a int32
                        //transReader.BaseStream.Seek(seekTo * 8 * 4, SeekOrigin.Current);
                        transStream.Seek(seekTo * 8 * 4, SeekOrigin.Current);
                    }

                    seekTo = 0;

                    // START NEW WAY
                    long todo = elements * 8;
                    Debug.Assert(BitConverter.IsLittleEndian);
                    //byte[] buffer = transReader.ReadBytes((int)todo * 4);
                    var buffer = new byte[todo * 4];
                    transStream.Read(buffer, 0, buffer.Length);
                    Buffer.BlockCopy(buffer, 0, translationBuffer, 0, buffer.Length);
                    int read = buffer.Length / 4;
                    // END NEW WAY

                    // START OLD WAY
                    //int read = 0;
                    //try
                    //{
                    //    long todo = elements * 8;
                    //
                    //    // The old way way
                    //    for (long i = 0; i < todo; i++)
                    //    {
                    //        translationBuffer[i] = transReader.ReadInt32();
                    //        ++read;
                    //    }
                    //}
                    //catch (EndOfStreamException)
                    //{
                    //    // only way to get EOF
                    //}
                    // END OLD WAY

                    //// START test results - Old way vs New Way
                    //Debug.Assert(testRead == read);
                    //for (int i = 0; i < read; ++i)
                    //    Debug.Assert(testBuffer[i] == translationBuffer[i]);
                    //// END test results

                    // How many elements to process?
                    elements = Math.Min(read, elements);
                    //elements = (elements > read) ? read : elements;

                    // for each bit in each byte
                    for (long i = 0; i < elements; i++)
                    {
                        Debug.Assert(sourcePointer < sourceBuffer.Length);
                        byte c = sourceBuffer[sourcePointer++];

                        if (c > 0) // '=' is ok
                        {
                            long temp = i * 8;

                            for (int j = 0; j <= 7; j++)
                            {
                                if ((c & (1 << j)) > 0)
                                {
                                    Int32 transUrn = translationBuffer[temp + j];
                                    if (transUrn < 0)
                                        throw new PaleyEngineException("Translation buffer error");

                                    long byteOffset = (transUrn / 8);
                                    int bitOffset = (byte)(transUrn % 8);

                                    // get the byte
                                    if (byteOffset < objectBuffer.Length)
                                        objectBuffer[byteOffset] |= (byte)(1 << bitOffset);
                                    else
                                        ++overflows;
                                }
                            }
                        }
                    }

                    // time to go?
                    if (sourceBuffer.Length < DB32_BYTES)
                        break;
                }
            }
        }

        return objectBuffer;
    }

    private static void Dump(byte[] buffer, string pathname)
    {
        // write the whole thing out, it will be compressed later
        // although there is an optimisation opportunity possible
        using var stream = new FileStream(pathname, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var writer = new BinaryWriter(stream);
        writer.Write(buffer);
    }

    //public void Dispose()
    //{
    //    // Nothing
    //}
}
