using Avalonia;
using AvaloniaApp = KinshipCalculator.App.App;

namespace KinshipCalculator.Desktop;

internal static class Program
{
    // 初始化代码。在 AppMain 之前不要使用任何 Avalonia / 第三方 API 或依赖同步上下文的代码。
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia 配置，勿删除；同时供可视化设计器使用。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
