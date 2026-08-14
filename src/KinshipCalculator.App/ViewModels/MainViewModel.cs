using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KinshipCalculator.App.Services;
using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;
using KinshipCalculator.Core.Serialization;

namespace KinshipCalculator.App.ViewModels;

/// <summary>称谓结果行（不可变展示模型，避免转换器）。</summary>
public sealed class KinshipResultRow
{
    public required string PersonId { get; init; }
    public required string Summary { get; init; }
    public required string Path { get; init; }
}

/// <summary>主视图模型：管理多个关系图谱、当前图谱的编辑与称谓计算。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storage;
    private KinshipDocument _document;
    private bool _syncing;
    private IReadOnlyList<KinshipResult> _lastRawResults = Array.Empty<KinshipResult>();

    private static readonly Gender[] GenderOrder = { Gender.Male, Gender.Female, Gender.Unknown };

    [ObservableProperty]
    private ObservableCollection<FamilyGraph> _graphs = new();

    [ObservableProperty]
    private FamilyGraph? _currentGraph;

    [ObservableProperty]
    private ObservableCollection<Person> _people = new();

    [ObservableProperty]
    private Person? _selectedPerson;

    [ObservableProperty]
    private Person? _selfPerson;

    [ObservableProperty]
    private ObservableCollection<KinshipResultRow> _kinshipResults = new();

    [ObservableProperty]
    private KinshipResultRow? _selectedResult;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MainViewModel(IStorageService storage)
    {
        _storage = storage;
        _document = storage.Load();
        if (_document.Graphs.Count == 0)
            _document = KinshipDocumentSerializer.CreateDefault();

        var current = _document.Graphs.FirstOrDefault(g => g.Id == _document.CurrentGraphId)
                      ?? _document.Graphs[0];
        _document.CurrentGraphId = current.Id;

        Graphs = new ObservableCollection<FamilyGraph>(_document.Graphs);
        CurrentGraph = current;
        Recalculate();
    }

    /// <summary>当前图谱的家谱数据（供视图/图谱画布读取）。</summary>
    public FamilyData Data => CurrentGraph!.Data;

    /// <summary>完整文档（供称谓规则窗口读取自定义规则集）。</summary>
    public KinshipDocument Document => _document;

    /// <summary>称谓规则被修改后，触发重算并保存。</summary>
    public void ApplyRulesChanged()
    {
        Recalculate();
        Save();
        StatusMessage = "称谓规则已更新";
    }

    public IReadOnlyList<KinshipResult> LastRawResults => _lastRawResults;

    public bool HasSelection => SelectedPerson is not null;

    public string SelfLabel => SelfPerson is null ? "未指定『我』" : $"『我』：{SelfPerson.Name}";

    /// <summary>当前图谱名称（双向，改名即时保存）。</summary>
    public string GraphName
    {
        get => CurrentGraph!.Name;
        set
        {
            if (CurrentGraph!.Name != value)
            {
                CurrentGraph.Name = value ?? string.Empty;
                OnPropertyChanged();
                Save();
            }
        }
    }

    partial void OnCurrentGraphChanged(FamilyGraph? value)
    {
        if (value is null)
            return;

        _document.CurrentGraphId = value.Id;
        RebuildFromCurrent();
    }

    partial void OnSelectedPersonChanged(Person? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        RefreshDetail();
        Recalculate();
    }

    partial void OnSelectedResultChanged(KinshipResultRow? value)
    {
        if (_syncing || value is null)
            return;
        SelectedPerson = Data.People.FirstOrDefault(p => p.Id == value.PersonId);
    }

    // ── 图谱管理 ──

    [RelayCommand]
    private void AddGraph()
    {
        var g = new FamilyGraph { Name = $"图谱 {Graphs.Count + 1}" };
        _document.Graphs.Add(g);
        Graphs.Add(g);
        CurrentGraph = g;
        Save();
        StatusMessage = $"已新建「{g.Name}」";
    }

    [RelayCommand]
    private void DeleteGraph()
    {
        if (Graphs.Count <= 1)
        {
            StatusMessage = "至少保留一个图谱";
            return;
        }

        var g = CurrentGraph!;
        var idx = Graphs.IndexOf(g);
        _document.Graphs.Remove(g);
        Graphs.Remove(g);

        CurrentGraph = Graphs[Math.Clamp(idx, 0, Graphs.Count - 1)];
        Save();
        StatusMessage = $"已删除「{g.Name}」";
    }

    // ── 成员操作 ──

    [RelayCommand]
    private void AddPerson()
    {
        var p = new Person { Name = "新成员", Gender = Gender.Unknown };
        Data.People.Add(p);
        People.Add(p);
        SelectedPerson = p;
    }

    [RelayCommand]
    private void DeletePerson()
    {
        var p = SelectedPerson;
        if (p is null)
            return;

        Data.People.Remove(p);
        Data.Relations.RemoveAll(r => r.FromId == p.Id || r.ToId == p.Id);
        if (Data.SelfId == p.Id)
        {
            Data.SelfId = null;
            SelfPerson = null;
            OnPropertyChanged(nameof(SelfLabel));
        }

        People.Remove(p);
        SelectedPerson = null;
        Recalculate();
    }

    [RelayCommand]
    private void SetSelf()
    {
        var p = SelectedPerson;
        if (p is null)
            return;

        Data.SelfId = p.Id;
        SelfPerson = p;
        OnPropertyChanged(nameof(SelfLabel));
        Recalculate();
    }

    [RelayCommand]
    private void ClearFather() => SetParent(null, isFather: true);

    [RelayCommand]
    private void ClearMother() => SetParent(null, isFather: false);

    [RelayCommand]
    private void ClearSpouse() => SetSpouse(null);

    // ── 详情编辑属性 ──

    public string SelectedName
    {
        get => SelectedPerson?.Name ?? string.Empty;
        set
        {
            if (SelectedPerson is { } p && p.Name != value)
            {
                p.Name = value ?? string.Empty;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    public int SelectedGenderIndex
    {
        get => SelectedPerson is { } p ? Array.IndexOf(GenderOrder, p.Gender) : 2;
        set
        {
            if (SelectedPerson is { } p && value >= 0 && value < GenderOrder.Length && p.Gender != GenderOrder[value])
            {
                p.Gender = GenderOrder[value];
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    public DateTimeOffset? SelectedBirthDate
    {
        get => SelectedPerson?.BirthDate is { } d ? new DateTimeOffset(d) : null;
        set
        {
            if (SelectedPerson is { } p)
            {
                var next = value?.Date;
                if (p.BirthDate != next)
                {
                    p.BirthDate = next;
                    OnPropertyChanged();
                    Recalculate();
                }
            }
        }
    }

    public string? SelectedNotes
    {
        get => SelectedPerson?.Notes;
        set
        {
            if (SelectedPerson is { } p && p.Notes != value)
            {
                p.Notes = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public Person? SelectedFather
    {
        get => SelectedPerson is { } p ? FindParent(p, isFather: true) : null;
        set => SetParent(value, isFather: true);
    }

    public Person? SelectedMother
    {
        get => SelectedPerson is { } p ? FindParent(p, isFather: false) : null;
        set => SetParent(value, isFather: false);
    }

    public Person? SelectedSpouse
    {
        get => SelectedPerson is { } p ? FindSpouse(p) : null;
        set => SetSpouse(value);
    }

    // ── 内部辅助 ──

    private Person? FindParent(Person p, bool isFather)
    {
        var kind = isFather ? RelationKind.Father : RelationKind.Mother;
        var edge = Data.Relations.FirstOrDefault(r => r.FromId == p.Id && r.Kind == kind);
        return edge is null ? null : Data.People.FirstOrDefault(x => x.Id == edge.ToId);
    }

    private Person? FindSpouse(Person p)
    {
        var edge = Data.Relations.FirstOrDefault(r =>
            r.Kind == RelationKind.Spouse && (r.FromId == p.Id || r.ToId == p.Id));
        if (edge is null)
            return null;
        return Data.People.FirstOrDefault(x => x.Id == (edge.FromId == p.Id ? edge.ToId : edge.FromId));
    }

    private void SetParent(Person? value, bool isFather)
    {
        var p = SelectedPerson;
        if (p is null || (value is not null && value.Id == p.Id))
            return;

        var kind = isFather ? RelationKind.Father : RelationKind.Mother;
        Data.Relations.RemoveAll(r => r.FromId == p.Id && r.Kind == kind);
        if (value is not null)
            Data.Relations.Add(new RelationEdge { FromId = p.Id, ToId = value.Id, Kind = kind });

        OnPropertyChanged(isFather ? nameof(SelectedFather) : nameof(SelectedMother));
        Recalculate();
    }

    private void SetSpouse(Person? value)
    {
        var p = SelectedPerson;
        if (p is null || (value is not null && value.Id == p.Id))
            return;

        Data.Relations.RemoveAll(r => r.Kind == RelationKind.Spouse && (r.FromId == p.Id || r.ToId == p.Id));
        if (value is not null)
            Data.Relations.Add(new RelationEdge { FromId = p.Id, ToId = value.Id, Kind = RelationKind.Spouse });

        OnPropertyChanged(nameof(SelectedSpouse));
        Recalculate();
    }

    private void RefreshDetail()
    {
        OnPropertyChanged(nameof(SelectedName));
        OnPropertyChanged(nameof(SelectedGenderIndex));
        OnPropertyChanged(nameof(SelectedBirthDate));
        OnPropertyChanged(nameof(SelectedNotes));
        OnPropertyChanged(nameof(SelectedFather));
        OnPropertyChanged(nameof(SelectedMother));
        OnPropertyChanged(nameof(SelectedSpouse));
    }

    private void RebuildFromCurrent()
    {
        var data = Data;
        People = new ObservableCollection<Person>(data.People);
        SelfPerson = data.People.FirstOrDefault(p => p.Id == data.SelfId);
        SelectedPerson = null;
        OnPropertyChanged(nameof(SelfLabel));
        OnPropertyChanged(nameof(GraphName));
        Recalculate();
    }

    private void Recalculate()
    {
        if (CurrentGraph is null)
            return;

        var rules = BuiltInRuleSets.Resolve(CurrentGraph.RuleSetId, _document.RuleSets);
        _lastRawResults = new RelationshipCalculator().ComputeAll(Data, rules);

        var rows = new ObservableCollection<KinshipResultRow>();
        foreach (var r in _lastRawResults)
        {
            var summary = $"{r.PersonName} —— {r.Term}";
            if (r.IsAmbiguous)
                summary += "（多重关系）";
            else if (r.NeedsBirthDate)
                summary += "（需补充生日）";

            rows.Add(new KinshipResultRow
            {
                PersonId = r.PersonId,
                Summary = summary,
                Path = r.PathDescription ?? string.Empty,
            });
        }

        KinshipResults = rows;

        _syncing = true;
        SelectedResult = KinshipResults.FirstOrDefault(x => x.PersonId == SelectedPerson?.Id);
        _syncing = false;

        Save();
    }

    private void Save() => _storage.Save(_document);

    // ── 数据导入 / 导出 ──

    /// <summary>把当前图谱序列化为 JSON 文本（供文件导出或复制到剪贴板）。</summary>
    public string SerializeCurrent() => FamilyDataSerializer.Serialize(Data);

    /// <summary>
    /// 从 JSON 文本导入：多图谱文档整体替换；旧单图格式导入到当前图谱。成功后持久化。
    /// </summary>
    public bool TryImportJson(string? json, out string? error)
    {
        // 1) 多图谱文档 → 整体替换。
        if (KinshipDocumentSerializer.TryParseDocument(json, out var doc, out error))
        {
            if (doc is null)
                return false;
            ReplaceDocument(doc);
            return true;
        }

        // 2) 单图谱（旧格式 / 导出 JSON 文件）→ 替换当前图谱数据。
        if (FamilyDataSerializer.TryDeserialize(json, out var data, out error) && data is not null)
        {
            ReplaceCurrentData(data);
            return true;
        }

        return false;
    }

    private void ReplaceDocument(KinshipDocument doc)
    {
        _document = KinshipDocumentSerializer.Normalize(doc);
        if (_document.Graphs.Count == 0)
            _document.Graphs.Add(new FamilyGraph());

        Graphs = new ObservableCollection<FamilyGraph>(_document.Graphs);

        var current = _document.Graphs.FirstOrDefault(g => g.Id == _document.CurrentGraphId)
                      ?? _document.Graphs[0];
        _document.CurrentGraphId = current.Id;
        CurrentGraph = current;

        RebuildFromCurrent();
        Save();
        StatusMessage = $"已导入 {_document.Graphs.Count} 个图谱";
    }

    private void ReplaceCurrentData(FamilyData data)
    {
        CurrentGraph!.Data = FamilyDataSerializer.Normalize(data);

        RebuildFromCurrent();
        Save();
        StatusMessage = $"已导入 {Data.People.Count} 位成员到「{CurrentGraph!.Name}」";
    }
}
