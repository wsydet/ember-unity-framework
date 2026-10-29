# lane-defense-2d 消费交接与待发布清单

日期：2026-10-29。此交接对应 **com.ember 0.25.0 / lane-defense-2d 0.1.1（preview）**，已通过项目中心保存、封存及父同步。发行引用为 `v0.25.0`；独立消费验收仍未完成。发布范围与验证见 [0.25.0 说明](release-0.25.0.md)。

## 实际包、模板与父基线

| 字段 | 实际值 |
| --- | --- |
| 包 | com.ember 0.25.0，发行引用 v0.25.0 |
| Git 发行引用 | annotated v0.25.0；实际提交使用该 tag 的 peeled commit，不使用实施前的旧 HEAD |
| package.json SHA-256 | 1C3ACA269047831CE954658B502539203EA6A69E5FE5DBC1732E690A7D54551F；仅为包声明文件指纹，不是完整发布包 hash |
| 模板 ID / version / channel | lane-defense-2d / 0.1.1 / preview |
| 模板声明 frameworkVersion | 0.25.0，经正式父同步继承 |
| 实时 contentHash | 608dc159f0dd54fc09455ea2e4d0529e |
| 封存 versionedContentHash | 608dc159f0dd54fc09455ea2e4d0529e |
| 父基线 | base 0.10.1 / e9d789311cad169a19b6a76331a2877a |
| 父实时/封存 hash | 均为 e9d789311cad169a19b6a76331a2877a |
| ParentSnapshot 实测 hash | e9d789311cad169a19b6a76331a2877a |
| 中心保存/封存 | 795 文件；发布绑定保存时间 2026-10-29 15:13:56；正式 Bump 0.1.1；最终实际内容、封存 hash 与父快照已通过 MCP 核对 |
| 开发环境 | Unity 6000.5.4f1；URP 17.5.0；Input System 1.19.0；Cinemachine 3.1.7 |

base 0.10.1 最初单独封存已核对的 3 个默认扩展声明文件。之后用户授权一并发布当前框架及全部已保存模板变更，七个模板兼容声明统一到 0.25.0，派生模板已通过正式父同步接收 base 0.10.1。

## 内容和程序集

运行源码：`Assets/Game/Module/LaneDefense/Runtime/`，程序集 `Game.LaneDefense.Runtime`。包括 LaneDefenseModule、LaneDefenseSceneParticipant、BattleSession、BoardModel/BoardProjection、冻结关卡定义和消费策略接口。

Editor：`Assets/Game/Module/LaneDefense/Editor/`，程序集 `Game.LaneDefense.Editor`。入口为 `Ember/LaneDefense/塔防开发工具`，受正式模板身份与模块开关控制；已打开窗口捕获身份失效时禁止写入/试跑并保留选择。

Tests：`Assets/Game/Module/LaneDefense/Tests/EditMode/`，程序集 `Game.LaneDefense.Tests`。本轮没有运行全量工程测试，也没有把 MCP Play 试跑称为 UTF PlayMode 套件。

正式 UI 用户源码及 EUI 中心生成 Binding：`Assets/Game/UI/Runtime/Module/LaneDefense/`。四个组件为 EUILaneBattleHUD、EUILaneCardItem、EUILanePausePopup、EUILaneResultPopup。GamePages.User.cs 注册由中心维护。

该模板显式启用 base GameInputModule，未启用空的持久进度示例。首版只有按初始配置重开，不存储中途战斗或产品成长。

## 场景和资源

| 内容 | 正式路径 |
| --- | --- |
| 启动 | Assets/Game/Scenes/FrameworkScene.unity |
| 主菜单 / 玩法 | Assets/Game/Scenes/MainScene.unity / GameplayScene.unity |
| 主示例 | Assets/GameResource/Resources/Config/LaneDefense/TrainingYard.asset（5×9、2 波） |
| 第二配置 | Assets/GameResource/Resources/Config/LaneDefense/CompactYard.asset（3×6、3 波，含空波） |
| 输入 | Assets/GameResource/Resources/Config/LaneDefense/LaneDefenseActions.asset |
| 输入通道配置 | Assets/GameResource/Resources/Config/GameInput/GameInputConfig.asset |
| 单位/敌人/弹丸/格子 | Assets/GameResource/Prefabs/LaneDefense/ |
| EUI Prefab | Assets/GameResource/Resources/UI/Module/LaneDefense/Prefabs/ |
| 正式文档 | Assets/Game/Documentation/lane-defense-2d/ |

输入 map 为 Gameplay，Action 为 **Point / Place / Cancel**；对应指针位置、指针按下、Esc/鼠标右键。选卡/开始波次/拆除/暂停/重开/返回由正式 EUI 按钮发命令。棋盘预览不扣费；点击 UI 不向棋盘提交。

## 消费扩展接口

| 接口 | 实际签名 |
| --- | --- |
| IPlacementRule | bool Allows(in PlacementQuery query) |
| IAttackPolicy | bool TryGetVelocity(in AttackQuery query, out Vector2 velocity) |
| IProjectilePolicy | ProjectileStepResult Step(ProjectileContext context, in ProjectileSnapshot projectile, float delta) |
| IHitRule | HitDecision Evaluate(in HitRequest request) |
| ILaneDefensePolicyProvider | LaneDefensePolicies CreatePolicies(LaneLevelSnapshot level) |

消费 MonoBehaviour 实现 ILaneDefensePolicyProvider，并挂到场景参与者的 Policy Provider。每次候选/重开提供独立策略。构造 BattleSession 的纯规则测试也可直接传 LaneDefensePolicies。

默认敌人固定 lane，默认射手同 lane 前方索敌；投射物位置/速度是二维 Vector2，底层不限制同 lane。跨行运动、查询和命中提交分别使用当前 ProjectileContext 的 Move / TrySweep / SubmitHit，派生弹丸使用同一上下文 Spawn。

唯一更新者是 BattleSession 调度器；当前唯一处理者是选定 IProjectilePolicy；唯一命中提交者是该次同步 Step 的 ProjectileContext；唯一清理者是 BattleSession。替换后默认处理器不再运行，退出 Step 的上下文不可留作异步命中。TTL、边界、预算、终局和会话代次由模板强制检查。35 项用例包含跨行替换和禁止双扣血验证。

经济入口 Credit 带会话、事件 ID 与来源并去重；结果由 Completed 输出不可变 BattleResult。本次运行 ID 不替代跨进程持久奖励幂等账本。具体示例及所有权见随模板 [ExtensionGuide](../../Packages/com.ember/Templates~/lane-defense-2d/Assets/Game/Documentation/lane-defense-2d/ExtensionGuide.md)。

彩色弹丸、转向、反射、分裂、传送、合成、特殊护盾、剧情与产品成长均由消费项目实现，本候选未收录这些实现或 Timid Fish 旧代码。

## 实际验证

- 最后代码批次主动 Unity MCP 编译，新运行/Editor/UI/Tests 程序集 MVID 已读取；最终 Console error/warning 0。
- EditMode 35/35：LaneDefenseTests 与 LaneDefenseAssetTests，详见[完整结果](lane-defense-2d/evidence/F6/editmode-final-35.json)。
- 正式 Framework → Main → Gameplay；Input System 注入点击实际放置；UI 遮挡；暂停/恢复。
- TrainingYard 7 击杀胜利；CompactYard 空波与 5 逃逸失败；单份结果弹窗；新代次重开；退出不胜利并清理场景/实体/输入所有权；原 UI 相机归还。
- 2560×1440 与 1280×960 画面检查、窄比例拾取；当前模板菜单和窗口捕获身份失配。
- 保存后实际模板磁盘、编辑副本、metadata 的实时 hash 一致，ParentSnapshot 与父基线一致。

详细边界见随模板 [Validation](../../Packages/com.ember/Templates~/lane-defense-2d/Assets/Game/Documentation/lane-defense-2d/Validation.md) 和[实施记录](lane-defense-2d-template-progress.md)。

## 部署步骤

1. 使用已配置 URP 的 Unity 6000.5 工程。当前运行程序集依赖 Universal.Runtime；com.ember 包声明没有自动安装 URP，消费工程需显式准备兼容 URP。参考已测版本为 17.5.0。
2. 新工程安装包含本实现的 com.ember v0.25.0；已有消费工程通过 Ember/UPM Manager 升级并确认实际包版本。不要使用 v0.24.3 或实施前的旧 HEAD 代替本次发行。
3. 在 Ember 项目中心选择 lane-defense-2d，走正式部署及人工确认流程。不要复制 ParentSnapshot，不修改 PackageCache 或身份/hash。
4. 主动通过 Unity MCP 编译，确认域重载后检查 Console；核对活动部署身份、输入模块 Ready、场景映射、EUI 绑定和必要 Sprite。
5. 从 FrameworkScene 启动，经 Main 进入 Gameplay，验收放置、波次、胜败、暂停、重开、返回；再接入消费策略与产品资源。
6. 消费工程单独记录包/模板身份和 T12 结果。当前尚未提供专用独立消费工程，因此这些步骤尚未在独立工程执行。

## 未完成与发布准备

未完成：独立消费 T12、完整真实跨模板/派生/无效身份/模块禁用/消费身份矩阵、准备失败和取消的完整故障注入、强制失焦/触摸/手柄实机、Player 构建、压力场景与 CPU Profiler 分析。一次编辑器内存采样不能作为模板成本或帧率承诺。画面检查不是用户人工签收。

此次最终范围扩展为框架及所有已保存模板，框架版本采用 **0.25.0**；原 lane-only 的 0.24.4 提议不再适用。lane-defense 已封存 **0.1.1**，模板内容 hash 为 `608dc159f0dd54fc09455ea2e4d0529e`。本轮发布绑定只改默认扩展声明，沿用对应业务源码的 35/35 与正式流程证据，未把封存当作独立消费验收。全部模板身份、门禁、依赖声明、实际发布引用和消费升级步骤见 [0.25.0 发行记录](release-0.25.0.md)。
