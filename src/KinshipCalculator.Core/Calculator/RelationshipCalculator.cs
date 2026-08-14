using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.Core.Calculator;

/// <summary>为图中除「我」外的每个人计算中文亲属称谓。</summary>
public sealed class RelationshipCalculator
{
    /// <summary>
    /// 计算全图称谓（按距离、姓名排序）。
    /// <paramref name="rules"/> 为空时使用内置普通话规则集。
    /// </summary>
    public IReadOnlyList<KinshipResult> ComputeAll(FamilyData data, IReadOnlyList<KinshipRule>? rules = null)
    {
        rules ??= BuiltInRuleSets.Mandarin.Rules;

        if (string.IsNullOrEmpty(data.SelfId))
            return Array.Empty<KinshipResult>();

        var graph = RelationshipGraph.Build(data);
        var selfNode = graph.GetNode(data.SelfId);
        if (selfNode is null)
            return Array.Empty<KinshipResult>();

        var self = selfNode.Person;
        var finder = new PathFinder(graph);

        var results = new List<KinshipResult>();
        foreach (var node in graph.Nodes)
        {
            if (node.Person.Id == self.Id)
                continue;
            results.Add(Compute(self, node.Person, graph, finder, rules));
        }

        return results
            .OrderBy(r => r.Distance)
            .ThenBy(r => r.PersonName, StringComparer.Ordinal)
            .ToList();
    }

    private static KinshipResult Compute(Person self, Person target, RelationshipGraph graph, PathFinder finder, IReadOnlyList<KinshipRule> rules)
    {
        var paths = finder.FindShortestPaths(self, target);
        if (paths.Count == 0)
        {
            return new KinshipResult
            {
                PersonId = target.Id,
                PersonName = target.Name,
                Term = "未知关系（关系较远，暂无标准称谓）",
                Distance = 0,
                PathDescription = null,
            };
        }

        var matches = new List<(string Term, bool NeedsBirthDate)>();
        PathStep[] firstPath = Array.Empty<PathStep>();

        foreach (var path in paths)
        {
            var arr = path as PathStep[] ?? path.ToArray();
            if (firstPath.Length == 0)
                firstPath = arr;

            if (TryMatch(self, target, arr, rules, out var term, out var needsBirthDate))
                matches.Add((term, needsBirthDate));
        }

        var distinct = matches.Select(m => m.Term).Distinct(StringComparer.Ordinal).ToList();

        if (distinct.Count == 0)
        {
            return new KinshipResult
            {
                PersonId = target.Id,
                PersonName = target.Name,
                Term = FallbackTerm(firstPath),
                Distance = firstPath.Length,
                PathDescription = Describe(firstPath),
            };
        }

        var candidates = distinct.Skip(1).ToList();
        return new KinshipResult
        {
            PersonId = target.Id,
            PersonName = target.Name,
            Term = distinct[0],
            IsAmbiguous = candidates.Count > 0,
            Candidates = candidates,
            Distance = firstPath.Length,
            NeedsBirthDate = matches.Any(m => m.NeedsBirthDate),
            PathDescription = Describe(firstPath),
        };
    }

    private static bool TryMatch(
        Person self,
        Person target,
        PathStep[] path,
        IReadOnlyList<KinshipRule> rules,
        out string term,
        out bool needsBirthDate)
    {
        foreach (var rule in rules)
        {
            var match = KinshipEngine.Match(rule, self, target, path);
            if (match is not null)
            {
                term = match.Term;
                needsBirthDate = match.NeedsBirthDate;
                return true;
            }
        }

        term = string.Empty;
        needsBirthDate = false;
        return false;
    }

    private static string FallbackTerm(PathStep[] path)
    {
        if (path.Length >= 4 && path.All(p => IsAncestorStep(p.Kind)))
            return $"第{path.Length}代祖先";
        if (path.Length >= 4 && path.All(p => IsDescendantStep(p.Kind)))
            return $"第{path.Length}代后代";
        return "未知关系（关系较远，暂无标准称谓）";
    }

    private static bool IsAncestorStep(StepKind k) => k is StepKind.Father or StepKind.Mother;
    private static bool IsDescendantStep(StepKind k) => k is StepKind.Son or StepKind.Daughter or StepKind.Child;

    private static string Describe(PathStep[] path)
    {
        var sb = new System.Text.StringBuilder("我");
        foreach (var step in path)
        {
            sb.Append("的");
            sb.Append(step.Kind switch
            {
                StepKind.Father => "父亲",
                StepKind.Mother => "母亲",
                StepKind.Son => "儿子",
                StepKind.Daughter => "女儿",
                StepKind.Spouse => "配偶",
                StepKind.Brother => "兄弟",
                StepKind.Sister => "姐妹",
                StepKind.Child => "孩子",
                StepKind.Sibling => "兄弟姐妹",
                _ => "?",
            });
        }
        return sb.ToString();
    }
}
