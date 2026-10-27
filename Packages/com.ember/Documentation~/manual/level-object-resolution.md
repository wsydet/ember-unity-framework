# 关卡场景对象适配器解析

公共能力归属 `Ember.LevelAuthoring.Editor` 程序集及同名命名空间，自 Ember 0.24.1 提供。适用于已有工作台的场景对象扩展；不要求 Prefab 源、素材登记或重建场景。旧版 0.24.0 不包含此接点，消费端需按授权升级并登记自己的解析器；包升级不会自动注册产品组件。

## 公开 API

```csharp
// EmberLevelAdapters 中新增：
public static void RegisterObjectResolver(
    string id, Func<GameObject, IEmberLevelObjectAdapter> resolver);
public static void UnregisterObjectResolver(string id);
public static IEmberLevelObjectAdapter ResolveObject(GameObject instance);
```

现有 `Register(IEmberLevelObjectAdapter)`、`Unregister(IEmberLevelObjectAdapter)`、`IEmberLevelAdapter` 与 `IEmberLevelObjectAdapter` 签名不变。项目 Editor 程序集需引用 `Ember.LevelAuthoring.Editor`，不能从运行时程序集引用。

## 解析契约

1. 先调用所有项目对象解析器。解析器按 ID 的 Ordinal 顺序求值以稳定诊断，顺序不代表优先级。返回 `null` 表示不匹配；匹配必须返回已通过 `Register` 登记的同一适配器实例。
2. 多个解析器返回同一实例允许合流；返回不同实例即冲突，抛出包含解析器 ID、适配器 ID 和对象名的异常。解析器抛错或返回失效/未登记实例时也抛错，不回退到无回调编辑。相同解析器 ID 不允许覆盖，需先撤销登记。项目负责使用唯一的命名空间前缀。
3. 所有项目解析器都不匹配时，沿用原工作台的 Original Prefab Source → 项目 Assets 素材 → AdapterId 路径。多个目录项指向同一源时须声明相同 AdapterId；冲突或声明非空 ID 却未登记适配器时阻止操作。空 ID、无匹配素材或非 Prefab 无匹配对象返回 `null`。
4. 项目匹配结果优先于 Prefab 目录。解析器应按组件和场景归属精确识别自己的对象；不能仅因选中了任意 GameObject 就返回适配器。解析器只读，不生成 ID、不改组件、不登记或撤销其他解析器。

复制/删除的公开服务在 adapter 参数省略或传 `null` 时自动调用 `ResolveObject`，且发生在 Undo 事务开始前。显式传非空适配器仍遵循原有调用者指定行为。工作台复制/删除按钮使用默认服务路径，Scene 范围绘制使用同一 `ResolveObject`，不缓存另一份范围几何。放置仍以素材声明的 AdapterId 执行预检和创建回调，不在尚未创建对象时猜测场景解析结果。

## 最小接入

消费端已有 `ProjectCheckpoint` 组件和 `ProjectCheckpointAdapter : IEmberLevelObjectAdapter` 时，在其 Editor 接线中增加登记即可；名称为示意项目类型，框架不包含这些业务类型。

```csharp
using Ember.LevelAuthoring.Editor;
using UnityEditor;

[InitializeOnLoad]
internal static class ProjectObjectAuthoringRegistration
{
    private const string RESOLVER_ID = "my-project.checkpoint";
    private static readonly ProjectCheckpointAdapter Adapter = new();

    static ProjectObjectAuthoringRegistration()
    {
        EmberLevelAdapters.Register(Adapter);
        EmberLevelAdapters.RegisterObjectResolver(RESOLVER_ID, instance =>
            instance.scene.IsValid() && instance.GetComponent<ProjectCheckpoint>() != null
                ? Adapter : null);
        AssemblyReloadEvents.beforeAssemblyReload += Unregister;
    }

    private static void Unregister()
    {
        EmberLevelAdapters.UnregisterObjectResolver(RESOLVER_ID);
        EmberLevelAdapters.Unregister(Adapter);
    }
}
```

`OnCreated(instance, duplicate)` 中为副本写入新身份，并维护必要关联；`BeforeDelete` 中解除关联。更改任何既有组件或引用宿主前使用 `Undo.RecordObject` / `Undo.RegisterCompleteObjectUndo`；Prefab 覆写还需 `PrefabUtility.RecordPrefabInstancePropertyModifications`。不要自行增加/提交 Undo 分组，工作台服务已把对象创建/删除和回调包在同一组内，异常会 `RevertAllDownToGroup` 后重新抛出。只对正确记录到该组的 Unity 序列化修改承诺回滚；磁盘、外部服务和未登记 Undo 的副作用不在此契约内。

`DrawScene(instance)` 每次读取实例真实组件及 Transform 绘制 Handles；回调应只读。窗口将解析/绘制失败显示为错误，Scene 视图在对象旁显示诊断。复制/删除失败使用窗口错误提示，不报告成功。

## 验证边界

直接回归为 `Ember.LevelAuthoring.Tests.EmberLevelObjectResolutionTests`：覆盖非 Prefab 身份、关联、Undo/Redo、删除回滚、实时范围、公开服务一致性、解析冲突/失效/异常、普通对象和登记 Prefab。另有三项 GUI 协程测试，通过实际绘制按钮矩形发送 MouseDown/MouseUp，验证完整 OnGUI 分发、创建/删除失败后的可见错误及 Undo 历史；从 Scene GUIView 缓冲区验证范围随真实组件修改和 Undo/Redo 改变。测试夹具组件位于独立的 `Ember.LevelAuthoring.TestFixtures` 程序集，仅在 `UNITY_INCLUDE_TESTS` 下启用。GUI 测试反射使用 Unity 内部调试/抓图接口，已验证 Unity 6000.5.4f1；接口变化时显式失败。

复制/删除按钮在事务结束（包括失败回滚）后退出当前 GUI 事件，下一帧重新布局，避免继续访问已销毁对象或在回滚后沿用失效的 Inspector/布局。

解析不向父对象递归，也不将选择的子节点隐式替换为机关根节点；有此需求的项目应明确自己的选择/组件约束。未知对象仍允许原有无适配器操作，因此项目必须完整登记其业务对象类型。本扩展不会自动发现未登记的产品规则，不扫描或自动重编号旧对象。消费端场景与产品回调仍需在获准接入后验收。
