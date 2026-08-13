namespace KinshipCalculator.Transfer.Protocol;

public enum FrameVerdictKind
{
    Ok,
    Foreign,
    OlderSender,
    NewerSender,
    UnsupportedFlags,
    Malformed,
}

/// <summary>帧分类结论。区分「换个地方扫」与「设备需要升级」。</summary>
public readonly record struct FrameVerdict(
    FrameVerdictKind Kind,
    byte Version = 0,
    byte Flags = 0)
{
    public static FrameVerdict Ok => new(FrameVerdictKind.Ok);
    public static FrameVerdict Foreign => new(FrameVerdictKind.Foreign);
    public static FrameVerdict Malformed => new(FrameVerdictKind.Malformed);
    public static FrameVerdict OlderSender(byte version) => new(FrameVerdictKind.OlderSender, Version: version);
    public static FrameVerdict NewerSender(byte version) => new(FrameVerdictKind.NewerSender, Version: version);
    public static FrameVerdict Unsupported(byte flags) => new(FrameVerdictKind.UnsupportedFlags, Flags: flags);
}
