# Ember Framework 0.13.1 发布说明

日期：2026-09-14。Tag：`v0.13.1`。归属：**框架升级（技能分发与维护文档）**。

## 发布 9 个技能

本次在已有 EUI 技能之外补充 8 个技能，共 9 个。UPM Manager 的 AI Skill 区域选择 `v0.13.1` 检查更新后，可逐项安装。

| Skill | 用途 |
|---|---|
| ember-eui-build | 制作 EUI Prefab、生成绑定与逻辑接线，沿用公开生成 API |
| ember-commit-review | 审查提交，覆盖暂存、未暂存、全部未跟踪文件及目录联接差异 |
| ember-region-organizer | 按项目规则整理 region，保护初始化顺序和条件编译 |
| ember-solution-design | 基于现有代码评估方案与改动归属，保留已有授权 |
| ember-package-scan | 核对声明与实际解析来源，生成或同步项目包清单 |
| ember-generate-doc | 为指定模块生成 API 文档，携带独立文档结构参考 |
| ember-odin-inspector | 按当前版本证据检查面板，保护序列化与业务 API |
| ember-doc-maintenance | 审计和维护自有文档，默认保护所有依赖包 |
| ember-odin-capture-style | 将面板经验写入项目自有文档，保留适用条件与证据 |

插件迁移仍留在开发仓库，空白 template-skill 不发布。九项均可独立使用，不自动安装相互依赖。

## 消费项目适配

技能读取当前项目规则，缺少框架开发仓库的 CLAUDE.md、docs/dev 或文档模板不会阻止基本流程。必要的 API 文档结构和 Odin 检查依据随各自技能下载。生成文档和面板风格记录写入项目自有目录，不改 PackageCache、模板快照或安装技能中的参考资料。

文档审计脚本默认 consumer 模式，把全部依赖包归为只读参考；明确维护框架源码时使用 framework 模式。支持额外第三方目录排除，并对指向同一实体的目录联接去重，避免兼容路径重复扫描。

Odin 的历史布局观察仅作为排查线索；不盲目把 public 字段或 ShowInInspector 属性改成 private SerializeField。记录风格也不自动修改项目脚本。

技能界面元数据统一为 interface 结构。框架开发仓库只跟踪 `.agents/skills`；取消历史 `.claude/skills` 镜像文件的重复跟踪，保留本地兼容 junction 和真实文件。

## 兼容与升级

本版没有修改 UPM Manager 或框架 C#、程序集、Unity 资产。九项的最低更新器基线仍为 0.13.0，EUI 仍要求 eui-regenerate-v1；Odin 条件在执行技能时检查，面板不做 Odin 专用阻断。文档审计需要 Python 3 及 rg 或 Git，安装不自动执行脚本。

已有框架 0.13.0 的项目也可以直接在 AI Skill 区域选择 `v0.13.1` 安装这批技能。需要同步框架版本时通过 **Ember/UPM Manager** 升级至 0.13.1，禁止手改消费项目 manifest/lock。安装后重新加载 AI 会话。

package、CHANGELOG、release/manifest 统一到 0.13.1；56 项依赖只推进 com.ember。base 0.6.3、source3d-2p5d 0.3.5、框架兼容声明 0.13.0、内容 Hash 和父快照均保持不变，不需要重新部署模板。

## 验证边界

- 九项技能的 YAML、调用提示、目录资源和本地引用完成检查，验证不依赖开发仓库文档的独立分发副本。
- 文档审计的隔离回归覆盖消费/框架所有权、第三方排除、UTF-8 与空格链接、代码围栏、Git 扫描回退、strict 返回值及非法范围。
- 检查发布声明、模板内容/父快照、共享字体配置和二进制，以及两种 autocrlf 干净检出。
- Unity 面板实际安装和此前版本的 Unity 编译、EditMode、消费项目验收仍未完成；Python/静态检查不能替代这些结果。本次技能与文档改动未新增 Unity 编译验证。
