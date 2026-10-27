# 实际工作台 GUI 续验

2026-10-27，Unity 6000.5.4f1，目标 `ember-unity-framework@b23e9b768e85ed30`。只修改框架工作台及框架测试，不修改 Windward、PackageCache、模板 Assets/快照，不升级、部署、提交或发布。

## 方法与结果

使用 `GUIViewDebuggerHelper` 读取临时工作台已实际绘制的“复制”“删除”文本及矩形，再用 `EditorWindow.SendEvent` 发送鼠标按下/抬起，进入 OnGUI → GUILayout.Button → Run → 公开对象编辑服务。没有反射调用按钮处理方法替代点击。两个按钮实际矩形为 `(3,482,106,20)`、`(112,482,106,20)`（GUI 点），处于可见区域。

范围验证通过正常 `SceneView.duringSceneGui` 订阅触发工作台与测试适配器：读取真实组件 range 和 Transform，绘制洋红色 Handles 线框。截图使用指定窗口 HostView 的 `GrabPixels`，不抓桌面、不用相机重渲染替代 Scene Handles。断言读取截图中的洋红色像素包围盒，并检查窗口绘制出的错误文本。

最终仅运行 `Ember.LevelAuthoring.Tests.EmberLevelObjectResolutionTests`：**14/14 通过，0 失败、0 跳过**，测试体耗时约 8.18 秒。job `e380231ec5444e2cb96792b5d237ee02`；[完整回执](tests.json)。其中 3 项新增实际 GUI 测试，其余 11 项为直接受影响的服务与处理方法回归；未扩大到全程序集或全量测试。

## 按钮、事务与失败

- [按钮逐步状态](buttons.json)：复制点击后创建回调一次，源 ID 保持 A、副本 ID 不同；源 Transform、父层级、非 Prefab 状态保持。Undo 后对象数由 2 变 1、关联回到源；Redo 后副本恢复同一新 ID。实际删除按钮调用 BeforeDelete，关联清空；Undo 恢复同一副本和引用，Redo 再删除。回调计数在 Undo/Redo 中不重复增加。
- [失败逐步状态](failures.json)：创建回调分配身份并改关联后抛错，回滚后仅源对象存在且关联恢复 A；删除回调解除引用后抛错，目标及关联保持。失败事务没有占据额外撤销步骤，随后一次 Undo/Redo 正确撤销/恢复之前成功的复制。
- 可见错误截图：[创建失败](create-failure.png)、[删除失败](delete-failure.png)。两者均为错误 HelpBox，不显示假成功。正常窗口：[复制前](buttons-before.png)、[复制后](buttons-copied.png)；身份和引用事实以 JSON 与断言为准，工作台原有 Transform Inspector 不显示测试组件 ID。

## Scene 实际范围

| 实际组件范围 | 线框像素包围盒 | 截图 |
|---|---|---|
| 3×7 | 176×411 | [初始](scene-range-3x7.png) |
| 8×2 | 468×119 | [修改后](scene-range-8x2.png) |
| Undo → 3×7 | 176×411 | [撤销](scene-range-undo.png) |
| Redo → 8×2 | 468×119 | [重做](scene-range-redo.png) |

[像素测量](range-pixels.json)与[组件/回调状态](scene.json)一致，说明显示来自当前真实组件，没有独立预览几何快照。测试中范围绘制回调失败时，[Scene 显示 injected-draw](scene-draw-failure.png)，[工作台显示错误](window-draw-failure.png)；回调恢复后错误清除。未把自动化描述为用户现场人工鼠标验收。

## 本次发现与最小修复

首次 GUI 回归实际复现了两项工作台缺陷：删除按钮成功后，同一帧 DrawSelection 继续读取已销毁的 GameObject；创建回调失败回滚后，当前 GUI 事件继续绘制触发 EndLayoutGroup 错误。服务级事务断言无法覆盖这两个 GUI 生命周期问题。

修复仅为复制/删除按钮在 `Run` 返回后调用 `GUIUtility.ExitGUI()`，让下一帧重新取得选择、Inspector 与布局；ExitGUI 位于 Run 的异常捕获之外。没有修改事务语义或设计新编辑器。

迭代期间还修正了夹具生命周期：已显示的窗口 Close、未显示的窗口 DestroyImmediate、每次 SetUp 重置 GUI 标志；临时 SceneView 正常 Close，避免残留窗口指向已销毁相机。失败迭代没有计为通过，摘要见[迭代发现](iteration-findings.json)。最后一轮没有忽略/吞掉预期之外的 Unity 日志。

## 编译与清理

修改后通过 MCP 显式请求编译；[状态](compilation-state.json)确认本轮开始 `1793103379724`、结束 `1793103382976`、域重载后 `1793103388672`（Unix 毫秒），编辑器非编译、非更新、无待重载。[已加载程序集](loaded-assemblies.json)：Editor MVID `14f1db77-b700-459b-a016-b310e65dd78f`，Tests MVID `e3696986-f006-4b86-8f0c-80f81c46a445`。[编译后 Console](compile-console.json)与[最终 Console](final-console.json)均为 0 条错误/警告。

[清理回执](cleanup.json)：测试组件 0、测试工作台窗口 0、临时资产目录为空；保留一个原 SceneView 且相机有效、活动场景仍为 FrameworkScene、活动模板仍为 platformer-2d，原模板机关适配器保留。

GUI 自动化依赖本 Unity 版本的内部调试/抓图接口，未来版本接口变化会明确失败。Windward 产品组件、回调和正式三关依旧不在本轮验证范围；本次补齐的是框架最小样例的实际按钮和 Scene 显示验证。
