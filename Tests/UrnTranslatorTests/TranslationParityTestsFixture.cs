using System;
using System.IO;

namespace Tests.UrnTranslatorTests;

internal sealed class TranslationParityTestsFixture : IDisposable
{
    public string SourceFile { get; }

    public string TranslationFile { get; }

    public long Length { get; }

    private TranslationParityTestsFixture(
        string sourceFile,
        string translationFile,
        long length)
    {
        SourceFile = sourceFile;
        TranslationFile = translationFile;
        Length = length;
    }

    public static TranslationParityTestsFixture CreateEmpty()
    {
        byte[] source = new byte[1024];

        return Create(source);
    }

    public static TranslationParityTestsFixture CreateAllBitsSet()
    {
        byte[] source = new byte[1024];

        Array.Fill(source, (byte)255);

        return Create(source);
    }

    public static TranslationParityTestsFixture CreateSparse(
        int seed)
    {
        Random random = new(seed);

        byte[] source = new byte[50_000];

        for (int i = 0; i < source.Length; i++)
        {
            source[i] =
                random.NextDouble() < 0.95
                    ? (byte)0
                    : (byte)random.Next(256);
        }

        return Create(source);
    }

    public static TranslationParityTestsFixture CreateLarge(
        int seed,
        int sourceLength)
    {
        Random random = new(seed);

        byte[] source = new byte[sourceLength];

        random.NextBytes(source);

        return Create(source);
    }

    public TranslationParityTestsFixture(
        int seed)
    {
        Random random = new(seed);

        int sourceLength =
            random.Next(1, 100_000);

        byte[] source =
            new byte[sourceLength];

        random.NextBytes(source);

        Length = sourceLength * 8L * 4L;

        SourceFile =
            Path.GetTempFileName();

        TranslationFile =
            Path.GetTempFileName();

        File.WriteAllBytes(
            SourceFile,
            source);

        CreateTranslationFile(
            TranslationFile,
            sourceLength * 8,
            random);
    }

    private static TranslationParityTestsFixture Create(
        byte[] source)
    {
        long length = source.Length * 8L * 4L;

        string sourceFile =
            Path.GetTempFileName();

        string translationFile =
            Path.GetTempFileName();

        File.WriteAllBytes(
            sourceFile,
            source);

        Random random = new(12345);

        CreateTranslationFile(
            translationFile,
            source.Length * 8,
            random);

        return new TranslationParityTestsFixture(
            sourceFile,
            translationFile,
            length);
    }

    private static void CreateTranslationFile(
        string path,
        int entries,
        Random random)
    {
        using FileStream stream =
            new(path, FileMode.Create);

        using BinaryWriter writer =
            new(stream);

        for (int i = 0; i < entries; i++)
        {
            writer.Write(random.Next(entries));
        }
    }

    public static TranslationParityTestsFixture CreateBoundaryCase(int length)
    {
        byte[] source = new byte[length];

        // First set bit occurs at end byte
        source[length - 1] = 0b00000001;

        return Create(source);
    }

    public void Dispose()
    {
        try
        {
            File.Delete(SourceFile);
        }
        catch
        {
        }

        try
        {
            File.Delete(TranslationFile);
        }
        catch
        {
        }
    }
}