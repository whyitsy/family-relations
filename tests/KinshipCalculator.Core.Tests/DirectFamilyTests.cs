using KinshipCalculator.Core.Models;
using Xunit;

namespace KinshipCalculator.Core.Tests;

public class DirectFamilyTests
{
    [Fact]
    public void 直系长辈()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("papa", Gender.Male, new DateTime(1930, 1, 1));  // 父之父
        b.Add("nana", Gender.Female, new DateTime(1932, 1, 1)); // 父之母
        b.Add("gpa", Gender.Male, new DateTime(1935, 1, 1));   // 母之父
        b.Add("gma", Gender.Female, new DateTime(1937, 1, 1)); // 母之母
        b.Father("me", "dad");
        b.Mother("me", "mom");
        b.Father("dad", "papa");
        b.Mother("dad", "nana");
        b.Father("mom", "gpa");
        b.Mother("mom", "gma");

        Assert.Equal("爸爸", b.Term("dad"));
        Assert.Equal("妈妈", b.Term("mom"));
        Assert.Equal("爷爷", b.Term("papa"));
        Assert.Equal("奶奶", b.Term("nana"));
        Assert.Equal("外公", b.Term("gpa"));
        Assert.Equal("外婆", b.Term("gma"));
    }

    [Fact]
    public void 曾祖辈()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(2000, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1970, 1, 1));
        b.Add("gdad", Gender.Male, new DateTime(1940, 1, 1));
        b.Add("ggdad", Gender.Male, new DateTime(1910, 1, 1)); // 父之父之父
        b.Add("ggmom", Gender.Female, new DateTime(1912, 1, 1)); // 父之父之母
        b.Father("me", "dad");
        b.Father("dad", "gdad");
        b.Father("gdad", "ggdad");
        b.Mother("gdad", "ggmom");

        Assert.Equal("曾祖父", b.Term("ggdad"));
        Assert.Equal("曾祖母", b.Term("ggmom"));
    }

    [Fact]
    public void 直系晚辈()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("son", Gender.Male, new DateTime(2015, 1, 1));
        b.Add("daughter", Gender.Female, new DateTime(2018, 1, 1));
        b.Add("grandson", Gender.Male, new DateTime(2040, 1, 1));
        b.Add("granddaughter", Gender.Female, new DateTime(2042, 1, 1));
        b.Add("outgrandson", Gender.Male, new DateTime(2044, 1, 1));
        b.Father("son", "me");
        b.Father("daughter", "me");
        b.Father("grandson", "son");
        b.Father("granddaughter", "son");
        b.Father("outgrandson", "daughter");

        Assert.Equal("儿子", b.Term("son"));
        Assert.Equal("女儿", b.Term("daughter"));
        Assert.Equal("孙子", b.Term("grandson"));
        Assert.Equal("孙女", b.Term("granddaughter"));
        Assert.Equal("外孙", b.Term("outgrandson"));
    }

    [Fact]
    public void 兄弟姐妹按长幼()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 6, 1));
        b.Add("elderBro", Gender.Male, new DateTime(1985, 1, 1));
        b.Add("youngerBro", Gender.Male, new DateTime(1995, 1, 1));
        b.Add("elderSis", Gender.Female, new DateTime(1987, 1, 1));
        b.Add("youngerSis", Gender.Female, new DateTime(1998, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Father("me", "dad"); b.Mother("me", "mom");
        b.Father("elderBro", "dad"); b.Mother("elderBro", "mom");
        b.Father("youngerBro", "dad"); b.Mother("youngerBro", "mom");
        b.Father("elderSis", "dad"); b.Mother("elderSis", "mom");
        b.Father("youngerSis", "dad"); b.Mother("youngerSis", "mom");

        Assert.Equal("哥哥", b.Term("elderBro"));
        Assert.Equal("弟弟", b.Term("youngerBro"));
        Assert.Equal("姐姐", b.Term("elderSis"));
        Assert.Equal("妹妹", b.Term("youngerSis"));
    }

    [Fact]
    public void 兄弟姐妹缺生日则用通用称谓并标记()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 6, 1));
        b.Add("bro", Gender.Male); // 无生日
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Father("me", "dad"); b.Mother("me", "mom");
        b.Father("bro", "dad"); b.Mother("bro", "mom");

        var r = b.Result("bro");
        Assert.Equal("哥哥/弟弟", r.Term);
        Assert.True(r.NeedsBirthDate);
    }

    [Fact]
    public void 半同胞按兄弟姐妹处理()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 6, 1));
        b.Add("halfBro", Gender.Male, new DateTime(1985, 1, 1)); // 同父异母
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("stepmom", Gender.Female, new DateTime(1965, 1, 1));
        b.Father("me", "dad"); b.Mother("me", "mom");
        b.Father("halfBro", "dad"); b.Mother("halfBro", "stepmom");

        Assert.Equal("哥哥", b.Term("halfBro"));
    }
}
