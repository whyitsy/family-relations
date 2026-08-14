namespace KinshipCalculator.Core.Models;

/// <summary>全部关系图谱的持久化根对象（支持多个图谱）。</summary>
public sealed class KinshipDocument
{
    public List<FamilyGraph> Graphs { get; set; } = new();

    /// <summary>当前打开的图谱 Id；为空时取第一个。</summary>
    public string? CurrentGraphId { get; set; }
}
