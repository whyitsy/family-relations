using System.Text.Json.Serialization;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Services;

/// <summary>System.Text.Json 源生成器上下文（Native AOT 必需，禁止反射式序列化）。</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FamilyData))]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(RelationEdge))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(List<RelationEdge>))]
public partial class FamilyDataJsonContext : JsonSerializerContext
{
}
