using System.ComponentModel;
using Avalonia.Controls;
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
}
