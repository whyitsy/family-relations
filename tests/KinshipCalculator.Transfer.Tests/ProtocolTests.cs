using KinshipCalculator.Transfer.Protocol;
using Xunit;

namespace KinshipCalculator.Transfer.Tests;

public class ProtocolTests
{
    [Fact]
    public void PackFrame_IsByteForByteCanonical()
    {
        var frame = FrameCodec.PackFrame(
            new FrameHeader(0xbeef, 0x01020304, 0x0111, 6, 0x00fedcba, 0x89abcdef, 0),
            new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.Equal(
            new byte[]
            {
                0xd1, 0xc3, 0x03, 0x00, 0xef, 0xbe, 0x04, 0x03, 0x02, 0x01, 0x11, 0x01, 0x06, 0x00,
                0xba, 0xdc, 0xfe, 0x00, 0xef, 0xcd, 0xab, 0x89, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            },
            frame);

        var header = FrameCodec.ParseFrame(frame);
        Assert.NotNull(header);
        Assert.Equal(0xbeef, header.Value.SessionId);
        Assert.Equal(0x01020304u, header.Value.Seq);
        Assert.Equal(0x0111, header.Value.K);
        Assert.Equal(6, header.Value.BlockLen);
        Assert.Equal(0x00fedcbau, header.Value.TotalLen);
        Assert.Equal(0x89abcdefu, header.Value.PayloadFnv);
        Assert.Equal(0, header.Value.Flags);
    }

    private static byte[] GoodFrame()
        => FrameCodec.PackFrame(new FrameHeader(1, 2, 3, 4, 10, 0, 0), new byte[] { 9, 9, 9, 9 });

    [Theory]
    [InlineData(0, 0xd2, FrameVerdictKind.Foreign)]              // magic0 错
    [InlineData(1, 0x42, FrameVerdictKind.Foreign)]              // magic1 任意其它值
    [InlineData(1, 0x0c, FrameVerdictKind.OlderSender)]          // v1 发送端
    [InlineData(1, 0x0d, FrameVerdictKind.OlderSender)]          // v2 发送端
    [InlineData(2, 0x04, FrameVerdictKind.NewerSender)]          // 更新版本
    [InlineData(2, 0x02, FrameVerdictKind.OlderSender)]          // 更旧版本
    [InlineData(2, 0x00, FrameVerdictKind.Malformed)]            // 版本 0
    [InlineData(3, 0x01, FrameVerdictKind.UnsupportedFlags)]     // 未知关键标志
    [InlineData(3, 0x10, FrameVerdictKind.Ok)]                   // 未知可忽略标志
    public void ClassifyFrame_Vectors(int offset, byte value, FrameVerdictKind expected)
    {
        var frame = GoodFrame();
        frame[offset] = value;
        Assert.Equal(expected, FrameCodec.ClassifyFrame(frame).Kind);
    }

    [Fact]
    public void ParseFrame_RejectsForeignAndMalformed()
    {
        Assert.NotNull(FrameCodec.ParseFrame(GoodFrame()));

        var wrongMagic = GoodFrame();
        wrongMagic[0] = 0xd2;
        Assert.Null(FrameCodec.ParseFrame(wrongMagic));

        Assert.Null(FrameCodec.ParseFrame(GoodFrame().AsSpan(0, FrameCodec.HeaderLength).ToArray()));
        Assert.Null(FrameCodec.ParseFrame(GoodFrame().AsSpan(0, GoodFrame().Length - 1).ToArray()));

        var zeroK = GoodFrame();
        zeroK[10] = 0;
        zeroK[11] = 0;
        Assert.Null(FrameCodec.ParseFrame(zeroK));
    }

    [Fact]
    public void StreamIdentity_ExcludesSeqAndIgnoresIgnorableFlags()
    {
        var baseHeader = new FrameHeader(7, 0, 100, 2933, 293300, 0xdeadbeef, 0);
        var identity = FrameCodec.StreamIdentity(baseHeader);
        Assert.Equal(identity, FrameCodec.StreamIdentity(baseHeader with { Seq = 9999 }));
        Assert.Equal(identity, FrameCodec.StreamIdentity(baseHeader with { Flags = 0x10 }));

        Assert.NotEqual(identity, FrameCodec.StreamIdentity(baseHeader with { SessionId = 8 }));
        Assert.NotEqual(identity, FrameCodec.StreamIdentity(baseHeader with { K = 101 }));
        Assert.NotEqual(identity, FrameCodec.StreamIdentity(baseHeader with { PayloadFnv = 1 }));
        Assert.NotEqual(identity, FrameCodec.StreamIdentity(baseHeader with { Flags = 0x01 }));
    }

    [Fact]
    public void Fnv1a_EmptyIsOffsetBasis()
    {
        // FNV-1a 空输入的哈希即 offset basis。
        Assert.Equal(0x811c9dc5u, FrameCodec.Fnv1a(Array.Empty<byte>()));
    }
}
