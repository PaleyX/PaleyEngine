using PaleyEngine;
using PaleyEngine.Database;
using PaleyEngine.Translations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit;

namespace Tests.UrnTranslatorTests;

public sealed class UrnTranslatorTests
{
    [Fact]
    public void SingleBitTranslatesCorrectly()
    {
        using TranslationFixture fixture = new(
            new byte[] { 0b00000001 },
            25, 1, 2, 3, 4, 5, 6, 7);

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                256);

        try
        {
            Assert.True(IsBitSet(result, 25));
            Assert.Equal(1, CountBits(result));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void MultipleBitsTranslateCorrectly()
    {
        using TranslationFixture fixture = new(
            new byte[] { 0b00000111 },
            10, 20, 30, 3, 4, 5, 6, 7);

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                512);

        try
        {
            Assert.True(IsBitSet(result, 10));
            Assert.True(IsBitSet(result, 20));
            Assert.True(IsBitSet(result, 30));

            Assert.Equal(3, CountBits(result));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void EmptySourceProducesEmptyBitmap()
    {
        using TranslationFixture fixture = new(
            new byte[] { 0, 0, 0, 0 },
            Enumerable.Range(0, 32).ToArray());

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                256);

        try
        {
            Assert.Equal(0, CountBits(result));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void FirstUrnCanBeSet()
    {
        using TranslationFixture fixture = new(
            new byte[] { 1 },
            0, 1, 2, 3, 4, 5, 6, 7);

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                256);

        try
        {
            Assert.True(IsBitSet(result, 0));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void HighestUrnCanBeSet()
    {
        const int highestUrn = 10000;

        using TranslationFixture fixture = new(
            new byte[] { 1 },
            highestUrn, 1, 2, 3, 4, 5, 6, 7);

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                (highestUrn + 1L) * 4);

        try
        {
            Assert.True(IsBitSet(result, highestUrn));
            Assert.Equal(1, CountBits(result));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Theory]
    [InlineData(9_999)]
    [InlineData(10_000)]
    [InlineData(10_001)]
    [InlineData(19_999)]
    [InlineData(20_000)]
    [InlineData(20_001)]
    public void BitAcrossBlockBoundary(int byteOffset)
    {
        byte[] source = new byte[byteOffset + 1];

        source[byteOffset] = 1;

        int translationCount =
            source.Length * 8;

        using TranslationFixture fixture = new(
            source,
            Enumerable.Range(
                0,
                translationCount).ToArray());

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                translationCount * 4L);

        try
        {
            Assert.Equal(1, CountBits(result));
            Assert.True(IsBitSet(result, byteOffset * 8));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void HandlesSeveralEmptyBlocks()
    {
        byte[] source = new byte[50_000];

        source[49_999] = 1;

        int translationCount =
            source.Length * 8;

        using TranslationFixture fixture = new(
            source,
            Enumerable.Range(
                0,
                translationCount).ToArray());

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                translationCount * 4L);

        try
        {
            Assert.Equal(1, CountBits(result));
            Assert.True(IsBitSet(result, 49_999 * 8));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void NegativeTranslationThrows()
    {
        using TranslationFixture fixture = new(
            new byte[] { 1 },
            -1, 1, 2, 3, 4, 5, 6, 7);

        Assert.Throws<PaleyEngineException>(
            () => UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                256));
    }

    [Fact]
    public void IdentityTranslationPreservesBitCount()
    {
        Random random = new(12345);

        byte[] source = new byte[5000];

        random.NextBytes(source);

        using TranslationFixture fixture = new(
            source,
            Enumerable.Range(
                0,
                source.Length * 8).ToArray());

        byte[] result =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                source.Length * 8L * 4L);

        try
        {
            Assert.Equal(
                CountBits(source),
                CountBits(result));
        }
        finally
        {
            TranslationPool.Instance.CheckIn(result);
        }
    }

    [Fact]
    public void EveryBytePatternTranslatesCorrectly()
    {
        for (int pattern = 0; pattern < 256; pattern++)
        {
            byte sourceByte = (byte)pattern;

            using TranslationFixture fixture = new(
                new[] { sourceByte },
                Enumerable.Range(0, 8).ToArray());

            byte[] result =
                UrnTranslator.BinaryPermToTemp(
                    fixture.SourceFile,
                    fixture.TranslationFile,
                    256);

            try
            {
                Assert.Equal(
                    BitOperations.PopCount(sourceByte),
                    CountBits(result));
            }
            finally
            {
                TranslationPool.Instance.CheckIn(result);
            }
        }
    }

    [Fact]
    public void RandomisedStressTest()
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            Random random = new(seed);

            byte[] source =
                new byte[random.Next(
                    1,
                    50_000)];

            random.NextBytes(source);

            using TranslationFixture fixture = new(
                source,
                Enumerable.Range(
                    0,
                    source.Length * 8).ToArray());

            byte[] result =
                UrnTranslator.BinaryPermToTemp(
                    fixture.SourceFile,
                    fixture.TranslationFile,
                    source.Length * 8L * 4L);

            try
            {
                Assert.NotNull(result);

                Assert.Equal(
                    CountBits(source),
                    CountBits(result));
            }
            finally
            {
                TranslationPool.Instance.CheckIn(result);
            }
        }
    }

    private static bool IsBitSet(
        byte[] buffer,
        int bit)
    {
        int byteOffset = bit / 8;
        int bitOffset = bit % 8;

        return
            (buffer[byteOffset] &
             (1 << bitOffset)) != 0;
    }

    private static int CountBits(
        byte[] buffer)
    {
        int count = 0;

        foreach (byte value in buffer)
        {
            count +=
                BitOperations.PopCount(
                    value);
        }

        return count;
    }

    private sealed class TranslationFixture : IDisposable
    {
        public string SourceFile { get; }

        public string TranslationFile { get; }

        public TranslationFixture(
            byte[] source,
            params int[] translations)
        {
            SourceFile =
                Path.GetTempFileName();

            TranslationFile =
                Path.GetTempFileName();

            File.WriteAllBytes(
                SourceFile,
                source);

            using FileStream stream = new(
                TranslationFile,
                FileMode.Create,
                FileAccess.Write);

            using BinaryWriter writer =
                new(stream);

            foreach (int translation in translations)
            {
                writer.Write(translation);
            }
        }

        public void Dispose()
        {
            if (File.Exists(SourceFile))
            {
                File.Delete(SourceFile);
            }

            if (File.Exists(TranslationFile))
            {
                File.Delete(TranslationFile);
            }
        }
    }
}