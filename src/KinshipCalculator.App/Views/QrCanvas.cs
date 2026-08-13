using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using KinshipCalculator.Transfer.Qr;

namespace KinshipCalculator.App.Views;

/// <summary>用矢量方式绘制二维码（避免 WriteableBitmap 像素格式/步幅/失效带来的白屏）。</summary>
public sealed class QrCanvas : Control
{
    private QrMatrix? _qr;

    public void SetQr(QrMatrix? qr)
    {
        _qr = qr;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_qr is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        int n = _qr.Size;
        double cell = Math.Min(Bounds.Width, Bounds.Height) / n;
        double offsetX = (Bounds.Width - cell * n) / 2;
        double offsetY = (Bounds.Height - cell * n) / 2;

        // 白底
        context.FillRectangle(Brushes.White, new Rect(0, 0, Bounds.Width, Bounds.Height));

        // 黑模块（略微重叠，避免抗锯齿产生缝隙）
        double draw = cell + 0.5;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                if (_qr[x, y])
                    context.FillRectangle(Brushes.Black, new Rect(offsetX + x * cell, offsetY + y * cell, draw, draw));
            }
        }
    }
}
