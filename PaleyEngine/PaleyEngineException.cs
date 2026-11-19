using System;
using System.Collections.Generic;
using System.Text;

namespace PaleyEngine;

public class PaleyEngineException : Exception
{
    public PaleyEngineException(string message) : base(message)
    {
    }

    public PaleyEngineException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public PaleyEngineException() : base()
    {
    }
}
