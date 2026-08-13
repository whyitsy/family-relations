namespace KinshipCalculator.Transfer.Protocol;

/// <summary>22 字节帧头（线格式 v3，全部小端）。</summary>
public readonly record struct FrameHeader(
    ushort SessionId,
    uint Seq,
    ushort K,
    ushort BlockLen,
    uint TotalLen,
    uint PayloadFnv,
    byte Flags);
