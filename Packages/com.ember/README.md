# Ember Framework（com.ember）

Ember 是以单个 UPM 包交付的 Unity 游戏框架，包含事件、资源、强类型配置表、UI、场景、音频、相机、输入、状态机、编辑器工具和业务模板。

框架必要的 `IEmberManager` 在 Init 阶段统一启动；可选的 `IEmberModule` 按特性的 Enabled 与 Phase 装配。
详见 [Manager 与 Module](Core/Runtime/Manager/README.md)。

## 安装

当前发布：Ember **0.21.5**，交付已封存的 **platformer-2d 0.11.2** 地面支撑修复。其他模板与第三方依赖不变；Windward接入及原台面人工复验待完成。

```json
{
  "com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.21.5"
}
```

首次安装时把该条目加入项目 `Packages/manifest.json` 的 `dependencies`；已有项目升级必须通过 `Ember/UPM Manager`。开发仓库自身使用 embedded `file:com.ember`。
本地未提交的新功能不在旧 tag 中；安装 tag 时以该 tag 随附的文档、模板和依赖为准。

Odin Inspector 和 DOTween 是当前代码的前置依赖，需要合法取得并安装。
`Ember/UPM Manager` 提供团队依赖检测入口；私有仓库需要访问权限。UniTask 随包内置，避免重复导入。
UniRx 从 OpenUPM 解析，消费项目需配置 `com.neuecc` scope。Unity Registry 依赖由 package.json 声明。

Rainbow 两包、Console Pro、InputDeviceDetector、Feel 也已确定纳入团队第三方交付，统一存放于私有 `ember-thirdparty-upm`，不内嵌到本包。可选包可通过 UPM Manager 按需安装；升级本包不会自动同步所有第三方依赖，详见开发仓库的包清单与交付维护文档。

完整工程的 56 项直接依赖见随包 [消费端依赖声明](Dependencies~/README.md)；已发布安装基线为 [0.21.4 manifest](Dependencies~/manifest-0.21.4.json)，本地候选为 [0.21.5 manifest](Dependencies~/manifest-0.21.5.json)。候选只改变框架tag，保留0.21.4的MCP 10.3.0及其他依赖声明；首次安装时按包名合并清单，保留自己的其他依赖，不能覆盖整份项目 manifest。

0.21.5 正式发布后，已有 platformer-2d 0.11.1 项目需先通过 UPM Manager 更新框架，再对0.11.2进行模板三方补丁预览，保护定制。包升级不会自动修复已部署的 Player/Entity 源码，详见[本版准备说明](../../docs/dev/release-0.21.5.md)。

## Unity MCP 的安装与更新

0.21.4 的可选包目录指向官方 Unity MCP **10.3.0**：
`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0`。
它来自 CoplayDev 官方仓库，不属于团队 `ember-thirdparty-upm`，也不在 `com.ember/package.json` 的必装 dependencies 中。

- 新项目在 UPM Manager 的可选包入口按需安装；已有项目升级至 Ember 0.21.4 后获得此入口。旧版 0.21.3 的安装目录保持原样。
- 已安装 MCP 的项目升级 Ember **不会自动替换 MCP**。在 `Ember > UPM Manager > 可选第三方包` 查看实际安装版本与框架推荐版本。官方 Git 来源的旧稳定版会显示“升级到推荐 v10.3.0”，点击后通过 Unity UPM 更新到上述固定标签；相同版本不重装，较新版本不降级。推荐版本由框架随包目录维护，不代表联网查询的最新版本。
- 一键升级仅接受官方 HTTPS 仓库 `/MCPForUnity` 的默认分支（无 ref / `main`）或 `vX.Y.Z` 标签，且必须是项目直接依赖。本地、内嵌、Registry、Assets 导入、分叉仓库、自定义分支、固定提交及间接依赖保留原来源，需自行在 Unity Package Manager 管理；未知或预发布版本不猜测升级顺序。点击时重新核对实际注册包和磁盘 manifest，来源不一致、重复或无法安全识别的声明会拒绝操作。未解析但已在 manifest 声明的 MCP 也不会被“安装”覆盖。
- 升级复用现有 UPM 安装流程，解析期间禁用操作，失败显示 UPM 错误；注册事件、窗口重新获得焦点或“刷新安装状态”会重新读取实际版本。UPM 请求完成仅表示解析请求结束，请等待 Unity 导入、编译和域重载完成，再确认插件版本及连接。升级可能暂时中断 MCP 连接，按[公共重连规则](Documentation~/maintenance/unity-validation.md#unity-mcp-断连恢复)恢复；该入口不修改服务器来源、端口或客户端配置。
- Unity 插件和 Python 服务端分别确认版本。若 MCP 设置中的服务器来源覆盖仍固定旧版，需要显式更新至 `mcpforunityserver==10.3.0` 并通过官方服务管理重启。服务器设置和端口可能被同机多个项目共享；服务端更新不等于这些项目的 Unity 插件已更新。
- 历史 Dependencies~/manifest-0.21.3.json 是已发布基线，不用当前开发状态改写它。不要替换消费项目的整份 manifest，也不要在 PackageCache 修改源码。

## 消费项目公共规则（所有模板）

对话用语见[通用术语字典](Documentation~/maintenance/glossary.md)，玩法词汇按正式活动模板读取其 Assets 文档。代码修改后必须主动请求 Unity 重编译，确认本轮编译/域重载完成后读取 Console，再进行验证。

[项目测试与 Unity 编译验证规范](Documentation~/maintenance/unity-validation.md) 随框架包交付，适用于任意模板及未部署模板的项目。默认仅运行新增或相关部分测试；全量重测或预计耗时较长需要用户明确授权。MCP 断连后每隔 30 秒重试，最多 10 次，全部失败后停止并请用户确认 MCP 状态。

安装或升级到包含本功能的框架版本并完成 Unity 编辑器编译/域重载后，框架自动同步项目根目录的 `AGENTS.md` 和 `CLAUDE.md`，无需手动补入口。不存在时创建；已存在时追加或更新 `<!-- EMBER:PROJECT-RULES:BEGIN -->` 与 `<!-- EMBER:PROJECT-RULES:END -->` 之间的受管区块。区块外的自定义规则原样保留，自定义规则请写在区块外。

入口链接使用 UPM 实际解析的包位置，兼容 PackageCache、embedded 包和外部本地包。每次编辑器初始化都会核对入口，包位置变化会更新链接，内容相同时不写文件。同步独立于技能 bundle 和模板部署，不写 `Assets/Game` 或模板快照，切换模板不移除入口。框架开发仓库继续维护自己的入口，自动同步会跳过。

文件编码与原有内容保留（无 BOM 时按严格 UTF-8 读取，支持 UTF-8/UTF-16/UTF-32 BOM）；遇到标记损坏/重复、不支持的编码、只读文件或符号链接时保留文件并报告失败。可在 `Ember/UPM Manager` 的 AI Skill 区域查看同步状态，修复问题后点击“重新同步项目公共规则入口”。已打开的 AI 会话可能缓存旧规则，需要重新加载会话才能发现新入口。未打开 Unity 或编译未完成时，同步尚未执行。

本地未发布修改不会更新旧 tag；消费项目需要升级到包含本实现的框架版本，无需重新部署模板。

## 能力与文档

| 子系统 | 文档 |
|---|---|
| Core：启动、事件、服务、Module、状态、Update、时间 | [Core](Documentation~/core/README.md) |
| Resource / Scene | [资源](Documentation~/resource/README.md)、[场景](Documentation~/scene/README.md) |
| Audio / Camera / Input | [音频](Documentation~/audio/README.md)、[相机](Documentation~/camera/README.md)、[输入](Documentation~/input/README.md) |
| 多语言中心 | [编辑、注册与辅助翻译](Documentation~/manual/localization-center.md) |
| UI / UIExtension | [UI](Documentation~/ui/README.md) |
| SceneUI | [接入](Documentation~/scene-ui/README.md)、[API](SceneUI/README.md) |
| Table | [强类型配置表](Table/Documentation~/table/README.md) |
| Core.Editor / FrameworkTools / UPMManager | [编辑器](Documentation~/core/README-Editor.md) |
| Basic / Extensions / 内置 UniTask | 集合、池、数据结构、存储、日志、扩展和异步基础 |
| AI Skill 独立更新 | [安装、来源与备份](Documentation~/maintenance/ai-skills.md) |
| UnityFarm 改动回流 | [项目 / 框架 / 模板升级判定规则](Documentation~/maintenance/unityfarm-change-routing.md) |

各目录通过 `.asmdef` 隔离 Runtime、Editor 和 Tests；包内子系统不等于可选业务 Module。

## 初始化与升级

当前开发版菜单为 `Ember/项目中心`。消费项目使用“项目初始化”部署兼容模板；embedded 开发环境额外显示“模板开发”。
已安装旧版时以其菜单为准。模板按“加载 → 修改项目 Assets → 保存 → 显式 Bump”维护。

**禁止直接修改项目的 manifest 文件来升级，所有的消费端升级都必须通过 `Ember/UPM Manager`。**
选择已发布版本执行升级，由升级器与 Package Manager 管理依赖及锁文件；不得手改 manifest 的版本/Git URL 或 packages-lock 的提交 hash。无法操作升级器时应由用户在 Unity 中执行，不能退回直接修改文件。上面的首次安装步骤不适用于已有消费项目升级。
框架包升级不会自动合并已部署的 Assets。本版交付 base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.20.1 与 platformer-2d 0.8.0，兼容声明沿用 0.20.0。同模板 major.minor 内的 patch 可走三方增量；跨 minor 须先备份定制内容再迁移，保护资源 GUID、剧情和专用图布局。UnityFarm 开发中发现改动时，先按[改动回流规则](Documentation~/maintenance/unityfarm-change-routing.md)判定项目、框架和模板归属。
旧小说项目还需迁移 Assets 中同 GUID 的思源宋体，避免与包内字体并存，见[字体皮肤迁移](Documentation~/manual/font-skins.md)。

开发仓库的完整资料见 [文档索引](../../docs/README.md)、[包维护](../../docs/dev/upm-migration-plan.md) 和
[模板体系](../../docs/dev/template-upgrade-system.md)；Git URL 仅安装本包时仓库级 docs 不随包交付。
