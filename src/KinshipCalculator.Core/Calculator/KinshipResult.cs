namespace KinshipCalculator.Core.Calculator;

/// <summary>某个目标人物相对「我」的称谓计算结果。</summary>
public sealed class KinshipResult
{
    public required string PersonId { get; init; }

    public required string PersonName { get; init; }

    /// <summary>主称谓。</summary>
    public required string Term { get; init; }

    /// <summary>是否存在多重关系（多条最短路径称谓不同）。</summary>
    public bool IsAmbiguous { get; init; }

    /// <summary>候选的其他称谓。</summary>
    public IReadOnlyList<string> Candidates { get; init; } = Array.Empty<string>();

    /// <summary>与「我」的关系步数（最短路径长度）。</summary>
    public int Distance { get; init; }

    /// <summary>因缺少生日而使用了通用称谓（如「哥哥/弟弟」）。</summary>
    public bool NeedsBirthDate { get; init; }

    /// <summary>人类可读路径，如「我的父亲的哥哥」。</summary>
    public string? PathDescription { get; init; }
}
