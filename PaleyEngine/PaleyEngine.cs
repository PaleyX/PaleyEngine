using PaleyEngine.Database;
using System.Xml.Linq;

namespace PaleyEngine
{
    public class PaleyEngine()
    {
        private PaleyDatabase? _database;

        public void OpenDatabase(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The database file '{path}' does not exist.");
            }

            _database = new PaleyDatabase(path);
        }

        public void Count(XDocument document)
        {
            if (_database is null)
            {
                throw new PaleyEngineException("Database is not opened. Please call OpenDatabase() first.");
            }

            var counter = new Counter(_database);
            counter.Count(document);
        }
    }
}
