using PaleyEngine.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PaleyEngine;

internal class VectorStack
{
    private readonly PaleyDatabase _database;

    private readonly Stack<BitVector> _vectorStack;

    internal VectorStack(PaleyDatabase database)
    {
        _database = database;
        _vectorStack = new Stack<BitVector>();
    }

    internal void Push(LogicType logicType)
    {
        var vector = new BitVector
            (
                _database.DataFile,
                _database.FileLength,
                _database.FileSplit,
                _database.BitmapLength
            );

        if(logicType == LogicType.And || logicType == LogicType.Not)
        {
            var value = _database.GetUniverse();
            vector.GetData(value);
        }
        else
        {
            throw new PaleyEngineException("Unsupported logic type: " + logicType);
        }

        _vectorStack.Push(vector);
    }

    internal long GetTopTally()
    {
        Debug.Assert(_vectorStack.Count > 0);

        return _vectorStack.Peek().Tally();
    }
}
