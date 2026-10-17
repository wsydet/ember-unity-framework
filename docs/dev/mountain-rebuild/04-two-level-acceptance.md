# T05/T06 两关正式链路实测

2026-10-17。Unity 6000.5.4f1，实际com.ember 0.19.2，活动platformer-2d 0.4.1。通过Unity MCP在正常Editor Play中执行；没有Test Runner替身、伪加载桥接、BatchMode或dotnet验证。

## 执行与结论

第一轮27项通过；扩展Brain实际输出、正式UI就绪及缺少Scope/玩家参与者失败分支后，14:15:50最终42项全部通过。这里是验收入口自己的42条运行断言，不是新增42个Unity Test Runner测试。上一轮564/564、108/108、29/29保留为历史回归证据，本轮没有重复声称运行它们。

真实流程：FrameworkScene启动 → Main → GameGameplayState.EnterLevel(A) → 两次B准备失败及候选清理 → EnterLevel(B) → 合法检查点采集 → 玩家空中/平台移动 → 失败保存留场 → 组合保存退出Main → 磁盘解码 → EnterLevel(B, checkpoint) → 目标SceneScope内准备世界及示例进度 → 玩家启动 → 死亡恢复 → 返回Main并卸载。

使用PlayerFormalAcceptance每次Play生成的隔离存档库；没有访问真实用户库。场景A/B由实际ResourcesProvider加载Editor场景资产路径，不改发行场景列表。世界数据通过仅Editor示例PlayerCheckpointAcceptanceParticipant在目标场景Prepare阶段恢复，玩家输入开放前即完成。

本轮没有复现需要修正公共运行逻辑的缺陷；修正了验收代码对EUI逻辑对象及Cinemachine 3 GetFinalPosition的使用。最终Console零错误。新增两关夹具、Editor执行器、示例准备参与者及文档；未改框架/base运行逻辑或Windward产品内容。

维护源和复跑方法见[模板两关验收说明](../../../Assets/Game/Documentation/platformer-2d/LevelAcceptance.md)。运行报告原件为Library/PlayerLevelAcceptance/latest.txt，下方为本轮原样归档。保存的场景可重复运行，已有资产不会被CreateScenes覆盖。

## 本轮运行报告

```text
2026/10/17 14:15:50
PASSED 42
PASS 使用独立正式验收存档库
PASS 正式Player模块存在
PASS 正式存档模块存在
PASS 正式入口 A: Completed
PASS A加载 相机绑定当前玩家
PASS A加载 虚拟相机无旧位置阻尼残留
PASS A加载 Brain实际输出匹配活动镜头
PASS A加载 正式玩法UI已打开
PASS 缺少Scope 拒绝换关且保留旧玩家
PASS 失败候选B卸载
PASS 失败候选相机无注册残留
PASS 缺少玩家参与者 拒绝换关且保留旧玩家
PASS 失败候选B卸载
PASS 失败候选相机无注册残留
PASS 正式入口 B: Completed
PASS B加载 相机绑定当前玩家
PASS B加载 虚拟相机无旧位置阻尼残留
PASS B加载 Brain实际输出匹配活动镜头
PASS B加载 正式玩法UI已打开
PASS 换关卸载A玩家与相机
PASS A相机注册释放
PASS 保存失败保留当前场景玩家
PASS 组合保存成功后正式退出Main
PASS 保存退出后正式主菜单UI已打开
PASS 退出卸载B及相机注册
PASS 磁盘组合记录保留最新已提交数据
PASS 正式入口 B: Completed
PASS 续玩回合法检查点而非空中保存位置
PASS 项目数据在玩家启动前准备完成
PASS 续玩恢复检查点世界平台
PASS B续玩 相机绑定当前玩家
PASS B续玩 虚拟相机无旧位置阻尼残留
PASS B续玩 Brain实际输出匹配活动镜头
PASS B续玩 正式玩法UI已打开
PASS 死亡恢复只提交一次:
PASS 死亡恢复玩家位置
PASS 死亡恢复世界平台
PASS B死亡恢复 相机绑定当前玩家
PASS B死亡恢复 虚拟相机无旧位置阻尼残留
PASS B死亡恢复 Brain实际输出匹配活动镜头
PASS B死亡恢复 正式玩法UI已打开
PASS 最终卸载无相机注册残留
```

## 渲染证据

- [A加载](../../../Library/PlayerLevelAcceptance/A加载.png)，SHA256 `b30ebea2bea94cf3aa1f9fd8d12ca485d038a2da86ddeb1545c7aa699bf64d65`。
- [B加载](../../../Library/PlayerLevelAcceptance/B加载.png)，SHA256 `25c0c8af084fab89479fda22d076dfcd35a694106c07e697d5205760a7d53b74`。
- [B续玩](../../../Library/PlayerLevelAcceptance/B续玩.png)，SHA256 `71af1415285c6bf113ffd305f62c4a2b54183f36ce85f5d46a9afed8d5c70ebf`。
- [B死亡恢复](../../../Library/PlayerLevelAcceptance/B死亡恢复.png)，SHA256 `3338ada79cdc5aee604a5d74494972234bbb306d8b1359eacb33ef2b3b0f14c9`。

已检查B加载和B死亡恢复截图，玩家、地面与正式玩法UI均显示；自动断言检查加载/续玩/死亡恢复时的相机绑定、虚拟相机位置及Brain实际输出，并检查卸载后的注册释放。截图与库在Library，仅本机保留，不纳入模板快照。

## 边界

完成了公共白盒的正式入口→换关→组合保存退出→检查点续玩，以及相机加载/恢复/卸载；默认UI确实运行，但动作由自动验收直接调用公共API触发，没有模拟按钮点击。默认快照保存按钮未改为组合存档。

续玩从实际磁盘读取且重新加载目标场景，但仍在同一Play中的Global存档服务；本页历史运行未包含进程重启；后续退出Play及发行Player两进程公共验收见[冷启动证据](05-cold-start-acceptance.md)。消费项目资源打包、产品房间镜头/旗面UI、Windward原关卡解法与人工手感仍未验收。T08未启动。无需将这些产品事项误写成本轮两关公共流程仍未验证。

## 最新普通保存（两关验收交付）

2026-10-17 14:17:47，通过Unity内项目中心正式SaveTemplate普通保存，返回1388文件。保存前0错误、0警告、19项预期差异；FrameworkScene未脏，未覆盖任何原有场景或发行列表。

- 当前contentHash：`f817fbbc7c77dc4e6697178635978246`，编辑记录一致。
- platformer-2d仍为`0.4.1`；versionedContentHash仍为`373437cccbe9f4ba6e93359da2c8b2fd`。
- 实际包`0.19.2`，模板frameworkVersion声明`0.19.0`，父base `0.8.3`，均未修改。
- 未Bump、提交、推送、发布或执行消费升级。Library的运行库、报告和截图没有收入模板。

保存后14:18:01项目中心复核：0错误、0警告、0差异；最终Console零错误。

## 2026-10-17 冷启动交付与最新普通保存

冷启动与发行运行的完整证据、失败尝试、调用方法及限制见[冷启动验收](05-cold-start-acceptance.md)。最终源码在编辑器两次Play通过16+19项断言；Windows64 Mono非Development Player在两个不同进程中也通过16+19项，退出码均0，运行错误列表为空。T05退出Play/进程后组合续玩和T06发行Player加载/恢复/卸载已补齐。T01—T04/T07沿用前轮结果，本轮未重复完整回归；T08未启动。

发现并修正框架Editor校验器依赖非序列化场景名称缓存的误报，维护源为`Packages/com.ember/Core/Editor/EmberProjectValidationService.cs`；没有修改运行时API。Unity实际扫描确认三个误报消失，项目校验EditMode回归14/14通过，job `e98fc546d17848d4b6fcd4188b05d44c`，0失败/跳过，0.214秒。模板夹具维护源为新ColdAcceptance Editor入口、运行器、ColdBoot及示例参与者编译条件；真实调用方法见模板LevelAcceptance.md。

14:47:21通过项目中心实际`SaveTemplate`普通保存1395文件，沿用原模板名称和说明。保存前0错误/1警告/9差异；保存后0错误/1警告/0差异。唯一警告是已保存未版本封存，按授权保持，不Bump。最终Console零错误/警告，FrameworkScene未脏且不在Play。

- 实际框架0.19.2；platformer-2d 0.4.1；模板frameworkVersion声明0.19.0；父base 0.8.3，均未变。
- 最新contentHash `db6747ebceedde8a4b1966a4ef00dfe6`，编辑记录一致。
- versionedContentHash仍为`373437cccbe9f4ba6e93359da2c8b2fd`，parentContentHash仍为`1270ce6dfcae3c01d050c09ecfd60337`。
- 只完成普通保存。未Bump、提交、推送、发布、消费升级或创建Windward。框架Editor修正仍为本地源码修改，未发布。消费项目不会自动获得这些内容，须后续另行授权正常版本升级与模板接入。
- 发行构建仍存在Odin构建预处理器PluginImporter路径错误；最终BuildReport为Succeeded但1 error。该生成Player运行验收通过，不等于零错误发行构建。IL2CPP、其他平台、Windward产品关卡/玩法及组合存档UI按钮接线仍未验收。

## Odin 构建问题后续状态（2026-10-17）

此前记录的PluginImporter路径错误已在正确第三方维护源修正，原回调保留并实际执行。通过UPM安装到PackageCache后重建Windows64 Mono非Development Player，BuildReport为Succeeded、0错误、20警告（18条既有内容警告和2条构建期间MCP重连警告）；两个新进程保存16/16、续玩19/19，退出码均0。详见[Odin修正与新证据](06-odin-build-path-fix.md)。历史失败记录保留，但不再作为当前未解决限制。

修正属于`ember-thirdparty-upm/com.sirenix.odin-inspector`，不是Ember运行时或模板能力。原远端tag未变化，尚未发布；工程的本机依赖接线与模板保存状态分别记录在上述文档。
