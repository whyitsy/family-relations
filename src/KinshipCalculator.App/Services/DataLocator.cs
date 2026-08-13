namespace KinshipCalculator.App.Services;

/// <summary>定位家谱数据文件路径（桌面端使用用户应用数据目录）。</summary>
public static class DataLocator
{
    public static string GetDataFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "亲戚关系计算器");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "data.json");
    }
}
