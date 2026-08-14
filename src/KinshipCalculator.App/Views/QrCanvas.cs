using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using KinshipCalculator.Transfer.Qr;

namespace KinshipCalculator.App.Views;

/// <summary>
/// 二维码显示控件：用 <see cref="Canvas"/> + 矩形形状逐模块绘制。
/// 与 GraphCanvas 使用同一套「真实控件子元素」的可靠机制，避免依赖
/// <see cref="Control.Render"/> 覆写 / WriteableBitmap 的底层细节。
/// </summary>
public sealed class QrCanvas : Canvas
{
    public void SetQr(QrMatrix? qr)
    {
        Children.Clear();

        if (qr is null)
            return;

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return; // 布局尚未完成，等下一次刷新

        int n = qr.Size;
        double cell = Math.Min(w, h) / n;
        double offsetX = (w - cell * n) / 2;
        double offsetY = (h - cell * n) / 2;

        // 白底
        Children.Add(new Rectangle
        {
            Width = w,
            Height = h,
            Fill = Brushes.White,
            IsHitTestVisible = false,
        });

        // 黑模块（按行合并连续暗模块，减少控件数量）
        for (int y = 0; y < n; y++)
        {
            int x = 0;
            while (x < n)
            {
                if (!qr[x, y])
                {
                    x++;
                    continue;
                }

                int start = x;
                while (x < n && qr[x, y])
                    x++;

                var rect = new Rectangle
                {
                    Width = cell * (x - start),
                    Height = cell,
                    Fill = Brushes.Black,
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(rect, offsetX + start * cell);
                Canvas.SetTop(rect, offsetY + y * cell);
                Children.Add(rect);
            }
        }
    }
}
