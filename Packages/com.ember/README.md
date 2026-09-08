# Ember Framework（com.ember）

Ember 是以单个 UPM 包交付的 Unity 游戏框架，包含事件、资源、UI、场景、音频、相机、输入、状态机、编辑器工具和业务模板。

框架必要的 `IEmberManager` 在 Init 阶段统一启动；可选的 `IEmberModule` 按特性的 Enabled 与 Phase 装配。
详见 [Manager 与 Module](Core/Runtime/Manager/README.md)。

## 安装

当前发布版本为 **0.11.5**，开发基线使用 Unity `6000.5.4f1`：

```json
{
  "com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.5"
}
```

把该条目加入项目 `Packages/manifest.json` 的 `dependencies`。开发仓库自身使用 embedded `file:com.ember`。
本地未提交的新功能不在旧 tag 中；安装 tag 时以该 tag 随附的文档、模板和依赖为准。

Odin Inspector 和 DOTween 是当前代码的前置依赖，需要合法取得并安装。
`Ember/UPM Manager` 提供团队依赖检测入口；私有仓库需要访问权限。UniTask 随包内置，避免重复导入。
UniRx 从 OpenUPM 解析，消费项目需配置 `com.neuecc` scope。Unity Registry 依赖由 package.json 声明。

Rainbow 两包、Console Pro、InputDeviceDetector、Feel 也已确定纳入团队第三方交付，统一存放于私有 `ember-thirdparty-upm`，不内嵌到本包。Feel 目前仅完成本地 UPM 封装，未切换工程或发布；升级本包不会自动同步所有第三方依赖，详见开发仓库的包清单与交付维护文档。

完整工程的 56 项直接依赖见随包 [消费端依赖声明](Dependencies~/README.md) 和 [manifest 基线](Dependencies~/manifest-0.11.5.json)。0.11.5 未改变第三方包内容，因此继续复用已发布的第三方基线 `ember-v0.11.1`；消费端按包名合并清单，保留自己的其他依赖，不能用该文件覆盖整份项目 manifest。

## 能力与文档

| 子系统 | 文档 |
|---|---|
| Core：启动、事件、服务、Module、状态、Update、时间 | [Core](Documentation~/core/README.md) |
| Resource / Scene | [资源](Documentation~/resource/README.md)、[场景](Documentation~/scene/README.md) |
| Audio / Camera / Input | [音频](Documentation~/audio/README.md)、[相机](Documentation~/camera/README.md)、[输入](Documentation~/input/README.md) |
| UI / UIExtension | [UI](Documentation~/ui/README.md) |
| SceneUI | [接入](Documentation~/scene-ui/README.md)、[API](SceneUI/README.md) |
| Core.Editor / FrameworkTools / UPMManager | [编辑器](Documentation~/core/README-Editor.md) |
| Basic / Extensions / 内置 UniTask | 集合、池、数据结构、存储、日志、扩展和异步基础 |
| UnityFarm 改动回流 | [项目 / 框架 / 模板升级判定规则](Documentation~/maintenance/unityfarm-change-routing.md) |

各目录通过 `.asmdef` 隔离 Runtime、Editor 和 Tests；包内子系统不等于可选业务 Module。

## 初始化与升级

当前开发版菜单为 `Ember/项目中心`。消费项目使用“项目初始化”部署兼容模板；embedded 开发环境额外显示“模板开发”。
已安装旧版时以其菜单为准。模板按“加载 → 修改项目 Assets → 保存 → 显式 Bump”维护。

框架升级使用 `Ember/UPM Manager` 选择已发布版本，由 Package Manager 解析依赖。
框架包升级不会自动把新模板内容合并进已有用户代码；消费端可选择同模板“补齐缺失”，或在确认覆盖后执行“完整重新部署”。后者会事务替换五个模板管理目录，适合应用 0.11.4 的 GUID 修复，但会覆盖这些目录中的项目修改。UnityFarm 开发中发现改动时，先按[改动回流规则](Documentation~/maintenance/unityfarm-change-routing.md)判定项目、框架和模板归属。
不要把删除整个 `packages-lock.json` 当作常规升级步骤。

开发仓库的完整资料见 [文档索引](../../docs/README.md)、[包维护](../../docs/dev/upm-migration-plan.md) 和
[模板体系](../../docs/dev/template-upgrade-system.md)；Git URL 仅安装本包时仓库级 docs 不随包交付。
