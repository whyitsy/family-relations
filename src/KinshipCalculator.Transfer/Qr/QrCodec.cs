using System.Text;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace KinshipCalculator.Transfer.Qr;

/// <summary>
/// 二维码编解码（ZXing.Net，QR-only，ECC L，固定 ISO-8859-1 字节模式）。
/// 字节经 Latin1 字符串映射为单字节，保证任意二进制载荷可无损往返。
/// </summary>
public static class QrCodec
{
    public static QrMatrix? Encode(byte[] data)
    {
        try
        {
            var content = Encoding.Latin1.GetString(data);
            var hints = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.L,
                [EncodeHintType.CHARACTER_SET] = "ISO-8859-1",
                [EncodeHintType.MARGIN] = 4,
            };

            var matrix = new QRCodeWriter().encode(content, BarcodeFormat.QR_CODE, 0, 0, hints);
            return FromBitMatrix(matrix);
        }
        catch
        {
            return null;
        }
    }

    public static byte[]? Decode(byte[] gray, int width, int height)
    {
        try
        {
            var source = new PlanarYUVLuminanceSource(gray, width, height, 0, 0, width, height, false);
            var bitmap = new BinaryBitmap(new HybridBinarizer(source));
            var hints = new Dictionary<DecodeHintType, object>
            {
                [DecodeHintType.CHARACTER_SET] = "ISO-8859-1",
            };

            var result = new QRCodeReader().decode(bitmap, hints);
            if (result is null)
                return null;

            // 用文本（Latin1）还原原始字节：ZXing 的 RawBytes 会混入 ECI 段，不可直接用作载荷。
            if (result.Text is not null)
                return Encoding.Latin1.GetBytes(result.Text);
            if (result.RawBytes is { Length: > 0 } raw)
                return raw;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static QrMatrix FromBitMatrix(BitMatrix matrix)
    {
        var qr = new QrMatrix(matrix.Width);
        for (int y = 0; y < matrix.Height; y++)
        {
            for (int x = 0; x < matrix.Width; x++)
            {
                qr[x, y] = matrix[x, y];
            }
        }

        return qr;
    }
}
