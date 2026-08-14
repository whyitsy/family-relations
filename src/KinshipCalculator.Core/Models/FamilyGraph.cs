using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.Core.Models;

/// <summary>一个关系图谱：命名的家谱数据 + 所使用的称谓规则集。</summary>
public sealed class FamilyGraph
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "默认图谱";

    /// <summary>称谓规则集 Id（见 <see cref="BuiltInRuleSets"/>）；空/未知时回落为普通话。</summary>
    public string RuleSetId { get; set; } = BuiltInRuleSets.MandarinId;

    public FamilyData Data { get; set; } = new();
}
