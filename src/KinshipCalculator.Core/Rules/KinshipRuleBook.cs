using KinshipCalculator.Core.Graph;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Rules;

/// <summary>
/// 称谓规则库（按普通话常见称谓）。新增称谓只需在此追加一条规则，无需改动匹配代码。
/// 符号：F 父亲 / M 母亲 / N 儿子 / D 女儿 / S 配偶 / B 兄弟 / Z 姐妹 / C 孩子(未知) / Sib 兄弟姐妹(未知)。
/// </summary>
public static class KinshipRuleBook
{
    public static readonly KinshipRule[] Rules = Build();

    private static KinshipRule[] Build()
    {
        var F = StepKind.Father;
        var M = StepKind.Mother;
        var N = StepKind.Son;
        var D = StepKind.Daughter;
        var S = StepKind.Spouse;
        var B = StepKind.Brother;
        var Z = StepKind.Sister;
        var C = StepKind.Child;
        var Sib = StepKind.Sibling;

        KinshipRule R(
            string id,
            StepKind[] p,
            Gender? sg = null,
            Gender? tg = null,
            AgeRule ar = AgeRule.None,
            int idx = 0,
            string? older = null,
            string? younger = null,
            string term = "")
            => new(id, p, sg, tg, ar, idx, older, younger, term);

        return new[]
        {
            // ── 配偶 ──
            R("丈夫", new[] { S }, tg: Gender.Male, term: "丈夫"),
            R("妻子", new[] { S }, tg: Gender.Female, term: "妻子"),
            R("配偶(未知)", new[] { S }, tg: Gender.Unknown, term: "配偶"),

            // ── 直系血亲·长辈 ──
            R("爸爸", new[] { F }, term: "爸爸"),
            R("妈妈", new[] { M }, term: "妈妈"),
            R("爷爷", new[] { F, F }, term: "爷爷"),
            R("奶奶", new[] { F, M }, term: "奶奶"),
            R("外公", new[] { M, F }, term: "外公"),
            R("外婆", new[] { M, M }, term: "外婆"),
            R("曾祖父", new[] { F, F, F }, term: "曾祖父"),
            R("曾祖母", new[] { F, F, M }, term: "曾祖母"),
            R("曾外祖父", new[] { F, M, F }, term: "曾外祖父"),
            R("曾外祖母", new[] { F, M, M }, term: "曾外祖母"),
            R("外曾祖父", new[] { M, F, F }, term: "外曾祖父"),
            R("外曾祖母", new[] { M, F, M }, term: "外曾祖母"),
            R("外曾外祖父", new[] { M, M, F }, term: "外曾外祖父"),
            R("外曾外祖母", new[] { M, M, M }, term: "外曾外祖母"),

            // ── 直系血亲·晚辈 ──
            R("儿子", new[] { N }, term: "儿子"),
            R("女儿", new[] { D }, term: "女儿"),
            R("孩子(未知)", new[] { C }, term: "孩子"),
            R("孙子", new[] { N, N }, term: "孙子"),
            R("孙女", new[] { N, D }, term: "孙女"),
            R("外孙", new[] { D, N }, term: "外孙"),
            R("外孙女", new[] { D, D }, term: "外孙女"),
            R("曾孙", new[] { N, N, N }, term: "曾孙"),
            R("曾孙女", new[] { N, N, D }, term: "曾孙女"),
            R("曾外孙", new[] { N, D, N }, term: "曾外孙"),
            R("曾外孙女", new[] { N, D, D }, term: "曾外孙女"),
            R("外曾孙", new[] { D, N, N }, term: "外曾孙"),
            R("外曾孙女", new[] { D, N, D }, term: "外曾孙女"),
            R("外曾外孙", new[] { D, D, N }, term: "外曾外孙"),
            R("外曾外孙女", new[] { D, D, D }, term: "外曾外孙女"),

            // ── 亲兄弟姐妹 ──
            R("哥哥/弟弟", new[] { B }, ar: AgeRule.StepVsSelf, idx: 0, older: "哥哥", younger: "弟弟", term: "哥哥/弟弟"),
            R("姐姐/妹妹", new[] { Z }, ar: AgeRule.StepVsSelf, idx: 0, older: "姐姐", younger: "妹妹", term: "姐姐/妹妹"),
            R("兄弟姐妹(未知)", new[] { Sib }, term: "兄弟姐妹"),

            // ── 父系旁系长辈 ──
            R("伯父/叔叔", new[] { F, B }, ar: AgeRule.StepVsPrevious, idx: 1, older: "伯父", younger: "叔叔", term: "伯父/叔叔"),
            R("姑姑", new[] { F, Z }, term: "姑姑"),
            R("伯母/婶婶", new[] { F, B, S }, tg: Gender.Female, ar: AgeRule.StepVsPrevious, idx: 1, older: "伯母", younger: "婶婶", term: "伯母/婶婶"),
            R("姑父", new[] { F, Z, S }, tg: Gender.Male, term: "姑父"),

            // ── 母系旁系长辈 ──
            R("舅舅", new[] { M, B }, term: "舅舅"),
            R("姨妈", new[] { M, Z }, term: "姨妈"),
            R("舅妈", new[] { M, B, S }, tg: Gender.Female, term: "舅妈"),
            R("姨父", new[] { M, Z, S }, tg: Gender.Male, term: "姨父"),

            // ── 堂/表兄弟姐妹 ──
            R("堂兄/堂弟", new[] { F, B, N }, tg: Gender.Male, ar: AgeRule.StepVsSelf, idx: 2, older: "堂兄", younger: "堂弟", term: "堂兄/堂弟"),
            R("堂姐/堂妹", new[] { F, B, D }, tg: Gender.Female, ar: AgeRule.StepVsSelf, idx: 2, older: "堂姐", younger: "堂妹", term: "堂姐/堂妹"),
            R("表兄/表弟(姑表)", new[] { F, Z, N }, tg: Gender.Male, ar: AgeRule.StepVsSelf, idx: 2, older: "表兄", younger: "表弟", term: "表兄/表弟"),
            R("表姐/表妹(姑表)", new[] { F, Z, D }, tg: Gender.Female, ar: AgeRule.StepVsSelf, idx: 2, older: "表姐", younger: "表妹", term: "表姐/表妹"),
            R("表兄/表弟(舅表)", new[] { M, B, N }, tg: Gender.Male, ar: AgeRule.StepVsSelf, idx: 2, older: "表兄", younger: "表弟", term: "表兄/表弟"),
            R("表姐/表妹(舅表)", new[] { M, B, D }, tg: Gender.Female, ar: AgeRule.StepVsSelf, idx: 2, older: "表姐", younger: "表妹", term: "表姐/表妹"),
            R("表兄/表弟(姨表)", new[] { M, Z, N }, tg: Gender.Male, ar: AgeRule.StepVsSelf, idx: 2, older: "表兄", younger: "表弟", term: "表兄/表弟"),
            R("表姐/表妹(姨表)", new[] { M, Z, D }, tg: Gender.Female, ar: AgeRule.StepVsSelf, idx: 2, older: "表姐", younger: "表妹", term: "表姐/表妹"),

            // ── 兄弟姐妹的配偶 ──
            R("嫂子/弟媳", new[] { B, S }, tg: Gender.Female, ar: AgeRule.StepVsSelf, idx: 0, older: "嫂子", younger: "弟媳", term: "嫂子/弟媳"),
            R("姐夫/妹夫", new[] { Z, S }, tg: Gender.Male, ar: AgeRule.StepVsSelf, idx: 0, older: "姐夫", younger: "妹夫", term: "姐夫/妹夫"),

            // ── 子女的配偶 ──
            R("儿媳", new[] { N, S }, tg: Gender.Female, term: "儿媳"),
            R("女婿", new[] { D, S }, tg: Gender.Male, term: "女婿"),

            // ── 兄弟姐妹的子女 ──
            R("侄子", new[] { B, N }, term: "侄子"),
            R("侄女", new[] { B, D }, term: "侄女"),
            R("外甥", new[] { Z, N }, term: "外甥"),
            R("外甥女", new[] { Z, D }, term: "外甥女"),

            // ── 配偶的父母 ──
            R("岳父", new[] { S, F }, sg: Gender.Male, term: "岳父"),
            R("公公", new[] { S, F }, sg: Gender.Female, term: "公公"),
            R("配偶的父亲", new[] { S, F }, sg: Gender.Unknown, term: "配偶的父亲"),
            R("岳母", new[] { S, M }, sg: Gender.Male, term: "岳母"),
            R("婆婆", new[] { S, M }, sg: Gender.Female, term: "婆婆"),
            R("配偶的母亲", new[] { S, M }, sg: Gender.Unknown, term: "配偶的母亲"),

            // ── 配偶的兄弟姐妹 ──
            R("大舅子/小舅子", new[] { S, B }, sg: Gender.Male, tg: Gender.Male, ar: AgeRule.StepVsPrevious, idx: 1, older: "大舅子", younger: "小舅子", term: "大舅子/小舅子"),
            R("大伯子/小叔子", new[] { S, B }, sg: Gender.Female, tg: Gender.Male, ar: AgeRule.StepVsPrevious, idx: 1, older: "大伯子", younger: "小叔子", term: "大伯子/小叔子"),
            R("配偶的兄弟", new[] { S, B }, sg: Gender.Unknown, tg: Gender.Male, term: "配偶的兄弟"),
            R("大姨子/小姨子", new[] { S, Z }, sg: Gender.Male, tg: Gender.Female, ar: AgeRule.StepVsPrevious, idx: 1, older: "大姨子", younger: "小姨子", term: "大姨子/小姨子"),
            R("大姑子/小姑子", new[] { S, Z }, sg: Gender.Female, tg: Gender.Female, ar: AgeRule.StepVsPrevious, idx: 1, older: "大姑子", younger: "小姑子", term: "大姑子/小姑子"),
            R("配偶的姐妹", new[] { S, Z }, sg: Gender.Unknown, tg: Gender.Female, term: "配偶的姐妹"),
        };
    }
}
