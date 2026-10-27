---
name: ember-level-add-mechanism
description: 为 platformer-2d 关卡工作台登记或更新已实现机关的Prefab、编辑适配与范围预览，验证身份、关联、Undo和正式试玩。
---

# 机关制作

执行前调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-level-add-mechanism")`，非空时停止业务写入。核对 `Ember.LevelAuthoring.Editor.IEmberLevelObjectAdapter` 和 `EmberLevelObjectEditing` 已加载。读取 `Assets/Game/Documentation/platformer-2d/LevelAuthoring.md` 与实际机关代码；最低交付组合为Ember 0.24.0与platformer-2d 0.13.0。

输入应包含已有组件/明确行为规格、Prefab、允许的变换、实际配置字段、身份/引用与复位语义。缺运行能力时指出具体缺口，不凭图片补造玩法。通用样例为 `Assets/Game/Development/LevelAuthoring/LaunchPad.prefab`，使用现有 `LaunchZone2D`，不依赖消费端新玩法。

使用项目自有路径及稳定素材ID。先 `EmberLevelAssetService.Find<EmberLevelMaterial>()` 查找已有ID；新增/更新均调用 `SaveMaterial(path,id,configure)`，`Configure` 指定 Mechanism、Prefab、adapterId、hasPhysics。保留Prefab源与用户覆写。人工操作从 `Ember/Level/2D 关卡制作工作台` → 机关进入；源参数通过Inspector和真实Collider手柄编辑。

项目编辑适配器实现并登记 `IEmberLevelObjectAdapter`：`ValidatePlacement` 预检，`OnCreated` 分配应变实例ID和维护关联，`BeforeDelete` 在同一Undo组更新引用表，`Validate` 返回只读诊断，`DrawScene` 读取真实几何。不得扫描时自动修全部ID，不引用Windward类型到框架。模板默认适配器仅覆盖样例弹射和检查点，不把它当成任意产品机关的适配器。

放置/复制/删除调用 `EmberLevelObjectEditing`；批量动作预检所有对象，关联对象要 `Undo.RecordObject`，Prefab覆写用正式PrefabUtility API登记。不直接改场景YAML或用反射私有字段。

样例弹射的公开配置入口为 `Game.Entity.LaunchZone2D.Configure(LaunchRequest2D, cooldown)`，读取 `Configuration`、`Cooldown`；位于 `Game.Entity.Runtime`。先以 `LaunchRequest2D` 完整验证策略，再用PrefabUtility打开项目自有Prefab并原位更新。Collider尺寸仍由真实Collider组件配置，不能用私有反射绕过验证。

在候选关新增与更新各一次，验证真实范围、参数、Prefab联系、身份唯一和必需关联；Undo/Redo及保存重开后复查。从入口进入正式试玩，观察触发、死亡/停止后的复位；未实测不得写成功。失败回滚当前Undo组；新资产另按本次路径清单回退。报告素材ID、适配器ID、资源路径、关联变化、验证结果和未覆盖项。
