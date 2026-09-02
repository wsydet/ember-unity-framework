# UI 开发参考（Ember）

> 版本：2026-09-02（v0.10.0）
> 本文规定 ember UI 代码的目录约定、页面打开规则、绑定使用、生命周期约定与提交前检查项，是 UI 开发的统一入口。
> 机制依据：`Packages/com.ember/UI`（EUIManager/EUIPage/EUILogic）、`Packages/com.ember/UIExtension`（EUIBinding 代码生成）。API 细节见 `docs/dev/ember-api-reference.md`。

---

## 1. 目录约定

**预制体与代码分离（资源加载友好）**：

| 内容 | 位置 |
| --- | --- |
| 逻辑代码根 | `Assets/Game/UI/Runtime/`（codegen 的 `CodePath`，可配置） |
| UI 资源根 | `Assets/GameResource/Resources/UI/`（codegen 的 `UIResourceRoot` 配置，与逻辑目录分离；位于 `Resources` 下，打包运行时默认后端可加载） |
| 页面注册表 | `Assets/Game/UI/GamePages.cs`（框架区，全文件框架所有）+ `GamePages.User.cs`（用户区，框架永不覆盖） |
| 生成配置资产 | `Assets/Ember/Editor/SOs/EmberCSharpImplementation.asset`（`codePath`/`pageDefFile`/`uiResourceRoot`/模板引用） |
| 编辑器 SO | `Assets/Ember/Editor/SOs/` |

**逻辑分模块放置（业务新页面约定）**：

```text
Assets/Game/UI/Runtime/<模块>/Page/<页面类名>.cs      ← 页面级 UI
Assets/Game/UI/Runtime/<模块>/Component/<组件类名>.cs  ← 可复用组件（Tab 项、列表单元等）
```

- 框架演示页面保持现状目录（`Framework/`、`MainScene/` 等）；业务新页面按模块目录写 `classPath`。
- `classPath` 必须等于逻辑类相对代码根（`Assets/Game/UI/Runtime`）的路径，不带 `.cs`。写错会导致生成到非预期目录（同名类/绑定混乱）。
- 框架模式的预制体固定生成到 `UI/Common/Prefabs/`。
- 用户模式从 `classPath` 第一段取得模块名：`Inventory/Page` → `UI/Module/Inventory/Prefabs/`。
- 模块配套资源按同级目录整理：`Animator/`、`Atlas/`、`Prefabs/`。

## 2. 打开页面

统一使用 `EUIPageDef` 常量（禁止手写 prefab 字符串）：

```csharp
EUIManager.Instance.ShowMainPage(GamePages.EUIMainPage);
EUIManager.Instance.ShowPopup(GamePages.EUISettingPage, SettingsContext.Main);
EUIManager.Instance.ShowTopMost(GamePages.EUILoadingPage);
EUIManager.Instance.ShowSubPage(GamePages.SomeTab, parentPage, args);
```

| PageType | 入口 | 行为 |
| --- | --- | --- |
| MainPage | `ShowMainPage` | 压入 MainPage 栈，暂停旧主页面；替换式状态切换应在旧状态退出钩子中主动关闭其所属页面 |
| Popup | `ShowPopup` | 叠加弹窗，**自动创建可点击遮罩**（颜色 `EUIManager.PopupMaskColor`） |
| FullScreenPopup | `ShowPopup` | 沿用 Popup 栈与遮罩，并在打开时隐藏下层页面 |
| TopMost | `ShowTopMost` | 高于所有 Popup（Loading、全局提示） |
| SubPage | **仅** `ShowSubPage(def, parentPage, ...)` | 经其他入口打开会被拒绝并警告 |
| FreePage | `ShowTopMost`（按 `PageType.FreePage` 路由） | 需在 `EUIPageDef` 显式指定 `freePageSortingOrder`；例如 GM 常驻页 |
| Background | `SetBackground` | 单槽位背景页 |

### 2.1 MainPage 与状态所有权

- `ShowMainPage` 是页面栈入口，不负责判断调用发生在同一状态内导航还是跨场景状态切换，因此不会自动销毁旧 MainPage。
- 对 `MainState → GameplayState` 这类 `TransitionTo` 替换式切换，状态应拥有并对称清理自己的 UI：进入时 `ShowMainPage`，退出时 `ClosePageByDef`。这样新状态入场前旧页面会立即移出活动栈，退出过渡结束后清理逻辑。
- `Push/Pop` 覆盖状态（例如 Settings）不关闭下层状态的 MainPage；下层只暂停，覆盖状态退出后恢复。

## 3. 生命周期（EUILogic 钩子，子类 override）

| 方法 | 时机 | 用法 |
| --- | --- | --- |
| `OnBeginLoad` | 最早、仅一次（Logic 创建时） | 轻量准备 |
| `OnPreload(param, isOpen)` | 预加载时（先于 OnInit） | 预热数据 |
| `OnBind` | 绑定字段创建后、仅一次 | 从 `ControlMap` 取控件引用 |
| `OnResetDefault` | 打开（OnInit 前）与关闭（OnClose 后）各一次 | 恢复默认显示状态 |
| `OnInit` | 首次初始化 | 注册按钮事件、设初始值 |
| `OnOpen(param)` | 每次打开 | 读取入参、刷新数据 |
| `OnReopen(param)` | 已显示页面再次 Show | 仅刷新数据（不重放 OnOpen/OnShow） |
| `OnShow` | 页面显示（动画前） | 刷新显示、轻量状态 |
| `OnPause` / `OnResume` | 被遮挡 / 恢复可见 | 暂停/恢复计时器等 |
| `OnHide` | 页面隐藏（关闭动画前） | 停止显示相关逻辑 |
| `OnClose` | 页面关闭 | 持久化输入、释放页面级状态 |
| `OnDispose` | 销毁/回池（最后一次） | **注销所有事件**、清引用 |
| `OnUpdate` / `OnLateUpdate` | 每帧（仅 `NeedUpdate = true` 时） | Framework 生成页勾选 UIUpdate 后，业务逐帧逻辑写在 `OnUpdateUser` |
| `OnCustomEnter` / `OnCustomExit` | 普通 UI 选择「自定义代码」时（Loading 方块特殊链路也可启用） | 返回 UniTask，框架等待完成 |

- 覆写时优先 `base.Xxx()`。
- **Framework 页面的 UIUpdate 以 EUIBinding 面板为唯一静态配置源**：勾选「使用 UIUpdate」并重新生成后，`NeedUpdate => true` 与 `OnUpdate()` 驱动入口位于 `[EmberManaged]` 块内；用户只编写块外 `OnUpdateUser()`，不手改 bool 或框架覆写。
- Business 页面仍是整文件用户所有；需要高级动态开关时，可不勾选面板选项，在业务代码内使用基类的 protected setter。
- Framework 页面 .cs 固定提供 6 个基础 `XxxUser` 钩子（OnInit/OnOpen/OnShow/OnHide/OnClose/OnDispose）；仅勾选「使用 UIUpdate」时额外生成 `OnUpdateUser`。取消勾选并重新生成时，默认空钩子自动删除；若其中已有用户代码，交互生成会先确认，非交互生成取消本次同步并警告。Popup / FullScreenPopup 勾选「生成遮罩点击钩子」后另生成 `OnClickMaskUser`。
- 事件清理：`TrackDisposable(...)` 注册的订阅在 `OnDispose` 自动清理；手动 `AddListener` 的必须在 `OnDispose` 对称 `RemoveAllListeners`。

### 3.1 过渡动画统一时序

普通 UI 在 EUIBinding「普通过渡模式」中四选一，一个页面只有一个过渡负责人：

| 模式 | 责任 |
| --- | --- |
| 无 | 不播放过渡，生命周期后立即打开/关闭 |
| 预设渐入渐出 | 框架驱动页面根 `CanvasGroup.alpha` |
| Animator 动画 | Animator 独立负责全部进入/退出表现 |
| 自定义代码 | `OnCustomEnter` / `OnCustomExit` 独立负责全部过渡，需要渐变+位移时在函数内显式组合 |

```text
打开：OnOpen → OnShow → await 唯一 Enter 过渡 → Opened / 开启交互
关闭：禁止重复交互 → OnHide → await 唯一 Exit 过渡 → Closed → 移除遮罩/恢复下层 → OnClose/OnDispose
```

- 普通 UI 不再自动串联「预设/Animator + Custom」。需要复合效果时选「自定义代码」，在一个 UniTask 中自行组合。
- Animator 状态名固定为 `EmberOpen` / `EmberClose`；尾帧事件分别是 `OnEmberOpenAnimationEnd` / `OnEmberCloseAnimationEnd`。Animator 可挂在页面根或子节点，框架会在 Animator 同节点挂载事件桥。
- Animator / 自定义代码模式进入前，页面根 Alpha 会被置为 1。如需自定义渐入，应在 `OnCustomEnter` 首次 await 前主动设为 0。
- 页面打开完成后，框架会统一恢复根与 `Animator` 容器 `CanvasGroup` 的交互；根组继续作为暂停、关闭和退出阶段的总闸，因此无过渡、自定义、方块、预设和 Animator 模式下都不需要手工切换子容器射线。`Background` 是例外，打开与恢复后仍保持不拦截射线。
- 进入过渡尚未完成时，非 Background 页面已经拦截下层射线，但本页 `Selectable` 保持不可交互；Loading 因此从方块扫入开始就不会点击穿透。
- **方块过渡不参与上述普通四选一**：Loading 保持原链路，打开为「方块扫入 → Custom Enter」，关闭为「Custom Exit → 方块扫出」。

## 4. 绑定字段（EUIBinding + .Binding.cs）

- `.Binding.cs` 为生成文件，**禁止手工修改**；手写逻辑只使用字段。
- 自动收集规则：节点名 `m_` 开头或 `m`+大写 进入候选；`m_Btn_Start` → 字段 `Btn_Start`；已声明不重复收集；遇子节点挂 `EUIBinding`（UILogic）停止递归，独立成子 Logic。
- 新增字段：在 prefab 上绑定 → 重新生成；节点改名后重新收集绑定并重新生成。
- 生成契约：`.cs` 骨架仅首次生成；Framework 重新生成在 `[EmberManaged]` 内增删 UIUpdate / Popup 高级覆写，并同步对应的可选用户钩子。关闭 UIUpdate 时只自动删除默认空 `OnUpdateUser`，已有用户代码必须确认；其余基础用户钩子永不删除。Business 发现自定义代码时删除前仍会确认。`.Binding.cs` 每次覆盖；页面注册框架模式写 `GamePages.cs`、用户模式写 `GamePages.User.cs`。简单生成格式的已有注册条目会同步路径、层级和 PageType。

## 5. 弹窗遮罩约定

- Popup / FullScreenPopup 打开自动创建半透明遮罩，点击默认关闭本弹窗；颜色全局配置：`EUIManager.PopupMaskColor`（默认 0,0,0,0.5）。
- 页面类型为互斥单选；需要完全遮盖并隐藏下层时直接选择 `FullScreenPopup`，不再组合 Popup 与 FullScreen bool。
- 普通需求直接使用 EUIBinding 的「创建遮罩 / 遮罩颜色 / 点击遮罩关闭」数据配置，不需要生成代码覆写。
- 只有需要条件式代码控制时才勾选「生成遮罩创建覆写」或「生成遮罩点击钩子」；未勾选时页面逻辑不生成对应成员。
- 不允许点遮罩关闭：关闭 EUIBinding「点击遮罩关闭」；Framework 自定义点击逻辑写在 `OnClickMaskUser()`。
- **状态机驱动的弹窗**：默认遮罩点击只执行 `EUIManager.ClosePage`，不会替业务状态调用 `Fsm.Pop()`。如果 Popup 由 `SettingsState` 等覆盖状态拥有，应关闭「点击遮罩关闭」，由关闭按钮统一走状态机 `Pop()`；否则会出现页面已关、状态仍停留在覆盖状态的失配。
- 完全不要遮罩：关闭 EUIBinding「创建遮罩」。
- 自定义点击行为：override `OnClickMask`，需要关闭时调用 `base.OnClickMask()`。
- **弹窗 prefab 内不制作额外全屏黑底**，避免与框架遮罩叠加。
- 运行时 `EmberBgMask` 是挂在 `UI Root` 下、与弹窗关联的独立 Canvas；它继承所属弹窗的 Layer，排序固定在弹窗正下方，并自带 `GraphicRaycaster` 接收点击。

## 6. SubPage 规则

- 只能经 `ShowSubPage(def, parentPage, args)` 打开；父关子关；sortingOrder 步长 50（框架自动递增）。
- 多子页并存：ember 原生支持（子页列表），无需额外管理；再次 `ShowSubPage` 同名 def 走已显示页面刷新路径。

## 7. 安全区

```csharp
if (HasSafeArea) { var root = SafeAreaRoot; }   // 组件存在且有效时
protected override void OnSafeAreaChanged() { }  // 安全区变化（旋转等）自动回调，刷新布局
```

- 组件：`EUI/Safe Area`（EUISafeArea，四边独立/横竖屏双配置/影响系数）。
- 公共预制体：`Assets/GameResource/Resources/UI/Common/Prefabs/EUISafeArea.prefab`。标准页面创建时会以嵌套预制体放入 `Animator/EUISafeArea`，保留公共实例关联。
- `EUISafeArea` 提供 `TopLeft`、`TopCenter`、`TopRight`、`MidLeft`、`Center`、`MidRight`、`BottomLeft`、`BottomCenter`、`BottomRight` 九个定位节点；常规内容优先放入 `Center`，需要贴边布局时再选对应节点。
- **可交互内容是否进安全区是 prefab 责任**；逻辑只处理安全区更新后的布局刷新。

## 8. 新增 UI 开发流程

优先使用菜单 `Ember/UI/UI 开发中心`，不再从空 GameObject 手工拼装标准页面。窗口分为「创建 UI / UI 总览 / 清理与删除」三个页签。

1. 在「创建 UI」中确定 prefab 名、页面名、页面类型、代码模式与模块归属（`<模块>/Page|Component`），并预先设置 UIUpdate、遮罩、过渡动画及高级代码钩子。
2. 确认写入预览中的 prefab、逻辑脚本、`.Binding.cs`、可选 Settings 脚本与 `GamePages` 目标均正确，再点击「创建并在编译后打开 Prefab」。若依赖缺失、名称非法或目标冲突，预检会阻止写入。
3. 工具生成标准根 Canvas、CanvasGroup、EUIBinding 和 `Animator` 容器；`CanvasScaler` 固定使用 **Scale With Screen Size / 2560×1440 / Match 0.5**。
4. `Animator` 默认复用 `UI/Common/Animator/EUICommon_Ani.controller`，初始保持禁用，由运行时按页面过渡模式启用；有独立表现需求时可在生成后自行替换 Controller。
5. 工具自动嵌套公共 `EUISafeArea.prefab`，并生成 `.cs`、`.Binding.cs`、可选 Settings 与 `GamePages` 条目；框架页面路由到 `Common/Prefabs`，业务页面按 `classPath` 模块路由到 `Module/<模块>/Prefabs`。
6. 生成脚本后等待 Unity 完成编译；编译成功才自动打开新 prefab。若编译失败，保留产物并提示错误，不继续打开编辑。
7. 在 Prefab Mode 中补充具体视觉节点与绑定，自动收集后按需重新生成；手写逻辑只改非 `.Binding.cs` 文件，Framework 业务增量写在 `XxxUser` 钩子/自定义 override。
8. 用 `GamePages.Xxx` 打开页面并完成 Play 验证（禁止手写 prefab 字符串）。

### 8.1 UI 总览与安全维护

- 「UI 总览」只读扫描配置资源根下含根级 EUIBinding 的 prefab，集中显示页面定义、逻辑脚本、Binding、Settings 与 `GamePages` 健康状态，可直接定位或打开资产。
- 「清理与删除」先做影响预览，再逐项确认操作；支持失效 `EUIPageDef`、Missing Script、空引用绑定、带自动生成锚点的孤儿脚本组，以及可安全识别的空叶子节点。
- 删除一个 UI 时只处理该 prefab、确认未被其他页面共用的生成脚本和精确匹配的 `EUIPageDef`；执行前会跨 Framework/User 注册文件重新查重，并重新扫描全 `Assets` 的根 EUIBinding 引用。共享脚本、重复/复杂注册条目、注释示例或越出配置根目录的目标会被保留或拒绝，避免扩大删除范围。
- 模板镜像不会由开发中心自动写入。dev 资产验证完成后，仍由用户在 `Ember/Setup/模板编辑器` 中手动保存目标模板。

## 9. 提交前检查

- [ ] 没手改 `.Binding.cs`
- [ ] `classPath` 与 `.Binding.cs` 实际路径一致
- [ ] `GamePages` 有对应条目，且 prefab 路径与 Common/Module 自动路由结果一致
- [ ] 打开页面使用 `GamePages.Xxx`，无手写 prefab 字符串
- [ ] SubPage 只经 `ShowSubPage` 打开
- [ ] Popup / FullScreenPopup 类型与遮罩行为符合需求；prefab 内无额外全屏黑底
- [ ] 状态机驱动的 Popup 已关闭遮罩直接退出，关闭入口统一走状态机 `Pop()`
- [ ] 跨场景替换状态在退出钩子中关闭所属 MainPage，不把旧场景 UI 留在活动栈
- [ ] 「使用 UIUpdate / Popup 高级代码钩子」与页面源码中的可选覆写一致
- [ ] CanvasScaler 为 Scale With Screen Size / 2560×1440 / Match 0.5
- [ ] Animator 默认使用 `EUICommon_Ani`（或已明确替换），且公共 `EUISafeArea` 实例完整
- [ ] 事件注册在 `OnDispose` 对称清理（或走 `TrackDisposable`）
- [ ] 修改 C# 后 Unity 编译 0 error；模板改动由用户在「模板编辑器」手动保存
