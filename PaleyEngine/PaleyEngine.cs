using PaleyEngine.Database;

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
    }
}
