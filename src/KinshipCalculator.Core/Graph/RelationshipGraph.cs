using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Graph;

/// <summary>
/// 由 <see cref="FamilyData"/> 构建的只读内存邻接索引。
/// 规范化：配偶/兄弟姐妹双向对称；子女由 Father/Mother 反向推导；
/// 兄弟姐妹除显式边外，还按共同父母自动推导（同父/同母即视为兄弟姐妹）。
/// </summary>
public sealed class RelationshipGraph
{
    private readonly Dictionary<string, PersonNode> _nodes;

    /// <summary>构建过程中发现的数据问题（非致命）。</summary>
    public IReadOnlyList<string> ValidationIssues { get; }

    private RelationshipGraph(Dictionary<string, PersonNode> nodes, IReadOnlyList<string> issues)
    {
        _nodes = nodes;
        ValidationIssues = issues;
    }

    public static RelationshipGraph Build(FamilyData data)
    {
        var issues = new List<string>();
        var nodes = new Dictionary<string, PersonNode>(StringComparer.Ordinal);

        foreach (var p in data.People)
        {
            if (string.IsNullOrWhiteSpace(p.Id))
            {
                issues.Add("存在 Id 为空的人员，已跳过。");
                continue;
            }
            if (nodes.ContainsKey(p.Id))
            {
                issues.Add($"人员 Id 重复：{p.Id}，已跳过重复项。");
                continue;
            }
            nodes[p.Id] = new PersonNode { Person = p };
        }

        PersonNode? Get(string? id)
            => string.IsNullOrEmpty(id) ? null : nodes.GetValueOrDefault(id);

        foreach (var e in data.Relations)
        {
            var from = Get(e.FromId);
            var to = Get(e.ToId);
            if (from is null || to is null)
            {
                issues.Add($"关系边引用了不存在的人员：{e.FromId} -> {e.ToId}（已跳过）。");
                continue;
            }
            if (e.FromId == e.ToId)
            {
                issues.Add($"关系边自环已忽略：{e.FromId}。");
                continue;
            }

            switch (e.Kind)
            {
                case RelationKind.Father:
                    if (from.Father is not null && from.Father.Id != to.Person.Id)
                        issues.Add($"{from.Person.Name} 存在多个父亲，保留第一个。");
                    else
                        from.Father = to.Person;
                    break;

                case RelationKind.Mother:
                    if (from.Mother is not null && from.Mother.Id != to.Person.Id)
                        issues.Add($"{from.Person.Name} 存在多个母亲，保留第一个。");
                    else
                        from.Mother = to.Person;
                    break;

                case RelationKind.Spouse:
                    from.Spouse ??= to.Person;
                    to.Spouse ??= from.Person;
                    break;

                case RelationKind.Sibling:
                    AddSibling(from, to.Person);
                    AddSibling(to, from.Person);
                    break;
            }
        }

        // 由共同父母推导兄弟姐妹（未显式声明时）。
        DeriveSiblingsFromParents(nodes);

        // 由 Father/Mother 反向推导子女。
        foreach (var node in nodes.Values)
        {
            if (node.Father is { } f && Get(f.Id) is { } fn)
                AddChild(fn, node.Person);
            if (node.Mother is { } m && Get(m.Id) is { } mn)
                AddChild(mn, node.Person);
        }

        return new RelationshipGraph(nodes, issues);
    }

    private static void AddSibling(PersonNode node, Person sibling)
    {
        if (!node.Siblings.Any(s => s.Id == sibling.Id))
            node.Siblings.Add(sibling);
    }

    private static void AddChild(PersonNode parent, Person child)
    {
        if (!parent.Children.Any(c => c.Id == child.Id))
            parent.Children.Add(child);
    }

    private static void DeriveSiblingsFromParents(Dictionary<string, PersonNode> nodes)
    {
        var ids = nodes.Keys.ToList();
        for (var i = 0; i < ids.Count; i++)
        {
            for (var j = i + 1; j < ids.Count; j++)
            {
                var a = nodes[ids[i]];
                var b = nodes[ids[j]];

                var shareFather = a.Father is not null && b.Father is not null && a.Father.Id == b.Father.Id;
                var shareMother = a.Mother is not null && b.Mother is not null && a.Mother.Id == b.Mother.Id;

                if (shareFather || shareMother)
                {
                    AddSibling(a, b.Person);
                    AddSibling(b, a.Person);
                }
            }
        }
    }

    public PersonNode? GetNode(string? id)
        => string.IsNullOrEmpty(id) ? null : _nodes.GetValueOrDefault(id);

    public IEnumerable<PersonNode> Nodes => _nodes.Values;
}
