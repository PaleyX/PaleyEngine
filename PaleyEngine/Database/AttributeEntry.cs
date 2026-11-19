using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace PaleyEngine.Database;

internal class AttributeEntry
{
    private readonly Dictionary<string, ValueEntry> _Values = new(StringComparer.OrdinalIgnoreCase);

    internal AttributeEntry(XElement element)
    {
        Id = Convert.ToInt32(element.Attribute("id")!.Value);
        Description = element.Attribute("name")!.Value;
        Group = element.Attribute("group") == null ? null : element.Attribute("group")!.Value;
        IsSystem = Convert.ToBoolean(element.Attribute("system")!.Value);
    }

    internal int Id { get; }
    internal string Description { get; }
    internal string? Group { get; }
    internal bool IsSystem { get; }


    internal void AddValue(ValueEntry value)
    {
        _Values.Add(value.Value, value);
    }

    internal ValueEntry GetValue(string value)
    {
        ValueEntry result;

        if (_Values.TryGetValue(value, out result))
            return result;

        throw new PaleyEngineException("Cannot find value: " + value);
    }

    internal IEnumerable<ValueEntry> Values
    {
        get
        {
            foreach (var value in _Values)
                yield return value.Value;
        }
    }
}
