using KinshipCalculator.Transfer.Container;
using KinshipCalculator.Transfer.Fountain;
using Xunit;

namespace KinshipCalculator.Transfer.Tests;

public class TransferTests
{
    [Fact]
    public void EndToEnd_WithFrameLoss()
    {
        var payload = TestData.Payload(50_000);
        var packed = ContainerCodec.Pack("family.json", "application/json", payload);

        var sender = new TransferSender(packed.Container, 1024, 12345);
        var receiver = new TransferReceiver();
        var rnd = new SplitMix32(12345);

        uint seq = 0;
        uint ceiling = (uint)(sender.BlockCount * 60 + 5000);
        while (!receiver.IsComplete && seq < ceiling)
        {
            if (rnd.Next() / 4294967296.0 >= 0.2)
                receiver.AddFrame(sender.EncodeFrame(seq));
            seq++;
        }

        Assert.True(receiver.IsComplete);
        Assert.NotNull(receiver.Container);

        var unpacked = ContainerCodec.Unpack(receiver.Container!);
        Assert.Equal("family.json", unpacked.Name);
        Assert.Equal(payload, unpacked.Bytes);
        Assert.True(ContainerCodec.Verify(unpacked));
    }

    [Fact]
    public void Receiver_LocksOntoNewStream()
    {
        // 用不可压缩随机数据，保证 BlockCount > 1（否则 sender1 第一帧即完成）。
        var payload = IncompressibleBytes(4096);
        var sender1 = new TransferSender(ContainerCodec.Pack("a.bin", "application/octet-stream", payload).Container, 512, 1);
        var sender2 = new TransferSender(ContainerCodec.Pack("b.bin", "application/octet-stream", payload).Container, 512, 2);
        Assert.True(sender1.BlockCount > 1);

        var receiver = new TransferReceiver();
        receiver.AddFrame(sender1.EncodeFrame(0));
        Assert.False(receiver.IsComplete);

        // 换到另一个会话 → 重置并重新锁定。
        uint seq = 0;
        while (!receiver.IsComplete && seq < 200)
        {
            receiver.AddFrame(sender2.EncodeFrame(seq));
            seq++;
        }

        Assert.True(receiver.IsComplete);
        Assert.Equal("b.bin", ContainerCodec.Unpack(receiver.Container!).Name);
    }

    private static byte[] IncompressibleBytes(int length)
    {
        var bytes = new byte[length];
        var rng = new Random(1234);
        rng.NextBytes(bytes);
        return bytes;
    }
}
