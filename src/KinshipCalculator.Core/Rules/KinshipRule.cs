using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Rules;

/// <summary>称谓规则中的年龄比较方式。</summary>
public enum AgeRule
{
    /// <summary>无需比较年龄。</summary>
    None,
    /// <summary>比较路径上某步人物与「我」的年龄（用于 兄/弟/姐/妹、堂/表、嫂/弟媳 等）。</summary>
    StepVsSelf,
    /// <summary>比较路径上某步人物与其上一步人物的年龄（用于 伯/叔、大舅子/小舅子 等）。</summary>
    StepVsPrevious,
}

/// <summary>
/// 一条称谓规则。Pattern 为规范化的单步序列；性别约束可选；
/// 若 AgeRule != None，则按 AgeStepIndex 指向的人物做年龄比较后取 older/younger/默认称谓。
/// </summary>
public sealed record KinshipRule(
    string Id,
    StepKind[] Pattern,
    Gender? SelfGender,
    Gender? TargetGender,
    AgeRule AgeRule,
    int AgeStepIndex,
    string? TermIfOlder,
    string? TermIfYounger,
    string Term);
