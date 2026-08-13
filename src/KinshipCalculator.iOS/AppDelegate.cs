using Avalonia;
using Avalonia.iOS;
using Foundation;
using UIKit;

namespace KinshipCalculator.iOS;

// 应用入口。Avalonia 12 iOS 采用场景式启动，此处为骨架，需按官方 iOS 文档核对。
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<KinshipCalculator.App.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
