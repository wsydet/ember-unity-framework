# topdown-2d 本轮交付审阅

> 2026-10-29 发行续接：用户已授权发布当前框架及全部已保存模板。最终版本、模板封存 hash、验证与限制以 [Ember 0.25.0 发行记录](release-0.25.0.md) 为准；下方未发布/未 Bump 表述保留实施当时的历史语义。

状态：T0—T5 开发端实现已完成，已按用户新增要求将攻击归属调整为武器独立状态机。T6 已完成当前工程相关测试、资源/中心检查、正式启动、隔离存档实播、新增武器版本的Editor人工体验，以及用户另行授权的项目中心普通保存。用户暂不构建；版本封存、Windows Player及干净消费工程验收尚未执行，不能称为已发布模板。

## 最终实现

活动身份为 `topdown-2d 0.1.0 preview`，通过项目中心创建，直接继承 `base 0.10.0`，与 platformer-2d 平级。业务全部位于活动 Assets，未手改 Templates~、ParentSnapshot~、模板版本或 hash。

- 正式 Framework→Main→Gameplay；独立候选准备、失败/取消清理、退出/重入和原位恢复，相机模块独立。
- 二维移动、独立鼠标/摇杆瞄准、平面扫掠与碰撞、角色方向动画、方案捕获和独立复制。
- 玩家图8节点：Alive、Locomotion(Idle/Move)、Action(Dash)、Hit、Dead；竞技场装配Focus为9节点。玩家图没有攻击节点，角色Powers没有attack资源。
- 每把武器独立拥有Definition、Graph、状态实例、fire资源、攻击方向、命中窗口与去重；默认各4节点（ready/windup/active/recovery）。训练剑锁移动/朝向，训练手枪允许移动瞄准射击，Q/左肩键切换。
- 近战多Hurtbox去重；远程示例为即时射线，实体墙遮挡、最近敌人命中和短暂黄色射线表现。角色动作可抢占武器租约，武器不能抢占其他角色/项目动作；切换、受击、死亡、冲刺释放自己的控制和残留命中，不退款。
- 统一场景结算、敌人追逐/预告、受击与死亡、独立暂停所有权、检查点与世界进度、保存退出/继续/读取/重试和Drain。
- 快照v2同时保存当前武器ID及每把武器开放/次数/CD；角色idle/move且所有武器ready才接受保存。此前未发布的v1开发存档明确不兼容，不静默迁移或覆盖。
- 正式EUI HUD/主菜单继续；玩家工具支持武器装配、图/参数及独立复制；蓝图分别观察角色和武器真实实例。两个关卡、Undo/Redo、隔离开发存档和试玩现场恢复。

未包含产品提灯、污染、茧、烛龙或剧情。未整体复制平台玩家或扩大base战斗职责。首版武器是固定装配列表、单个当前装备，不包含背包拾取、弹匣、换弹、实体弹丸、自动连射或完整武器库。扩展协议见 [武器文档](../../Assets/Game/Documentation/topdown-2d/Weapons.md)。

## 差异与保护

本次授权审阅的是 **369个文件差异**，包含脚本、资源及meta。通过项目中心正式SaveTemplate已普通保存，共复制1050个文件（含继承内容）；保存后中心比较为 **0差异、0错误、0警告**，完整Scan为 **0差异、0错误、1警告**，唯一警告为“已保存但未封存到版本”，未越权Bump。保存前路径/哈希见 [保存前比较](topdown-2d-evidence/t6-center-comparison.txt)、[保存前校验](topdown-2d-evidence/t6-center-validation.txt)、[审阅文件清单](topdown-2d-evidence/t6-active-template-file-manifest.json)；当前结果见 [保存回执](topdown-2d-evidence/t6-center-save-receipt.json)、[保存后比较](topdown-2d-evidence/t6-center-after-save-comparison.txt)、[保存后校验](topdown-2d-evidence/t6-center-after-save-validation.txt)。

保存版本保持 `0.1.0 preview`，parent保持 `base 0.10.0`。实时contentHash为 `5ef1d9ae0042f8852d4b34566d7cd8bc`；versionedContentHash仍为 `61cbe49aa797b1604f293b9756674566`，未封存本轮实现。保存前存储快照及编辑记录保存在 `Library/EmberBackups/topdown-2d-before-first-save/`，zip SHA256为 `46D95918444E6F1B1AC3F7F6198498B9405A0560DA49DEA39797C3FF087AB0E4`。698个ParentSnapshot文件逐文件哈希及文件数均未变化；369个已审阅活动文件也未被保存过程改写。

| 归属 | 主要路径 |
|---|---|
| 玩家、武器、状态、进度、扩展、工具与测试 | `Assets/Game/Module/Player/`，武器位于其 `Weapons/` |
| 选择性移植/适配的战斗原语 | `Assets/Game/Module/Entity/` |
| 独立相机及关卡制作 | `Assets/Game/Module/PlayerCamera/`、`Assets/Game/Module/LevelAuthoring/` |
| 正式状态及EUI接线 | `Assets/Game/State/`、`Assets/Game/UI/` |
| 角色/武器配置、场景、输入、Prefab | `Assets/GameResource/`、`Assets/Game/Scenes/` |
| 模板维护源 | `Assets/Game/Documentation/topdown-2d/` |

框架另有必要的两文件诊断修正：`EmberTemplateTransaction.cs` 保留提交与回滚双异常，为目录移动错误附源/目标；定向回归在 `EmberTemplateTransactionEditTests.cs`。不改变事务顺序、权限或失败时复制降级。

T0保护包仍为2962个逐文件验证文件，209.77MiB，SHA256 `377A988A6ABA724209D18937E0C9946AE7BF452A725FE2A881F7FA327D34A08E`，武器调整后再次核对一致。882个受保护非Assets文件仍仅Unity自动更新的 `.slnx` 不同；原平台和UI草稿保留恢复路径。武器调整前的活动实现另有 `Library/EmberBackups/topdown-2d-before-weapon/active-implementation.zip`，SHA256 `37ACC89F0FF44CBB31B163439C4E5E3E7E6F0F9A7AE01E021772850AC41505F5`。

默认PlayerGraph/PlayerPowers曾检出沿用原稿GUID，已通过Unity资产API克隆/重接修正，旧三份资源及meta另有备份。最终55个非base继承的新配置/Prefab meta与原平台草稿GUID复用为0（不将此结论扩展为所有源码GUID独立）。旧角色攻击定义/Skill四份资产在确认无引用后由AssetDatabase删除。EditorSettings临时域重载选项已恢复原enabled=true/flags=1。没有暂存或提交，整个Git工作区不能直接归为本任务。

## 本轮验证

| 范围 | 结果与证据 |
|---|---|
| 最终玩家/武器/快照/命中相关PlayMode | **39/39**，0失败/跳过，20.482秒；job `bc4872875c0a4be6bc4a883d3b78bbe3` |
| 关卡、身份、方案/武器独立复制Editor | **8/8**，0失败/跳过，27.154秒；job `9e1a692342584112a5ad94c8c39e155e` |
| 框架事务定向用例 | 本任务先前 **5/5**，1.372秒；job `4f16ba1b013b46a897c30dad3797085c`，武器批未改变这些源码 |
| 武器EUI链路 | 暂停drift=0、保存/换武器/读档/保存退出/继续成功，恢复手枪，正式存档哈希不变；`t6-weapons-live-chain.json` |
| 实际配对输入的移动射击 | 玩家move、武器active，向上移动0.978单位、朝右射击；`t6-weapons-paired-moving-shot.json` |
| 独立武器蓝图 | 4节点、真实路径active；见live-chain中的weaponBlueprint |
| 最新普通流程与Focus抢占 | 非试玩Arena、9个玩家节点、两把武器各4节点，Focus中断挥剑后会话有效；`t6-weapons-nontrial-ready.json` |
| 试玩停止与设置恢复 | 正式Stop后Active/Play/Bridge=false，Framework场景/选择恢复，设置恢复原值；`t6-weapons-trial-restored.json` |

每批C#/程序集均主动通过Unity MCP请求编译并确认本轮完成/域重载、读取Console。最后代码批started `1793204119574`、finished `1793204123368`、reload `1793204131508`，加载Player MVID `e190c566-51b8-4697-99a9-fa82959e43e6`，随后运行39项。EUI布局修正后经公开生成器重新生成，只有Binding时间注释改变，用户逻辑与绑定保持；同样主动编译。

第一次实播注入的新模拟手柄未配对Input Host，因此live-chain中的movingShot实际delta=0，不能记为移动射击通过。随后显式配对模拟设备后成功重放，独立证据已列；没有借此声称真实手柄或热插拔已验收。截图均通过Unity原生帧末捕获，未改MCP PackageCache。编译重载期间有MCP WebSocket短暂工具警告；实际运行及最后业务Console错误/警告均为0。没有全仓全量测试。

[移动射击画面](topdown-2d-evidence/t6-weapons-paired-moving-shot.png)及[竞技场HUD](topdown-2d-evidence/t6-weapons-human-ready.png)已查看。新HUD为角色状态/武器状态分行，进度提示下移。用户随后报告切回Unity触发编译并取消Play，人工体验未完成。106个相关源码/asmdef哈希未变，额外编译未报代码错误，触发源未锁定。现将测试和全部落盘之后的最终刷新/显式编译作为交接边界，完成域重载、读取Console后重新启动，之后不再写Assets；新证据见 `t6-weapons-handoff-compile-state.json`、`t6-weapons-handoff-ready.json`。

最终交接编译started1793204812524、finished1793204814417、reload1793204821897；重新进入后稳定运行，编译/导入均false，未再写Assets。用户随后反馈“没问题，继续吧”，本批武器Editor体验记为通过，见 `t6-weapons-human-validation.json`。已正常等待ShutdownFrameworkAsync结束后停止Play，Edit完整Scan/中心比较复验仍369项、0错误/警告，Console错误/警告0。

## 未完成与独立授权

1. 用户已确认武器拆分版本的Editor体验通过。真实手柄、热插拔和不同分辨率未签收，不能从本次体验推导这些设备与平台通过。
2. Windows64构建模块可用，目标StandaloneWindows64、Mono2x、5个启用场景。用户明确选择“暂不构建”，因此没有执行实际构建/Player冒烟。后续如重新授权，首次预计数分钟至十余分钟；拟输出 `Library/EmberValidationBuilds/topdown-2d/Windows64/EmberTopdown.exe`。
3. 纯身份判定矩阵不替代实际其他模板切换；没有创建干净消费工程。
4. **已普通保存；未Bump、未提交、未发布、未部署/升级消费工程。** 存储Assets已包含本轮实现；版本封存hash仍保持原值。
5. 本次普通保存授权已执行完成。构建/Player冒烟暂缓，Bump、框架发布/tag、提交及消费部署仍分别确认。T6交付未全部完成，不以普通保存代替版本封存或消费验收。

阶段细节、失败与修正见 [实施记录](topdown-2d-implementation.md)。
