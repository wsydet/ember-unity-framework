# Ember 0.24.1

本补丁修复2D关卡工作台中存量非 Prefab 场景对象不能解析项目对象适配器的问题。复制、删除及范围展示统一走公开解析机制，项目身份与关联回调不再因缺少 Prefab 源而被跳过。

发行包为 `Packages/com.ember`，引用 `v0.24.1`。只纳入本次对象适配器解析、工作台接线、相关测试与发布资料；工作区其他 UI、FSM、字体及项目设置改动不进入提交。

## 公开扩展与行为

`Ember.LevelAuthoring.Editor` 程序集/命名空间的 `EmberLevelAdapters` 新增：

```csharp
public static void RegisterObjectResolver(string id, Func<GameObject, IEmberLevelObjectAdapter> resolver);
public static void UnregisterObjectResolver(string id);
public static IEmberLevelObjectAdapter ResolveObject(GameObject instance);
```

项目解析器优先；无匹配时回退到既有 Prefab 素材 AdapterId。多个不同适配器结果、失效返回、解析异常和素材声明冲突明确报错，不静默传 null。已有两个适配器接口无必实现成员变化；显式非空 adapter 参数保持原行为，省略或传 null 时公开复制/删除服务自动解析。

实际按钮验证发现并修复删除后继续访问销毁对象、失败回滚后继续使用失效布局两处 GUI 问题：复制/删除按钮在操作结束后退出当前 GUI 事件，下一帧重新绘制。未重做编辑器或新增产品玩法。

## 版本、模板与依赖

platformer-2d 保持 **0.13.0**；base 0.10.0、source3d-2p5d 0.6.0、ui-game 0.2.0、visual-novel 0.22.1 保持原内容、封存 hash 和父快照。框架同 minor 补丁允许继续保留模板兼容声明0.24.0，本次不保存或 Bump 模板。

全部第三方及 Unity 依赖、通用/模板技能和 bundle 内容沿用0.24.0，仅完整 manifest 的框架引用变为 v0.24.1。

## 验证与门禁

- Unity 6000.5.4f1：本次代码主动编译、域重载及新程序集已确认；发布准备另请求编译并核对实际包版本0.24.1。
- 相关 EditMode 回归 **14/14通过**，含真实 OnGUI 按钮分发、非 Prefab ID、关联与对象同组 Undo/Redo、创建/删除回调失败回滚、可见错误，以及 Scene GUIView 实际范围像素随组件修改和 Undo/Redo 变化。[GUI 回执](level-object-resolution-20261027/gui/README.md)。未全量重测。
- 发布候选门禁、模板实际 hash/父快照、源文件范围和无关工作区保留结果见[发布门禁记录](level-object-resolution-20261027/release/README.md)。
- 本次未执行发行构建、干净消费迁移或 Windward 产品回调/正式三关验收；不沿用0.24.0构建结果冒充本补丁验证，不将 GUI 自动化称为用户现场人工签收。

## 消费接入与回退

已有消费项目经授权后通过 `Ember/UPM Manager` 升级到0.24.1，等待编译完成，再按[接入契约与最小样例](../../Packages/com.ember/Documentation~/manual/level-object-resolution.md)登记只读场景对象解析器，返回既有已注册适配器实例。已有 platformer-2d 0.13.0 项目无需为此重新部署模板；项目回调负责登记 Undo 及其产品身份和引用规则。

本次发布不升级、部署或修改 Windward。回退可经 UPM Manager 恢复0.24.0并撤回新增解析器接线；旧版不支持该接点，不能让消费代码继续引用新 API。场景原结构与 Prefab 关系没有迁移。
