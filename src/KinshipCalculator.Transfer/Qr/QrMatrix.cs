namespace KinshipCalculator.Transfer.Qr;

/// <summary>二维码模块矩阵（含静区），行主序，true 表示暗模块。</summary>
public sealed class QrMatrix
{
    private readonly bool[] _modules;

    public int Size { get; }

    public QrMatrix(int size)
    {
        Size = size;
        _modules = new bool[size * size];
    }

    public bool this[int x, int y]
    {
        get => _modules[y * Size + x];
        set => _modules[y * Size + x] = value;
    }
}
