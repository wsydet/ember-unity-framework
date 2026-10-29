# Ember 0.25.0

2026-10-29。框架与七个模板的源码发行记录，发布引用为 annotated `v0.25.0`。模板切换由用户确认，内容保存、封存及父级同步全部通过项目中心完成；远端结果以该 tag 的实际提交为准。

本次交付可组合扩展、FSM 蓝图与关卡工具的正式启用门禁、通用房间图编辑适配，以及直接继承 base 的 topdown-2d 和 lane-defense-2d 基础模板。产品世界、剧情、养成、特殊战斗规则仍由消费项目实现。

## 框架变化

- `EmberExtensionCatalog` 求解依赖、循环和兼容；`EmberExtensionProject` 在项目中心保存正式组合。schema v2 绑定模板精确版本、扩展契约版本和源码 SHA256，包含传递依赖；未启用或绑定失效时阻止工具、Skill 和专属文档访问。
- FSM 蓝图使用正式扩展 `ember.fsm.blueprint`。旧 Register 保留兼容，但不能绕过扩展开关；参数、观察源和业务接线继续检查模板及模块条件，旧窗口按版本指纹拒绝操作并保留草稿。
- 关卡工作台、资产、地形、对象和试玩入口使用同一门禁；关闭扩展后仍保留已持有会话的停止/清理能力。
- `IEmberRoomGraphAdapter` 投影项目原有图资产，提供任意数量端口、可选十二槽预设、排版与连接校验、外部修改冲突、Undo 和失败回滚。图布局与实际世界坐标、关卡加载和产品初态分别持有。
- 修正三项发布回归的测试夹具，使其显式建立 FSM 扩展条件并恢复原始配置字节，可在默认关闭扩展的模板上运行。

公共使用入口：[可组合扩展与迁移](../../Packages/com.ember/Documentation~/manual/composable-extensions.md)、[FSM 蓝图能力登记](../../Packages/com.ember/Documentation~/manual/fsm-blueprint-access.md)。房间图样例随包位于 `Documentation~/samples/WindwardRoomGraphAdapter.cs.txt`，尚未在 Windward 编译验证。

## 模板与父基线

所有模板框架兼容声明为 `0.25.0`。所有派生模板通过项目中心正式同步至 **base 0.10.1**，父内容 hash 为 `e9d789311cad169a19b6a76331a2877a`；保留子模板自有扩展组合与既有 GUID。

| 模板 | 内容版本 | 实际内容与封存 hash |
| --- | --- | --- |
| base | 0.10.1 | `e9d789311cad169a19b6a76331a2877a` |
| lane-defense-2d | 0.1.1 | `608dc159f0dd54fc09455ea2e4d0529e` |
| platformer-2d | 0.13.1 | `466b7434d6dc8bcf6fa55141b32ccc8f` |
| source3d-2p5d | 0.6.1 | `9be0d1035d4396e096a3eaa820765eed` |
| topdown-2d | 0.1.1 | `406816c22a27be1fd281be126732d93e` |
| ui-game | 0.2.1 | `43030af8571b29f5c1d2d0631faa1733` |
| visual-novel | 0.22.2 | `437a3552583df64cf2ab3d1c26083864` |

lane-defense 提供棋盘、固定分路敌人、默认同路索敌、卡牌放置、资源与冷却、基础战斗、波次、胜负、暂停、重开及 Framework → Main → Gameplay 正式启动流程。`IPlacementRule`、`IAttackPolicy`、`IProjectilePolicy`、`IHitRule` 与策略提供者供项目扩展；会话唯一更新、`ProjectileContext` 唯一命中提交和会话清理所有权明确，二维跨行弹道受支持。同路索敌没有成为全部弹丸的底层运动限制。

lane-defense 场景位于 `Assets/Game/Scenes/{FrameworkScene,MainScene,GameplayScene}.unity`，样例资源位于 `Assets/GameResource/Resources/Config/LaneDefense`，模块位于 `Assets/Game/Module/LaneDefense`，正式 EUI 位于 `Assets/Game/UI/Runtime/Module/LaneDefense`。精确资源路径与部署检查见[交接](lane-defense-2d-template-handoff.md)。彩色弹丸、转向、反射、分裂、传送、合成、特殊护盾、养成与剧情不在模板或公共层内。

topdown 提供独立玩家与武器状态机、固定射击基础、关卡示例、世界快照与检查点恢复接线；具体交付与边界见[交付审查](topdown-2d-delivery-review.md)。两个新模板均为 preview，不代表完成消费项目或发行平台验收。

platformer/topdown 的启用组合和受控文档需与封存版本一致。base、source3d、ui-game、visual-novel 保留已保存的空扩展组合，仅基础设施生效；其旧 owner 版本不授予任何可选能力。首次启用或升级后签收组合时，在项目中心审阅并保存，生成当前版本及源码绑定，不手改 hash 或借用其他模板的覆盖配置。

## 验证与限制

- 通过 Unity MCP 显式请求编译，并核对空闲状态与加载的程序集；本轮修正夹具后确认新测试程序集及新增 SetUp/备份字段已加载，再读取 Console。保留 NuGetForUnity 访问 OpenUPM 的 ECONNRESET 错误，未将依赖联网解析描述为全部成功。
- 公共扩展/FSM/房间图/Skill 门禁定向范围首次 **26/29**，三项夹具失败修正后相关 **5/5** 复验通过，两批有重叠。platformer 新版绑定后 **4/4**、topdown 新版绑定后 **2/2** 隔离回归通过。
- lane-defense 同业务源码已有 **35/35** EditMode、正式启动、鼠标放置与 UI 阻断、两套棋盘/波次的胜负、暂停、重开、回主页清理及 16:9/4:3 画面证据。本轮版本绑定更改没有冒充重新运行这些测试；范围见[实施记录](lane-defense-2d-template-progress.md)与[验证目录](lane-defense-2d/evidence/F6)。
- 最终封存后再次通过 MCP 核对七个实际内容 hash、封存 hash、六份父快照与父指针，全部一致，谱系零问题。维护工程留在 topdown-2d 0.1.1，编辑副本未过期；新的 Player.Editor MVID 为 `f74763dc-c9da-4170-a807-761b0fbbc8b6`，编辑器非编译/非更新状态。
- 候选 index 与提交 `629d0f9c4425e20d6f22753c2375899645fa07a6` 的共享字体二进制/配置检查通过，四份 TTF 的结构、hash 及两种 autocrlf 干净检出通过；相关公共 C# 的暂存差异空白检查通过。该提交的干净 Codex 工作树中正式 `build-ai-skill-bundle.ps1 -Check` 通过：11 项公共技能、33 文件。公共技能源和 bundle 内容未改变，声明源码提交 `5868837542df759baf6ffd9412c7ae10cf7ee66b`；本版最终验证补记仅修改文档，未改变这些已验证资源、代码或模板。详见[准备记录](release-0.25.0-preparation.md)。

没有执行完整测试集、干净消费项目部署、此次发行构建、全部模板交互矩阵或真实设备验收。上一版 Windows 原生退出 `0xC0000005` 仍未解决，不随此次发布自动关闭。

## 消费项目接入

1. 保护本地业务、配置和未提交修改。已有项目通过 `Ember/UPM Manager` 升级框架至正式发布后的 `v0.25.0`，确认实际包版本并完成编译。框架升级不会自动更新已部署模板。
2. 第三方依赖沿用 0.24.3 的 57 项声明，按需合并而非覆盖整份 manifest；Odin、DOTween 等权限与许可证由项目确认。2D 示例使用 URP 17.5.0，随附完整 manifest 已包含该依赖。
3. 新项目通过项目中心部署所需模板；已有项目先审阅模板三方增量，保护定制和 GUID，不直接覆盖业务。模板加载/切换保留人工弹窗确认。
4. 在项目中心核对正式模板身份、扩展组合与版本指纹，必要时审阅保存组合；确认工具、Skill、文档及模块条件匹配。
5. 检查 Build Settings、场景映射、资源、EUI 和输入配置，再运行正式 Framework 启动链。lane-defense 按交接记录验证放置、胜负、暂停、重开与清理；跨行弹道接入项目策略时遵守唯一更新、命中提交和清理所有权。
6. 在目标平台完成消费回归和构建验收。此次发布不替 Windward 升级框架或重新部署模板。
