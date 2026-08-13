using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace KinshipCalculator.Android;

/// <summary>Avalonia 12：主 Activity 继承非泛型 <see cref="AvaloniaMainActivity"/>。</summary>
[Activity(
    Label = "亲戚关系计算器",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
