using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KinshipCalculator.Core.Models;

/// <summary>人物节点。实现 <see cref="INotifyPropertyChanged"/> 以便 UI 列表实时刷新。</summary>
public sealed class Person : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = string.Empty;
    private Gender _gender = Gender.Unknown;
    private DateTime? _birthDate;
    private string? _notes;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public Gender Gender
    {
        get => _gender;
        set => SetField(ref _gender, value);
    }

    /// <summary>出生日期，用于长幼判定；可为空。</summary>
    public DateTime? BirthDate
    {
        get => _birthDate;
        set => SetField(ref _birthDate, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
