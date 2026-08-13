namespace KinshipCalculator.Transfer.Qr;

/// <summary>把二维码模块矩阵渲染为像素字节（供测试与 UI 使用）。</summary>
public static class QrRenderer
{
    /// <summary>渲染为灰度图（0=黑，255=白），每模块 <paramref name="scale"/> 像素。</summary>
    public static (byte[] Pixels, int Width, int Height) RenderGray(QrMatrix qr, int scale = 4)
    {
        int width = qr.Size * scale;
        var pixels = new byte[width * width];
        for (int y = 0; y < qr.Size; y++)
        {
            for (int x = 0; x < qr.Size; x++)
            {
                byte value = qr[x, y] ? (byte)0 : (byte)255;
                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                    {
                        pixels[(y * scale + dy) * width + (x * scale + dx)] = value;
                    }
                }
            }
        }

        return (pixels, width, width);
    }

    /// <summary>渲染为 RGBA 字节（每像素 4 字节，不透明），供 UI 显示。</summary>
    public static (byte[] Rgba, int Width, int Height) RenderRgba(QrMatrix qr, int scale = 4)
    {
        int width = qr.Size * scale;
        var pixels = new byte[width * width * 4];
        for (int y = 0; y < qr.Size; y++)
        {
            for (int x = 0; x < qr.Size; x++)
            {
                byte value = qr[x, y] ? (byte)0 : (byte)255;
                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int i = ((y * scale + dy) * width + (x * scale + dx)) * 4;
                        pixels[i] = value;
                        pixels[i + 1] = value;
                        pixels[i + 2] = value;
                        pixels[i + 3] = 255;
                    }
                }
            }
        }

        return (pixels, width, width);
    }
}
