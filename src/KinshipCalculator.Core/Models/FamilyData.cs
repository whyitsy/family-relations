namespace KinshipCalculator.Core.Models;

/// <summary>家谱整体数据（持久化根对象）。</summary>
public sealed class FamilyData
{
    public List<Person> People { get; set; } = new();

    public List<RelationEdge> Relations { get; set; } = new();

    /// <summary>「我」对应的人员 Id；为空表示尚未指定。</summary>
    public string? SelfId { get; set; }
}
