using System.Text;
using KinshipCalculator.Transfer.Container;
using Xunit;

namespace KinshipCalculator.Transfer.Tests;

public class ContainerTests
{
    [Fact]
    public void Container_RoundTripsArbitraryBytes()
    {
        var source = new byte[] { 0, 1, 2, 127, 128, 254, 255 };
        var packed = ContainerCodec.Pack("résumé.bin", "application/octet-stream", source);
        var recovered = ContainerCodec.Unpack(packed.Container);

        Assert.Equal(CompressionMode.None, packed.Compression);
        Assert.Equal("résumé.bin", recovered.Name);
        Assert.Equal("application/octet-stream", recovered.Type);
        Assert.Equal(source, recovered.Bytes);
        Assert.True(ContainerCodec.Verify(recovered));
    }

    [Fact]
    public void Sha256Verification_RejectsChangedBytes()
    {
        var packed = ContainerCodec.Pack("message.txt", "text/plain", Encoding.UTF8.GetBytes("hello"));
        var recovered = ContainerCodec.Unpack(packed.Container);
        recovered.Bytes[0] ^= 0xff;

        Assert.False(ContainerCodec.Verify(recovered));
    }

    [Fact]
    public void CompressibleFile_UsesGzipAndRecovers()
    {
        var source = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("decimen optical transfer\n", 4000)));
        var packed = ContainerCodec.Pack("notes.txt", "text/plain", source);
        var recovered = ContainerCodec.Unpack(packed.Container);

        Assert.Equal(CompressionMode.Gzip, packed.Compression);
        Assert.True(packed.TransmittedSize < source.Length / 10);
        Assert.Equal(source, recovered.Bytes);
        Assert.True(ContainerCodec.Verify(recovered));
    }

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Windows\System32\drivers\etc\hosts", "hosts")]
    [InlineData("évidence.pdf", "évidence.pdf")]
    [InlineData("..", "transfer.bin")]
    [InlineData(".", "transfer.bin")]
    [InlineData("   ", "transfer.bin")]
    public void SafeFileName_StripsPathAndFallsBack(string input, string expected)
    {
        Assert.Equal(expected, ContainerCodec.SafeFileName(input));
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("video/mp4", true)]
    [InlineData("application/zip", true)]
    [InlineData("application/epub+zip", true)]
    [InlineData("text/plain", false)]
    [InlineData("application/json", false)]
    [InlineData("image/svg+xml", false)]
    [InlineData("image/bmp", false)]
    [InlineData("audio/wav", false)]
    [InlineData("", false)]
    public void IsPrecompressedType_MatchesReference(string type, bool expected)
    {
        Assert.Equal(expected, ContainerCodec.IsPrecompressedType(type));
    }
}
