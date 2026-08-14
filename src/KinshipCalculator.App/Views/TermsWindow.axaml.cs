using Avalonia.Controls;
using KinshipCalculator.App.ViewModels;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Views;

/// <summary>称谓规则管理窗口：选择方言预设、编辑/添加/删除称谓规则。</summary>
public partial class TermsWindow : Window
{
    public TermsWindow()
    {
        InitializeComponent();
    }

    /// <summary>绑定文档、当前图谱与「变更后重算」回调。</summary>
    public void Initialize(KinshipDocument document, FamilyGraph graph, Action onChanged)
    {
        DataContext = new TermsViewModel(document, graph, onChanged);
    }
}
