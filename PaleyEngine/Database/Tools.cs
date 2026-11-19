using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace PaleyEngine.Database;

internal static class Tools
{
    internal static void ValidateXml(XDocument document, string schemaPath)
    {
        Assembly a = Assembly.GetExecutingAssembly();

        using Stream? stream = a.GetManifestResourceStream(schemaPath);
        if (stream is null)
        {
            throw new PaleyEngineException($"Cannot find schema resource: {schemaPath}");
        }

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add("", XmlReader.Create(stream));

        document.Validate(schemaSet, SchemaValidationEventHandler);
    }

    private static void SchemaValidationEventHandler(object? sender, ValidationEventArgs e)
    {
        throw new PaleyEngineException("Schema validation error: " + e.Message);
    }
}

