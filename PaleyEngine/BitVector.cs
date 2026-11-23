using PaleyEngine.Database;
using System.Diagnostics;

namespace PaleyEngine;

internal class BitVector
{
    private readonly string _dataPath;
    private readonly int _fileLength;
    private readonly int _fileSplit;
    private readonly int _bitmapLength;

    private readonly byte[] _vector;

    internal BitVector(string dataPathname, int fileLength, int fileSplit, int bitmapLength)
    {
        _dataPath = dataPathname;
        _fileLength = fileLength;
        _fileSplit = fileSplit;
        _bitmapLength = bitmapLength;

        _vector = new byte[_fileLength];
    }

    internal long Tally()
    {
        return BitVectorOperations.Tally(_vector);
    }

    internal void GetData(ValueEntry value)
    {
        GetData(value.Offset, value.Number);
    }

    internal void GetData(long offset, long number)
    {
        if(number <= 0)
        {
            return;
        }

        using var stream = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream);

        //TODO: the -1 works for the trading floor data but not the test data - 21.11.26
        reader.BaseStream.Seek(offset - 1, SeekOrigin.Begin);

        for (int i = 0; i < number; ++i)
        {
            // read header
            // valueId is always 1 so isn't really a valueid
            Int32 valueId = reader.ReadInt32();
            Debug.Assert(valueId == 1);
            Int32 length = reader.ReadInt32();
            Int32 urnStart = reader.ReadInt32();
            Int32 urnEnd = reader.ReadInt32();
            Int32 uncompressedLength = reader.ReadInt32();
            // end read header

            if (length > _bitmapLength)
            {
                throw new PaleyEngineException("Data length is greater than bitmap length");
            }

            // get the data
            byte[] bitmap = reader.ReadBytes(length);

            AddData(bitmap, urnStart, length, uncompressedLength);
        }
    }

    private void AddData(byte[] data, int urnStart, int length, int uncompressedLength)
    {
        Debug.Assert(data != null);

        var pIn = 0;

        int offset = (urnStart / 8);
        int pOut = offset;

        if (uncompressedLength <= length)
        {
            // Hack - without this in extremely rare cases there is an array out of bounds
            //        Happened with the first cut of Define 2.0 - I have never been able to 
            //        recreate a LK database which causes it
            if (pOut + length >= _vector.Length)
            {
                while (pOut + length >= _vector.Length)
                {
                    --length;
                }
            }
            Debug.Assert(length >= 0);
            Debug.Assert(pOut + length <= _vector.Length);

            for (int l = 0; l < length; l++)
            {
                Debug.Assert(pOut <= _vector.Length);
                Debug.Assert(pIn <= data.Length);

                _vector[pOut++] |= data[pIn++];
            }

            return;
        }

        int iSeq = 0;

        for (int l = 0; l < length;)
        {
            if (data[pIn] == 0xFF)
            {
                iSeq = data[++pIn];
                while (iSeq-- > 0) _vector[pOut++] = 0xFF;
                pIn++;
                l += 2;
            }
            else
                if (data[pIn] == 0)
                {
                    iSeq = data[++pIn];
                    pOut += iSeq;
                    pIn++;
                    l += 2;
                }
                else
                {
                    _vector[pOut++] |= data[pIn++];
                    l++;
                }
        }
    }

    internal void AndVector(BitVector vector)
    {
        Debug.Assert(vector != null);
        Debug.Assert(vector != this);
        Debug.Assert(_fileLength == vector.Length);

        BitVectorOperations.And(_vector, vector._vector);
    }

    internal void OrVector(BitVector vector)
    {
        Debug.Assert(vector != null);
        Debug.Assert(vector != this);
        Debug.Assert(_fileLength == vector.Length);

        BitVectorOperations.Or(_vector, vector._vector);
    }

    internal void NotVector(BitVector vector)
    {
        Debug.Assert(vector != null);
        Debug.Assert(vector != this);
        Debug.Assert(_fileLength == vector.Length);

        BitVectorOperations.Not(_vector, vector._vector);
    }

    internal int Length
    {
        get
        {
            Debug.Assert(_vector.Length == _fileLength);
            return _fileLength;
        }
    }

    internal void DumpVector(string pathname)
    {
        Debug.Assert(_vector != null);

        using var stream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);

        Debug.Assert(_vector.Length == _fileLength);
        writer.Write(_vector);
        writer.Flush();
    }
}
