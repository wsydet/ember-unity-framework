---
name: ember-level-add-background
description: 为 platformer-2d 关卡新增或更新背景素材与图层，配置排序、平铺、基础视差并验证运行与编辑恢复。
---

# 背景制作

先调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-level-add-background")`；非空时停止业务写入。确认 `EmberLevelAssetService`、`EmberLevelObjectEditing` 与运行组件 `Ember.LevelAuthoring.EmberParallaxLayer` 存在。读取 `Assets/Game/Documentation/platformer-2d/LevelAuthoring.md`。最低交付组合为Ember 0.24.0与platformer-2d 0.13.0。

输入：Sprite/Prefab、位置、覆盖尺寸、色调/透明度、Sorting Layer/Order、单图或平铺、视差系数与已有相机归属。先检查现有背景是否已有驱动，避免同时驱动。缺素材时可用 `Assets/Game/Development/LevelAuthoring/BackgroundMaterial.asset` 白盒样例；不猜正式美术。

用稳定ID查找并通过 `SaveMaterial` 原位新增/更新 Background 目录项，Sprite或Prefab需项目自有路径。人工入口为 `Ember/Level/2D 关卡制作工作台` → 背景，可登记 Project 中选中的素材；调用 `EmberLevelObjectEditing.Place` 放置实例，`ConfigureRenderer` 配置实际渲染排序和覆盖。平铺要求适合平铺的Full Rect图片；不能用拉伸整张正式图冒充平铺。

需要视差时 `Undo.AddComponent<EmberParallaxLayer>`，`Undo.RecordObject` 后用 `Configure(camera,factor,baseline)`；默认零系数，0为世界固定、1为完全跟随。模板 `PlatformerLevelPresentation` 将其显式登记到SceneScope并通过 `BindRuntimeCamera` 绑定正式主相机，不另建相机驱动。视差始终按基准与相机偏移求值；预览只绘制计算结果，不写回Transform基准。

图层复制/删除用公开编辑服务；重排改变真实Sorting Order。编辑隐藏和锁定使用 `SetEditorVisibility`，不改运行enabled。更新目录项不覆盖已放置实例的用户覆写；需要批量应用时先列目标和差异。

新增和更新各一次；验证保存重开、Undo、渲染顺序、覆盖与相机往返/切换、停止恢复。记录实际相机接线与未验证情形，不能仅凭Scene预览宣称运行通过。失败撤销当前组；磁盘新增只回退本次文件。交接资源ID/路径、层参数、视差语义、验证证据。
