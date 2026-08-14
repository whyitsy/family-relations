using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Services;

/// <summary>家谱文档（多图谱）持久化抽象。</summary>
public interface IStorageService
{
    KinshipDocument Load();

    void Save(KinshipDocument doc);
}
