# EUITransitionBlock 过渡块开发指南

本文档面向「扩展过渡块」的开发场景——尤其当你需要**添加新动画、新排布模式、新预设**时，看完即可上手，无需重新通读源码。

---

## 一、架构概览

过渡块把屏幕切成方块网格，方块按某种「排布顺序」逐组出现/消失，每个方块用某种「动画」播放出现/消失动作。

三个独立层级，职责不同，不要混淆：

```
预设 (EUIBlockPreset)            命名配置，一键填好「排布 + 方向 + 动画」字段
   │ 填充
   ├── 排布模式 (EUIBlockOrderPattern)   决定方块「按什么顺序」出现   → EUIBlockOrderCalculator
   └── 动画类型 (EUIBlockAnimationType)  决定方块「怎么动」出现/消失  → EUIBlockAnimation 子类
```

- **预设是配置，不是行为**。切换预设时 `ApplyPreset` 填充字段 + 重置动画参数。
- **排布模式**是顺序计算器（静态类 + switch，已抽离）。
- **动画类型**是动画类（多态 + 反射自动注册）。

---

## 二、文件地图

| 文件 | 命名空间 | 内容 |
|------|---------|------|
| `Assets/Ember/UI/Runtime/Components/EUIBlockEnums.cs` | `Ember.UI` | 所有枚举 + `EUIBlockPresetDefaults`（每个预设的默认错开间隔） |
| `Assets/Ember/UI/Runtime/Components/EUIBlockOrderCalculator.cs` | `Ember.UI` | 排布顺序计算器（9 种模式 + `EUIBlockOrderConfig`） |
| `Assets/Ember/UI/Runtime/IEUITransitionEffect.cs` | `Ember.UI` | 对外接口（`PlayEnterAsync`/`PlayExitAsync`/`HideAllImmediate`） |
| `Packages/com.ember.uiextension/Runtime/Components/EUITransitionBlock.cs` | `Ember.UIExtension` | 主组件：编排、对象池、网格、编辑器预览 |
| `Packages/com.ember.uiextension/Runtime/Components/EUIBlockAnimation.cs` | `Ember.UIExtension` | 动画基类 + `EUIBlockAnimationContext` 上下文 |
| `.../EUIBlockAnimationScaleUp.cs` / `ScaleAndFade` / `SlideFromDirection` | `Ember.UIExtension` | 3 个具体动画类 |
| `.../EUIBlockAnimationRegistry.cs` | `Ember.UIExtension` | 反射注册表（自动发现动画子类） |
| `Packages/com.ember.uiextension/Editor/EUITransitionBlockPreviewDriver.cs` | `Ember.UIExtension.Editor` | 编辑器实时预览驱动（`EditorApplication.update`） |

---

## 三、添加新动画（最常用，只需两步）

### 第 1 步：加枚举值

`EUIBlockEnums.cs` 的 `EUIBlockAnimationType` 加一个值：

```csharp
public enum EUIBlockAnimationType
{
    ScaleUp = 0,
    SlideFromDirection = 1,
    ScaleAndFade = 2,
    MyNewAnim = 3,   // ← 新增
}
```

### 第 2 步：新建动画类

在 `Packages/com.ember.uiextension/Runtime/Components/` 新建 `EUIBlockAnimationMyNewAnim.cs`：

```csharp
using DG.Tweening;
using Ember.UI;
using UnityEngine;

namespace Ember.UIExtension
{
    public sealed class EUIBlockAnimationMyNewAnim : EUIBlockAnimation
    {
        public override EUIBlockAnimationType Type => EUIBlockAnimationType.MyNewAnim;

        public override void PlayEnter(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            // 进入：方块从初始状态 → 目标状态（出现）
        }

        public override void PlayExit(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            // 退出：方块从可见 → 消失
        }
    }
}
```

**就这两步，不用改任何 switch 或注册表。** `EUIBlockAnimationRegistry` 在首次播放时反射扫描，自动发现新子类并建立 `Type → 实例` 映射。

然后在 Inspector 的「方块动画类型」下拉里选它，或在某个预设的 `ApplyPreset` case 里指定它。

> 参考现有实现：
> - `EUIBlockAnimationScaleUp.cs` —— 只动 `localScale`。
> - `EUIBlockAnimationScaleAndFade.cs` —— 动 `localScale` + `Raw.color`/`DOFade`。
> - `EUIBlockAnimationSlideFromDirection.cs` —— 动 `anchoredPosition` + 用 `ctx.SlideOffset`。

---

## 四、动画上下文参考（EUIBlockAnimationContext）

`PlayEnter`/`PlayExit` 收到的上下文字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Rect` | `RectTransform` | 方块矩形（位移/缩放目标） |
| `Raw` | `RawImage` | 方块图形（颜色/淡入淡出目标，`DOFade` 作用于它） |
| `Position` | `Vector2` | 方块当前网格锚点位置（进入=目标位置，退出=起始位置，二者值相同） |
| `Delay` | `float` | 该方块的延迟（由组间错开 `_staggerInterval` 算出） |
| `Duration` | `float` | 该方块动画时长（`perBlockDuration`） |
| `Ease` | `Ease` | 缓动曲线（`_blockEase`） |
| `ManualUpdate` | `bool` | 是否编辑器预览（true = DOTween 手动推进） |
| `SlideOffset` | `Vector2` | 滑动动画的屏外偏移（仅 SlideFromDirection 用，由 `GetSlideOffset` 预计算） |

**更新类型**：用基类辅助方法 `ResolveUpdateType(ctx)` 取 `UpdateType`（预览=Manual，运行时=Normal）。**所有 tween 记得 `.SetUpdate(update)`**，否则编辑器预览推不动。

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
3. `EUITransitionBlock.cs` 的 `ApplyPreset` 加一个 case，填 `_enterPattern` / `_enterDirection` / `_exitPattern` / `_blockAnimation` 及子参数默认值。
4. （可选）`EUIBlockEnums.cs` 的 `EUIBlockPresetDefaults.GetStagger` 给它配默认错开间隔（组数多的预设要用更小间隔）。
5. 若该模式有专属子参数，加一个 `[SerializeField]` 字段 + `ShowXxxParams()` 显隐方法，并在 `GetOrder` 里读取它。

---

## 七、关键细节与坑

1. **动画参数在切预设时会被重置**（`ApplyPreset` → `ApplyDefaultAnimationParameters`）。螺旋等「每块一组」的排布用更小错开间隔（见 `EUIBlockPresetDefaults.GetStagger`），否则总时长会因 `staggerInterval × (组数-1)` 累加过长。

2. **非「自定义」预设会锁定排布模式与动画类型字段**（`[DisableIf("IsPresetLocked")]`）；方向/基准角 `_enterDirection` 与菱形/螺旋子参数仍可手动调整（`[ShowIf]` 按 `_enterPattern` 显隐）。切到「自定义」才解锁全部。

3. **退出倒放**：侧扫/对角/逐行等用 `ExitDirection = _exitForward ? _enterDirection : Opposite(_enterDirection)`。菱形/螺旋的「内外」与「倒放」都归结为 `isReversed = !isEnter && !_exitForward`，`DiamondMode` 与螺旋反转由 `(子参数 ^ isReversed)` 决定——倒放即反转进入序列（倒带，中心→基准角）。滑动动画的倒放是「原路返回」——滑动统一用 `_enterDirection` 作为偏移方向（`SlideOffset` 已预计算好），**不随 `_exitForward` 变**。

4. **编辑器预览的驱动在 Editor 程序集**：预览按钮只负责创建 `UpdateType.Manual` 的 tween；真正逐帧推进靠 `EUITransitionBlockPreviewDriver`（`EditorApplication.update` + `DOTween.ManualUpdate` + `SceneView.RepaintAll`）。运行时 asmdef 不能引用 `UnityEditor`，所以预览驱动必须放 Editor 程序集。

5. **注册表反射**：首次播放时扫描一次（`EUIBlockAnimationRegistry` 跳过 System/Sirenix/Feel 等第三方程序集）。IL2CPP 打包若发现动画类被裁剪，给子类加 `[UnityEngine.Scripting.Preserve]` 或在 `link.xml` 保留。

6. **图片切片**：勾选「使用图片」后，方块用 `RawImage` + `uvRect` 切出该格子区域；纯色则用白色 1×1 贴图 + 颜色。图片需匹配「网格」分组里的「当前网格」宽高比，否则会被拉伸变形。

7. **对象池自愈**：`EnsurePoolSize` 会清理已销毁的引用并重建；方块 `HideFlags.DontSave`，不序列化进场景/预制体。

---

## 八、对外 API（给业务层调用）

- `FadeIn(Action onComplete = null)` / `FadeOut(Action onComplete = null)` —— 带回调的渐入/渐出。
- `PlayEnterAsync(float duration = -1f)` / `PlayExitAsync(float duration = -1f)` —— UniTask 版本。
- `HideAllImmediate()` —— 立即隐藏所有方块。
