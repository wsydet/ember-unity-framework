# 外部 CSV/TSV 源表交付记录

日期：2026-10-21。归属：框架 + 视觉小说模板；不包含消费项目业务迁移。

## 最终版本与正式模板状态

| 内容 | 最终状态 |
| --- | --- |
| com.ember | 发布版本 0.21.0；Unity PackageInfo 已实际解析为 Embedded 0.21.0 |
| visual-novel | 0.21.2；通过项目中心 SaveTemplate/BumpTemplateVersion 正式保存与封存 |
| visual-novel hash | `56cba1d73472bba8f1fa4baa293466f0`，contentHash 与 versionedContentHash 相同 |
| 编辑副本 | 五个受管目录 1,301 个文件与正式快照逐字节一致，编辑记录版本/hash 一致 |
| base | 仍为 0.8.3，内容 hash `1270ce6dfcae3c01d050c09ecfd60337` 不变 |
| platformer-2d | 仍为 0.10.0，内容 hash `58d152503e6f95494434f095a8df6ef4` 不变 |
| source3d-2p5d | 仍为 0.5.0，内容 hash `89e49f1c8c620812a15b181a0a049180` 不变 |
| 框架兼容声明 | base 正式 DeclareFrameworkVersion；三个派生模板正式 ApplyParentSync，均对齐 0.21.0 |
| 父子状态 | base Root；其余 Synced，无冲突；ValidateTemplateGraph 无问题，实际目录 hash 与封存 hash 均核实 |
| 模板技能 | PreviewCurrentTemplateSkills / SyncCurrentTemplateSkills 正式同步到当前开发工程发现目录 |
| 发布 | 本记录随 v0.21.0 正式发布；发布提交以远端 annotated tag 的 peeled commit 为准 |

版本声明最初遇到 PackageInfo 尚缓存旧版本，已等待包解析完成后重新通过正式 API 声明和同步；
没有手改 template.json、ParentSnapshot、模板 hash 或消费部署基线。

## 实现与兼容边界

- `EmberTableSourceFiles` 是公共 Editor 定位/读写接口；项目共享配置为 `ProjectSettings/EmberTableSources.json`。
  配置包含 allowedRoots、defaultRoot、bindings；没有把 Design、Program 或游戏名硬编码到框架默认值。
- 保留 Definition 的 `_source` 与多语言注册的 `Source` 序列化字段，新增资产级外部路径，并支持按 Table ID 的共享绑定。
  映射优先，显式外部绑定失效不回退旧 TextAsset；共享映射与资产路径冲突会失败。
- CSV/TSV 由磁盘读取，外部源不进入 TextAsset、不 ImportAsset；原运行产物仍在既有 Assets 路径。
- 公共配表中心、项目校验、图片库基类、多语言和接线生成器在 Package 内统一适配。
  base/平台/3D 模板没有额外旧路径调用，不复制源表访问实现；旧资产缺少新字段仍可读。
- 视觉小说的五处调用方在项目 Assets 修改后正式保存：NovelContentToolsSetup、NarrativeE2AssetImport、
  NarrativeE3Assets、NarrativeM2Assets、NarrativeSampleArtMigration。对应业务副本需在消费端核对迁移。
- 图片导入/同步 Python 读取同一共享配置，保留边界、确认指纹与失效语义；支持 TSV。
  剧情导入技能明确使用 SourcePath/SourceDocument，禁止补建旧目录副本。
- 原始字节并发检查、干净编辑窗口刷新、草稿冲突、源文件原子替换及失败恢复、提交前快照复核已接入。
  Windows Unity/扫描器短暂持有源文件时，原子替换最多进行 3 次额外的 25ms 有界重试；不降级为非原子覆盖。
- 运行时 Table ID、Resources 键、Schema、ETBL V1 编码与加载 API 未改。

完整配置、优先级、迁移清单、冲突处理、恢复流程和消费脚本调整示例见
[外部源表手册](../../Packages/com.ember/Table/Documentation~/table/external-sources.md)。

## 实际验证结果

所有以下 Unity 操作均在 `ember-unity-framework@b23e9b768e85ed30`、Unity 6000.5.4f1 中通过 MCP 执行。

| 验证 | 结果与证据 |
| --- | --- |
| Unity 编译 | MCP 刷新/编译后 Console 无编译错误；新增类型及正式模板 API 可实际执行 |
| 配表 Editor/Runtime/Integration 全集 | **65/65 Passed，0 skipped**；job `e2cb158e6aac43bfbd66197e52ac28a8` |
| 多语言源表专项 | **5/5 Passed，0 skipped**；job `f258882565f5473e8afdb45468fb6e19` |
| Python 图片导入 | **17/17 OK**，包括中文/空格 Design/Art/Program 夹具、外部 TSV、缺失不回退、越界/绝对/重复绑定拒绝、配置指纹失效 |
| Python 图片同步 | **8/8 OK**，复用相同定位和边界方法 |
| Windows 目录联接逃逸 | 框架实例实际读取 allowed/link/test.csv；统一接口返回“不允许符号链接或目录联接”，未读取范围外 CSV |
| 外部全流程 | 在同一个框架实例临时映射 11 个 Definition、3 个文案注册到 Assets 外 12 份源，校验、预览、逐表 BakeCurrent、BakeAndGenerateAll 均通过 |
| 多语言及模板接线 | 外部映射下实际调用 NovelContentToolsSetup.RegisterTables、InitializeCommon 及 LocalizationBuildProcessor；通过 |
| 构建前源表检查 | 上述场景实际调用 TableBuildProcessor.OnPreprocessBuild(null)；通过；没有执行 Player Build |
| 输出兼容 | 外部全流程前后所有现有 .bytes 与多语言 .asset 逐字节一致，注册对象及 Reviews 的序列化 JSON 一致 |
| 恢复检查 | 临时共享配置已恢复；测试引入的三个空 ExternalSourcePath 序列化字段和清单换行变化已恢复到测试前正式快照，1,301 文件再次一致 |
| 静态检查 | git diff --check 无错误；变更文件与新 .meta 配对、配置/文档链接及技能安装副本一致性检查 |

65 项配表测试包括旧 Assets 流程、外部中文路径、反斜杠与相对路径解析、CSV/TSV 解析、相同数据二进制一致、
资源键不变、缺失源、非法 CSV、重复主键、跨表引用、映射冲突、陈旧快照、草稿并发、只读保存失败恢复、
图片库与原有生成事务。5 项文案测试包括外部共享映射、运行产物、审核状态、跨表 Key 冲突、失败恢复及草稿序列化。

首次组合回归曾在多语言保存测试遇到 Windows File.Replace 的 IOException；增加有界原子替换重试后，
专项 19/19、组合 41/41 与最终上述两组均通过。首次 Python 运行因默认沙箱临时目录权限失败，改为仓库
`.utmp/python-tests` 后通过。保留这些失败记录，不把第一次失败算作通过。

外部整链路运行报告保存在本地 `.utmp/framework-source-regression/report.json`，不作为必须提交的产品资源。
该目录是框架测试数据，不是另一个 Unity 项目。

## 待用户在消费端验收

用户已明确要求停止操作消费项目、不再新建测试项目。独立消费验证未完成；先前新开的测试 Unity 进程已退出。
后续没有连接、升级、部署或修改消费项目；现有产品 PackageCache、manifest、lock 与部署基线未操作。

- [ ] 发布后，通过 **Ember/UPM Manager** 从实际消费版本升级至 0.21.0；不要手改 manifest/lock 或 PackageCache。
- [ ] 在真实 Design/Art/Program 布局完成消费 Unity 编译、项目中心校验、场景和产品回归。
- [ ] 按实际清单迁移用户项目的 22 份源表、21 个 Definition 与 common_ui_text 注册；保留资产 GUID/引用。
- [ ] 核对自定义业务脚本的 Source/Source.bytes/text、旧路径和 ImportAsset 调用；吸收五处模板调用方调整。
- [ ] 验证 common_ui_text、小说 UI/剧情文案的完整产品内容、语言切换、翻译审核与关联 .bytes。
- [ ] 用真实素材完成图片导入、混合图片同步与剧情导入的交互确认、保存和运行预览；本次运行的是工具回归与框架链路。
- [ ] 实际操作编辑窗口，验证外部文件改动刷新、未保存草稿冲突和人工合并体验。
- [ ] 全新克隆、仓库换目录、策划只写 Design 的权限流程、产品持续集成和真实发行构建。
- [ ] 在实际迁移中途模拟失败，按备份和清单恢复；框架测试证明单次保存/生成事务，不等同于真实项目迁移演练。

没有把静态检查、框架工程测试或构建前回调调用写成消费项目编译、Play Mode 或发行构建已通过。

## 用户验收顺序

1. 完成发布后，用 UPM Manager 升级；在 Unity 手动触发编译，先解决首条错误。
2. 保留迁移前 .bytes SHA-256、Resources 路径、注册资产与 Reviews，建立迁移清单及备份。
3. 移动唯一源到 Design，提交 Program 的共享映射并调整业务调用方；不同内容目标冲突必须先人工处理。
4. 配表中心：校验全部 → 预览全部变更 → 导出当前表 → 烘焙并生成全部。
5. 多语言中心逐表保存；核对 SHA-256、资源键与审核状态；再完成图片/剧情导入、产品运行和实际构建。
6. 用全新克隆重复，确认无需复制 CSV、目录联接或额外策划目录权限。

本轮保留了原有 Odin 本地依赖、字体动态数据、URP/Unity 设置等既存工作区改动；它们不包含在本次选择性发布提交内。

发布前复核补齐首次公共文案初始化与小说示例空表填充的 TSV 分隔符；MCP 重新编译无错误，多语言专项复测 5/5（job `49a7e9e01a734fd49195da148a3c81ee`）。小说模板再次正式保存/Bump，最终版本 0.21.2。发布候选共享二进制（含 autocrlf 两种检出）、字体静态策略和公共技能 bundle 检查通过。
