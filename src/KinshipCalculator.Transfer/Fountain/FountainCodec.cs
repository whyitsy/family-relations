namespace KinshipCalculator.Transfer.Fountain;

/// <summary>
/// 喷泉码（wire v2/v3 的「系统性-旋转木马」实现）的帧组合规则。
/// 发送端与接收端各自独立推导每帧覆盖的块子集，因此本文件的任何改动都是线格式破坏性变更。
/// 全部为 32 位整数运算，可与 JS 参考实现比特级一致。
/// </summary>
public static class FountainCodec
{
    private const uint RepairDegreeMin = 4;
    private const uint RepairDegreeMax = 24;

    /// <summary>一个循环周期的帧数：k 个系统性帧 + k 个修复帧。</summary>
    public static int CycleLength(int k) => 2 * k;

    /// <summary>帧序号种子：混合 sessionId 与 seq，纯整数运算。</summary>
    public static uint FrameSeed(ushort sessionId, uint seq)
    {
        // h = imul(sessionId + 1, 0x9e3779b1) ^ (seq + 0x85ebca6b)   （按 int32 截断）
        int left = unchecked((sessionId + 1) * unchecked((int)0x9e3779b1));
        int right = unchecked((int)(seq + 0x85ebca6bu));
        uint h = unchecked((uint)(left ^ right));
        // h = imul(h ^ (h >>> 13), 0xc2b2ae35)
        uint t = h ^ (h >> 13);
        uint h2 = unchecked((uint)unchecked(unchecked((int)t) * unchecked((int)0xc2b2ae35u)));
        // return h ^ (h >>> 16)
        return h2 ^ (h2 >> 16);
    }

    /// <summary>帧 seq 覆盖的块索引集合：扫描期单块，之后为 4–24 度的随机修复子集。</summary>
    public static int[] FrameComposition(int k, ushort sessionId, uint seq)
    {
        int pos = (int)(seq % (uint)CycleLength(k));
        return pos < k ? new[] { pos } : RepairIndices(k, sessionId, seq);
    }

    private static int[] RepairIndices(int k, ushort sessionId, uint seq)
    {
        var rnd = new SplitMix32(unchecked((int)FrameSeed(sessionId, seq)));
        uint d = (uint)Math.Min(k, (int)(RepairDegreeMin + rnd.Next() % (RepairDegreeMax - RepairDegreeMin + 1)));

        // 与 JS 的 Set 一致：保持「首次出现」顺序 + 去重。
        var set = new HashSet<int>();
        var list = new List<int>((int)d);
        while (list.Count < d)
        {
            var idx = (int)(rnd.Next() % (uint)k);
            if (set.Add(idx))
                list.Add(idx);
        }

        return list.ToArray();
    }
}
