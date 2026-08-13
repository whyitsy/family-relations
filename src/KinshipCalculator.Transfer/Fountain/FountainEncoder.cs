namespace KinshipCalculator.Transfer.Fountain;

/// <summary>喷泉码编码器：把载荷切块，按 <see cref="FountainCodec.FrameComposition"/> 输出帧。</summary>
public sealed class FountainEncoder
{
    private readonly int _k;
    private readonly int _words;
    private readonly uint[] _blocks;

    public int BlockCount => _k;
    public int BlockLength { get; }
    public ushort SessionId { get; }

    public FountainEncoder(byte[] payload, int blockLen, ushort sessionId)
    {
        BlockLength = blockLen;
        SessionId = sessionId;
        _k = Math.Max(1, (payload.Length + blockLen - 1) / blockLen);
        _words = (blockLen + 3) / 4;
        _blocks = new uint[_k * _words];

        for (int b = 0; b < _k; b++)
        {
            int start = b * blockLen;
            int len = Math.Min(blockLen, payload.Length - start);
            // 按字节拷入（uint[] 视作小端字节缓冲），不足处保持 0 填充。
            Buffer.BlockCopy(payload, start, _blocks, b * _words * 4, len);
        }
    }

    /// <summary>编码第 <paramref name="seq"/> 帧（长度恒为 <see cref="BlockLength"/>）。</summary>
    public byte[] Encode(uint seq)
    {
        var indices = FountainCodec.FrameComposition(_k, SessionId, seq);
        var words = new uint[_words];
        foreach (var b in indices)
        {
            int offset = b * _words;
            for (int w = 0; w < _words; w++)
                words[w] ^= _blocks[offset + w];
        }

        var result = new byte[BlockLength];
        Buffer.BlockCopy(words, 0, result, 0, BlockLength);
        return result;
    }
}
