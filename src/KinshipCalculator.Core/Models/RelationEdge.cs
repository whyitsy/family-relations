namespace KinshipCalculator.Core.Models;

/// <summary>关系边。语义：<see cref="FromId"/> 的 <see cref="Kind"/> 是 <see cref="ToId"/>。</summary>
public sealed class RelationEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string FromId { get; set; } = string.Empty;

    public string ToId { get; set; } = string.Empty;

    public RelationKind Kind { get; set; }

    /// <summary>仅当 <see cref="Kind"/> == Sibling 时有效。</summary>
    public SiblingKind? SiblingKind { get; set; }
}
