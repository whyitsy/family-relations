using Android.App;
using Android.Runtime;
using Avalonia.Android;

namespace KinshipCalculator.Android;

/// <summary>Avalonia 12 Android 启动入口：派生自 <see cref="AvaloniaAndroidApplication{TApp}"/>。</summary>
[Application]
public class AndroidApp : AvaloniaAndroidApplication<KinshipCalculator.App.App>
{
    public AndroidApp(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}
