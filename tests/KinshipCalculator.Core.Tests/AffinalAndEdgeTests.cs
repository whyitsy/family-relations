using KinshipCalculator.Core.Models;
using Xunit;

namespace KinshipCalculator.Core.Tests;

public class AffinalAndEdgeTests
{
    [Fact]
    public void 配偶称谓()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("wife", Gender.Female, new DateTime(1991, 1, 1));
        b.Spouse("me", "wife");

        Assert.Equal("妻子", b.Term("wife"));
    }

    [Fact]
    public void 配偶的父母按我的性别区分()
    {
        // 我为男 → 岳父/岳母
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("wife", Gender.Female, new DateTime(1991, 1, 1));
        b.Add("wifedad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("wifemom", Gender.Female, new DateTime(1962, 1, 1));
        b.Spouse("me", "wife");
        b.Father("wife", "wifedad");
        b.Mother("wife", "wifemom");
        Assert.Equal("岳父", b.Term("wifedad"));
        Assert.Equal("岳母", b.Term("wifemom"));

        // 我为女 → 公公/婆婆
        var b2 = new FamilyBuilder { SelfId = "me" };
        b2.Add("me", Gender.Female, new DateTime(1990, 1, 1));
        b2.Add("husband", Gender.Male, new DateTime(1989, 1, 1));
        b2.Add("husdad", Gender.Male, new DateTime(1960, 1, 1));
        b2.Add("husmom", Gender.Female, new DateTime(1962, 1, 1));
        b2.Spouse("me", "husband");
        b2.Father("husband", "husdad");
        b2.Mother("husband", "husmom");
        Assert.Equal("公公", b2.Term("husdad"));
        Assert.Equal("婆婆", b2.Term("husmom"));
    }

    [Fact]
    public void 配偶的兄弟姐妹()
    {
        // 我为男：妻子的哥哥/弟弟 → 大舅子/小舅子；姐姐/妹妹 → 大姨子/小姨子
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("wife", Gender.Female, new DateTime(1991, 6, 1));
        b.Add("wifedad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("wifemom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("bigBro", Gender.Male, new DateTime(1985, 1, 1));  // 妻子之兄 → 大舅子
        b.Add("littleBro", Gender.Male, new DateTime(1995, 1, 1)); // 小舅子
        b.Add("bigSis", Gender.Female, new DateTime(1987, 1, 1)); // 大姨子
        b.Add("littleSis", Gender.Female, new DateTime(1998, 1, 1)); // 小姨子
        b.Spouse("me", "wife");
        b.Father("wife", "wifedad"); b.Mother("wife", "wifemom");
        b.Father("bigBro", "wifedad"); b.Mother("bigBro", "wifemom");
        b.Father("littleBro", "wifedad"); b.Mother("littleBro", "wifemom");
        b.Father("bigSis", "wifedad"); b.Mother("bigSis", "wifemom");
        b.Father("littleSis", "wifedad"); b.Mother("littleSis", "wifemom");

        Assert.Equal("大舅子", b.Term("bigBro"));
        Assert.Equal("小舅子", b.Term("littleBro"));
        Assert.Equal("大姨子", b.Term("bigSis"));
        Assert.Equal("小姨子", b.Term("littleSis"));

        // 我为女：丈夫的哥哥/弟弟 → 大伯子/小叔子
        var b2 = new FamilyBuilder { SelfId = "me" };
        b2.Add("me", Gender.Female, new DateTime(1990, 1, 1));
        b2.Add("husband", Gender.Male, new DateTime(1989, 6, 1));
        b2.Add("husdad", Gender.Male, new DateTime(1960, 1, 1));
        b2.Add("husmom", Gender.Female, new DateTime(1962, 1, 1));
        b2.Add("dabBo", Gender.Male, new DateTime(1985, 1, 1));  // 丈夫之兄 → 大伯子
        b2.Add("xiaoShu", Gender.Male, new DateTime(1995, 1, 1)); // 小叔子
        b2.Add("daGu", Gender.Female, new DateTime(1987, 1, 1));  // 大姑子
        b2.Add("xiaoGu", Gender.Female, new DateTime(1998, 1, 1)); // 小姑子
        b2.Spouse("me", "husband");
        b2.Father("husband", "husdad"); b2.Mother("husband", "husmom");
        b2.Father("dabBo", "husdad"); b2.Mother("dabBo", "husmom");
        b2.Father("xiaoShu", "husdad"); b2.Mother("xiaoShu", "husmom");
        b2.Father("daGu", "husdad"); b2.Mother("daGu", "husmom");
        b2.Father("xiaoGu", "husdad"); b2.Mother("xiaoGu", "husmom");

        Assert.Equal("大伯子", b2.Term("dabBo"));
        Assert.Equal("小叔子", b2.Term("xiaoShu"));
        Assert.Equal("大姑子", b2.Term("daGu"));
        Assert.Equal("小姑子", b2.Term("xiaoGu"));
    }

    [Fact]
    public void 子女的配偶()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("son", Gender.Male, new DateTime(2015, 1, 1));
        b.Add("daughter", Gender.Female, new DateTime(2018, 1, 1));
        b.Add("daughterInLaw", Gender.Female, new DateTime(2014, 1, 1));
        b.Add("sonInLaw", Gender.Male, new DateTime(2013, 1, 1));
        b.Father("son", "me");
        b.Father("daughter", "me");
        b.Spouse("son", "daughterInLaw");
        b.Spouse("daughter", "sonInLaw");

        Assert.Equal("儿媳", b.Term("daughterInLaw"));
        Assert.Equal("女婿", b.Term("sonInLaw"));
    }

    [Fact]
    public void 未指定自我时返回空()
    {
        var b = new FamilyBuilder();
        b.Add("a", Gender.Male);
        b.Add("b", Gender.Male);
        b.Father("a", "b");

        Assert.Empty(b.Results());
    }

    [Fact]
    public void 自我不计入结果()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male);
        b.Add("dad", Gender.Male);
        b.Father("me", "dad");

        Assert.DoesNotContain(b.Results(), r => r.PersonId == "me");
    }

    [Fact]
    public void 无法判定时回退未知关系()
    {
        // 配偶的父亲的兄弟的妻子 —— 规则库未覆盖（[S,F,B,S]），应回退
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("wife", Gender.Female, new DateTime(1991, 1, 1));
        b.Add("wifedad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("gf", Gender.Male, new DateTime(1930, 1, 1));
        b.Add("bro", Gender.Male, new DateTime(1955, 1, 1));
        b.Add("browife", Gender.Female, new DateTime(1956, 1, 1));
        b.Spouse("me", "wife");
        b.Father("wife", "wifedad");
        b.Father("wifedad", "gf"); b.Father("bro", "gf");
        b.Spouse("bro", "browife");

        var term = b.Term("browife");
        Assert.Contains("未知关系", term);
    }

    [Fact]
    public void 多重关系返回歧义与候选()
    {
        // 同一目标既是我配偶又是我姐妹（两条等长路径 → 歧义）
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("a", Gender.Female, new DateTime(1991, 1, 1));
        b.Spouse("me", "a");
        b.Sibling("me", "a");

        var r = b.Result("a");
        Assert.True(r.IsAmbiguous);
        Assert.NotEmpty(r.Candidates);
    }
}
