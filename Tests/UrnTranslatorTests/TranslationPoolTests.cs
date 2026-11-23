using PaleyEngine.Translations;

namespace Tests.UrnTranslatorTests;

public sealed class TranslationPoolTests
{
    [Fact]
    public void Instance_ReturnsSingleton()
    {
        TranslationPool first =
            TranslationPool.Instance;

        TranslationPool second =
            TranslationPool.Instance;

        Assert.Same(first, second);
    }

    [Fact]
    public void CheckOut_ReturnsBufferOfRequestedSize()
    {
        byte[] buffer =
            TranslationPool.Instance.CheckOut(1024);

        Assert.Equal(1024, buffer.Length);
    }

    [Fact]
    public void CheckOut_CreatesNewBufferWhenPoolEmpty()
    {
        TranslationPool.Instance.Clear();

        byte[] first =
            TranslationPool.Instance.CheckOut(512);

        byte[] second =
            TranslationPool.Instance.CheckOut(512);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CheckIn_And_CheckOut_ReusesBuffer()
    {
        TranslationPool.Instance.Clear();

        byte[] original =
            TranslationPool.Instance.CheckOut(1024);

        TranslationPool.Instance.CheckIn(original);

        byte[] reused =
            TranslationPool.Instance.CheckOut(1024);

        Assert.Same(original, reused);
    }

    [Fact]
    public void CheckIn_ClearsBufferBeforeReuse()
    {
        TranslationPool.Instance.Clear();

        byte[] buffer =
            TranslationPool.Instance.CheckOut(128);

        Array.Fill(
            buffer,
            (byte)255);

        TranslationPool.Instance.CheckIn(buffer);

        byte[] reused =
            TranslationPool.Instance.CheckOut(128);

        Assert.All(
            reused,
            value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void CheckIn_DoesNotStoreDuplicateBufferLength()
    {
        TranslationPool.Instance.Clear();

        byte[] first =
            TranslationPool.Instance.CheckOut(256);

        byte[] second =
            TranslationPool.Instance.CheckOut(256);

        TranslationPool.Instance.CheckIn(first);
        TranslationPool.Instance.CheckIn(second);

        byte[] buffer1 =
            TranslationPool.Instance.CheckOut(256);

        byte[] buffer2 =
            TranslationPool.Instance.CheckOut(256);

        Assert.Same(first, buffer1);
        Assert.NotSame(first, buffer2);
        Assert.NotSame(second, buffer2);
    }

    [Fact]
    public void BuffersOfDifferentSizesAreStoredSeparately()
    {
        TranslationPool.Instance.Clear();

        byte[] small =
            TranslationPool.Instance.CheckOut(128);

        byte[] large =
            TranslationPool.Instance.CheckOut(1024);

        TranslationPool.Instance.CheckIn(small);
        TranslationPool.Instance.CheckIn(large);

        byte[] smallResult =
            TranslationPool.Instance.CheckOut(128);

        byte[] largeResult =
            TranslationPool.Instance.CheckOut(1024);

        Assert.Same(small, smallResult);
        Assert.Same(large, largeResult);
    }

    [Fact]
    public void Clear_RemovesAllBuffers()
    {
        TranslationPool.Instance.Clear();

        byte[] buffer =
            TranslationPool.Instance.CheckOut(512);

        TranslationPool.Instance.CheckIn(buffer);

        TranslationPool.Instance.Clear();

        byte[] newBuffer =
            TranslationPool.Instance.CheckOut(512);

        Assert.NotSame(buffer, newBuffer);
    }

    [Fact]
    public void ReusedBufferRetainsRequestedLength()
    {
        TranslationPool.Instance.Clear();

        byte[] buffer =
            TranslationPool.Instance.CheckOut(4096);

        TranslationPool.Instance.CheckIn(buffer);

        byte[] reused =
            TranslationPool.Instance.CheckOut(4096);

        Assert.Equal(4096, reused.Length);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(8192)]
    public void CheckOut_ReturnsCorrectLengthForVariousSizes(
        long size)
    {
        byte[] buffer =
            TranslationPool.Instance.CheckOut(size);

        Assert.Equal(size, buffer.Length);
    }

    [Fact]
    public void MultipleCheckInCheckOutCyclesReuseBuffer()
    {
        TranslationPool.Instance.Clear();

        byte[] original =
            TranslationPool.Instance.CheckOut(2048);

        for (int i = 0; i < 10; i++)
        {
            TranslationPool.Instance.CheckIn(original);

            byte[] reused =
                TranslationPool.Instance.CheckOut(2048);

            Assert.Same(original, reused);

            original = reused;
        }
    }
}