using System.Text.Json.Serialization;
using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;

namespace KinshipCalculator.Core.Serialization;

/// <summary>FamilyData / KinshipDocument 的 System.Text.Json 源生成器上下文（Native AOT 必需，禁止反射式序列化）。</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FamilyData))]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(RelationEdge))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(List<RelationEdge>))]
[JsonSerializable(typeof(KinshipDocument))]
[JsonSerializable(typeof(FamilyGraph))]
[JsonSerializable(typeof(List<FamilyGraph>))]
[JsonSerializable(typeof(KinshipRuleSet))]
[JsonSerializable(typeof(KinshipRule))]
[JsonSerializable(typeof(List<KinshipRule>))]
[JsonSerializable(typeof(StepKind[]))]
public partial class FamilyDataJsonContext : JsonSerializerContext
{
}
