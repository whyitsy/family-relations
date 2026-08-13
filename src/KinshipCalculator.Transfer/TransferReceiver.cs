using KinshipCalculator.Transfer.Fountain;
using KinshipCalculator.Transfer.Protocol;

namespace KinshipCalculator.Transfer;

/// <summary>接收端：从任意顺序的帧还原容器；按流身份自动锁定/重置，完成后校验 FNV。</summary>
public sealed class TransferReceiver
{
    private FountainDecoder? _decoder;
    private string? _streamIdentity;
    private byte[]? _container;

    public bool IsComplete => _container is not null;
    public byte[]? Container => _container;
    public int SolvedCount => _decoder?.SolvedCount ?? 0;
    public int FramesNew => _decoder?.FramesNew ?? 0;
    public int FramesDup => _decoder?.FramesDup ?? 0;

    /// <summary>喂入一帧（完整字节，含 22 字节头）。返回 true 表示该帧属于当前流并被接受。</summary>
    public bool AddFrame(byte[] frame)
    {
        var header = FrameCodec.ParseFrame(frame);
        if (header is null)
            return false;

        var identity = FrameCodec.StreamIdentity(header.Value);

        // 流变化（含关键标志位变化）→ 重置解码器。
        if (_decoder is not null && _streamIdentity != identity)
        {
            _decoder = null;
            _streamIdentity = null;
            _container = null;
        }

        if (_decoder is null)
        {
            _decoder = new FountainDecoder(
                header.Value.K,
                header.Value.BlockLen,
                header.Value.SessionId,
                (int)header.Value.TotalLen);
            _streamIdentity = identity;
        }

        _decoder.AddFrame(header.Value.Seq, frame.AsSpan(FrameCodec.HeaderLength));

        if (_decoder.IsComplete)
        {
            var assembled = _decoder.Assemble();
            if (assembled is not null && FrameCodec.Fnv1a(assembled) == header.Value.PayloadFnv)
            {
                _container = assembled;
            }
            else
            {
                // 校验不符（帧损坏/串流）→ 丢弃重建，等待后续帧重试。
                _decoder = null;
                _streamIdentity = null;
            }
        }

        return true;
    }

    public void Reset()
    {
        _decoder = null;
        _streamIdentity = null;
        _container = null;
    }
}
