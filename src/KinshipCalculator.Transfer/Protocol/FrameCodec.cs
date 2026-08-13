using System.Buffers.Binary;

namespace KinshipCalculator.Transfer.Protocol;

/// <summary>帧协议：自描述 22 字节头 + 载荷。无握手，接收端可在流中途锁定。</summary>
public static class FrameCodec
{
    public const int HeaderLength = 22;
    public const byte Magic0 = 0xD1;
    public const byte Magic1 = 0xC3;
    public const byte WireVersion = 3;
    public const byte CriticalFlags = 0x0F;
    public const byte FlagEncrypted = 0x01;
    private const byte SupportedFlags = 0x00;

    public static byte[] PackFrame(FrameHeader header, ReadOnlySpan<byte> block)
    {
        var output = new byte[HeaderLength + block.Length];
        output[0] = Magic0;
        output[1] = Magic1;
        output[2] = WireVersion;
        output[3] = header.Flags;
        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(4), header.SessionId);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(6), header.Seq);
        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(10), header.K);
        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(12), header.BlockLen);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(14), header.TotalLen);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(18), header.PayloadFnv);
        block.CopyTo(output.AsSpan(HeaderLength));
        return output;
    }

    public static FrameVerdict ClassifyFrame(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != Magic0)
            return FrameVerdict.Foreign;

        if (bytes[1] != Magic1)
        {
            return bytes[1] switch
            {
                0x0C => FrameVerdict.OlderSender(1),
                0x0D => FrameVerdict.OlderSender(2),
                _ => FrameVerdict.Foreign,
            };
        }

        byte version = bytes[2];
        if (version == 0)
            return FrameVerdict.Malformed;
        if (version != WireVersion)
            return version > WireVersion ? FrameVerdict.NewerSender(version) : FrameVerdict.OlderSender(version);

        byte unknownCritical = (byte)(bytes[3] & CriticalFlags & ~SupportedFlags);
        if (unknownCritical != 0)
            return FrameVerdict.Unsupported(unknownCritical);

        if (bytes.Length <= HeaderLength)
            return FrameVerdict.Malformed;

        ushort k = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(10));
        ushort blockLen = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(12));
        uint totalLen = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(14));
        if (k == 0 || blockLen == 0 || totalLen == 0)
            return FrameVerdict.Malformed;
        if (bytes.Length != HeaderLength + blockLen)
            return FrameVerdict.Malformed;

        return FrameVerdict.Ok;
    }

    public static FrameHeader? ParseFrame(byte[] bytes)
    {
        if (ClassifyFrame(bytes).Kind != FrameVerdictKind.Ok)
            return null;

        return new FrameHeader(
            SessionId: BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4)),
            Seq: BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(6)),
            K: BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(10)),
            BlockLen: BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(12)),
            TotalLen: BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(14)),
            PayloadFnv: BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(18)),
            Flags: bytes[3]);
    }

    /// <summary>流身份：除 seq 外所有必须恒定的字段（含 flags 的关键半字节）。</summary>
    public static string StreamIdentity(FrameHeader header)
    {
        byte critical = (byte)(header.Flags & CriticalFlags);
        return $"{header.SessionId}:{header.K}:{header.BlockLen}:{header.TotalLen}:{header.PayloadFnv}:{critical}";
    }

    /// <summary>FNV-1a 32 位哈希（纯整数，跨语言一致）。</summary>
    public static uint Fnv1a(ReadOnlySpan<byte> bytes)
    {
        uint hash = 0x811c9dc5;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash = unchecked(hash * 0x01000193u);
        }

        return hash;
    }
}
