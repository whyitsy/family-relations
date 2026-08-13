# Changelog

本项目所有值得注意的变更都会记录在此文件。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [1.1.0] - 2026-08-14

### Added

- 数据传递 · 手动导入/导出：导出 JSON 文件、复制 JSON、导入 JSON 文件、粘贴导入识别（全平台，导入自动规范化）。
- 数据传递 · 光学发送：新增 `KinshipCalculator.Transfer` 核心库（系统性-旋转木马喷泉码、22 字节自描述协议、文件容器 + SHA-256/gzip、ZXing.Net QR 编解码），与 decimen-optical-transfer 线上比特级兼容（喷泉码流指纹经黄金向量验证）。
- 光学发送窗口：屏幕持续播放二维码流；接收端（摄像头）留待后续接入。
- 序列化重构：`FamilyDataJsonContext` 与 `FamilyDataSerializer` 移入 Core，供存储/导入导出/光学传输共用。
- 测试新增 60 个（序列化 6 + 传输 54），总计 80 个；Native AOT 发布仍约 37 MB。

## [1.0.0] - 2026-08-14

### Added

- 中文亲属称谓计算引擎：覆盖直系（爸爸/妈妈/爷爷/奶奶/外公/外婆/曾祖辈/子孙辈）、旁系（伯父/叔叔/姑姑/舅舅/姨妈、堂/表兄弟姐妹、侄甥辈）、姻亲（岳父/公公/大伯子/小叔子/大舅子/小姨子/嫂子/弟媳等）。
- 长幼判定与多重关系歧义处理：多条最短路径称谓不同时标记 `IsAmbiguous` 并列出候选；缺生日时给出通用称谓并标记 `NeedsBirthDate`。
- 自绘关系图谱：分层树状布局、拖拽平移、滚轮缩放、节点点击选中、性别/本人高亮。
- Avalonia UI 桌面应用（Windows）：Native AOT 发布，最小体积、快速启动。
- Android / iOS 工程骨架（Avalonia 12 启动方式）。
- xUnit 单元测试（20 个用例），覆盖直系/旁系/姻亲/长幼/半同胞/歧义/未知回退。
