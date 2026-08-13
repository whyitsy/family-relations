using KinshipCalculator.Transfer.Qr;
using Xunit;

namespace KinshipCalculator.Transfer.Tests;

public class QrTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(512)]
    public void QrRoundTrip(int payloadBytes)
    {
        var data = new byte[payloadBytes];
        for (int i = 0; i < payloadBytes; i++)
            data[i] = (byte)(i * 131 + 17);

        var qr = QrCodec.Encode(data);
        Assert.NotNull(qr);
        Assert.True(qr!.Size > 0);

        var (gray, width, height) = QrRenderer.RenderGray(qr, 4);
        var decoded = QrCodec.Decode(gray, width, height);

        Assert.NotNull(decoded);
        Assert.Equal(data, decoded!);
    }
}
