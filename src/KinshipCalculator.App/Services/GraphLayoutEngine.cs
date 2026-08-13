using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Services;

/// <summary>图谱节点布局结果。</summary>
public sealed record GraphNodeLayout(Person Person, string Term, double X, double Y);

/// <summary>图谱边。</summary>
public sealed record GraphEdgeLayout(string FromId, string ToId);

/// <summary>把家谱数据布局为分层树（父/母在上、子女在下、配偶/兄弟姐妹同行，「我」居中）。</summary>
public static class GraphLayoutEngine
{
    private const double XGap = 170;
    private const double YGap = 130;

    public static (IReadOnlyList<GraphNodeLayout> Nodes, IReadOnlyList<GraphEdgeLayout> Edges) Compute(
        FamilyData data,
        string? selfId,
        IReadOnlyList<KinshipResult> results)
    {
        if (string.IsNullOrEmpty(selfId))
            return (Array.Empty<GraphNodeLayout>(), Array.Empty<GraphEdgeLayout>());

        var graph = RelationshipGraph.Build(data);
        if (graph.GetNode(selfId) is null)
            return (Array.Empty<GraphNodeLayout>(), Array.Empty<GraphEdgeLayout>());

        var termByPerson = results.ToDictionary(r => r.PersonId, r => r.Term, StringComparer.Ordinal);

        // BFS 计算代数：父/母 -1，子女 +1，配偶/兄弟姐妹 0。
        var generation = new Dictionary<string, int>(StringComparer.Ordinal) { [selfId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(selfId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var node = graph.GetNode(id);
            if (node is null)
                continue;
            var g = generation[id];

            void Visit(Person? p, int dg)
            {
                if (p is not null && !generation.ContainsKey(p.Id))
                {
                    generation[p.Id] = dg;
                    queue.Enqueue(p.Id);
                }
            }

            Visit(node.Father, g - 1);
            Visit(node.Mother, g - 1);
            foreach (var c in node.Children) Visit(c, g + 1);
            Visit(node.Spouse, g);
            foreach (var s in node.Siblings) Visit(s, g);
        }

        // 未连通的孤立节点放到末行。
        foreach (var p in data.People)
        {
            if (!generation.ContainsKey(p.Id))
                generation[p.Id] = int.MaxValue;
        }

        var nodes = new List<GraphNodeLayout>();
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);

        var row = 0;
        foreach (var group in generation
                     .GroupBy(kv => kv.Value)
                     .OrderBy(g => g.Key == int.MaxValue ? 10_000 : g.Key))
        {
            var members = group
                .Select(kv => graph.GetNode(kv.Key)?.Person)
                .Where(p => p is not null)
                .Cast<Person>()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            var y = row * YGap;
            var totalWidth = (members.Count - 1) * XGap;
            for (var i = 0; i < members.Count; i++)
            {
                var x = i * XGap - totalWidth / 2.0;
                var term = termByPerson.GetValueOrDefault(members[i].Id, string.Empty);
                nodes.Add(new GraphNodeLayout(members[i], term, x, y));
                positions[members[i].Id] = (x, y);
            }
            row++;
        }

        var edges = new List<GraphEdgeLayout>();
        foreach (var node in graph.Nodes)
        {
            void AddEdge(Person? p)
            {
                if (p is not null && positions.ContainsKey(p.Id))
                    edges.Add(new GraphEdgeLayout(node.Person.Id, p.Id));
            }

            AddEdge(node.Father);
            AddEdge(node.Mother);
            AddEdge(node.Spouse);
            foreach (var s in node.Siblings) AddEdge(s);
            foreach (var c in node.Children) AddEdge(c);
        }

        return (nodes, edges);
    }
}
