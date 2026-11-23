using PaleyEngine.Database;
using System.Diagnostics;

namespace PaleyEngine;

internal class VectorStack
{
    private readonly PaleyDatabase _database;

    private readonly Stack<BitVector> _stack;

    internal VectorStack(PaleyDatabase database)
    {
        _database = database;
        _stack = new Stack<BitVector>();
    }

    internal int Size => _stack.Count;

    internal void Push(LogicType logicType)
    {
        var vector = new BitVector
            (
                _database.DataFile,
                _database.FileLength,
                _database.FileSplit,
                _database.BitmapLength
            );

        if(logicType is LogicType.And or LogicType.Not)
        {
            var value = _database.GetUniverse();
            vector.GetData(value);
        }
        else
        {
            if(logicType != LogicType.Or)
            {
                throw new PaleyEngineException("Unsupported logic type: " + logicType);
            }
        }

        _stack.Push(vector);
    }

    internal void OrValue(ValueEntry value)
    {
        Debug.Assert(Size >= 1);
        _stack.Peek().GetData(value);
    }

    internal void Pop(LogicType logicType)
    {
        Debug.Assert(Size >= 2);

        var vector = _stack.Pop();

        switch (logicType)
        {
            case LogicType.Or:
                GetTopVector().OrVector(vector);
                break;
            case LogicType.And:
                GetTopVector().AndVector(vector);
                break;
            case LogicType.Not:
                GetTopVector().NotVector(vector);
                break;
            default:
                Debug.Assert(false);
                break;
        }
    }

    internal BitVector GetTopVector()
    {
        Debug.Assert(Size >= 1);
        return _stack.Peek();
    }

    internal long GetTopTally()
    {
        Debug.Assert(_stack.Count > 0);

        return _stack.Peek().Tally();
    }
}
