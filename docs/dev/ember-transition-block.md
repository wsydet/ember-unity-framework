# EUITransitionBlock 过渡块开发指南

本文档面向「扩展过渡块」的开发场景——尤其当你需要**添加新动画、新排布模式、新预设**时，看完即可上手，无需重新通读源码。

---

## 一、架构概览

过渡块把屏幕切成方块网格，方块按某种「排布顺序」逐组出现/消失，每个方块用某种「动画」播放出现/消失动作。

三个独立层级，职责不同，不要混淆：

```
预设 (EUIBlockPreset)            命名配置，一键填好「排布 + 方向」字段
   │ 填充
   ├── 排布模式 (EUIBlockOrderPattern)   决定方块「按什么顺序」出现   → EUIBlockOrderCalculator
   └── 动画曲线 (EUIBlockCurves)         决定方块「怎么动」出现/消失  → 6 条 AnimationCurve
```

- **预设是配置，不是行为**。切换预设时 `ApplyPreset` 填充排布/方向字段 + 重置动画参数。
- **排布模式**是顺序计算器（静态类 + switch，已抽离）。
- **动画曲线**是 6 条 `AnimationCurve`（缩放 x/y、位移 x/y、旋转、透明度），可自由组合；「曲线预设」下拉一键填充常见效果。

---

## 二、文件地图

| 文件 | 命名空间 | 内容 |
|------|---------|------|
| `Assets/Ember/UI/Runtime/Components/EUIBlockEnums.cs` | `Ember.UI` | 所有枚举 + `EUIBlockPresetDefaults`（每个预设的默认错开间隔） |
| `Assets/Ember/UI/Runtime/Components/EUIBlockOrderCalculator.cs` | `Ember.UI` | 排布顺序计算器（9 种模式 + `EUIBlockOrderConfig`） |
| `Assets/Ember/UI/Runtime/IEUITransitionEffect.cs` | `Ember.UI` | 组件级接口（`PlayEnterAsync`/`PlayExitAsync`/`HideAllImmediate`） |
| `Assets/Ember/UI/Runtime/IEUITransitionHandler.cs` | `Ember.UI` | 预设过渡槽接口（`PlayShowAsync`/`PlayHideAsync`），默认实现为整面板 alpha 渐变 |
| `Packages/com.ember/Runtime/Components/EUITransitionBlock.cs` | `Ember.UIExtension` | 主组件：编排、对象池、网格、曲线驱动动画、编辑器预览；同时实现上述两个接口 |
| `Packages/com.ember/Runtime/Components/EUIBlockCurves.cs` | `Ember.UIExtension` | 6 条动画曲线 + `EUIBlockCurvePreset`（曲线预设工厂） |
| `Packages/com.ember/Editor/EUITransitionBlockPreviewDriver.cs` | `Ember.UIExtension.Editor` | 编辑器实时预览驱动（`EditorApplication.update`） |

---

## 三、添加新动画效果（曲线预设）

动画由 6 条曲线驱动，新增效果 = 加一个曲线预设工厂，**无需新建类**。

### 第 1 步：加枚举值

`EUIBlockCurves.cs` 的 `EUIBlockCurvePreset` 加一个值（如 `MyNewEffect`）。

### 第 2 步：写曲线工厂

在 `EUIBlockCurves` 加一个静态工厂返回一套 6 曲线，并在 `Create` 的 switch 里加一个 case：

```csharp
private static EUIBlockCurves MyNewEffect()
{
    var c = new EUIBlockCurves();
    c.Rotation = AnimationCurve.Linear(0f, 0f, 1f, 1f);   // 转一整圈
    c.Alpha = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);   // 同时淡入
    return c;
}
```

### 第 3 步：加下拉项

`EUIBlockCurves.cs` 的 `PresetItems` 加一项（中文名）。

就这三步。用户选该预设即自动填充 6 条曲线，之后仍可手动微调。

---

## 四、动画曲线参考（EUIBlockCurves）

6 条曲线，横轴 = 进度 0→1（0=隐藏初始态，1=完全显现最终态），纵轴 = 属性目标值：

| 字段 | 终点应为 | 说明 |
|------|---------|------|
| `XScale` / `YScale` | 1.0 | x/y 缩放乘数 |
| `XPosition` / `YPosition` | 0.0 | x/y 位移（单位=方块尺寸 `_blockSize`） |
| `Rotation` | 整数 | 旋转（1.0 = 360°） |
| `Alpha` | 1.0 | 透明度 0→1 |

进入播放 0→1、退出播放 1→0。`ApplyCurves` 每帧求值 6 条曲线，`DOTween.To` 以 `Ease.Linear` 驱动进度（缓动由曲线本身编码）；编辑器预览用 `SetUpdate(Manual)`，运行时用 `Normal`。

---

## 五、添加新排布模式

排布模式在 `EUIBlockOrderCalculator`，是 switch 分派（不是多态）：

1. `EUIBlockEnums.cs` 的 `EUIBlockOrderPattern` 加枚举值。
2. `EUIBlockOrderCalculator.Calculate` 的 switch 加一个 case，指向新的 `BuildXxx`。
3. 实现 `BuildXxx(int columns, int rows, EUIBlockOrderConfig config)`，返回 `List<List<Vector2Int>>`：
   - 外层 List = 组（依次播放）。
   - 内层 List = 该组的网格坐标 `(x, y)`。

`config` 里有 `Direction`、`BlocksPerGroup`、`DiamondMode`、`SpiralMode`、`SpiralCorner`、`LinesHorizontal`、`TeethHorizontal` 等，可参考现有 `BuildXxx` 用法。

---

## 六、添加新预设

预设是「基础排布模式 + 默认子参数」的组合，方向变体由子参数字段控制，不再为每个方向变体建单独预设：

| 预设 | 方向/角 | 子参数 |
|------|--------|--------|
| 侧扫 | `_enterDirection`（上下左右） | — |
| 菱形 | — | `_diamondOutward`（扩散/收缩） |
| 螺旋 | `_enterDirection`（四角） | `_spiralClockwise`（顺/逆时针）+ `_spiralCenterOut`（内外） |
| 逐行 | `_enterDirection`（四角，扫入方向） | `_linesHorizontal`（水平/垂直，交错：先奇后偶） |
| 锯齿 | — | `_teethHorizontal`（水平/垂直） |

添加一个基础预设：

1. `EUIBlockEnums.cs` 的 `EUIBlockPreset` 加枚举值（用显式数值保持序列化兼容）。
2. `EUITransitionBlock.cs` 的 `PresetItems` 加一个下拉项（中文名）。
3. `EUITransitionBlock.cs` 的 `ApplyPreset` 加一个 case，填 `_enterPattern` / `_enterDirection` / `_exitPattern` 及子参数默认值。
4. （可选）`EUIBlockEnums.cs` 的 `EUIBlockPresetDefaults.GetStagger` 给它配默认错开间隔（组数多的预设要用更小间隔）。
5. 若该模式有专属子参数，加一个 `[SerializeField]` 字段 + `ShowXxxParams()` 显隐方法，并在 `GetOrder` 里读取它。

---

## 七、关键细节与坑

1. **动画参数在切预设时会被重置**（`ApplyPreset` → `ApplyDefaultAnimationParameters`）。螺旋等「每块一组」的排布用更小错开间隔（见 `EUIBlockPresetDefaults.GetStagger`），否则总时长会因 `staggerInterval × (组数-1)` 累加过长。

2. **非「自定义」预设会锁定排布模式字段**（`[DisableIf("IsPresetLocked")]`）；方向/基准角 `_enterDirection`、菱形/螺旋/逐行/锯齿子参数与动画曲线仍可手动调整（`[ShowIf]` 按 `_enterPattern` 显隐）。切到「自定义」才解锁排布模式。

3. **退出倒放**：侧扫/对角/逐行等用 `ExitDirection = _exitForward ? _enterDirection : Opposite(_enterDirection)`。菱形/螺旋的「内外」与「倒放」都归结为 `isReversed = !isEnter && !_exitForward`，`DiamondMode` 与螺旋反转由 `(子参数 ^ isReversed)` 决定——倒放即反转进入序列（倒带，中心→基准角）。动画曲线的倒放由「进度 1→0」实现，与排布无关。

4. **编辑器预览的驱动在 Editor 程序集**：预览按钮只负责创建 `UpdateType.Manual` 的 tween；真正逐帧推进靠 `EUITransitionBlockPreviewDriver`（`EditorApplication.update` + `DOTween.ManualUpdate` + `SceneView.RepaintAll`）。运行时 asmdef 不能引用 `UnityEditor`，所以预览驱动必须放 Editor 程序集。

5. **曲线是序列化数据**：`EUIBlockCurves` 用 `[Serializable]` + `AnimationCurve`，随组件一起序列化进场景/预制体，无需反射注册或 IL2CPP 保留。

6. **图片切片**：勾选「使用图片」后，方块用 `RawImage` + `uvRect` 切出该格子区域；纯色则用白色 1×1 贴图 + 颜色。图片需匹配「网格」分组里的「当前网格」宽高比，否则会被拉伸变形。

7. **对象池自愈**：`EnsurePoolSize` 会清理已销毁的引用并重建；方块 `HideFlags.DontSave`，不序列化进场景/预制体。

---

## 八、对外 API（给业务层调用）

`EUITransitionBlock` 同时实现两个接口：

- **`IEUITransitionHandler`**（预设过渡槽）：`PlayShowAsync(GameObject page, float duration)` / `PlayHideAsync(GameObject page, float duration)`。
  - `PlayShowAsync` 会先把 page 的 CanvasGroup alpha 置 1（根 CanvasGroup alpha=0 会连同方块一起隐藏），再播方块扫入。
  - 作为预设过渡时与 `DefaultUITransitionHandler`（整面板 alpha 渐变）**互斥二选一**：页面挂了方块组件 → 用方块，未挂 → 回退全局默认 alpha。
- **`IEUITransitionEffect`**（组件级）：
  - `FadeIn(Action onComplete = null)` / `FadeOut(Action onComplete = null)` —— 带回调的渐入/渐出。
  - `PlayEnterAsync(float duration = -1f)` / `PlayExitAsync(float duration = -1f)` —— UniTask 版本。
  - `HideAllImmediate()` —— 立即隐藏所有方块。
