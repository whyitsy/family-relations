namespace KinshipCalculator.Core.Graph;

/// <summary>路径中的单步关系（性别已编码进种类，便于规则精确匹配）。</summary>
public enum StepKind
{
    /// <summary>父亲。</summary>
    Father,
    /// <summary>母亲。</summary>
    Mother,
    /// <summary>儿子（男性子女）。</summary>
    Son,
    /// <summary>女儿（女性子女）。</summary>
    Daughter,
    /// <summary>配偶。</summary>
    Spouse,
    /// <summary>兄弟（男性兄弟姐妹）。</summary>
    Brother,
    /// <summary>姐妹（女性兄弟姐妹）。</summary>
    Sister,
    /// <summary>孩子（性别未知）。</summary>
    Child,
    /// <summary>兄弟姐妹（性别未知）。</summary>
    Sibling,
}
