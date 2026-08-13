using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Serialization;
using Xunit;

namespace KinshipCalculator.Core.Tests;

public class SerializationTests
{
    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var b = new FamilyBuilder();
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 5, 5));
        b.Add("mom", Gender.Female);
        b.Father("me", "dad");
        b.Mother("me", "mom");
        b.Spouse("dad", "mom");
        b.SelfId = "me";
        var original = b.Build();

        var json = FamilyDataSerializer.Serialize(original);
        var restored = FamilyDataSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(3, restored!.People.Count);
        Assert.Equal(3, restored.Relations.Count);
        Assert.Equal("me", restored.SelfId);
        Assert.Equal(
            original.People.Select(p => p.Id).OrderBy(x => x),
            restored.People.Select(p => p.Id).OrderBy(x => x));
        Assert.Equal(Gender.Male, restored.People.Single(p => p.Id == "dad").Gender);
    }

    [Fact]
    public void Normalize_DropsDanglingAndSelfLoops()
    {
        var data = new FamilyData
        {
            People = new List<Person> { new() { Id = "a" }, new() { Id = "b" } },
            Relations = new List<RelationEdge>
            {
                new() { FromId = "a", ToId = "b", Kind = RelationKind.Father },
                new() { FromId = "a", ToId = "missing", Kind = RelationKind.Mother },
                new() { FromId = "a", ToId = "a", Kind = RelationKind.Spouse },
            },
            SelfId = "a",
        };

        var n = FamilyDataSerializer.Normalize(data);

        Assert.Single(n.Relations);
        Assert.Equal("a", n.Relations[0].FromId);
        Assert.Equal("b", n.Relations[0].ToId);
    }

    [Fact]
    public void Normalize_AssignsUniqueIdsAndFixesEnum()
    {
        var data = new FamilyData
        {
            People = new List<Person>
            {
                new() { Id = "", Gender = (Gender)99 },
                new() { Id = "", Gender = Gender.Female },
            },
            Relations = new List<RelationEdge>(),
            SelfId = "nope",
        };

        var n = FamilyDataSerializer.Normalize(data);

        Assert.Equal(2, n.People.Count);
        Assert.NotEqual(n.People[0].Id, n.People[1].Id);
        Assert.All(n.People, p => Assert.False(string.IsNullOrEmpty(p.Id)));
        Assert.Equal(Gender.Unknown, n.People[0].Gender);
        Assert.Null(n.SelfId);
    }

    [Fact]
    public void TryDeserialize_ExtractsJsonFromProse()
    {
        var b = new FamilyBuilder();
        b.Add("me", Gender.Male);
        b.SelfId = "me";
        var json = FamilyDataSerializer.Serialize(b.Build());
        var prose = "这是我的家族数据，下面开始：\n" + json + "\n以上就是全部。";

        var ok = FamilyDataSerializer.TryDeserialize(prose, out var data, out var error);

        Assert.True(ok, error);
        Assert.NotNull(data);
        Assert.Equal("me", data!.SelfId);
    }

    [Fact]
    public void TryDeserialize_RejectsGarbage()
    {
        var ok = FamilyDataSerializer.TryDeserialize("这不是 JSON", out var data, out var error);

        Assert.False(ok);
        Assert.Null(data);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryDeserialize_RejectsEmpty()
    {
        var ok = FamilyDataSerializer.TryDeserialize("   ", out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
