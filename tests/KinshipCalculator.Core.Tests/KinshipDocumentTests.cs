using System.Text.Json;
using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Rules;
using KinshipCalculator.Core.Serialization;
using Xunit;

namespace KinshipCalculator.Core.Tests;

public class KinshipDocumentTests
{
    [Fact]
    public void Document_RoundTrip_PreservesGraphsAndCurrent()
    {
        var g1 = new FamilyGraph { Name = "图谱A" };
        g1.Data.People.Add(new Person { Id = "me", Name = "我", Gender = Gender.Male });
        g1.Data.SelfId = "me";
        var g2 = new FamilyGraph { Name = "图谱B" };
        var doc = new KinshipDocument { Graphs = new List<FamilyGraph> { g1, g2 }, CurrentGraphId = g2.Id };

        var json = KinshipDocumentSerializer.Serialize(doc);
        var restored = KinshipDocumentSerializer.Normalize(KinshipDocumentSerializer.Deserialize(json)!);

        Assert.Equal(2, restored.Graphs.Count);
        Assert.Equal(g2.Id, restored.CurrentGraphId);
        Assert.Equal("图谱A", restored.Graphs[0].Name);
        Assert.Single(restored.Graphs[0].Data.People);
    }

    [Fact]
    public void TryParseDocument_RejectsSingleFamilyData()
    {
        var b = new FamilyBuilder();
        b.Add("me", Gender.Male);
        b.SelfId = "me";
        var json = FamilyDataSerializer.Serialize(b.Build());

        var ok = KinshipDocumentSerializer.TryParseDocument(json, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void Wrap_MigratesSingleToDocument()
    {
        var b = new FamilyBuilder();
        b.Add("me", Gender.Male);
        b.SelfId = "me";

        var doc = KinshipDocumentSerializer.Wrap(b.Build());

        Assert.Single(doc.Graphs);
        Assert.Equal("me", doc.Graphs[0].Data.SelfId);
        Assert.Equal(doc.Graphs[0].Id, doc.CurrentGraphId);
    }

    [Fact]
    public void CustomRuleSet_ChangesTerm()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Father("me", "dad");

        var custom = new KinshipRuleSet
        {
            Id = "custom",
            Name = "自定义",
            Rules = new List<KinshipRule>
            {
                new("爸爸", new[] { StepKind.Father }, null, null, AgeRule.None, 0, null, null, "老豆"),
            },
        };

        var results = new RelationshipCalculator().ComputeAll(b.Build(), custom.Rules);
        Assert.Equal("老豆", results.Single(r => r.PersonId == "dad").Term);
    }

    [Fact]
    public void RuleSet_RoundTrip_PreservesRules()
    {
        var set = BuiltInRuleSets.Mandarin;

        var json = JsonSerializer.Serialize(set, FamilyDataJsonContext.Default.KinshipRuleSet);
        var restored = JsonSerializer.Deserialize(json, FamilyDataJsonContext.Default.KinshipRuleSet);

        Assert.NotNull(restored);
        Assert.Equal(set.Id, restored!.Id);
        Assert.Equal(set.Rules.Count, restored.Rules.Count);
        Assert.Equal(set.Rules[0].Term, restored.Rules[0].Term);
    }

    [Fact]
    public void Cantonese_ChangesFatherTerm()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Father("me", "dad");

        var results = new RelationshipCalculator().ComputeAll(b.Build(), BuiltInRuleSets.Cantonese.Rules);

        Assert.Equal("老豆", results.Single(r => r.PersonId == "dad").Term);
    }

    [Fact]
    public void Resolve_CustomSet_TakesPrecedence()
    {
        var custom = new KinshipRuleSet
        {
            Id = "mycustom",
            Name = "自定义",
            Rules = new List<KinshipRule>
            {
                new("爸爸", new[] { StepKind.Father }, null, null, AgeRule.None, 0, null, null, "老豆"),
            },
        };

        var rules = BuiltInRuleSets.Resolve("mycustom", new[] { custom });

        Assert.Equal("老豆", rules.Single(r => r.Id == "爸爸").Term);
    }

    [Fact]
    public void Resolve_UnknownId_FallsBackToMandarin()
    {
        var rules = BuiltInRuleSets.Resolve("does-not-exist", null);

        Assert.Equal("爸爸", rules.Single(r => r.Id == "爸爸").Term);
    }

    [Fact]
    public void Normalize_KeepsCustomRuleSets_AndRenamesBuiltInCollision()
    {
        var doc = new KinshipDocument
        {
            Graphs = new List<FamilyGraph> { new FamilyGraph { RuleSetId = "mycustom" } },
            RuleSets = new List<KinshipRuleSet>
            {
                new KinshipRuleSet { Id = "mandarin", Name = "冲突", Rules = new List<KinshipRule>() },
                new KinshipRuleSet { Id = "mycustom", Name = "我的", Rules = new List<KinshipRule>() },
            },
        };

        var n = KinshipDocumentSerializer.Normalize(doc);

        Assert.Equal(2, n.RuleSets.Count);
        Assert.DoesNotContain(n.RuleSets, s => s.Id == "mandarin");
        Assert.Contains(n.RuleSets, s => s.Id == "mycustom");
        Assert.Equal("mycustom", n.Graphs[0].RuleSetId); // 指向自定义集，未被回落
    }
}
