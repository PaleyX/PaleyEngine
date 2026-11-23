using System.Xml.Linq;

namespace PaleyEngine.Database;

internal class ValueEntry
{
    internal ValueEntry(XElement element)
    {
        Value = element.Attribute("value")!.Value;
        Description = element.Attribute("description")!.Value;
        Tally = Convert.ToInt64(element.Attribute("tally")!.Value);
        Start = Convert.ToInt64(element.Attribute("start")!.Value);
        Number = Convert.ToInt64(element.Attribute("number")!.Value);
        Offset = Convert.ToInt64(element.Attribute("offset")!.Value);
    }

    internal string Value { get; }
    internal string Description { get; }
    internal long Tally { get; }
    internal long Start { get; }
    internal long Number { get; }
    internal long Offset { get; }
}
