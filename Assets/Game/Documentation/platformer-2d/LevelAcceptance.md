# 两关正式链路白盒验收

归属platformer-2d模板。使用真实GameLauncher、GameGameplayState、SceneCoordinator、GameSceneScope、PlayerModule、EUI和Cinemachine；没有替换状态生命周期或注入伪场景加载器。此入口仅用于Editor开发验证，不是产品关卡选择器。

## 资产与运行

- `Assets/Game/Development/LevelAcceptance/LevelA.unity`、`LevelB.unity`：两张独立最小场景，各含一个正式玩家Prefab、地面、检查点世界Scope、复位平台、虚拟相机、相机/玩家/示例数据参与者和灯光。主Camera/Brain复用FrameworkScene。
- 同目录`ConfigA.asset`、`ConfigB.asset`：各自场景名与出生点，复用当前模板完整玩家方案。
- `Game.State.Editor.PlayerLevelAcceptance.CreateScenes()`：首次创建夹具，已有场景不覆盖；部分资产冲突会拒绝。不会改发行场景列表。
- `Game.State.Editor.PlayerLevelAcceptance.Launch()`：在非播放状态、活动FrameworkScene中调用。沿用PlayerFormalAcceptance的隔离库，然后自动执行整条链路。
- `PlayerLevelAcceptance.Status`：运行阶段或最终PASSED/FAILED。详细断言保存到`Library/PlayerLevelAcceptance/latest.txt`，渲染检查截图位于同目录。每次重跑会更新本机报告与截图。

工具实际操作检查正式活动模板身份和Player模块启用状态；无额外常驻菜单。可通过Unity MCP execute_code调用上述公开Editor入口。不要直接播放A/B场景绕过框架准备，也不要用此工具操作真实用户存档。

## 验证内容

1. 从正式Main进入A；核对玩家绑定、虚拟相机初始构图、Brain实际输出和正式玩法UI。
2. 加载B时分别注入缺少启用Scope、缺少启用玩家参与者，要求准备失败、旧A继续有效、失败B和相机注册被清理。
3. 正常A→B换关，要求A玩家和相机销毁，A注册释放，仅B运行。
4. 在稳定地面采集B检查点与平台记录，再移动平台和玩家、登记限时空中外部运动。非法槽位保存退出必须失败留场；有效槽位组合保存成功后才返回Main并卸载B。
5. 读取实际磁盘组合记录，保留示例“已确认进度”；再次通过正式入口进入B，在SceneScope准备阶段恢复世界、基线和示例进度，再启动玩家。确认回检查点而非空中保存位置。
6. 在续玩后的B中再次移动玩家/平台，执行死亡原因的检查点恢复；确认只提交一次、平台复位、相机历史清除及Brain输出正确。
7. 最终返回Main，检查B卸载且相机注册不残留。

`PlayerCheckpointAcceptanceParticipant`是在UNITY_EDITOR或EMBER_PLAYER_ACCEPTANCE下编译的示例参与者，位于Player/Development。它演示项目如何在目标场景Prepare阶段使用WorldResetScope2D.Restore和PlayerCheckpointRecovery.SetBaseline。示例字符串没有贴纸、旗面、计时或产品含义。Pending只属于此串行白盒运行，正式项目应使用请求拥有的数据传递，不复制成全局业务状态。

## 边界

验收操作调用正式公共入口，实际UI照常打开/关闭，但没有通过鼠标点击触发各步，也没有将默认单玩家快照按钮改为组合存档按钮。续玩重新加载场景并从磁盘解码，仍在同一Play中的Global存档服务；本入口本身不覆盖进程重启；下方ColdAcceptance独立补齐退出Play和发行Player验证。消费项目资源打包仍须在消费项目验收。镜头验证覆盖加载、死亡/续玩恢复和卸载，不代表产品房间镜头、旗面UI或Windward关卡完成。

场景和代码通过项目中心普通保存进入模板；测试报告与截图位于Library，不纳入模板。保存、Bump和发布分别记录。

## 冷启动与发行 Player

`Game.State.Editor.PlayerColdAcceptance.CreateBoot()` 创建独立 ColdBoot，已有资产不覆盖；它复制正式 FrameworkScene 并增加 `PlayerColdAcceptanceRunner`，不改正式入口或发行场景列表。入口仅活动 platformer-2d 且 Player 模块启用时可用。

```csharp
// 每轮 save 使用新隔离目录；resume 必须保留同一目录。
PlayerColdAcceptance.CreateBoot();
PlayerColdAcceptance.Launch(absoluteIsolatedRoot, "save");
// 等报告生成并退出 Play 后再调用：
PlayerColdAcceptance.Launch(absoluteIsolatedRoot, "resume");
// 非播放状态；输出必须在 Library/Assets 之外，例如仓库 .utmp：
PlayerColdAcceptance.Build(absoluteBuildDirectory);
// 非增量发行验收：让Unity清理构建缓存，不删除Library或重新部署模板。
PlayerColdAcceptance.Build(anotherAbsoluteBuildDirectory, cleanBuild: true);
// Build只排队并返回；等待BuildStatus终态和build-report.txt，不能立即启动exe。
```

Build 使用 StandaloneWindows64、BuildOptions.None 和单次 extraScriptingDefines `EMBER_PLAYER_ACCEPTANCE`，不写常驻 define 或发行场景列表；沿用项目当前后端。本次实测为 Mono。普通产品构建不要包含验收场景与此标记。

上述两个Build示例择一执行。cleanBuild重载使用BuildOptions.CleanBuildCache，仍不含Development；报告中的options必须体现CleanBuildCache才算本入口的非增量构建。build-messages.txt保留BuildReport各步骤消息，可与原始诊断对照。清理的是Player构建缓存，不等于重新导入全部项目资产；不可将增量构建零警告当作历史警告已修复。

Build通过一次性EditorApplication.update执行，先退订再构建，避免在MCP同步上下文的调用栈内直接BuildPlayer及重入。排队与执行时均检查编辑器状态；重复构建请求会拒绝。编译/域重载打断尚未执行的请求时，Queued不代表完成，应待编辑器就绪后重新发起。输出目录同时生成build-entry-stack.txt和build-diagnostics.txt，后者记录Warning/Error/Assert/Exception原始信息及Assert/Error计数；这些诊断不被屏蔽。验收须同时检查BuildReport和诊断文件，不能仅凭Succeeded判断无Assert。

生成程序以 `--ember-acceptance-root "绝对隔离目录" --ember-acceptance-phase save` 运行，退出后另起进程用同目录和 `resume`。两个阶段输出各自 `report-*.json`：结果、断言、错误、启动标识、进程号、构建GUID和development状态；成功退出码为0，失败为1。不得把没有报告或非零退出码计为通过。

保存阶段走正式Main→A→B→组合保存退出，续玩阶段从实际磁盘读回B，在Prepare恢复项目/世界后启动玩家，并验证死亡恢复和最终卸载。`expected.json`只用于核对，不代替存档。相机证据由Brain驱动的实际主相机离屏渲染到PNG，兼容隐藏Player窗口；不代表整窗口Overlay UI截图，UI另外检查正式打开状态。不得用此夹具操作真实用户存档。

2026-10-17已验证退出Play后的组合续玩及Windows64非Development Player两进程流程。初次发行构建遇到Odin预处理器的PackageCache路径问题，后续已在第三方包维护源修正并实测零错误构建（仍有既有警告）。旧远端odin-v4.0.2尚未包含此未发布修正；模板普通保存不分发第三方包修正。历史运行证据见仓库开发文档`docs/dev/mountain-rebuild/05-cold-start-acceptance.md`，修正及新构建证据见`06-odin-build-path-fix.md`。模板普通保存不等于Bump或发布。
