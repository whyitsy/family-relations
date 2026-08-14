using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Serialization;
using KinshipCalculator.Transfer;
using KinshipCalculator.Transfer.Container;
using KinshipCalculator.Transfer.Qr;

namespace KinshipCalculator.App.Views;

/// <summary>光学发送窗口：把家谱数据编码为喷泉码二维码流，屏幕持续播放。</summary>
public partial class TransferWindow : Window
{
    private const int BlockLength = 512;

    private TransferSender? _sender;
    private DispatcherTimer? _timer;
    private uint _seq;
    private bool _sending;

    public FamilyData Data { get; set; } = new();

    public TransferWindow()
    {
        InitializeComponent();
    }

    private void OnStartStop(object? sender, RoutedEventArgs e)
    {
        if (_sending)
            Stop();
        else
            Start();
    }

    private void Start()
    {
        try
        {
            var json = FamilyDataSerializer.Serialize(Data);
            var container = ContainerCodec.Pack("family.json", "application/json", Encoding.UTF8.GetBytes(json)).Container;
            ushort sessionId = (ushort)Random.Shared.Next(1, 65536);
            _sender = new TransferSender(container, BlockLength, sessionId);

            _seq = 0;
            RenderFrame();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _timer.Tick += OnTick;
            _timer.Start();

            _sending = true;
            StartStopButton.Content = "停止发送";
        }
        catch (Exception ex)
        {
            StatusText.Text = "启动失败：" + ex.Message;
        }
    }

    private void OnTick(object? sender, EventArgs e) => RenderFrame();

    private void RenderFrame()
    {
        if (_sender is null)
            return;

        var qr = QrCodec.Encode(_sender.EncodeFrame(_seq));
        if (qr is null)
        {
            StatusText.Text = "编码失败：数据过大";
            return;
        }

        QrView.SetQr(qr);
        _seq++;
        StatusText.Text = $"帧 {_seq} · QR {qr.Size}×{qr.Size} · 共 {_sender.BlockCount} 块 · 循环播放中（另一台设备扫码接收）";
    }

    private void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _sending = false;
        StartStopButton.Content = "开始发送";
        StatusText.Text = "已停止";
    }

    protected override void OnClosed(EventArgs e)
    {
        Stop();
        base.OnClosed(e);
    }
}
