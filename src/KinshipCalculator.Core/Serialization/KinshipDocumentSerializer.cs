using System.Text.Json;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.Core.Serialization;

/// <summary>多图谱文档的 JSON 序列化与规范化（AOT 安全：走源生成器上下文）。</summary>
public static class KinshipDocumentSerializer
{
    public static string Serialize(KinshipDocument doc)
        => JsonSerializer.Serialize(doc, FamilyDataJsonContext.Default.KinshipDocument);

    public static KinshipDocument? Deserialize(string json)
        => JsonSerializer.Deserialize(json, FamilyDataJsonContext.Default.KinshipDocument);

    /// <summary>新建一个空文档（含一个默认图谱）。</summary>
    public static KinshipDocument CreateDefault()
    {
        var graph = new FamilyGraph();
        return new KinshipDocument { Graphs = new List<FamilyGraph> { graph }, CurrentGraphId = graph.Id };
    }

    /// <summary>把单个 <see cref="FamilyData"/>（旧格式/单图导出）包成一个文档。</summary>
    public static KinshipDocument Wrap(FamilyData data)
    {
        var normalized = FamilyDataSerializer.Normalize(data);
        var graph = new FamilyGraph { Data = normalized };
        return new KinshipDocument { Graphs = new List<FamilyGraph> { graph }, CurrentGraphId = graph.Id };
    }

    /// <summary>
    /// 只把文本解析为「多图谱文档」（顶层含 graphs 数组）；旧单图格式返回 false。
    /// 对「纯文本粘贴」友好：去空白/BOM，必要时提取首个 <c>{</c> 到末个 <c>}</c> 之间的对象。
    /// </summary>
    public static bool TryParseDocument(string? json, out KinshipDocument? doc, out string? error)
    {
        doc = null;
        error = null;

        var text = (json ?? string.Empty).Trim().TrimStart('\uFEFF');
        if (text.Length == 0)
        {
            error = "内容为空";
            return false;
        }

        var parsed = TryDeserializeDocument(text);
        if (parsed is { Graphs.Count: > 0 })
        {
            doc = Normalize(parsed);
            return true;
        }

        var extracted = ExtractJsonObject(text);
        if (extracted is not null)
        {
            parsed = TryDeserializeDocument(extracted);
            if (parsed is { Graphs.Count: > 0 })
            {
                doc = Normalize(parsed);
                return true;
            }
        }

        error = "内容不是多图谱文档";
        return false;
    }

    /// <summary>
    /// 规范化文档：补齐/去重图谱与规则集 Id、规范化每个图谱的数据、校验规则集与当前图谱指针。
    /// </summary>
    public static KinshipDocument Normalize(KinshipDocument doc)
    {
        var graphs = doc.Graphs ?? new List<FamilyGraph>();

        // 1) 规范化自定义规则集：唯一且不与内置预设冲突的 Id。
        var ruleSets = doc.RuleSets ?? new List<KinshipRuleSet>();
        var builtInIds = new HashSet<string>(BuiltInRuleSets.All.Select(s => s.Id), StringComparer.Ordinal);
        var ruleSetIds = new HashSet<string>(builtInIds, StringComparer.Ordinal);
        var cleanedRuleSets = new List<KinshipRuleSet>();
        foreach (var rs in ruleSets)
        {
            if (string.IsNullOrWhiteSpace(rs.Id) || builtInIds.Contains(rs.Id) || !ruleSetIds.Add(rs.Id))
            {
                string id;
                do { id = Guid.NewGuid().ToString("N"); } while (!ruleSetIds.Add(id));
                rs.Id = id;
            }

            if (string.IsNullOrWhiteSpace(rs.Name))
                rs.Name = "自定义规则";

            rs.Rules ??= new List<KinshipRule>();
            cleanedRuleSets.Add(rs);
        }

        // 2) 规范化图谱。
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in graphs)
        {
            if (string.IsNullOrWhiteSpace(g.Id) || !ids.Add(g.Id))
            {
                string id;
                do { id = Guid.NewGuid().ToString("N"); } while (!ids.Add(id));
                g.Id = id;
            }

            if (string.IsNullOrWhiteSpace(g.Name))
                g.Name = "未命名图谱";

            if (BuiltInRuleSets.FindSet(g.RuleSetId, cleanedRuleSets) is null)
                g.RuleSetId = BuiltInRuleSets.MandarinId;

            g.Data = FamilyDataSerializer.Normalize(g.Data ?? new FamilyData());
        }

        string? current = doc.CurrentGraphId;
        if (current is null || !ids.Contains(current))
            current = graphs.Count > 0 ? graphs[0].Id : null;

        return new KinshipDocument { Graphs = graphs, RuleSets = cleanedRuleSets, CurrentGraphId = current };
    }

    private static KinshipDocument? TryDeserializeDocument(string text)
    {
        try
        {
            return Deserialize(text);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;
        return text.Substring(start, end - start + 1);
    }
}
