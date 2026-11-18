# Popup 背景效果

Popup 效果与控件皮肤独立。`EUIManager.PopupMaskEffect` 默认 `Blur`，无新增字段的旧 Popup 也继承此默认值；旧控件、旧 GM 和旧资源地址继续有效。要恢复纯色遮罩，明确设置 `Solid`；`UseMask=false` 完全不创建该 Popup 的遮罩。

```csharp
EUIManager.PopupMaskEffect = EUIMaskEffect.Blur; // 后续打开的 Popup
EUIManager.PopupMaskColor = new Color(0.04f, 0.06f, 0.1f, 0.2f);
// 页面 OnOpen 内按次覆盖，在展示前解析：
Page.MaskEffectOverride = EUIMaskEffect.Solid;
// 已打开的页面需要显式刷新：
EUIManager.Instance.RefreshBackdrop(Page);
```

顺序为页面显式 Solid/Blur → 全局值 → 内建 Blur。Inherit 的序列化零值保持旧资产兼容。质量设置为页面 `BackdropSettingsOverride` → `EUIBackdrop.DefaultSettings` → 内建值。染色依次取显式 `MaskColorOverride`、Binding 明确 tint、全局颜色。颜色 alpha 为零只取消染色，不取消模糊或输入阻挡。

Binding 的 `backdropSchema=0` 表示旧配置，继续把原 `maskColor`（包括黑色0.5）作为明确 tint；不会在运行时重写 Prefab。新 EUI 创建为 schema1，默认继承全局 tint。编辑旧资产时若要采用全局 tint，使用 EUI Binding 快照明确设置 schema1 与 `OverrideMaskTint=false`，再保存并经中心重新生成。普通 nullable 快照补丁未提供新字段时保留现值。契约导出包含这些序列化字段，分类为 binding-config。

数据 `UseMask` 与逻辑 `AutoCreateClickableMask` 为双门禁。点击仍转发逻辑 `OnClickMask`；默认行为遵从 `ClickMaskToClose`。Solid 模式仍阻挡下层输入，局部玻璃组可独立采样。旧 `ShowBgMask` 重载仍可用；Popup 使用解析后的新重载。

`IEUIBackdropProvider` 只处理请求边界与可释放 lease，不依赖 URP。请求包含前景 Canvas、排序边界、设置与代次。lease 提供状态、纹理版本、诊断、Invalidate/Freeze 和幂等 Dispose。纹理属于后端，Graphic 只借用。UIExtension 的玻璃表面和同层遮罩共享 lease，避免重复采样当前前景。

URP17 后端位于独立 `Ember.UIRendering.URP` 程序集。它需要专用 UniversalRendererData、EUIBackdropRendererFeature、正式保留的空闲层及 Base+Overlay 栈。运行时通过 `EUIBackdropInstallation.Install` 显式装配，不从 UI core 反向引用 URP。捕获发生在每个下层段透明绘制之后，下一段之前；导入持久 RT 并声明跨相机依赖，最终 resolve 留在最后段。不能以 OpaqueTexture 或所有 UI 之后抓屏替代。

独立边界的前景必须是 **root Canvas**。Unity 的嵌套 Canvas 沿用根相机，单设 overrideSorting 不能形成另一个捕获相机段；后端拒绝该请求并返回诊断。普通嵌套 Item 和 Graphic 使用祖先组。需要采同页低层内容时，把低层内容制作成独立根 Canvas，明确生命周期、排序和输入所有权；不要把两个边界放在同一个根里。四个保留层最多同时承载四个独立边界，mask 与同页玻璃共享一个。

FullScreenPopup 先等待首次背景捕获，再提交隐藏下层并冻结输出。准备最多两个呈现帧；不可用时保留可读纯色过渡/回退。普通 Popup 默认持续更新，下层 HUD、场景透明物及低层 Popup 均属于输入，本层文字和更高层不属于输入。后端不支持或资源耗尽时实际回退为 Solid；不能把此状态记为模糊验证通过。

关闭/回池撤销 lease、清纹理引用和点击回调，最后消费者释放输出 RT；无消费者时不登记捕获 Pass。池内对象不保留 GPU 租约。外观页面仍有局部玻璃消费者时应按真实消费者统计，不能仅用“遮罩Solid”断言整个 UI 零采样。

当前后端仅面向 URP17、单显示目标和完整 viewport；部分 viewport、XR、其他管线保持明确 Solid 回退。空闲层池耗尽也回退，不覆盖已有业务层。移动真机、其他图形 API、多显示器和 Player 构建需对应验证。当前任务的实测范围与未完成项以任务实施/独立验收记录为准，接口说明不表示已通过所有平台验收。
