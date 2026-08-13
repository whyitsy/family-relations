using KinshipCalculator.Transfer.Fountain;
using KinshipCalculator.Transfer.Protocol;
using Xunit;

namespace KinshipCalculator.Transfer.Tests;

public class FountainTests
{
    // 与 decimen 参考实现逐字节一致：64 帧流经 FNV-1a 后的指纹。
    [Theory]
    [InlineData(1, 64, 1, 0xf6a115c5u)]
    [InlineData(23, 64, 7, 0x4a5d3eaau)]
    [InlineData(179, 2933, 4242, 0x54f78d05u)]
    [InlineData(716, 1445, 65535, 0x75b73b85u)]
    public void EncodedStream_IsBitExact(int k, int blockLen, int sessionId, uint expectedFnv)
    {
        var encoder = new FountainEncoder(TestData.Payload(k * blockLen - 7), blockLen, (ushort)sessionId);
        Assert.Equal(k, encoder.BlockCount);

        var stream = new byte[64 * blockLen];
        for (uint seq = 0; seq < 64; seq++)
        {
            var frame = encoder.Encode(seq);
            Assert.Equal(blockLen, frame.Length);
            Array.Copy(frame, 0, stream, (int)(seq * blockLen), blockLen);
        }

        Assert.Equal(expectedFnv, FrameCodec.Fnv1a(stream));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(179)]
    [InlineData(4096)]
    public void FrameComposition_IsSystematicThenRepair(int k)
    {
        Assert.Equal(2 * k, FountainCodec.CycleLength(k));

        foreach (var pos in new[] { 0, k / 2, k - 1 })
        {
            Assert.Equal(new[] { pos }, FountainCodec.FrameComposition(k, 9, (uint)pos));
            Assert.Equal(new[] { pos }, FountainCodec.FrameComposition(k, 9, (uint)(pos + 6 * FountainCodec.CycleLength(k))));
        }

        foreach (var seq in new[] { k, k + 1, 2 * k - 1 })
        {
            var idx = FountainCodec.FrameComposition(k, 9, (uint)seq);
            Assert.InRange(idx.Length, Math.Min(k, 4), Math.Min(k, 24));
            Assert.Equal(idx.Distinct().Count(), idx.Length);
            Assert.All(idx, b => Assert.InRange(b, 0, k - 1));
        }
    }

    [Theory]
    [InlineData(7, 2933)]
    [InlineData(2933, 2933)]
    [InlineData(50000, 1445)]
    [InlineData(512 * 1024, 2933)]
    [InlineData(2 * 1024 * 1024, 2933)]
    public void Payload_SurvivesFountain(int byteLength, int blockLen)
    {
        var payload = TestData.Payload(byteLength);
        var encoder = new FountainEncoder(payload, blockLen, 11);
        var decoder = new FountainDecoder(encoder.BlockCount, blockLen, 11, byteLength);

        uint seq = 0;
        uint ceiling = (uint)(encoder.BlockCount * 80 + 5000);
        while (!decoder.IsComplete && seq < ceiling)
        {
            decoder.AddFrame(seq, encoder.Encode(seq));
            seq++;
        }

        Assert.True(decoder.IsComplete);
        Assert.Equal(payload, decoder.Assemble());
    }

    [Fact]
    public void DroppingFrames_CostsTimeNeverCorrectness()
    {
        int byteLength = 512 * 1024, blockLen = 2933;
        var payload = TestData.Payload(byteLength);
        var encoder = new FountainEncoder(payload, blockLen, 23);
        var decoder = new FountainDecoder(encoder.BlockCount, blockLen, 23, byteLength);
        var rnd = new SplitMix32(23);

        uint seq = 0;
        uint ceiling = (uint)(encoder.BlockCount * 80 + 5000);
        while (!decoder.IsComplete && seq < ceiling)
        {
            if (rnd.Next() / 4294967296.0 >= 0.3)
                decoder.AddFrame(seq, encoder.Encode(seq));
            seq++;
        }

        Assert.True(decoder.IsComplete);
        Assert.Equal(payload, decoder.Assemble());
    }

    [Fact]
    public void SingleBlock_CompletesOnFirstFrame()
    {
        var payload = TestData.Payload(900);
        var encoder = new FountainEncoder(payload, 2933, 5);
        Assert.Equal(1, encoder.BlockCount);

        var decoder = new FountainDecoder(1, 2933, 5, 900);
        decoder.AddFrame(0, encoder.Encode(0));
        Assert.True(decoder.IsComplete);
        Assert.Equal(payload, decoder.Assemble());
    }

    [Fact]
    public void Receiver_JoiningMidCycle_CompletesWithoutHandshake()
    {
        int byteLength = 512 * 1024, blockLen = 2933;
        var payload = TestData.Payload(byteLength);
        var encoder = new FountainEncoder(payload, blockLen, 91);
        var decoder = new FountainDecoder(encoder.BlockCount, blockLen, 91, byteLength);

        uint start = (uint)(encoder.BlockCount / 3);
        uint seq = start;
        while (!decoder.IsComplete && seq < start + encoder.BlockCount * 4)
        {
            decoder.AddFrame(seq, encoder.Encode(seq));
            seq++;
        }

        Assert.True(decoder.IsComplete);
        Assert.Equal(payload, decoder.Assemble());
    }
}
