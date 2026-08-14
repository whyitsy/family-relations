using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.App.ViewModels;

/// <summary>规则集下拉选项。</summary>
public sealed class RuleSetOption
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsBuiltIn { get; init; }
}

/// <summary>规则列表行（只读展示模型）。</summary>
public sealed class RuleRow
{
    public required int Index { get; init; }
    public required KinshipRule Rule { get; init; }
    public required string PathText { get; init; }
    public required string ConditionText { get; init; }
}

/// <summary>称谓规则管理：选择预设、编辑/添加/删除规则，编辑结果写回文档并触发主视图重算。</summary>
public partial class TermsViewModel : ObservableObject
{
    private static readonly string[] StepNames =
    {
        "父亲", "母亲", "儿子", "女儿", "配偶", "兄弟", "姐妹", "孩子(未知)", "兄弟姐妹(未知)",
    };

    private static readonly StepKind[] StepKinds =
    {
        StepKind.Father, StepKind.Mother, StepKind.Son, StepKind.Daughter,
        StepKind.Spouse, StepKind.Brother, StepKind.Sister, StepKind.Child, StepKind.Sibling,
    };

    private readonly KinshipDocument _document;
    private readonly FamilyGraph _graph;
    private readonly Action _onChanged;

    private List<StepKind> _composing = new();

    [ObservableProperty]
    private ObservableCollection<RuleSetOption> _ruleSetOptions = new();

    [ObservableProperty]
    private RuleSetOption? _selectedRuleSetOption;

    [ObservableProperty]
    private ObservableCollection<RuleRow> _ruleRows = new();

    [ObservableProperty]
    private RuleRow? _selectedRuleRow;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // ── 添加规则组合器状态 ──
    [ObservableProperty]
    private bool _isComposing;

    [ObservableProperty]
    private int _selectedStepIndex;

    [ObservableProperty]
    private string _newPatternText = "（尚未选择任何关系步）";

    [ObservableProperty]
    private int _newTargetGenderIndex;   // 0 不限 / 1 男 / 2 女

    [ObservableProperty]
    private int _newSelfGenderIndex;

    [ObservableProperty]
    private int _newAgeRuleIndex;        // 0 不比较 / 1 与我比 / 2 与上一步比

    [ObservableProperty]
    private string _newTerm = string.Empty;

    [ObservableProperty]
    private string _newOlder = string.Empty;

    [ObservableProperty]
    private string _newYounger = string.Empty;

    public TermsViewModel(KinshipDocument document, FamilyGraph graph, Action onChanged)
    {
        _document = document;
        _graph = graph;
        _onChanged = onChanged;
        RebuildOptions();
    }

    public bool IsCustomSelected => SelectedRuleSetOption is { IsBuiltIn: false };

    public bool CanEditSelected => IsCustomSelected && SelectedRuleRow is not null;

    public string[] AvailableSteps => StepNames;

    // ── 规则集选择 ──

    private void RebuildOptions()
    {
        var options = new List<RuleSetOption>();
        foreach (var s in BuiltInRuleSets.All)
            options.Add(new RuleSetOption { Id = s.Id, Name = s.Name, IsBuiltIn = true });
        foreach (var s in _document.RuleSets)
            options.Add(new RuleSetOption { Id = s.Id, Name = s.Name, IsBuiltIn = false });

        var currentId = _graph.RuleSetId;
        RuleSetOptions = new ObservableCollection<RuleSetOption>(options);
        SelectedRuleSetOption = RuleSetOptions.FirstOrDefault(o => o.Id == currentId)
                                ?? RuleSetOptions.First();
    }

    partial void OnSelectedRuleSetOptionChanged(RuleSetOption? value)
    {
        if (value is null)
            return;

        _graph.RuleSetId = value.Id;
        OnPropertyChanged(nameof(IsCustomSelected));
        RebuildRowsCore();
        SelectedRuleRow = null;
        _onChanged();
    }

    // ── 规则列表 ──

    private void RebuildRowsCore()
    {
        var set = BuiltInRuleSets.FindSet(SelectedRuleSetOption?.Id, _document.RuleSets);
        var rows = new List<RuleRow>();
        if (set is not null)
        {
            for (var i = 0; i < set.Rules.Count; i++)
            {
                var r = set.Rules[i];
                rows.Add(new RuleRow
                {
                    Index = i,
                    Rule = r,
                    PathText = Describe(r.Pattern),
                    ConditionText = DescribeCondition(r),
                });
            }
        }

        RuleRows = new ObservableCollection<RuleRow>(rows);
    }

    partial void OnSelectedRuleRowChanged(RuleRow? value)
    {
        OnPropertyChanged(nameof(SelectedTerm));
        OnPropertyChanged(nameof(SelectedOlder));
        OnPropertyChanged(nameof(SelectedYounger));
        OnPropertyChanged(nameof(CanEditSelected));
    }

    public string SelectedTerm
    {
        get => SelectedRuleRow?.Rule.Term ?? string.Empty;
        set => EditRule(r => r with { Term = value ?? string.Empty });
    }

    public string SelectedOlder
    {
        get => SelectedRuleRow?.Rule.TermIfOlder ?? string.Empty;
        set => EditRule(r => r with { TermIfOlder = BlankToNull(value) });
    }

    public string SelectedYounger
    {
        get => SelectedRuleRow?.Rule.TermIfYounger ?? string.Empty;
        set => EditRule(r => r with { TermIfYounger = BlankToNull(value) });
    }

    private void EditRule(Func<KinshipRule, KinshipRule> edit)
    {
        if (!IsCustomSelected || SelectedRuleRow is not { } row)
            return;

        var set = BuiltInRuleSets.FindSet(SelectedRuleSetOption!.Id, _document.RuleSets);
        if (set is null)
            return;

        set.Rules[row.Index] = edit(row.Rule);
        RebuildRowsCore();
        SelectedRuleRow = RuleRows.FirstOrDefault(r => r.Index == row.Index);
        _onChanged();
    }

    // ── 规则集命令 ──

    [RelayCommand]
    private void NewCustomRuleSet()
    {
        var source = BuiltInRuleSets.FindSet(SelectedRuleSetOption?.Id, _document.RuleSets)
                     ?? BuiltInRuleSets.Mandarin;

        var copy = new KinshipRuleSet
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = source.Name + "（自定义）",
            Rules = source.Rules.Select(r => new KinshipRule(
                r.Id, r.Pattern.ToArray(), r.SelfGender, r.TargetGender,
                r.AgeRule, r.AgeStepIndex, r.TermIfOlder, r.TermIfYounger, r.Term)).ToList(),
        };

        _document.RuleSets.Add(copy);
        RebuildOptions();
        SelectedRuleSetOption = RuleSetOptions.First(o => o.Id == copy.Id);
        StatusMessage = $"已创建「{copy.Name}」";
        _onChanged();
    }

    [RelayCommand]
    private void DeleteCustomRuleSet()
    {
        if (SelectedRuleSetOption is not { IsBuiltIn: false } opt)
            return;

        var set = _document.RuleSets.FirstOrDefault(s => s.Id == opt.Id);
        if (set is null)
            return;

        _document.RuleSets.Remove(set);
        if (_graph.RuleSetId == set.Id)
            _graph.RuleSetId = BuiltInRuleSets.MandarinId;

        RebuildOptions();
        StatusMessage = $"已删除「{set.Name}」";
        _onChanged();
    }

    [RelayCommand]
    private void DeleteRule()
    {
        if (!IsCustomSelected || SelectedRuleRow is not { } row)
            return;

        var set = BuiltInRuleSets.FindSet(SelectedRuleSetOption!.Id, _document.RuleSets);
        if (set is null)
            return;

        set.Rules.RemoveAt(row.Index);
        RebuildRowsCore();
        SelectedRuleRow = null;
        StatusMessage = "已删除规则";
        _onChanged();
    }

    // ── 添加规则 ──

    [RelayCommand]
    private void BeginAddRule()
    {
        _composing = new List<StepKind>();
        NewPatternText = "（尚未选择任何关系步）";
        NewTargetGenderIndex = 0;
        NewSelfGenderIndex = 0;
        NewAgeRuleIndex = 0;
        NewTerm = string.Empty;
        NewOlder = string.Empty;
        NewYounger = string.Empty;
        IsComposing = true;
    }

    [RelayCommand]
    private void AddStep()
    {
        _composing.Add(StepKinds[SelectedStepIndex]);
        UpdateNewPatternText();
    }

    [RelayCommand]
    private void RemoveLastStep()
    {
        if (_composing.Count == 0)
            return;
        _composing.RemoveAt(_composing.Count - 1);
        UpdateNewPatternText();
    }

    [RelayCommand]
    private void CancelAddRule()
    {
        IsComposing = false;
        _composing = new List<StepKind>();
    }

    [RelayCommand]
    private void ConfirmAddRule()
    {
        if (!IsCustomSelected)
            return;

        var set = BuiltInRuleSets.FindSet(SelectedRuleSetOption!.Id, _document.RuleSets);
        if (set is null)
            return;

        if (_composing.Count == 0)
        {
            StatusMessage = "请至少选择一个关系步";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTerm))
        {
            StatusMessage = "请填写称谓";
            return;
        }

        var ageRule = NewAgeRuleIndex switch
        {
            1 => AgeRule.StepVsSelf,
            2 => AgeRule.StepVsPrevious,
            _ => AgeRule.None,
        };

        var rule = new KinshipRule(
            Guid.NewGuid().ToString("N"),
            _composing.ToArray(),
            NewSelfGenderIndex == 1 ? Gender.Male : NewSelfGenderIndex == 2 ? Gender.Female : null,
            NewTargetGenderIndex == 1 ? Gender.Male : NewTargetGenderIndex == 2 ? Gender.Female : null,
            ageRule,
            _composing.Count - 1,
            BlankToNull(NewOlder),
            BlankToNull(NewYounger),
            NewTerm.Trim());

        set.Rules.Add(rule);
        RebuildRowsCore();
        SelectedRuleRow = RuleRows.FirstOrDefault(r => r.Index == set.Rules.Count - 1);
        StatusMessage = "已添加规则";
        IsComposing = false;
        _composing = new List<StepKind>();
        _onChanged();
    }

    private void UpdateNewPatternText()
    {
        NewPatternText = _composing.Count == 0
            ? "（尚未选择任何关系步）"
            : "我的" + string.Concat(_composing.Select(s => "的" + StepName(s)));
    }

    private static string StepName(StepKind k) => k switch
    {
        StepKind.Father => "父亲",
        StepKind.Mother => "母亲",
        StepKind.Son => "儿子",
        StepKind.Daughter => "女儿",
        StepKind.Spouse => "配偶",
        StepKind.Brother => "兄弟",
        StepKind.Sister => "姐妹",
        StepKind.Child => "孩子",
        StepKind.Sibling => "兄弟姐妹",
        _ => "?",
    };

    private static string Describe(StepKind[] pattern)
        => pattern.Length == 0 ? "（空）" : "我的" + string.Concat(pattern.Select(StepName).Select(s => "的" + s));

    private static string DescribeCondition(KinshipRule r)
    {
        var parts = new List<string>();
        if (r.TargetGender is { } tg)
            parts.Add(tg switch { Gender.Male => "目标：男", Gender.Female => "目标：女", _ => "目标：不限" });
        if (r.SelfGender is { } sg)
            parts.Add(sg switch { Gender.Male => "本人：男", Gender.Female => "本人：女", _ => "" });
        if (r.AgeRule != AgeRule.None)
            parts.Add(r.AgeRule == AgeRule.StepVsSelf ? "长幼：与我比" : "长幼：与上一步比");

        return parts.Count == 0 ? "无额外约束" : string.Join(" · ", parts.Where(p => p.Length > 0));
    }

    private static string? BlankToNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
