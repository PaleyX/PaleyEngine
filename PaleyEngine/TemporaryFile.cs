namespace PaleyEngine;

internal class TemporaryFile : IDisposable
{
    internal TemporaryFile()
    {
        string path = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.tmp");

        // create it
        using FileStream _ = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        Pathname = path;
    }

    internal string Pathname { get; }

    public void Dispose()
    {
        if (File.Exists(Pathname))
        {
            File.Delete(Pathname);
        }
    }
}
