using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KinshipCalculator.App.Services;
using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.ViewModels;

/// <summary>称谓结果行（不可变展示模型，避免转换器）。</summary>
public sealed class KinshipResultRow
{
    public required string PersonId { get; init; }
    public required string Summary { get; init; }
    public required string Path { get; init; }
}

/// <summary>主视图模型：管理家谱数据、编辑与称谓计算。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storage;
    private readonly FamilyData _data;
    private bool _syncing;
    private IReadOnlyList<KinshipResult> _lastRawResults = Array.Empty<KinshipResult>();

    private static readonly Gender[] GenderOrder = { Gender.Male, Gender.Female, Gender.Unknown };

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

    public MainViewModel(IStorageService storage)
    {
        _storage = storage;
        _data = storage.Load();
        People = new ObservableCollection<Person>(_data.People);
        SelfPerson = _data.People.FirstOrDefault(p => p.Id == _data.SelfId);
        Recalculate();
    }

    public FamilyData Data => _data;

    public IReadOnlyList<KinshipResult> LastRawResults => _lastRawResults;

    public bool HasSelection => SelectedPerson is not null;

    public string SelfLabel => SelfPerson is null ? "未指定『我』" : $"『我』：{SelfPerson.Name}";

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
        SelectedPerson = _data.People.FirstOrDefault(p => p.Id == value.PersonId);
    }

    [RelayCommand]
    private void AddPerson()
    {
        var p = new Person { Name = "新成员", Gender = Gender.Unknown };
        _data.People.Add(p);
        People.Add(p);
        SelectedPerson = p;
    }

    [RelayCommand]
    private void DeletePerson()
    {
        var p = SelectedPerson;
        if (p is null)
            return;

        _data.People.Remove(p);
        _data.Relations.RemoveAll(r => r.FromId == p.Id || r.ToId == p.Id);
        if (_data.SelfId == p.Id)
        {
            _data.SelfId = null;
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

        _data.SelfId = p.Id;
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
        var edge = _data.Relations.FirstOrDefault(r => r.FromId == p.Id && r.Kind == kind);
        return edge is null ? null : _data.People.FirstOrDefault(x => x.Id == edge.ToId);
    }

    private Person? FindSpouse(Person p)
    {
        var edge = _data.Relations.FirstOrDefault(r =>
            r.Kind == RelationKind.Spouse && (r.FromId == p.Id || r.ToId == p.Id));
        if (edge is null)
            return null;
        return _data.People.FirstOrDefault(x => x.Id == (edge.FromId == p.Id ? edge.ToId : edge.FromId));
    }

    private void SetParent(Person? value, bool isFather)
    {
        var p = SelectedPerson;
        if (p is null || (value is not null && value.Id == p.Id))
            return;

        var kind = isFather ? RelationKind.Father : RelationKind.Mother;
        _data.Relations.RemoveAll(r => r.FromId == p.Id && r.Kind == kind);
        if (value is not null)
            _data.Relations.Add(new RelationEdge { FromId = p.Id, ToId = value.Id, Kind = kind });

        OnPropertyChanged(isFather ? nameof(SelectedFather) : nameof(SelectedMother));
        Recalculate();
    }

    private void SetSpouse(Person? value)
    {
        var p = SelectedPerson;
        if (p is null || (value is not null && value.Id == p.Id))
            return;

        _data.Relations.RemoveAll(r => r.Kind == RelationKind.Spouse && (r.FromId == p.Id || r.ToId == p.Id));
        if (value is not null)
            _data.Relations.Add(new RelationEdge { FromId = p.Id, ToId = value.Id, Kind = RelationKind.Spouse });

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

    private void Recalculate()
    {
        _lastRawResults = new RelationshipCalculator().ComputeAll(_data);

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

    private void Save() => _storage.Save(_data);
}
