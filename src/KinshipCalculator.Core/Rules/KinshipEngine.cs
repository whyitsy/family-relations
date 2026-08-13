using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Rules;

/// <summary>纯数据的规则匹配器（AOT 安全：无反射、无动态代码）。</summary>
public static class KinshipEngine
{
    public sealed record MatchResult(string RuleId, string Term, bool NeedsBirthDate);

    /// <summary>尝试用规则匹配一条路径；不匹配返回 null。</summary>
    public static MatchResult? Match(KinshipRule rule, Person self, Person target, PathStep[] path)
    {
        if (rule.Pattern.Length != path.Length)
            return null;

        for (var i = 0; i < path.Length; i++)
        {
            if (rule.Pattern[i] != path[i].Kind)
                return null;
        }

        if (rule.SelfGender is { } sg && self.Gender != sg)
            return null;
        if (rule.TargetGender is { } tg && target.Gender != tg)
            return null;

        var term = rule.Term;
        var needsBirthDate = false;

        if (rule.AgeRule != AgeRule.None)
        {
            // AgeStepIndex 是「步」索引；该步到达的人物是 path[AgeStepIndex].Person。
            var subject = path[rule.AgeStepIndex].Person;
            var cmp = rule.AgeRule == AgeRule.StepVsSelf
                ? CompareAge(subject, self)
                : CompareAge(subject, rule.AgeStepIndex == 0 ? self : path[rule.AgeStepIndex - 1].Person);

            if (cmp > 0)
                term = rule.TermIfOlder ?? rule.Term;
            else if (cmp < 0)
                term = rule.TermIfYounger ?? rule.Term;
            else
                needsBirthDate = true; // 缺生日：保留通用称谓并标记
        }

        return new MatchResult(rule.Id, term, needsBirthDate);
    }

    /// <summary>返回 1 表示 a 比 b 年长，-1 表示 a 比 b 年幼，0 表示无法判定（缺生日/同日）。</summary>
    private static int CompareAge(Person a, Person b)
    {
        if (a.BirthDate is null || b.BirthDate is null)
            return 0;
        var ab = a.BirthDate.Value;
        var bb = b.BirthDate.Value;
        if (ab < bb) return 1;
        if (ab > bb) return -1;
        return 0;
    }
}
