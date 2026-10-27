# 场景对象适配器解析修复回执

日期：2026-10-27。范围：框架公共 `LevelAuthoring.Editor` 扩展与现有工作台接线。只读对照 Windward 提供的设计缺口记录、原始 JSON 回执与安装版 `com.ember@fee8b9092700` 的接口/工作台；消费端说明的发布组合为 Ember 0.24.0 / platformer-2d 0.13.0。本轮未改 Windward、PackageCache、模板源或快照，也未升级、部署、保存模板、Bump、提交、推送或发布。

续验结果：工作台实际 GUI 按钮事件、Scene 缓冲区范围及失败诊断已补齐，相关测试 **14/14 通过**；发现并修复按钮操作后继续绘制失效对象/布局的问题。详见[实际 GUI 续验回执](gui/README.md)。下文 11/11 为首轮服务与处理方法级证据，保留原始日期及结果。后续另获0.24.1发布授权，发布范围和门禁见[发布记录](release/README.md)；上文“未提交/发布”描述修复阶段边界。

## API 与变更

完整签名、注册/卸载样例及事务责任见[公共契约](../../../Packages/com.ember/Documentation~/manual/level-object-resolution.md)。新增 `RegisterObjectResolver(string, Func<GameObject, IEmberLevelObjectAdapter>)`、`UnregisterObjectResolver(string)`、`ResolveObject(GameObject)`；程序集/命名空间均为 `Ember.LevelAuthoring.Editor`。已有两个适配器接口无必实现成员变化。

- `EmberLevelContracts.cs`：带 ID 的只读对象解析器注册表，项目解析优先、Prefab 素材回退；多解析器不同结果、未注册实例、解析异常、Prefab 多素材冲突及素材适配器失效均抛错。
- `EmberLevelObjectEditing.cs`：Duplicate/Delete 在省略或传 null 时先自动解析，再开始原 Undo 事务；保留显式非空适配器参数。
- `EmberLevelWorkbench.cs`：复制/删除按钮调用共用处理方法，由公开服务解析；Scene 绘制调用同一个 ResolveObject；窗口及 Scene 显示错误诊断。
- 新增定向测试及带 `UNITY_INCLUDE_TESTS` 约束的独立夹具程序集；夹具只含示意稳定 ID、关联与范围字段，无产品业务。
- 新增公共契约文档及包 README 导航。修复阶段包和模板版本不变；后续单独授权以框架0.24.1补丁交付，模板仍不变。

## 编译证据

通过 Unity MCP 固定目标 `ember-unity-framework@b23e9b768e85ed30`，Unity 6000.5.4f1；正式活动编辑模板为 platformer-2d 0.13.0。修改后显式请求 `refresh_unity(compile=request)`，最后一批请求使用 `wait_for_ready=false`，随后确认编译与域重载完成。

[编辑器状态](final-editor-state.json)记录最后编译开始 `1793102207102`、结束 `1793102211990`、域重载后 `1793102218687`（Unix 毫秒）；非编译、非更新、无待重载。[已加载 API](loaded-api.json)记录实际程序集 MVID `b93e6e6b-ea62-408d-bd0c-c394b3a5eabc` 和新增解析器签名。[编译后 Console](compile-console.json)错误/警告为 0，最终测试清理后再次读取也为 0。

第一次刷新等待 readiness 超时，但后续状态确认该次编译/重载已完成，没有重复提交结果不明的请求。首轮测试的 11 个用例在 SetUp 失败：MonoBehaviour 夹具误放在 Editor-only 测试程序集，Unity 拒绝挂载。随后将该组件及 asmdef 分离到 TestFixtures，并完成重新编译和重测。不是产品实现失败，也没有把首轮结果计为通过。

## 首轮增量回归

仅执行 `Ember.LevelAuthoring.Tests.EmberLevelObjectResolutionTests`，没有执行整个 LevelAuthoring 程序集或全量测试。[原始测试回执](tests.json)：job `ade52d2df553489eb0f936af59541598`，**11/11 通过**，0 失败、0 跳过；测试体耗时约 6.38 秒。

| 验收范围 | 本轮证据 |
|---|---|
| 非 Prefab 复制、回调分配新 ID、源身份/Transform/父层级/非 Prefab 状态 | `WorkbenchDuplicatePreservesSourceAndUndoesIdentityAndReferencesTogether` |
| 创建/删除与必要引用同组 Undo/Redo | 上述复制测试及 `WorkbenchDeleteUndoesObjectAndReferenceTogether` |
| 创建/删除回调异常回滚，无残留副本/关联 | `WorkbenchCallbackFailureRollsBack(True/False)` |
| 范围来自真实组件，每次重读；绘制异常可传递诊断 | `WorkbenchRangeReadsRealComponentAndReportsCallbackFailure` |
| 公开 Duplicate/Delete（显式 null）与工作台一致 | `PublicServiceUsesSameResolverEvenWithExplicitNull` |
| 多解析器冲突阻止编辑，失效返回/解析异常不降级 | `ResolverConflictBlocksWorkbenchBeforeMutation`、`StaleResolverAndThrownResolverCannotFallBackToNull` |
| 重名登记拒绝、同一实例结果合流 | `RegistrationRejectsDuplicateNamesAndAllowsResolverAgreement` |
| 普通无适配器对象合法复制/删除 | `PlainObjectsRemainEditableWithoutAdapter` |
| Prefab 放置、连接、覆写、复制、删除、项目优先、素材缺失/冲突 | `PrefabFallbackPlacementOverridesDuplicateDeleteAndPriorityRemainValid` |

工作台测试反射调用与可见复制/删除按钮绑定的 `DuplicateSelection` / `DeleteSelection`，范围调用与 SceneGUI 共用的 `DrawSelectedScene`，再检查真实 Unity 对象及 Undo 状态；不是手工传 adapter 的替代服务测试。另静态核对了按钮与这些处理方法的直接绑定。

## 已知限制与清理

- 首轮桌面自动化未暴露框架 Unity 窗口。续验已通过 Unity IMGUI 调试器定位实际绘制按钮并发送 MouseDown/MouseUp，完整经过 OnGUI/GUILayout.Button；Scene Handles 已从指定 GUIView 缓冲区截图和像素断言验证。见[续验](gui/README.md)。这属于实际 GUI 自动化，仍不冒充用户现场人工鼠标签收。
- Windward 真实组件、项目回调和正式三关没有修改或复测。消费端仍安装旧包；此次修改不会自动进入已安装的 0.24.0。
- Undo 回滚依赖项目回调正确登记所有序列化修改；磁盘/外部副作用、未登记 Undo 的修改不受事务保护。解析器与 DrawScene 必须只读。
- 不自动归一选择到父机关根，不扫描重编号。未匹配对象仍沿用合法无适配器行为；项目需确保业务对象识别完整。Prefab 回退按当前 Assets 素材查找，未新增性能缓存或异步解析。
- [清理回执](cleanup.json)：预览场景已关闭、夹具对象为 0、临时适配器和解析器已撤销；活动场景仍为 `Assets/Game/Scenes/FrameworkScene.unity`，保留模板原有机关适配器。
