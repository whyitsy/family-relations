using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Calculator;

/// <summary>在图内寻找「我」到目标的最短简单路径（限定最大深度，且每步显式携带关系种类）。</summary>
public sealed class PathFinder
{
    public const int MaxDepth = 8;

    private readonly RelationshipGraph _graph;

    public PathFinder(RelationshipGraph graph) => _graph = graph;

    /// <summary>
    /// 返回从 self 到 target 的所有最短简单路径（每步含关系种类与到达的人）。
    /// 无路径或超出最大深度时返回空列表。
    /// </summary>
    public IReadOnlyList<IReadOnlyList<PathStep>> FindShortestPaths(Person self, Person target)
    {
        if (self.Id == target.Id)
            return Array.Empty<IReadOnlyList<PathStep>>();

        var dist = BfsDistances(self);
        if (!dist.TryGetValue(target.Id, out var min) || min > MaxDepth)
            return Array.Empty<IReadOnlyList<PathStep>>();

        var results = new List<IReadOnlyList<PathStep>>();
        var stack = new List<PathStep>();
        var visited = new HashSet<string>(StringComparer.Ordinal) { self.Id };
        const int cap = 64; // 每个目标最多保留的最短路径数，防爆
        Enumerate(self, target, min, stack, visited, results, cap);
        return results;
    }

    private Dictionary<string, int> BfsDistances(Person self)
    {
        var dist = new Dictionary<string, int>(StringComparer.Ordinal) { [self.Id] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(self.Id);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var d = dist[id];
            if (d >= MaxDepth)
                continue;

            foreach (var (_, nbId) in Neighbors(id))
            {
                if (dist.ContainsKey(nbId))
                    continue;
                dist[nbId] = d + 1;
                queue.Enqueue(nbId);
            }
        }
        return dist;
    }

    private void Enumerate(
        Person cur,
        Person target,
        int maxLen,
        List<PathStep> stack,
        HashSet<string> visited,
        List<IReadOnlyList<PathStep>> results,
        int cap)
    {
        if (results.Count >= cap)
            return;

        if (stack.Count == maxLen)
        {
            if (cur.Id == target.Id)
                results.Add(stack.ToArray());
            return;
        }

        foreach (var (kind, nbId) in Neighbors(cur.Id))
        {
            if (visited.Contains(nbId))
                continue;

            var nb = _graph.GetNode(nbId)!.Person;
            visited.Add(nbId);
            stack.Add(new PathStep(kind, nb));

            Enumerate(nb, target, maxLen, stack, visited, results, cap);

            stack.RemoveAt(stack.Count - 1);
            visited.Remove(nbId);

            if (results.Count >= cap)
                return;
        }
    }

    private IEnumerable<(StepKind Kind, string Id)> Neighbors(string id)
    {
        var node = _graph.GetNode(id);
        if (node is null)
            yield break;

        if (node.Father is { } f) yield return (StepKind.Father, f.Id);
        if (node.Mother is { } m) yield return (StepKind.Mother, m.Id);
        if (node.Spouse is { } s) yield return (StepKind.Spouse, s.Id);
        foreach (var sib in node.Siblings) yield return (SiblingStep(sib.Gender), sib.Id);
        foreach (var c in node.Children) yield return (ChildStep(c.Gender), c.Id);
    }

    private static StepKind SiblingStep(Gender g) => g switch
    {
        Gender.Male => StepKind.Brother,
        Gender.Female => StepKind.Sister,
        _ => StepKind.Sibling,
    };

    private static StepKind ChildStep(Gender g) => g switch
    {
        Gender.Male => StepKind.Son,
        Gender.Female => StepKind.Daughter,
        _ => StepKind.Child,
    };
}
