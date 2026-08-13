using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KinshipCalculator.App.Services;
using KinshipCalculator.App.ViewModels;
using KinshipCalculator.App.Views;

namespace KinshipCalculator.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 不使用反射 ViewLocator（AOT 安全）：直接构造视图与视图模型。
        var storage = new JsonFileStorageService(DataLocator.GetDataFilePath());
        var vm = new MainViewModel(storage);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new MainView { DataContext = vm };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
