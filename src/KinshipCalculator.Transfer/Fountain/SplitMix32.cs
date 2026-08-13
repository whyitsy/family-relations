namespace KinshipCalculator.Transfer.Fountain;

/// <summary>
/// splitmix32 —— 纯 32 位整数运算的确定性伪随机数发生器，
/// 与 decimen-optical-transfer 的 JS 实现比特级一致（跨语言/平台可复现）。
/// </summary>
public struct SplitMix32
{
    private uint _state;

    public SplitMix32(int seed) => _state = unchecked((uint)seed);

    public uint Next()
    {
        _state = unchecked(_state + 0x9e3779b9u);
        uint t = _state ^ (_state >> 16);
        t = unchecked(t * 0x21f0aaadu);
        t ^= t >> 15;
        t = unchecked(t * 0x735a2d97u);
        t ^= t >> 15;
        return t;
    }
}
