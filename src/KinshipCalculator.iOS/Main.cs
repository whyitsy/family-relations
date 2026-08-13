using UIKit;

namespace KinshipCalculator.iOS;

public static class Application
{
    private static void Main(string[] args)
    {
        // 如需使用不同的 Application Delegate，可在此指定类型。
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
