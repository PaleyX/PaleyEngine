using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace PaleyEngine.Database;

internal class PaleyDatabase
{
    private Dictionary<string, AttributeEntry> _attributes;

    internal PaleyDatabase(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        DatabasePath = path;

        LoadNewDatabase(path);
    }

    private string DatabasePath { get; }

    private void LoadNewDatabase(string path)
    {
        try
        {
            LoadAttributes();
            LoadValues();
        }
        catch(Exception ex)
        {
            throw new PaleyEngineException($"Failed to load the database from '{path}'.", ex);
        }
    }

    private void LoadAttributes()
    {
        _attributes = new Dictionary<string, AttributeEntry>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string pathname = Path.Combine(Path.GetDirectoryName(DatabasePath), "attributes.lki");

            var document = XDocument.Load(pathname);

            Tools.ValidateXml(document, "PaleyEngine.Xml.AttributesSchema.xsd");

            var attributes = from a in document.Descendants("Attribute")
                             select new AttributeEntry(a);

            // Add the attributes to the dictionary
            attributes.ToList().ForEach(a => _attributes.Add(a.Description, a));
        }
        catch (Exception e)
        {
            throw new PaleyEngineException("Cannot load attributes: " + e.Message, e);
        }
    }

    private void LoadValues()
    {
        try
        {
            string pathname = Path.Combine(Path.GetDirectoryName(DatabasePath), "values.lki");

            var document = XDocument.Load(pathname);

            Tools.ValidateXml(document, "PaleyEngine.Xml.ValuesSchema.xsd");

            var values = from e in document.Descendants("Value")
                         select
                         (
                            value: new ValueEntry(e),
                            attributeId: Convert.ToInt32(e.Attribute("id")!.Value)
                         );


            foreach (var value in values)
            {
                var attribute = _attributes.FirstOrDefault(a => a.Value.Id == value.attributeId);
                if (attribute.Key == null || attribute.Value == null)
                {
                    throw new PaleyEngineException(string.Format("Cannot find AttributeId {0} loading values", value.attributeId));
                }

                attribute.Value.AddValue(value.value);
            }
        }
        catch (Exception e)
        {
            throw new PaleyEngineException("Cannot load values: " + e.Message, e);
        }
    }
}
