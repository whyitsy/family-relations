using KinshipCalculator.Core.Models;
using Xunit;

namespace KinshipCalculator.Core.Tests;

public class CollateralTests
{
    [Fact]
    public void 父系旁系长辈()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("gdad", Gender.Male, new DateTime(1930, 1, 1));
        b.Add("bo", Gender.Male, new DateTime(1955, 1, 1));    // 父之兄（伯父）
        b.Add("shu", Gender.Male, new DateTime(1965, 1, 1));   // 父之弟（叔叔）
        b.Add("gu", Gender.Female, new DateTime(1958, 1, 1));  // 父之妹（姑姑）
        b.Add("bomu", Gender.Female, new DateTime(1956, 1, 1)); // 伯母
        b.Add("shenshen", Gender.Female, new DateTime(1966, 1, 1)); // 婶婶
        b.Add("gufu", Gender.Male, new DateTime(1957, 1, 1));  // 姑父
        b.Father("me", "dad");
        b.Father("dad", "gdad");
        b.Father("bo", "gdad"); b.Father("shu", "gdad"); b.Father("gu", "gdad");
        b.Spouse("bo", "bomu");
        b.Spouse("shu", "shenshen");
        b.Spouse("gu", "gufu");

        Assert.Equal("伯父", b.Term("bo"));
        Assert.Equal("叔叔", b.Term("shu"));
        Assert.Equal("姑姑", b.Term("gu"));
        Assert.Equal("伯母", b.Term("bomu"));
        Assert.Equal("婶婶", b.Term("shenshen"));
        Assert.Equal("姑父", b.Term("gufu"));
    }

    [Fact]
    public void 母系旁系长辈()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("gma", Gender.Female, new DateTime(1935, 1, 1));
        b.Add("jiu", Gender.Male, new DateTime(1960, 1, 1));   // 母之兄（舅舅）
        b.Add("yi", Gender.Female, new DateTime(1965, 1, 1));  // 母之妹（姨妈）
        b.Add("jiuma", Gender.Female, new DateTime(1961, 1, 1));
        b.Add("yifu", Gender.Male, new DateTime(1964, 1, 1));
        b.Mother("me", "mom");
        b.Mother("mom", "gma");
        b.Mother("jiu", "gma"); b.Mother("yi", "gma");
        b.Spouse("jiu", "jiuma");
        b.Spouse("yi", "yifu");

        Assert.Equal("舅舅", b.Term("jiu"));
        Assert.Equal("姨妈", b.Term("yi"));
        Assert.Equal("舅妈", b.Term("jiuma"));
        Assert.Equal("姨父", b.Term("yifu"));
    }

    [Fact]
    public void 堂兄弟姐妹()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("gdad", Gender.Male, new DateTime(1930, 1, 1));
        b.Add("bo", Gender.Male, new DateTime(1955, 1, 1));     // 父之兄
        b.Add("tangGe", Gender.Male, new DateTime(1988, 1, 1)); // 伯父之子，长于我 → 堂兄
        b.Add("tangDi", Gender.Male, new DateTime(1995, 1, 1)); // 堂弟
        b.Add("tangJie", Gender.Female, new DateTime(1986, 1, 1));
        b.Add("tangMei", Gender.Female, new DateTime(1997, 1, 1));
        b.Father("me", "dad");
        b.Father("dad", "gdad"); b.Father("bo", "gdad");
        b.Father("tangGe", "bo"); b.Father("tangDi", "bo");
        b.Father("tangJie", "bo"); b.Father("tangMei", "bo");

        Assert.Equal("堂兄", b.Term("tangGe"));
        Assert.Equal("堂弟", b.Term("tangDi"));
        Assert.Equal("堂姐", b.Term("tangJie"));
        Assert.Equal("堂妹", b.Term("tangMei"));
    }

    [Fact]
    public void 表兄弟姐妹()
    {
        // 舅表：母之兄之子
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("gma", Gender.Female, new DateTime(1935, 1, 1));
        b.Add("jiu", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("biaoGe", Gender.Male, new DateTime(1988, 1, 1));
        b.Add("biaoMei", Gender.Female, new DateTime(1995, 1, 1));
        b.Mother("me", "mom");
        b.Mother("mom", "gma"); b.Mother("jiu", "gma");
        b.Father("biaoGe", "jiu"); b.Father("biaoMei", "jiu");

        Assert.Equal("表兄", b.Term("biaoGe"));
        Assert.Equal("表妹", b.Term("biaoMei"));
    }

    [Fact]
    public void 兄弟姐妹的配偶按长幼()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 6, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("elderBro", Gender.Male, new DateTime(1985, 1, 1));
        b.Add("youngerBro", Gender.Male, new DateTime(1995, 1, 1));
        b.Add("sao", Gender.Female, new DateTime(1986, 1, 1));    // 兄之妻
        b.Add("dixi", Gender.Female, new DateTime(1996, 1, 1));   // 弟之妻
        b.Add("jie", Gender.Female, new DateTime(1984, 1, 1));
        b.Add("jiefu", Gender.Male, new DateTime(1983, 1, 1));
        b.Add("mei", Gender.Female, new DateTime(1998, 1, 1));
        b.Add("meifu", Gender.Male, new DateTime(1997, 1, 1));
        b.Father("me", "dad"); b.Mother("me", "mom");
        b.Father("elderBro", "dad"); b.Mother("elderBro", "mom");
        b.Father("youngerBro", "dad"); b.Mother("youngerBro", "mom");
        b.Father("jie", "dad"); b.Mother("jie", "mom");
        b.Father("mei", "dad"); b.Mother("mei", "mom");
        b.Spouse("elderBro", "sao");
        b.Spouse("youngerBro", "dixi");
        b.Spouse("jie", "jiefu");
        b.Spouse("mei", "meifu");

        Assert.Equal("嫂子", b.Term("sao"));
        Assert.Equal("弟媳", b.Term("dixi"));
        Assert.Equal("姐夫", b.Term("jiefu"));
        Assert.Equal("妹夫", b.Term("meifu"));
    }

    [Fact]
    public void 兄弟姐妹的子女()
    {
        var b = new FamilyBuilder { SelfId = "me" };
        b.Add("me", Gender.Male, new DateTime(1990, 1, 1));
        b.Add("dad", Gender.Male, new DateTime(1960, 1, 1));
        b.Add("mom", Gender.Female, new DateTime(1962, 1, 1));
        b.Add("bro", Gender.Male, new DateTime(1988, 1, 1));
        b.Add("sis", Gender.Female, new DateTime(1986, 1, 1));
        b.Add("zhi", Gender.Male, new DateTime(2010, 1, 1));   // 兄之子
        b.Add("zhinv", Gender.Female, new DateTime(2012, 1, 1)); // 兄之女
        b.Add("sheng", Gender.Male, new DateTime(2014, 1, 1));  // 姐之子
        b.Add("shengnv", Gender.Female, new DateTime(2016, 1, 1)); // 姐之女
        b.Father("me", "dad"); b.Mother("me", "mom");
        b.Father("bro", "dad"); b.Mother("bro", "mom");
        b.Father("sis", "dad"); b.Mother("sis", "mom");
        b.Father("zhi", "bro"); b.Father("zhinv", "bro");
        b.Father("sheng", "sis"); b.Father("shengnv", "sis");

        Assert.Equal("侄子", b.Term("zhi"));
        Assert.Equal("侄女", b.Term("zhinv"));
        Assert.Equal("外甥", b.Term("sheng"));
        Assert.Equal("外甥女", b.Term("shengnv"));
    }
}
