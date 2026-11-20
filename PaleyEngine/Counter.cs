using PaleyEngine.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace PaleyEngine;

internal class Counter
{
    private readonly PaleyDatabase _database;

    internal Counter(PaleyDatabase database)
    {
        _database = database;
    }

    internal long UniverseTally { get; set; }

    internal void Count(XDocument document)
    {
        if (_database is null)
        {
            throw new PaleyEngineException("Database is not opened. Please call OpenDatabase() first.");
        }

        // Validate the XML document against the schema
        Tools.ValidateXml(document, "PaleyEngine.Xml.CountSchema.xsd");


        var countLevel = document.Root?.Attribute("lev")!.Value;

        // sections
        var databaseRoot = document.Root?.Element("database");
        var selectionRoot = document.Root?.Element("selection");
        var splitsRoot = document.Root?.Element("splits");

         var selectionTally = CountSelection(selectionRoot);

        Console.WriteLine($"Counting completed successfully. Selection tally: {selectionTally}");
    }

    private long CountSelection(XElement selectionRoot)
    {
        var vectorStack = new VectorStack(_database);

        // initial And
        vectorStack.Push(LogicType.And);

        UniverseTally = vectorStack.GetTopTally();

        // return universe for now
        return UniverseTally;
    }
}
