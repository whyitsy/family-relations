using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KinshipCalculator.App.ViewModels;

namespace KinshipCalculator.App.Views;

public partial class MainView : UserControl
{
    private MainViewModel? _vm;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as MainViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            RefreshGraph();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.KinshipResults) or nameof(MainViewModel.SelfPerson))
            RefreshGraph();
    }

    private void RefreshGraph()
    {
        if (_vm is null)
            return;

        Graph.SetData(
            _vm.Data,
            _vm.SelfPerson?.Id,
            _vm.SelectedPerson?.Id,
            _vm.LastRawResults,
            p => _vm.SelectedPerson = p);
    }

    // ── 数据传递 ──

    private static readonly FilePickerFileType JsonFileType = new("JSON 文件")
    {
        Patterns = new[] { "*.json" },
    };

    private async void OnExportFile(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
        {
            _vm.StatusMessage = "当前平台不支持文件对话框";
            return;
        }

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "家族关系.json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { JsonFileType },
        });
        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_vm.SerializeCurrent());
            _vm.StatusMessage = "已导出到文件";
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = "导出失败：" + ex.Message;
        }
    }

    private async void OnImportFile(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
        {
            _vm.StatusMessage = "当前平台不支持文件对话框";
            return;
        }

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { JsonFileType, FilePickerFileTypes.All },
        });
        var file = files.FirstOrDefault();
        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            if (!_vm.TryImportJson(text, out var error))
                _vm.StatusMessage = error ?? "导入失败";
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = "导入失败：" + ex.Message;
        }
    }

    private async void OnCopyText(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            _vm.StatusMessage = "当前平台不支持剪贴板";
            return;
        }

        try
        {
            await clipboard.SetTextAsync(_vm.SerializeCurrent());
            _vm.StatusMessage = "已复制到剪贴板";
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = "复制失败：" + ex.Message;
        }
    }

    private async void OnPasteImport(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            _vm.StatusMessage = "当前平台不支持剪贴板";
            return;
        }

        try
        {
            var text = await clipboard.TryGetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                _vm.StatusMessage = "剪贴板为空";
                return;
            }

            if (!_vm.TryImportJson(text, out var error))
                _vm.StatusMessage = error ?? "导入失败";
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = "粘贴导入失败：" + ex.Message;
        }
    }

    private async void OnOpenTransfer(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var window = new TransferWindow { Data = _vm.Data };
        if (TopLevel.GetTopLevel(this) is Window owner)
            await window.ShowDialog(owner);
        else
            window.Show();
    }
}
