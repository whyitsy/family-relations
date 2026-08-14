using System.Text.Json;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Serialization;

/// <summary>
/// 家谱数据的 JSON 序列化与导入规范化（AOT 安全：全程走源生成器上下文）。
/// 供文件存储与手动导入/导出共用。
/// </summary>
public static class FamilyDataSerializer
{
    public static string Serialize(FamilyData data)
        => JsonSerializer.Serialize(data, FamilyDataJsonContext.Default.FamilyData);

    public static FamilyData? Deserialize(string json)
        => JsonSerializer.Deserialize(json, FamilyDataJsonContext.Default.FamilyData);

    /// <summary>
    /// 尝试把文本解析为家谱数据。对「纯文本粘贴识别」友好：
    /// 去掉首尾空白与 BOM；若整段不是 JSON，尝试提取首个 <c>{</c> 到末个 <c>}</c> 之间的对象。
    /// 成功时返回规范化后的数据。
    /// </summary>
    public static bool TryDeserialize(string? json, out FamilyData? data, out string? error)
    {
        data = null;
        error = null;

        var text = (json ?? string.Empty).Trim().TrimStart('\uFEFF');
        if (text.Length == 0)
        {
            error = "内容为空";
            return false;
        }

        FamilyData? parsed;
        try
        {
            parsed = Deserialize(text);
        }
        catch (JsonException ex)
        {
            // 文本可能夹带说明文字：尝试剥离出最外层 JSON 对象。
            var extracted = ExtractJsonObject(text);
            if (extracted is null)
            {
                error = "无法识别为家族数据：" + ex.Message;
                return false;
            }

            try
            {
                parsed = Deserialize(extracted);
            }
            catch (Exception ex2)
            {
                error = "无法识别为家族数据：" + ex2.Message;
                return false;
            }
        }
        catch (Exception ex)
        {
            error = "导入失败：" + ex.Message;
            return false;
        }

        if (parsed is null)
        {
            error = "内容不是有效的家族数据";
            return false;
        }

        data = Normalize(parsed);
        return true;
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;
        return text.Substring(start, end - start + 1);
    }

    /// <summary>
    /// 规范化导入数据：补齐/去重人员 Id、修正非法枚举、丢弃悬空/自环/重复关系、校验「我」。
    /// 显式范围判断而不用 <see cref="Enum.IsDefined"/>，保证 Native AOT 安全。
    /// </summary>
    public static FamilyData Normalize(FamilyData data)
    {
        var people = data.People ?? new List<Person>();
        var relations = data.Relations ?? new List<RelationEdge>();

        // 1) 每人唯一非空 Id。
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in people)
        {
            if (string.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id))
            {
                string id;
                do { id = Guid.NewGuid().ToString("N"); } while (!ids.Add(id));
                p.Id = id;
            }
        }

        // 2) 性别非法值回落为 Unknown。
        foreach (var p in people)
        {
            if (p.Gender is not (Gender.Male or Gender.Female or Gender.Unknown))
                p.Gender = Gender.Unknown;
        }

        // 3) 关系过滤与规范化。
        var cleaned = new List<RelationEdge>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in relations)
        {
            if (r.Kind is not (RelationKind.Father or RelationKind.Mother or RelationKind.Spouse or RelationKind.Sibling))
                continue;
            if (string.IsNullOrEmpty(r.FromId) || string.IsNullOrEmpty(r.ToId))
                continue;
            if (r.FromId == r.ToId)
                continue;
            if (!ids.Contains(r.FromId) || !ids.Contains(r.ToId))
                continue;

            if (r.Kind == RelationKind.Sibling)
                r.SiblingKind = r.SiblingKind is SiblingKind.Full or SiblingKind.HalfPaternal or SiblingKind.HalfMaternal
                    ? r.SiblingKind
                    : SiblingKind.Full;
            else
                r.SiblingKind = null;

            if (string.IsNullOrWhiteSpace(r.Id))
                r.Id = Guid.NewGuid().ToString("N");

            var key = r.Kind == RelationKind.Sibling
                ? $"{r.FromId}|{r.ToId}|{(int)RelationKind.Sibling}|{(int)(r.SiblingKind ?? SiblingKind.Full)}"
                : $"{r.FromId}|{r.ToId}|{(int)r.Kind}";
            if (!seen.Add(key))
                continue;

            cleaned.Add(r);
        }

        // 4) 「我」必须指向存在的人。
        var selfId = data.SelfId;
        if (selfId is not null && !ids.Contains(selfId))
            selfId = null;

        return new FamilyData { People = people, Relations = cleaned, SelfId = selfId };
    }
}
