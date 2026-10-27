---
name: ember-level-add-decoration
description: 为 platformer-2d 关卡新增或更新装饰素材，维护锚点、允许变换、排序和物理声明，并验证放置、复制、撤销及保存。
---

# 装饰制作

调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-level-add-decoration")`，非空时停止业务写入。核对公开 `EmberLevelAssetService`、`EmberLevelObjectEditing`，读取 `Assets/Game/Documentation/platformer-2d/LevelAuthoring.md`；最低交付组合为Ember 0.24.0与platformer-2d 0.13.0。

输入Sprite/Prefab、锚点/世界尺度、旋转/缩放/镜像许可、排序及是否有物理交互。含碰撞或玩法的素材必须显式说明；不自动删除原组件。带交互逻辑的内容转机关适配流程，不因外观像装饰而降级。

先按稳定ID查找现有 `EmberLevelMaterial`。新增/更新使用 `EmberLevelAssetService.SaveMaterial` 与 `Configure`，种类 Decoration。保持ID/GUID及已有用户字段；重复调用同路径更新，不生成编号副本。人工入口 `Ember/Level/2D 关卡制作工作台` → 装饰 → 登记选中Prefab/Sprite。白盒参考 `Assets/Game/Development/LevelAuthoring/DecorationMaterial.asset`。

用 `Place(material,scene,position,rotation,scale)`、`Duplicate`、`Delete` 编辑。公开放置服务检查许可和未声明Collider；检查非2D碰撞及其他交互组件仍由项目适配器负责。渲染通过 `ConfigureRenderer`，锁定/隐藏通过 `SetEditorVisibility`。Prefab默认保持实例关系，更新源资产是独立且明确的操作。

在候选关新增与更新各一次，重复更新检查ID/路径不变。验证放置、复制、许可变换、Undo/Redo、保存重开、层级与锚点；从游戏相机检查前景对玩家、落点和机关反馈的遮挡。按真实结果记录，不以目录登记成功替代可编辑和可运行。场景变更失败撤销当前事务；文件生成按本次新建路径回退。交接路径、ID、物理声明、变换范围与验收证据。
