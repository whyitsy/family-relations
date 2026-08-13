using KinshipCalculator.Transfer.Fountain;
using KinshipCalculator.Transfer.Protocol;

namespace KinshipCalculator.Transfer;

/// <summary>发送端：把容器编码为无穷尽的 QR 帧序列（22 字节头 + 喷泉码块）。</summary>
public sealed class TransferSender
{
    private readonly FountainEncoder _encoder;
    private readonly uint _payloadFnv;
    private readonly uint _totalLen;
    private readonly ushort _sessionId;

    public int BlockCount => _encoder.BlockCount;
    public int BlockLength => _encoder.BlockLength;
    public uint PayloadFnv => _payloadFnv;

    public TransferSender(byte[] container, int blockLen, ushort sessionId)
    {
        if (blockLen <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockLen));
        if (container.Length == 0)
            throw new ArgumentException("载荷为空", nameof(container));

        _encoder = new FountainEncoder(container, blockLen, sessionId);
        if (_encoder.BlockCount > FrameCapacity.MaxSourceBlocks)
            throw new ArgumentException($"块数 {_encoder.BlockCount} 超过线格式上限 {FrameCapacity.MaxSourceBlocks}");

        _payloadFnv = FrameCodec.Fnv1a(container);
        _totalLen = (uint)container.Length;
        _sessionId = sessionId;
    }

    /// <summary>编码第 <paramref name="seq"/> 帧（自描述，接收端可无握手锁定）。</summary>
    public byte[] EncodeFrame(uint seq)
    {
        var header = new FrameHeader(
            SessionId: _sessionId,
            Seq: seq,
            K: (ushort)_encoder.BlockCount,
            BlockLen: (ushort)_encoder.BlockLength,
            TotalLen: _totalLen,
            PayloadFnv: _payloadFnv,
            Flags: 0);

        return FrameCodec.PackFrame(header, _encoder.Encode(seq));
    }
}
