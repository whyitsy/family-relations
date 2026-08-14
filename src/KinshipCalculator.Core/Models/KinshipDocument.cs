using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.Core.Models;

/// <summary>全部关系图谱的持久化根对象（支持多个图谱与用户自定义称谓规则集）。</summary>
public sealed class KinshipDocument
{
    public List<FamilyGraph> Graphs { get; set; } = new();

    /// <summary>用户自定义称谓规则集（内置预设不入库，按 Id 引用）。</summary>
    public List<KinshipRuleSet> RuleSets { get; set; } = new();

    /// <summary>当前打开的图谱 Id；为空时取第一个。</summary>
    public string? CurrentGraphId { get; set; }
}
