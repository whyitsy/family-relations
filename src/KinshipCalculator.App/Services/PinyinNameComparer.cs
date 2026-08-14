using System.Text;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Services;

/// <summary>
/// 按拼音顺序比较成员姓名。GB2312 的一级汉字按拼音排序，因此用汉字在 GB2312 中的
/// 双字节编码值排序，即可得到常见汉字的拼音序（首字母分组自然呈现）。
/// ASCII 字符排在最前，GB2312 未收录的生僻字按 Unicode 码点排在最后。
/// </summary>
public sealed class PinyinNameComparer : IComparer<Person>
{
    public static readonly PinyinNameComparer Instance = new();

    private static readonly Encoding Gb2312;

    static PinyinNameComparer()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Gb2312 = Encoding.GetEncoding("GB2312");
    }

    public int Compare(Person? x, Person? y)
    {
        var a = x?.Name ?? string.Empty;
        var b = y?.Name ?? string.Empty;

        var len = Math.Min(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var c = CharCode(a[i]).CompareTo(CharCode(b[i]));
            if (c != 0)
                return c;
        }

        return a.Length.CompareTo(b.Length);
    }

    private static int CharCode(char c)
    {
        // ASCII：0~127，排在汉字之前
        if (c < 0x80)
            return c;

        var bytes = Gb2312.GetBytes(c.ToString());
        if (bytes.Length == 2)
            return 0x10000 | (bytes[0] << 8) | bytes[1];

        // 生僻字/未收录：按 Unicode 码点排在最后
        return 0x20000 | c;
    }
}
