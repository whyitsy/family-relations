namespace KinshipCalculator.Transfer.Fountain;

/// <summary>喷泉码解码器：收集任意顺序的帧，剥皮（peeling）还原全部块。丢帧只耗时、不损正确性。</summary>
public sealed class FountainDecoder
{
    private readonly int _k;
    private readonly int _blockLen;
    private readonly ushort _sessionId;
    private readonly int _totalLen;
    private readonly int _words;
    private readonly uint[]?[] _solved;
    private readonly Dictionary<int, HashSet<PendingFrame>> _byBlock = new();
    private readonly HashSet<uint> _seen = new();

    public int SolvedCount { get; private set; }
    public int FramesNew { get; private set; }
    public int FramesDup { get; private set; }
    public int FramesRedundant { get; private set; }
    public bool IsComplete => SolvedCount >= _k;

    public FountainDecoder(int k, int blockLen, ushort sessionId, int totalLen)
    {
        _k = k;
        _blockLen = blockLen;
        _sessionId = sessionId;
        _totalLen = totalLen;
        _words = (blockLen + 3) / 4;
        _solved = new uint[]?[k];
    }

    public void AddFrame(uint seq, ReadOnlySpan<byte> block)
    {
        if (!_seen.Add(seq))
        {
            FramesDup++;
            return;
        }

        FramesNew++;
        if (IsComplete)
            return;

        var indices = new HashSet<int>(FountainCodec.FrameComposition(_k, _sessionId, seq));
        var words = new uint[_words];
        int copyLen = Math.Min(_blockLen, block.Length);
        var blockBytes = block.Slice(0, copyLen);
        var temp = new byte[copyLen];
        blockBytes.CopyTo(temp);
        Buffer.BlockCopy(temp, 0, words, 0, copyLen);

        // 扣除已解出的块。
        var removed = new List<int>();
        foreach (var b in indices)
        {
            var solved = _solved[b];
            if (solved is not null)
            {
                XorInto(words, solved);
                removed.Add(b);
            }
        }

        foreach (var b in removed)
            indices.Remove(b);

        if (indices.Count == 0)
        {
            FramesRedundant++;
            return;
        }

        if (indices.Count == 1)
        {
            Resolve(indices.First(), words);
            return;
        }

        var pending = new PendingFrame { Idx = indices, Words = words };
        foreach (var b in indices)
        {
            if (!_byBlock.TryGetValue(b, out var set))
            {
                set = new HashSet<PendingFrame>();
                _byBlock[b] = set;
            }

            set.Add(pending);
        }
    }

    private void Resolve(int first, uint[] words)
    {
        var queue = new Stack<(int Block, uint[] Words)>();
        queue.Push((first, words));

        while (queue.Count > 0)
        {
            var (b, w) = queue.Pop();
            if (_solved[b] is not null)
                continue;

            _solved[b] = w;
            SolvedCount++;

            if (!_byBlock.TryGetValue(b, out var waiting))
                continue;

            _byBlock.Remove(b);
            foreach (var pending in waiting)
            {
                XorInto(pending.Words, w);
                pending.Idx.Remove(b);
                if (pending.Idx.Count == 1)
                {
                    var r = pending.Idx.First();
                    if (_byBlock.TryGetValue(r, out var rSet))
                        rSet.Remove(pending);
                    if (_solved[r] is null)
                        queue.Push((r, pending.Words));
                }
            }
        }
    }

    public byte[]? Assemble()
    {
        if (!IsComplete)
            return null;

        var output = new byte[_totalLen];
        for (int b = 0; b < _k; b++)
        {
            int start = b * _blockLen;
            int len = Math.Min(_blockLen, _totalLen - start);
            if (len <= 0)
                continue;

            Buffer.BlockCopy(_solved[b]!, 0, output, start, len);
        }

        return output;
    }

    private static void XorInto(uint[] dst, uint[] src)
    {
        for (int i = 0; i < dst.Length; i++)
            dst[i] ^= src[i];
    }

    private sealed class PendingFrame
    {
        public HashSet<int> Idx { get; set; } = new();
        public uint[] Words { get; set; } = Array.Empty<uint>();
    }
}
