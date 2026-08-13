using KinshipCalculator.Transfer.Fountain;
using KinshipCalculator.Transfer.Protocol;

namespace KinshipCalculator.Transfer;

/// <summary>帧容量计算：载荷大小 → 块长 / 块数约束。</summary>
public static class FrameCapacity
{
    public const int MaxSourceBlocks = 0xFFFF;

    public static int BlockLength(int frameBytes) => frameBytes - FrameCodec.HeaderLength;

    public static int SourceBlockCount(int payloadBytes, int frameBytes)
    {
        int blockLen = BlockLength(frameBytes);
        return (payloadBytes + blockLen - 1) / blockLen;
    }

    public static bool FitsInOneStream(int payloadBytes, int frameBytes)
        => SourceBlockCount(payloadBytes, frameBytes) <= MaxSourceBlocks;
}
