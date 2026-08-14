namespace KinshipCalculator.Core.Rules;

/// <summary>
/// 一套称谓规则（方言/地区预设或用户自定义）。
/// 规则为纯数据、可序列化，供计算与后续编辑界面共用。
/// </summary>
public sealed class KinshipRuleSet
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<KinshipRule> Rules { get; set; } = new();
}
