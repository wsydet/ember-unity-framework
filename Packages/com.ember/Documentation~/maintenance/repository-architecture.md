# 仓库架构与模板开发约束

本规范由根 CLAUDE.md 指定为必读；从原规则迁移，未改变架构或保存授权。

## 项目概述

**ember-unity-framework** 是一个通用的 Unity 游戏开发框架，目标是：

1. **隔离式的框架层与业务层** —— 框架提供基础能力（事件系统、资源管理、UI 管理、场景管理等），业务层在此之上构建具体游戏逻辑，两者通过清晰的 API 边界解耦
2. **开箱即用** —— 其他人拿到框架后可以快速开始一个新项目，只需关注业务逻辑
3. **蓝图式可视化编辑** —— 长期目标是提供类似蓝图的节点编辑器，让策划/设计师也能通过可视化界面搭建游戏逻辑（基于 Unity Visual Scripting 或自定义编辑器扩展）

## 技术栈

| 类别 | 选型 |
|------|------|
| 引擎 | Unity 6000.x（Unity 6） |
| 渲染管线 | URP（Universal Render Pipeline） |
| 语言 | C# |
| 输入系统 | Unity Input System 1.19 |
| 可视化脚本 | Unity Visual Scripting（用于蓝图基础） |
| UI 系统 | uGUI / UI Toolkit |
| 测试 | Unity Test Framework |

## 架构理念

```
┌─────────────────────────────────┐
│          业务层 (Game)           │  ← 具体游戏逻辑，可替换
│   ┌──────────┐ ┌──────────┐     │
│   │ 战斗系统  │ │  背包系统  │ ... │
│   └──────────┘ └──────────┘     │
├─────────────────────────────────┤
│         框架层 (Framework)       │  ← 通用能力，稳定不变
│   ┌─────┐ ┌─────┐ ┌─────┐      │
│   │Event│ │ResM│ │UIMgr│ ...    │
│   └─────┘ └─────┘ └─────┘      │
├─────────────────────────────────┤
│           Unity Engine           │
└─────────────────────────────────┘
```

### Manager 与 Module 的组合模型

项目统一使用下面的架构定义：

```text
具体游戏 = 框架基础 + 必备 Managers + 按需选装的 Modules
```

| 概念 | Manager | Module |
|------|---------|--------|
| 定位 | 框架运行所必需的管理器和全局基础设施 | 可选、可组合的业务功能积木 |
| 接口 | `IEmberManager` | `IEmberModule` |
| 启动 | `InitState` 中由 `EmberManagerCollector` 反射发现，按 `EmberInitOrder` 调用 `Init` | `InitState` 先发现并构造启用模块，再于 Manager 初始化后调用 Global 模块的 `OnInit`；其他模块在对应 Phase 激活 |
| 生命周期 | Init 阶段启动，跨游戏状态持续存活，框架退出时逆序销毁 | 仅在所属 Phase 激活，退出 Phase 时销毁业务状态，实例可供再次进入时复用 |
| 模板关系 | 所有模板共同具备，不作为玩法选项裁剪 | 模板按玩法自由添加、移除或通过 `Enabled = false` 关闭 |

- Manager 定义的是框架基座。被定义为 Manager 的能力必须在 `base` 和业务模板中都可用，不能为了
  某个模板临时省略；例如 `EmberInputManager`。
- “必要”是架构约束，不代表 Collector 内置了固定 Manager 清单；Collector 只会初始化当前已加载
  程序集中实际存在的实现，因此 Package 和模板维护者要负责保证必要 Managers 没有缺失。
- Module 定义的是业务组合单元。不同模板可以在同一套框架和 Managers 上装配不同 Modules，形成
  不同类型的游戏；例如 `PlayerControlModule` 只属于需要该操作方式的玩法模板。
- **分类依据是职能与可选性，不是启动阶段。** Manager 是框架不可按玩法裁剪的必要基座；Module
  是可以像积木一样选装、移除或禁用的业务能力。某个游戏必需的业务能力，不等于所有模板必需的 Manager。
- **Init 阶段也会启动 Module。** `[EmberModule(ModulePhase.Global, Enabled = true)]` 的模块在
  `InitState` 内启动，并可常驻到退出；提前启动或全局常驻都不会改变它的 Module 身份。
  `Enabled` 决定是否装配，`Phase` 决定何时激活；`ModulePhase.Global` 不是新增的顶层游戏状态。
- 新增业务 Module 必须显式声明 `[EmberModule(ModulePhase.对应阶段, Enabled = true/false)]`，不得省略 `Enabled` 依赖默认值；依据当前项目是否使用该模块填写明确的布尔值，配套编辑器入口也必须服从该启用开关。
- Module 可以消费 Manager 提供的通用能力；Manager 不依赖具体业务 Module。不要为了提前拿到实例
  而把可选业务功能实现成 `IEmberManager`。
- “发现并构造”不等于“已激活”：Module 只有 `OnInit` 成功后才进入活动状态并接收 Update。
- `Enabled` 是启动扫描时的类型级装配开关，不是运行时热插拔接口；当前内置自动接线覆盖 Global
  与 Gameplay，Main 或自定义 Phase 需要由对应状态显式驱动。

### 模板开发落盘规范

- `Packages/com.ember/Templates~/*/Assets` 与派生模板的 `ParentSnapshot~` 是模板系统管理的快照，
  不是日常业务开发目录。业务内容必须先在项目 `Assets` 中修改，再通过项目中心的模板保存操作落盘。
- **模板保存与 Bump 必须通过项目中心完成。** 正常流程固定为“通过项目中心加载模板 → 修改项目
  `Assets` → 通过项目中心保存模板 → 通过项目中心显式 Bump 版本”。普通保存只更新实时
  `contentHash`，Bump 才把内容封存进 `versionedContentHash`。
- 禁止直接编辑已储存的模板文件（包括模板内脚本、资源、文档和 Skill），禁止用文件复制、补丁、
  自写脚本或手改 `template.json` 的版本号、hash、父级指针代替项目中心的保存或 Bump。否则会破坏
  根模板、派生模板 `ParentSnapshot~`、父级指针和当前编辑记录之间的谱系一致性。
- 如果项目中心报告磁盘内容与 metadata 不一致，应先确认真实差异，将需要保留的内容回到项目
  `Assets` 维护源，再通过项目中心的正式流程保存和 Bump，并核对派生模板父基线与编辑记录。
  不得以“修复 metadata”“同步文档”或“快速补丁”为由直接修补快照或 hash。
- 项目中心不可用、操作失败或当前模板身份不明确时，保留项目 `Assets` 改动并报告保存/Bump 尚未
  完成；不得绕过中心落盘，也不得为使直接修改的快照生效而要求用户重新部署模板。
- 保存、Bump、提交、发布和消费项目升级分别按任务授权执行；仅授权修改内容或普通保存，不等于
  授权 Bump。交付时说明项目修改、中心保存与 Bump 各自是否完成，不把文件已改当作模板已保存。

### 模板专属文档、Skill 与工具隔离

- 新内容先确定框架/base/玩法模板所有者，按[统一隔离规范](../../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md#模板专属文档skill-与-tool-的隔离)维护、分发和验收。模板文档与技能源随模板管理，发现副本不作为维护源；专属 Tool 位于模块 Editor 程序集。
- 专属菜单统一为 `Ember/<业务分类>/<工具名>`，不放 `Tools/Ember` 或注册重复入口。

### 模板专属工具可见性

- 模板专属编辑器工具只在所属活动模板体系（含合法派生模板）显示入口，非所属模板必须隐藏，不能仅置灰。
- 复用`EmberProjectSetup.IsTemplateActive`区分开发端编辑身份和消费端活动部署身份；禁止以残留脚本、资源或场景推断模板。
- 菜单、窗口、Inspector/蓝图快捷入口及写入/试跑操作使用一致校验；模板变化后旧窗口安全失效并保留草稿。公共工具不受业务模板限制。
- 完整约束及验收要求见[业务代码与资源目录规范](../../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md#模板专属编辑器工具的可见性与入口)。

### UnityFarm 改动回流规范（强制阅读）

从 UnityFarm 发现、验证或提出任何改动时，开始修改前必须完整阅读
[`Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md`](../../../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)。

必须先判定改动属于“仅 UnityFarm 项目”“框架升级”“模板升级”或“框架 + 模板升级”。不得直接修改
UnityFarm 的 PackageCache，不得把产品专用业务误收进通用框架或模板，也不得把升级框架误认为已部署
模板会自动更新。模板内容随 `com.ember` 交付，因此任何模板发布也必须配套新的框架 tag。

### 核心设计原则

- **依赖方向**：业务层 → 框架层 → 引擎，禁止反向依赖
- **单包交付**：框架统一由 `Packages/com.ember` 交付，并用 `.asmdef` 划分内部子系统；文档中的
  `IEmberModule` / Module 专指业务层可选积木，不等同于 Package 或框架程序集
- **程序集隔离**：通过 `.asmdef` 严格划分框架层和业务层的编译边界
- **接口驱动**：框架提供接口，业务层实现；框架不依赖业务层的具体类型

## 业务代码与资源目录规范

新增功能及目录整理必须遵循包内公共规范
[`business-directory-layout.md`](../../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)。

- 主要业务代码按职责分模块放到 `Assets/Game/Module/<模块名>/`。
- UI 逻辑放在 `Assets/Game/UI/Runtime/Module/<模块名>/`，**`Runtime` 是必需层级**，不得省略。
  **所有正式 UI 的创建及代码生成
  必须通过 UI 中心（EUI 开发中心）完成**，覆盖页面、弹窗、Item、Prefab 骨架、Binding 和配套注册。
  保持 UI 中心生成的 `Game/UI/Runtime/Module` 结构和模块归属。按实际 Binding
  编写用户逻辑，调整绑定后通过中心重新生成，不手工拼造生成文件或绕开中心另建 UI 骨架。
- 业务资源放到 `Assets/GameResource` 下对应的资源类别和模块位置。当前模块 UI 资源为
  `Resources/UI/Module/<模块名>/`；图片放其 `Atlas`，通用图片与同组通用控件图标放
  `Resources/UI/Common/Atlas/`；配置、音频分别归 `Resources/Config/<模块名>/`、
  `Resources/Audio/<模块名>/`。
- 模板名称不作为混放各类内容的顶层业务目录；项目公共接线及配表生成目录继续遵循各自规则。
- 实际迁移时保持生成配置、Prefab ClassPath、页面注册和资源加载路径一致，保留 GUID；不为匹配
  文档示意而自行修改 UI 中心生成的路径。

```
Assets/
├── Game/                           # 业务层（示例/模板）
│   ├── Module/<模块名>/            #   主要业务代码
│   ├── UI/Runtime/Module/<模块名>/ #   UI 逻辑与 Binding，Runtime 必需，由 UI 中心生成
│   └── UI/Editor/<模块名>/         #   UI 编辑工具
├── GameResource/                   # 按资源类别、模块与 UI 归属组织
├── Editor/                         # 项目级编辑器配置
└── ThirdParty/                     # 非 UPM 第三方内容

Packages/
└── com.ember/                       # 统一框架 Package
    ├── Core/                        #   生命周期、状态机、Manager、Update
    ├── Resource/ Scene/ Audio/      #   框架必要 Managers 与通用能力
    ├── Camera/ Input/ UI/           #   框架必要 Managers 与通用能力
    ├── SceneUI/                     #   可由业务 Module 持有的通用引擎
    └── Templates~/                  #   base 与玩法模板
```
