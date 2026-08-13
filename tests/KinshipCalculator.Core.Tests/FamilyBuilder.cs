using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Tests;

/// <summary>便捷构建家谱数据与断言的辅助类。</summary>
internal sealed class FamilyBuilder
{
    public List<Person> People { get; } = new();
    public List<RelationEdge> Relations { get; } = new();
    public string? SelfId { get; set; }

    public Person Add(string id, Gender g, DateTime? birth = null)
    {
        var p = new Person { Id = id, Name = id, Gender = g, BirthDate = birth };
        People.Add(p);
        return p;
    }

    public void Father(string child, string father)
        => Relations.Add(new RelationEdge { FromId = child, ToId = father, Kind = RelationKind.Father });

    public void Mother(string child, string mother)
        => Relations.Add(new RelationEdge { FromId = child, ToId = mother, Kind = RelationKind.Mother });

    public void Spouse(string a, string b)
        => Relations.Add(new RelationEdge { FromId = a, ToId = b, Kind = RelationKind.Spouse });

    public void Sibling(string a, string b, SiblingKind kind = SiblingKind.Full)
        => Relations.Add(new RelationEdge { FromId = a, ToId = b, Kind = RelationKind.Sibling, SiblingKind = kind });

    public FamilyData Build() => new() { People = People, Relations = Relations, SelfId = SelfId };

    public IReadOnlyList<KinshipResult> Results()
        => new RelationshipCalculator().ComputeAll(Build());

    public KinshipResult Result(string targetId)
        => Results().First(r => r.PersonId == targetId);

    public string Term(string targetId)
        => Result(targetId).Term;
}
