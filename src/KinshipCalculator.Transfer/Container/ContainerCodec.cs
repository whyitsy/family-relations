using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace KinshipCalculator.Transfer.Container;

public enum CompressionMode
{
    None,
    Gzip,
}

public sealed record PackedContainer(
    byte[] Container,
    CompressionMode Compression,
    int OriginalSize,
    int TransmittedSize);

public sealed record UnpackedFile(
    string Name,
    string Type,
    byte[] Bytes,
    byte[] Sha256,
    CompressionMode Compression,
    int TransmittedSize);

/// <summary>
/// 文件容器：保存文件名、媒体类型、可选 gzip 与原文 SHA-256。
/// 布局（小端）：DCF2 + 1B 压缩 + u16 nameLen + u16 typeLen + u32 fileLen + u32 transmittedLen + 32B SHA-256。
/// </summary>
public static class ContainerCodec
{
    public const int MaxFileBytes = 64 * 1024 * 1024;
    private const int FileHeaderLength = 49;
    private static readonly byte[] FileMagic = { 0x44, 0x43, 0x46, 0x32 }; // "DCF2"

    private static readonly HashSet<string> PrecompressedTypes = new(StringComparer.Ordinal)
    {
        "application/gzip", "application/java-archive", "application/vnd.rar",
        "application/x-7z-compressed", "application/x-brotli", "application/x-bzip",
        "application/x-bzip2", "application/x-gzip", "application/x-lzma",
        "application/x-rar-compressed", "application/x-xz", "application/x-zip-compressed",
        "application/zip", "application/zstd",
    };

    private static readonly HashSet<string> CompressibleImages = new(StringComparer.Ordinal)
    {
        "image/bmp", "image/x-ms-bmp", "image/svg+xml", "image/tiff", "image/x-icon", "image/vnd.microsoft.icon",
    };

    private static readonly HashSet<string> CompressibleAudio = new(StringComparer.Ordinal)
    {
        "audio/wav", "audio/x-wav", "audio/wave", "audio/vnd.wave", "audio/aiff", "audio/x-aiff", "audio/basic", "audio/l16",
    };

    public static PackedContainer Pack(string name, string type, byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new ArgumentException("文件为空");
        if (bytes.Length > MaxFileBytes)
            throw new ArgumentException($"文件超过 {MaxFileBytes / 1024 / 1024} MB 上限");

        var nameBytes = Encoding.UTF8.GetBytes(SafeFileName(name));
        var typeBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(type) ? "application/octet-stream" : type);
        if (nameBytes.Length > 0xFFFF || typeBytes.Length > 0xFFFF)
            throw new ArgumentException("文件名或类型过长");

        var sha256 = SHA256.HashData(bytes);
        var tryGzip = bytes.Length >= 768 && !IsPrecompressedType(type);
        var compressed = tryGzip ? GzipCompress(bytes) : null;
        var useGzip = compressed is not null && compressed.Length + 64 < bytes.Length;
        var transmitted = useGzip ? compressed! : bytes;
        var compression = useGzip ? CompressionMode.Gzip : CompressionMode.None;

        var container = new byte[FileHeaderLength + nameBytes.Length + typeBytes.Length + transmitted.Length];
        FileMagic.CopyTo(container, 0);
        container[4] = useGzip ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(5), (ushort)nameBytes.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(7), (ushort)typeBytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(container.AsSpan(9), (uint)bytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(container.AsSpan(13), (uint)transmitted.Length);
        sha256.CopyTo(container, 17);
        nameBytes.CopyTo(container, FileHeaderLength);
        typeBytes.CopyTo(container, FileHeaderLength + nameBytes.Length);
        transmitted.CopyTo(container, FileHeaderLength + nameBytes.Length + typeBytes.Length);

        return new PackedContainer(container, compression, bytes.Length, transmitted.Length);
    }

    public static UnpackedFile Unpack(byte[] container)
    {
        if (container.Length < FileHeaderLength)
            throw new InvalidDataException("容器头无效");
        for (int i = 0; i < FileMagic.Length; i++)
        {
            if (container[i] != FileMagic[i])
                throw new InvalidDataException("容器魔数无效");
        }

        byte compressionByte = container[4];
        if (compressionByte > 1)
            throw new InvalidDataException("容器压缩标志无效");
        var compression = compressionByte == 1 ? CompressionMode.Gzip : CompressionMode.None;

        int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(container.AsSpan(5));
        int typeLength = BinaryPrimitives.ReadUInt16LittleEndian(container.AsSpan(7));
        int fileLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan(9));
        int transmittedLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(container.AsSpan(13));
        int dataOffset = FileHeaderLength + nameLength + typeLength;

        if (fileLength == 0 || fileLength > MaxFileBytes ||
            transmittedLength == 0 || transmittedLength > MaxFileBytes ||
            dataOffset + transmittedLength != container.Length)
        {
            throw new InvalidDataException("容器长度不匹配");
        }

        var transmitted = container.AsSpan(dataOffset, transmittedLength).ToArray();
        byte[] bytes;
        if (compression == CompressionMode.Gzip)
        {
            if (transmitted.Length < 18)
                throw new InvalidDataException("gzip 数据不完整");
            uint isize = BinaryPrimitives.ReadUInt32LittleEndian(transmitted.AsSpan(transmitted.Length - 4));
            if (isize != (uint)fileLength)
                throw new InvalidDataException("gzip 长度不匹配");
            bytes = GzipDecompress(transmitted, fileLength);
        }
        else
        {
            bytes = transmitted;
        }

        if (bytes.Length != fileLength)
            throw new InvalidDataException("解压后长度不匹配");

        var sha256 = container.AsSpan(17, 32).ToArray();
        var name = SafeFileName(Encoding.UTF8.GetString(container, FileHeaderLength, nameLength));
        var mime = typeLength > 0
            ? Encoding.UTF8.GetString(container, FileHeaderLength + nameLength, typeLength)
            : "application/octet-stream";

        return new UnpackedFile(name, mime, bytes, sha256, compression, transmittedLength);
    }

    public static bool Verify(UnpackedFile file)
    {
        var actual = SHA256.HashData(file.Bytes);
        return actual.AsSpan().SequenceEqual(file.Sha256);
    }

    public static string SafeFileName(string name)
    {
        var parts = name.Split('\\', '/');
        var baseName = parts.Length > 0 ? parts[^1] : string.Empty;

        var sb = new StringBuilder(baseName.Length);
        foreach (var c in baseName)
        {
            if (c >= '\u0020' && c != '\u007f')
                sb.Append(c);
        }

        var cleaned = sb.ToString().Trim();
        return cleaned is "" or "." or ".." ? "transfer.bin" : cleaned;
    }

    public static bool IsPrecompressedType(string type)
    {
        var media = type.Split(';')[0].Trim().ToLowerInvariant();
        if (media.StartsWith("video/", StringComparison.Ordinal)) return true;
        if (media.StartsWith("image/", StringComparison.Ordinal)) return !CompressibleImages.Contains(media);
        if (media.StartsWith("audio/", StringComparison.Ordinal)) return !CompressibleAudio.Contains(media);
        if (media.StartsWith("application/vnd.openxmlformats-officedocument.", StringComparison.Ordinal)) return true;
        if (media.StartsWith("application/vnd.oasis.opendocument.", StringComparison.Ordinal)) return true;
        if (media.EndsWith("+zip", StringComparison.Ordinal)) return true;
        return PrecompressedTypes.Contains(media);
    }

    private static byte[] GzipCompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gz.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private static byte[] GzipDecompress(byte[] data, int maxBytes)
    {
        using var input = new MemoryStream(data);
        using var gz = new GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = gz.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
            if (output.Length > maxBytes)
                throw new InvalidDataException("gzip 解压超出声明长度");
        }

        return output.ToArray();
    }
}
