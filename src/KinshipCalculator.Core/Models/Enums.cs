namespace KinshipCalculator.Core.Models;

/// <summary>性别。</summary>
public enum Gender
{
    Male,
    Female,
    Unknown,
}

/// <summary>基础关系边的种类（子女关系由 Father/Mother 反向推导，不显式存储）。</summary>
public enum RelationKind
{
    Father,
    Mother,
    Spouse,
    Sibling,
}

/// <summary>兄弟姐妹的血缘类别。</summary>
public enum SiblingKind
{
    /// <summary>同父同母。</summary>
    Full,
    /// <summary>同父异母。</summary>
    HalfPaternal,
    /// <summary>同母异父。</summary>
    HalfMaternal,
}
