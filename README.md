# 亲戚关系计算器（Kinship Calculator）

以「我」为中心的中文亲属称谓计算器：录入人物与基础关系（父/母/配偶，兄弟姐妹与子女自动推导），指定「我」，自动为图中每个人计算中文称谓（爸爸/妈妈/爷爷/外婆/伯父/叔叔/堂兄/表妹/岳父/大舅子……），并以分层树状图谱可视化展示。

- **技术栈**：.NET 10 SDK + Avalonia UI 12 + CommunityToolkit.Mvvm（源生成器）
- **发布**：Native AOT（Windows 桌面端），追求最小体积与最快启动
- **存储**：JSON 文件（`System.Text.Json` 源生成器，AOT 安全）

---

## 1. 解决方案结构

```
KinshipCalculator.slnx
├── src/
│   ├── KinshipCalculator.Core/        # 纯 C# 核心（零 UI 依赖，AOT 安全）
│   │   ├── Models/                    # Person / RelationEdge / FamilyData / 枚举
│   │   ├── Graph/                     # RelationshipGraph（邻接索引）、PathStep、StepKind
│   │   ├── Rules/                     # KinshipRule / KinshipRuleBook / KinshipEngine
│   │   └── Calculator/                # RelationshipCalculator / PathFinder / KinshipResult
│   ├── KinshipCalculator.App/         # 共享 Avalonia UI（net10.0）
│   │   ├── Services/                  # 存储、JSON 上下文、图谱布局引擎
│   │   ├── ViewModels/                # MainViewModel
│   │   └── Views/                     # MainWindow / MainView / GraphCanvas（自绘）
│   ├── KinshipCalculator.Desktop/     # Windows 桌面头（WinExe，PublishAot）
│   ├── KinshipCalculator.Android/     # Android 骨架（net10.0-android）
│   └── KinshipCalculator.iOS/         # iOS 骨架（net10.0-ios）
└── tests/
    └── KinshipCalculator.Core.Tests/  # xUnit 单元测试（覆盖称谓规则/长幼/歧义/回退）
```

---

## 2. 构建与发布

### 2.1 运行单元测试

```powershell
dotnet test tests\KinshipCalculator.Core.Tests
```

### 2.2 桌面端调试运行

```powershell
dotnet run --project src\KinshipCalculator.Desktop
```

### 2.3 Native AOT 发布（Windows x64，推荐）

```powershell
dotnet publish src\KinshipCalculator.Desktop -c Release -r win-x64
```

产物位于 `src\KinshipCalculator.Desktop\bin\Release\net10.0\win-x64\publish\`：

| 文件 | 说明 |
|------|------|
| `KinshipCalculator.Desktop.exe` | 原生 AOT 可执行文件（约 19 MB） |
| `libSkiaSharp.dll` | Skia 渲染引擎 |
| `av_libglesv2.dll` | ANGLE（OpenGL→D3D） |
| `libHarfBuzzSharp.dll` | 文本整形 |

整体约 **37 MB**。这四个文件是一个完整可分发目录（Skia 系为原生共享库，无法并入单文件）。

> AOT 关键配置已在 `KinshipCalculator.Desktop.csproj` 中：`PublishAot` / `PublishTrimmed` / `TrimMode=full` / `IlcTrimMetadata` / `InvariantGlobalization` / `BuiltInComInteropSupport=true`。Release 下关闭了 PDB 与原生符号复制（`CopyOutputSymbolsToPublishDirectory=false` 等）以精简体积。

### 2.4 Android / iOS

工程骨架已生成（Avalonia 12 的 Android 启动方式：非泛型 `AvaloniaMainActivity` + `[Application] AvaloniaAndroidApplication<App>`；iOS 为 `AvaloniaAppDelegate`）。但：

- 本机构建 Android 需先安装 workload：`dotnet workload install android`
- 构建 iOS 需 **macOS** 与 Xcode，且需按 Avalonia 12 iOS 文档核对场景式启动（`AppDelegate` 部分为骨架，可能需微调）
- 应用图标、启动屏、样式等资源需自行补充

---

## 3. 使用说明

1. **新增成员**：点击「新增成员」，在右侧详情面板填写姓名/性别/生日/备注。
2. **设置关系**：在详情面板为成员选择「父亲 / 母亲 / 配偶」。兄弟姐妹与子女会**按父/母关系自动推导**（同父/同母即视为兄弟姐妹），无需手动录入。
3. **指定「我」**：选中某人后点击「设为『我』」。
4. **查看称谓**：右侧列表自动显示每个人相对「我」的称谓与关系路径；中间图谱可**拖拽平移、滚轮缩放、点击节点选中**。

数据自动保存到 `%AppData%\亲戚关系计算器\data.json`（桌面端）。

---

## 4. 称谓规则与扩展

### 4.1 算法流程

1. 由 `FamilyData` 构建内存图 `RelationshipGraph`（配偶/兄弟姐妹双向规范化；子女由父/母反向推导；兄弟姐妹亦按共同父母推导）。
2. 对每个目标，用 BFS/DFS 找「我」到它的**最短简单路径**（最大深度 8），路径每步显式携带关系种类（`PathStep`），以正确处理近亲导致的多种关系。
3. 将路径序列与规则库 `KinshipRuleBook` 匹配，结合「我」的性别、目标性别、长幼（生日比较）得到称谓。
4. 多条最短路径称谓不同 → 标记 `IsAmbiguous` 并列出候选；规则库未覆盖 → 「未知关系（关系较远，暂无标准称谓）」；缺生日导致长幼无法判定 → 使用通用称谓（如「哥哥/弟弟」）并标记 `NeedsBirthDate`。

### 4.2 覆盖范围（示例）

直系：爸爸/妈妈、爷爷/奶奶/外公/外婆、曾祖父辈；儿子/女儿、孙子/孙女/外孙/外孙女、曾孙辈。
旁系：哥哥/弟弟/姐姐/妹妹、伯父/叔叔/姑姑、舅舅/姨妈、伯母/婶婶/姑父/舅妈/姨父、堂/表兄弟姐妹（姑表/舅表/姨表）、侄子/侄女/外甥/外甥女。
姻亲：丈夫/妻子、岳父/岳母/公公/婆婆、大伯子/小叔子/大姑子/小姑子、大舅子/小舅子/大姨子/小姨子、嫂子/弟媳/姐夫/妹夫、儿媳/女婿。

### 4.3 新增称谓

只需在 `src/KinshipCalculator.Core/Rules/KinshipRuleBook.cs` 的规则数组中追加一条：

```csharp
R("新称谓", new[] { F, B, S }, tg: Gender.Male, term: "某某"),
```

符号：`F` 父 / `M` 母 / `N` 儿子 / `D` 女儿 / `S` 配偶 / `B` 兄弟 / `Z` 姐妹 / `C` 孩子(未知) / `Sib` 兄弟姐妹(未知)。
长幼规则：`AgeRule.StepVsSelf`（与「我」比，`idx` 为步索引）+ `StepVsPrevious`（与上一步人物比）。

---

## 5. AOT 兼容性要点

- **序列化**：`System.Text.Json` 必须走源生成器上下文 `FamilyDataJsonContext`（禁止无上下文的 `JsonSerializer.Serialize(obj)`）。
- **视图解析**：未使用反射式 `ViewLocator`，直接在 `App.axaml.cs` 构造视图与视图模型。
- **绑定**：`AvaloniaUseCompiledBindingsByDefault=true`，所有 `{Binding}` 配 `x:DataType`。
- **图谱绘制**：节点/边用代码创建（无动态 XAML、无反射）。
- **MVVM**：`CommunityToolkit.Mvvm` 源生成器（编译期生成，零反射）。

---

## 6. 参考

- 需求参考文件：`参考1.txt`（技术栈/AOT）、`参考2.txt`（称谓算法）
- [Avalonia 12 Breaking Changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Avalonia Native AOT](https://docs.avaloniaui.net/docs/deployment/native-aot)
