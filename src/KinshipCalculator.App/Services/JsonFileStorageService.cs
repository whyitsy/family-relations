using KinshipCalculator.Core.Models;
using KinshipCalculator.Core.Serialization;

namespace KinshipCalculator.App.Services;

/// <summary>基于 JSON 文件的家谱文档存储（AOT 安全：走 Core 的源生成器序列化）。</summary>
public sealed class JsonFileStorageService : IStorageService
{
    private readonly string _path;

    public JsonFileStorageService(string path) => _path = path;

    public KinshipDocument Load()
    {
        try
        {
            if (!File.Exists(_path))
                return KinshipDocumentSerializer.CreateDefault();

            var json = File.ReadAllText(_path);

            // 新格式：多图谱文档。
            if (KinshipDocumentSerializer.TryParseDocument(json, out var doc, out _) && doc is not null)
                return doc;

            // 旧格式：单个 FamilyData → 迁移为一个图谱。
            if (FamilyDataSerializer.TryDeserialize(json, out var data, out _) && data is not null)
                return KinshipDocumentSerializer.Wrap(data);

            return KinshipDocumentSerializer.CreateDefault();
        }
        catch
        {
            // 数据损坏或读取失败时回退为默认文档，避免应用崩溃。
            return KinshipDocumentSerializer.CreateDefault();
        }
    }

    public void Save(KinshipDocument doc)
    {
        try
        {
            File.WriteAllText(_path, KinshipDocumentSerializer.Serialize(doc));
        }
        catch
        {
            // 存储失败不应导致应用崩溃。
        }
    }
}
