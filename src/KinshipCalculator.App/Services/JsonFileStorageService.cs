using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Serialization;

namespace KinshipCalculator.App.Services;

/// <summary>基于 JSON 文件的家谱存储（AOT 安全：走 Core 的源生成器序列化）。</summary>
public sealed class JsonFileStorageService : IStorageService
{
    private readonly string _path;

    public JsonFileStorageService(string path) => _path = path;

    public FamilyData Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new FamilyData();

            var json = File.ReadAllText(_path);
            return FamilyDataSerializer.Deserialize(json) ?? new FamilyData();
        }
        catch
        {
            // 数据损坏或读取失败时回退为空数据，避免应用崩溃。
            return new FamilyData();
        }
    }

    public void Save(FamilyData data)
    {
        try
        {
            File.WriteAllText(_path, FamilyDataSerializer.Serialize(data));
        }
        catch
        {
            // 存储失败不应导致应用崩溃。
        }
    }
}
