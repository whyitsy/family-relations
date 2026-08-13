namespace KinshipCalculator.Transfer.Tests;

/// <summary>与 decimen-optical-transfer 一致的确定性测试载荷。</summary>
internal static class TestData
{
    public static byte[] Payload(int byteLength)
    {
        var payload = new byte[byteLength];
        for (int i = 0; i < byteLength; i++)
            payload[i] = (byte)((i * 37 + (i >> 8) * 11) & 0xff);
        return payload;
    }
}
