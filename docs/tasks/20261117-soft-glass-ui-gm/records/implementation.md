# 柔光玻璃 UI 与 GM：实施及自检

当前有效增量为独立验收 A1 后的定向返工 A1-RW1，详见 [rework-a1.md](rework-a1.md) 与 handoff.md H12。需求保持 R4/F01—F04，R2 用户视觉确认有效；A1 判定 AC03/AC04 失败后，本轮已修复 SG-IA-02 键盘穿透和 SG-IA-01 失效借用，结果为实施自检通过、待原独立聊天复验，不能记整体验收完成。

2026-11-17，本轮最终显式 MCP 编译开始1794908041615、完成1794908046809、域重载完成1794908055450（Unix ms）；UI MVID aad4d568-81c2-43f1-87df-31511fd34c11。18/18 相关 EditMode 通过，1.2262788秒，job3faf0fa5330949a5bc8b336cbb92b418；40/40 Play 定向检查通过，包含真实 GameView 字符、EventSystem Submit/Move、双 Popup 乱序关闭/销毁、隐藏重显、策略切换、FullScreen、旧 Popup、GM 与业务禁用状态保护。全部消费者关闭后60帧捕获4551→4551，0边界/RT/段/专用材质。最终 Console、编辑器恢复和890文件指纹见 evidence/rework-a1/；delivery 文件优先于同目录历史轮次。无全量或长时测试。

R2 的 Shader/URP/Prefab/Binding/材质/字体/布局未改、原五图未漂移。本轮六份原有 core C#、一份租约回归及一份说明修改，新增作用域与焦点回归（含meta）；原886文件之外加4份，详见新manifest。AC09真实设备体验、AC11正式模板保存/分发仍未完成；未切换/保存/Bump/同步/部署模板，未提交发布、未归档清理。下面为 R2/R1 实施历史，不替代 H12 或独立 A1 结论。

## 基线、归属与操作边界

沿用当前checkout，分支codex/local-snapshot-20261111-162819，HEAD16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。S0真实复制993个文件/11,343,546B至evidence/baseline-implementation/files，另补Assets/Settings备份；Assets/模板41,338个SHA与Git状态保留。开始时入口962个UI指纹差异0，旧GM GUID bba4cad8012e3f64690d24af5becf55d。

活动topdown-2d0.1.2与扩展ownerVersion0.1.1差异保持，没有新扩展ID/hash。未切换、保存、Bump、同步或部署模板；新增Assets UI是base待回流草稿，base未保存/未分发。未创建worktree/子代理，未提交/发布，保留其他未提交改动。没有UnityFarm来源改动。

## 实际实现与维护源

| 层 | 结果 |
|---|---|
| UI core | Backdrop请求/设置/租约/Group/Graphic/MaskSession，管线无关；共享材质、SDF圆角与屏幕UV；Manager默认Blur、OnOpen后解析、FullScreen首次采样屏障；池回收清理。 |
| UIExtension/Editor | GlassImage/Style、FloatingEntry、ControlGlyph、响应式布局；Binding schema、Bridge、CreationRequest、快照往返、Inspector和契约字段。 |
| 独立URP17 | 相机分段、透明绘制后捕获、RenderGraph降采样与HV九采样、跨相机持久RT；安装通过core事件，不反向依赖URP。 |
| 配置 | 专用SoftGlassUI_Renderer加到PC/Mobile RP index1，原世界Renderer/SSAO保留；原空闲8—11层登记EUIBackdrop0—3。TagManager由Unity原生升级schema3并规范化空renderingLayer尾项，无旧业务层名被替换。 |
| Business | Assets/Game/UI/Runtime/Module/UIFoundation；独立Appearance与Dialog facade、GM/action registry、静态/动态消费扩展示例；GameInit只替换GM选择调用。旧GM受管代码保持。 |
| 资源 | EUI中心24个Prefab、22 Binding、5新注册、独立SoftGlassLibrary/catalog；DefaultLibrary不重建。 |

地址前缀UI/Module/UIFoundation/Prefabs/EUISoftGlass。2骨架StandardPage/StandardPopupPage；17Item：Panel、PrimaryButton、SecondaryButton、IconButton、CloseButton、Switch、Slider、VerticalSlider、Input、Search、ExtensionExample、GMEntry、GMState、GMTime、GMAction、GMTest、GMCenter；5Page：Alert、Confirm、Showcase、ExtensionExample、GM。骨架StructureOnly，Item无PageDef；四组Variant保留SourceSkeleton。

创建/绑定/注册走中心，布局用Prefab API，无手写YAML/Binding字段。读取真实生成字段后写用户partial。eui-build-receipt.json记录制作；contracts-final.json为最终24契约。连续两次22 Binding再生成保留用户逻辑、GUID及注册；后续再生成对44个.cs排除生成时间戳后语义漂移0（regeneration-final-result.json）。库公开文字、表面、控件及矢量图标slot。

CommonSkin与GmPresentation可独立选Legacy/SoftGlass，缺配置Legacy；本工程显式SoftGlass。Popup全局默认Blur与皮肤选择独立，可显式Solid、UseMask=false；旧schema0把原maskColor作为明确tint，nullable快照缺字段保留原值。GM保留状态、0.1—3连续时间与Ember同步、复位、增强及旧图像测试、正常退出；核心动作只在中心打开期间注册，消费样例提供背景/verbose动作。入口单指/鼠标拖动、安全区吸边，收起不重置时间。

正式说明：Packages/com.ember/Documentation~/ui/popup-backdrop.md、soft-glass.md及UI索引；Assets/Game/Documentation/base/soft-glass-ui.md及base索引。

## 偏差与返工

- Unity嵌套Canvas沿用根相机，overrideSorting不能形成独立采样段。失败的嵌套重绑定/临时beginCameraRendering探针已移除。最终独立边界必须root Canvas，嵌套Graphic共享祖先组；GM共用根组。示例用SoftGlassBackdropAttachment借出已制作背景Canvas成为独立根，释放时归还父级/序位/Rect并同步可见性，不运行时搭建正式UI。
- 修复FullScreen遮挡恢复把排序Layer当planeDistance的问题，保存并恢复真实原距离；普通Popup恢复不移动Canvas。运行实证100→隐藏→100，并有专门回归。
- 修复竖Slider手柄stretch、小Panel标题、Icon圆按钮被拉高；GM缺字Unicode改矢量Graphic。示例最小列宽340DU，720窄屏单列滚动，避免固定内部控件被双列裁切；宽屏保持三列。
- 模糊由5tap改9tap，修正放大文字上的栅格感；正式R1重拍，不采用返工诊断图。首次StandardPage制作分支误用和租约测试缺Camera夹具已修正，失败证据未抹去。
- SaveAssets把动态字体运行缓存落盘：已备份并恢复DefaultLight真实S0文件；Fallback先导出Git blob核对SHA与S0完全一致才恢复。未用HEAD覆盖本地改动。最终两字体均与S0相同，后续不再SaveAssets写测试缓存。

## 编译与定向验证

每批代码/资产修改均主动MCP refresh compile=request；等待编译/域重载，再读Console。断连按30秒重试，没有BatchMode/dotnet替代。最终状态、MVID及Console见compile-final-state.json、compile-final.json、console-final.json；FrameworkScene未保存脏改动。

- tests-final.json：13/13，1.2159199秒，job cf975ec85d814267a2424e1f9232864d。覆盖效果优先级5例、旧tint、快照、隐藏释放、圆角命中、真实距离恢复和既有池/中心配置回归。
- runtime-validation-final.json：23项均通过。20表面共享；默认Blur ready；RaycastAll首命中mask；Solid/UseMask切换；click-close false/true及一次回调；双Popup顺序/独立输出；FullScreen首帧后冻结与原距离恢复；20次开关无租约增长；GM时间2.25同步/收起保值/禁用回1、增强/注销、拖动不点击/第二指干扰/安全区/点击展开、Legacy切换及新GM单实例、静态/动态消费Item和背景清理。
- quit-action.json：真实执行已注册Dangerous退出动作，随后editor/state确认Play=false；不是仅检查文案或委托。
- clip-probe.png/json：GPU实图验证Stencil、RectMask2D、CanvasGroup .25和圆角。中心红1、裁切外0、透明sRGB .537、圆角外0。临时探针已销毁。
- S1 frame-debugger.json：同帧HUD事件7，B1捕获9—13，Popup1前景14—16，B2捕获18—22，Popup2/上层标记23—25，最终resolve27。含场景/下层UI，排除本层前景；MSAA1/4和方向/Linear/HDR实证，之后恢复1。暂停节流必须Invalidate才捕获，证据明确记录。九采样改动不改变排序架构。
- 最后一轮只改制作器/展示布局和生成注释，核心13例/23项逻辑没有变化；仍重新编译并按720实际截图复查布局。portrait-final-layout.json记录列宽与80×80圆按钮。

## 性能与平台限制

Unity6000.5.4f1/URP17.5，Windows Editor/D3D12/Linear/HDR，RTX5060Ti、Core Ultra7 265KF，1920×1080。16DU、1/4、两次HV、Throttled30Hz；每组预热30帧测180帧。performance.json原值，performance-notes.md完整解释。

| 场景 | CPU主线程p50 ms | GPU p50 ms | 边界/输出RT |
|---|---:|---:|---|
| A Solid，停局部采样 | 2.4240 | .757504 | 0/0 |
| B 1 Popup+下层共享组+收起GM | 3.5004 | .759808 | 3/约1.49MiB |
| C 2 Popup+下层共享组+展开GM | 4.1436 | .872960 | 4/约1.98MiB |
| D 全部SoftGlass关闭 | 1.8858 | .652544 | 0/0 |

输出480×270 B10G11R11_UFloatPack32、MSAA1，每个519570B，不含可复用Scratch、URP全尺寸目标/驱动总显存。D180帧捕获0，0相机段/模糊材质/表面材质。首轮RT精确名称过滤漏自动尺寸后缀，修成前缀并重测，以performance.json为准。

数值是整个GameView帧，非单pass成本；GPU仅统计非零样本，逐帧值保留。没有用户批准的目标机型/性能阈值，不宣称移动达标。URP17完整viewport/单显示器为当前支持；部分viewport、XR、其他管线或层池耗尽Solid回退。真机触摸/刘海、其他API、Player构建、热稳态/功耗和长时/全量测试未执行。

## AC映射、预览与接续

AC01由旧GUID/资产、Appearance、Legacy实测支持；AC02/05/06由深浅彩色图、24契约、圆角与裁切探针、横竖布局支持；AC03/04由策略用例、23项行为/20循环/释放支持；AC07—09由GM布局、时间/增强/退出/扩展/入口运行检查支持；AC10有短测及降级说明；AC11的框架/业务边界和中心来源落实，但base保存/分发未授权且未完成；AC12有本轮MCP编译/自检；AC13待用户明确确认。

上述为自检依据，不能复制为独立通过结论。逻辑主动拒绝mask的自定义Logic分支只核对实现，未单独注入运行夹具；系统键盘、所有输入焦点状态和真实设备触摸未覆盖，独立验收按相关AC定向复核。R1实际GameView共11图，涵盖控件、单/双Popup、GM横竖上下滚动、720控件/Popup和正常页面的收起入口。manifest记录源码/资产/图片指纹，preview-confirmation.md记录展示及回复；参考图不替代实现预览。

下一阶段使用prompts/04-acceptance.md，由另一聊天逐项独立复核。可以先独立验收，用户确认仍是最终收束必要条件。未自行创建验收聊天、未归档清理、未提交发布。中间过程另存implementation-progress-history.md，不作为当前状态。
