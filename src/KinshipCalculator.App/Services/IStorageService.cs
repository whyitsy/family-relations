using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Services;

/// <summary>家谱数据持久化抽象。</summary>
public interface IStorageService
{
    FamilyData Load();

    void Save(FamilyData data);
}
