using PaleyEngine.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
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
    internal long SelectionTally { get; set; }

    internal long Count(XDocument document)
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

        SelectionTally = CountSelection(selectionRoot);

        return SelectionTally;
    }

    private long CountSelection(XElement selectionRoot)
    {
        var vectorStack = new VectorStack(_database);

        // initial And
        vectorStack.Push(LogicType.And);

        UniverseTally = vectorStack.GetTopTally();

        var elements = selectionRoot.Elements();

        if (elements.Any())
        {
            // must have universe
            Debug.Assert(vectorStack.Size == 1);

            ParseAndCount(elements, LogicType.And, vectorStack);
        }

        Debug.Assert(vectorStack.Size == 1);

        //TODO: count level

        //TODO: not required if no selection logic or level
        return vectorStack.GetTopTally();
    }

    private void ParseAndCount(IEnumerable<XElement> elements, LogicType logicType, VectorStack vectorStack)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(vectorStack);

        foreach (var element in elements)
        {
            // TODO: check for disabled attribute
            //if (IsDisabled(node))
            //    continue;

            switch(element.Name.LocalName)
            {
                case "attr":
                    {
                        vectorStack.Push(LogicType.Or);
                        ProcessAttributeXml(element, vectorStack);
                        vectorStack.Pop(logicType);
                    }
                    break;
            }
        }
    }

    internal void ProcessAttributeXml(XElement attributeElement, VectorStack vectorStack)
    {
        ArgumentNullException.ThrowIfNull(attributeElement);
        ArgumentNullException.ThrowIfNull(vectorStack);

        Debug.Assert(attributeElement.Name.LocalName == "attr");
        Debug.Assert(vectorStack.Size > 0);
        Debug.Assert(_database != null);

        var id = attributeElement.Attribute("id")!.Value;
        var op = attributeElement.Attribute("op")!.Value;
        var lv = attributeElement.Attribute("lev")!.Value;

        if (op == "=")
        {
            ProcessAttributeXmlSimple(attributeElement.Elements(), id, vectorStack);
        }
        else
        {
            throw new NotImplementedException($"Unsupported operator '{op}' in ProcessAttributeXML");
        }
    }

    private void ProcessAttributeXmlSimple(IEnumerable<XElement> elements, string attribute, VectorStack vectorStack)
    {
        Debug.Assert(elements != null);
        Debug.Assert(vectorStack != null);

        foreach (var element in elements)
        {
            var value = _database.GetValue(attribute, element.Value);

            vectorStack.OrValue(value);
        }
    }
}
