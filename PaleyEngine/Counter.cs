using PaleyEngine.Database;
using System.Diagnostics;
using System.Xml.Linq;
using PaleyEngine.Translations;

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

        //TODO: move this elsewhere - this is just for testing
        DumpPerms(vectorStack);

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

    private void DumpPerms(VectorStack vectorStack, string? keycode = null, string keycode2 = null)
    {
        Debug.Assert(vectorStack.Size == 1);

        // temp for now - will need a chosen path later
        var path = Path.Combine(Path.GetDirectoryName(_database.DataFile), "PermDump.txt");

        //TODO - write to CSV 

        using (var writer = new StreamWriter(path))
        {
            var vector = vectorStack.GetTopVector();

            using (TemporaryFile tempSource = new(),
                                 tempObject = new())
            {
                vector.DumpVector(tempSource.Pathname);

                // Translate Temp 2 Perm
                long length = new FileInfo(_database.PermTemp).Length;

                //using (var translator = new UrnTranslator())
                //{
                //    translator.BinaryTempToPerm(tempSource.Pathname, tempObject.Pathname, _database.TempPerm, length);
                //}
                UrnTranslator.BinaryTempToPerm(tempSource.Pathname, tempObject.Pathname, _database.TempPerm, length);
                //LegacyUrnTranslator.BinaryTempToPerm(tempSource.Pathname, tempObject.Pathname, _database.TempPerm, length);


                var exported = CreateUrnsFromBitFile(writer, tempObject.Pathname, keycode, keycode2, vector.Tally());
            }
        }
    }

    private int CreateUrnsFromBitFile(StreamWriter writer, string dumpedPath, string keycode1, string keycode2, long expected)
    {
        //Debug.Assert(HasCount());
        int currentTally = 0;

        using (var stream = new FileStream(dumpedPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            using (var reader = new BinaryReader(stream))
            {
                bool hasKeycode1 = !string.IsNullOrWhiteSpace(keycode1);
                bool hasKeycode2 = !string.IsNullOrWhiteSpace(keycode2);

                //OnUrnExportProgress.Raise(this, new ProgressEventArgs(0, 100));

                int urn = 0;
                int oldPercent = 0;

                const int TOREAD = 2048;

                for (; ; )
                {
                    byte[] buffer = reader.ReadBytes(TOREAD);

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        byte b = buffer[i];
                        int temp = 0;

                        while (b > 0)
                        {
                            if ((b & 1) > 0)
                            {
                                long write = urn + temp;

                                writer.Write(write.ToString());

                                if (hasKeycode1)
                                {
                                    writer.Write(keycode1);

                                    if (hasKeycode2)
                                    {
                                        writer.Write(keycode2);
                                    }
                                }

                                writer.WriteLine();
                                ++currentTally;
                            }

                            b >>= 1;
                            ++temp;
                        }

                        urn += 8;
                    }

                    // progress
                    //if (currentTally > 0)
                    //{
                    //    int percent = (int)(((double)currentTally / (double)expected) * 100d);
                    //    if (percent != oldPercent)
                    //    {
                    //        OnUrnExportProgress.Raise(this, new ProgressEventArgs(percent, 100));
                    //        oldPercent = percent;
                    //    }
                    //}

                    if (buffer.Length < TOREAD)
                        break;
                }

                if (currentTally != expected)
                {
                    throw new PaleyEngineException($"WARNING - Export didn't export all URNs. Exported {currentTally:N0} from {expected:N0}");
                    //OnStatusText.Raise(this, new TextInfoEventArgs(string.Format("WARNING - Export didn't export all URNs. Exported {0:N0} from {1:N0}", currentTally, expected)));
                }

                //OnUrnExportProgress.Raise(this, new ProgressEventArgs(100, 100));
            }
        }

        return currentTally;
    }

    /// <summary>
    /// Is there a current count active
    /// </summary>
    /// <returns>true if a count is currently active</returns>
    //public bool HasCount()
    //{
    //    if (_VectorStack == null || _VectorStack.Size < 1)
    //        return false;

    //    return true;
    //}
}
