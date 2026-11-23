using PaleyEngine.Translations;
using Tests.LegacyCode;

namespace Tests.UrnTranslatorTests;

public class UrnTranslatorParityTests
{
    public static IEnumerable<object[]> Seeds()
    {
        for (var i = 0; i < 100; i++)
        {
            yield return [i];
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RandomData_ShouldMatchLegacyImplementation(
        int seed)
    {
        using var fixture =
            new TranslationParityTestsFixture(seed);

        byte[] expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        byte[] actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }

    [Fact]
    public void EmptyBitmap_ShouldMatchLegacyImplementation()
    {
        using var fixture =
            TranslationParityTestsFixture.CreateEmpty();

        var expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        var actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }

    [Fact]
    public void AllBitsSet_ShouldMatchLegacyImplementation()
    {
        using var fixture =
            TranslationParityTestsFixture.CreateAllBitsSet();

        byte[] expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        byte[] actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }

    [Fact]
    public void LargeBitmap_ShouldMatchLegacyImplementation()
    {
        using var fixture =
            TranslationParityTestsFixture.CreateLarge(
                seed: 12345,
                sourceLength: 250_000);

        byte[] expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        byte[] actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }

    [Fact]
    public void SparseBitmap_ShouldMatchLegacyImplementation()
    {
        using var fixture =
            TranslationParityTestsFixture.CreateSparse(
                seed: 9999);

        byte[] expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        byte[] actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }

    [Fact]
    public void OptimisedImplementation_ShouldMatchLegacy_For1000RandomInputs()
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            using var fixture =
                new TranslationParityTestsFixture(seed);

            byte[] expected =
                LegacyUrnTranslator.BinaryPermToTemp(
                    fixture.SourceFile,
                    fixture.TranslationFile,
                    fixture.Length);

            byte[] actual =
                UrnTranslator.BinaryPermToTemp(
                    fixture.SourceFile,
                    fixture.TranslationFile,
                    fixture.Length);

            Assert.Equal(expected, actual);

            TranslationPool.Instance.CheckIn(expected);
            TranslationPool.Instance.CheckIn(actual);
        }
    }

    [Theory]
    [InlineData(9_999)]
    [InlineData(10_000)]
    [InlineData(10_001)]
    [InlineData(19_999)]
    [InlineData(20_000)]
    [InlineData(20_001)]
    [InlineData(29_999)]
    [InlineData(30_000)]
    [InlineData(30_001)]
    public void MatchesLegacyAcrossBlockBoundary(int length)
    {
        using TranslationParityTestsFixture fixture =
            TranslationParityTestsFixture.CreateBoundaryCase(length);

        byte[] expected =
            LegacyUrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        byte[] actual =
            UrnTranslator.BinaryPermToTemp(
                fixture.SourceFile,
                fixture.TranslationFile,
                fixture.Length);

        Assert.Equal(expected, actual);

        TranslationPool.Instance.CheckIn(expected);
        TranslationPool.Instance.CheckIn(actual);
    }
}

