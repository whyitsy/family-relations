using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Graph;

/// <summary>关系图中的节点，缓存邻接信息。</summary>
public sealed class PersonNode
{
    public required Person Person { get; init; }

    public Person? Father { get; set; }
    public Person? Mother { get; set; }
    public Person? Spouse { get; set; }

    /// <summary>兄弟姐妹（含显式边与按共同父母推导出的）。</summary>
    public List<Person> Siblings { get; } = new();

    /// <summary>子女（由 Father/Mother 反向推导）。</summary>
    public List<Person> Children { get; } = new();
}
