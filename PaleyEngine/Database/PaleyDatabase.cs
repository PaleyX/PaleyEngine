using System.Diagnostics;
using System.Xml.Linq;

namespace PaleyEngine.Database;

internal class PaleyDatabase
{
    private readonly Dictionary<string, AttributeEntry> _attributes 
        = new(StringComparer.OrdinalIgnoreCase);

    private ValueEntry? _universe;

    internal PaleyDatabase(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        DatabasePath = path;

        LoadNewDatabase(path);

        // counts for properties display
        AttributeCount = _attributes.Count;
        ValuesCount = _attributes.Sum(a => a.Value.Values.Count());
        InfoCount = _attributes.Sum(a => a.Value.Values.Sum(v => v.Tally));
    }

    internal int FileLength { get; private set; }
    internal int BitmapLength { get; private set; }
    internal string BitmapVersion { get; private set; }
    public string DatabaseDescription { get; private set; }
    internal string DatabaseVersion { get; private set; }
    internal string DataFile { get; private set; }
    internal int FileSplit { get; private set; }
    internal int MaxPermUrn { get; private set; }
    internal int MaxTempUrn { get; private set; }
    internal string PermTemp { get; private set; }
    internal string TempPerm { get; private set; }
    internal string Indexer { get; private set; }

    internal long AttributeCount { get; private set; }
    internal long ValuesCount { get; private set; }
    internal long InfoCount { get; private set; }

    private string DatabasePath { get; }

    private void LoadNewDatabase(string path)
    {
        _attributes.Clear();

        try
        {
            LoadAttributes();
            LoadValues();
            GetDatabaseInfo();
        }
        catch(Exception ex)
        {
            throw new PaleyEngineException($"Failed to load the database from '{path}'.", ex);
        }
    }

    internal ValueEntry GetUniverse()
    {
        _universe ??= GetValue("UNIVERSE", "EVERYBODY");

        return _universe;
    }

    internal ValueEntry GetValue(string attributeName, string valueName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(attributeName);
        ArgumentNullException.ThrowIfNullOrEmpty(valueName);

        if (!_attributes.TryGetValue(attributeName, out var attribute))
        {
            throw new PaleyEngineException("Cannot find attribute: " + attributeName);
        }

        return attribute.GetValue(valueName);
    }

    private void LoadAttributes()
    {
        Debug.Assert(_attributes.Count == 0, "Attributes dictionary should be empty before loading attributes.");

        try
        {
            string pathname = GetPath("attributes.lki");

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
            string pathname = GetPath("values.lki");

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
                    throw new PaleyEngineException($"Cannot find AttributeId {value.attributeId} loading values");
                }

                attribute.Value.AddValue(value.value);
            }
        }
        catch (Exception e)
        {
            throw new PaleyEngineException("Cannot load values: " + e.Message, e);
        }
    }

    private void GetDatabaseInfo()
    {
        try
        {
            var document = XDocument.Load(DatabasePath);

            Tools.ValidateXml(document, "PaleyEngine.Xml.InfoSchema.xsd");

            BitmapLength = Tools.GetInfo<int>(document, "BitmapLength");
            BitmapVersion = Tools.GetInfo<string>(document, "BitmapVersion");
            DatabaseDescription = Tools.GetInfo<string>(document, "DatabaseDescription");
            DatabaseVersion = Tools.GetInfo<string>(document, "DatabaseVersion");
            DataFile = Tools.GetInfo<string>(document, "DataFile");
            FileLength = Tools.GetInfo<int>(document, "FileLength");
            FileSplit = Tools.GetInfo<int>(document, "FileSplit");
            MaxPermUrn = Tools.GetInfo<int>(document, "MaxPermUrn");
            MaxTempUrn = Tools.GetInfo<int>(document, "MaxTempUrn");
            PermTemp = Tools.GetInfo<string>(document, "PermTemp");
            TempPerm = Tools.GetInfo<string>(document, "TempPerm");

            ValidateInfo();
        }
        catch (Exception e)
        {
            throw new PaleyEngineException("Cannot get database info: " + e.Message);
        }
    }

    private string GetPath(string filename)
    {
        var path = Path.Combine(Path.GetDirectoryName(DatabasePath), filename);

        if(!File.Exists(path))
        {
            throw new PaleyEngineException($"File '{filename}' not found in the database directory.");
        }

        return path;
    }

    private void ValidateInfo()
    {
        string? directory = Path.GetDirectoryName(DatabasePath);
        if (directory == null)
        {
            throw new PaleyEngineException("Cannot get directory name from: " + DatabasePath);
        }

        DataFile = Tools.GetPath(directory, DataFile);
        PermTemp = Tools.GetPath(directory, PermTemp);
        TempPerm = Tools.GetPath(directory, TempPerm);

        if (FileSplit == 0)
            throw new PaleyEngineException("Bad file split in info");

        if (BitmapLength == 0)
            throw new PaleyEngineException("Bad bitmap length in info");

        if (FileLength == 0)
            throw new PaleyEngineException("Bad file length in info");
    }
}
