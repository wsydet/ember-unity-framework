# 柔光玻璃 UI 与 GM：实施进行中

- 任务 20261117-soft-glass-ui-gm，按 R3 / D1 实施，2026-11-17 Asia/Shanghai。
- 本聊天仅实施与自检；独立验收未执行，AC13 实际成品预览未展示/未确认，整个任务未完成。
- 保持当前 checkout；未切换/保存/Bump 模板、未提交/发布。Assets 内容为 base 待回流集成草稿，base 未保存/未分发。

## S0 基线

已读项目规则、公共工作流/交接模板、架构/目录/编码规范、API 速查、完整 Unity 验证规则、EUI 制作及 Unity MCP 技能。正式 topdown-2d 0.1.2、扩展配置 ownerVersion 0.1.1 的差异未改变，未为其重签收。

993 个真实当前文件（含 meta）已复制备份，11,343,546 字节；另补备份 Assets/Settings 下真实 RPAsset/Renderer。路径 `evidence/baseline-implementation/files/`，原清单 manifest.json；全 Assets/模板 41,338 个 SHA256 见 assets-template-hashes.json。方案原 962 个 UI 指纹差异为 0。FrameworkScene 未脏；旧 GM GUID 为 bba4cad8012e3f64690d24af5becf55d。

## S1 最小渲染实证

实际环境：Unity6000.5.4f1/URP17.5，Windows Editor，D3D12，RTX5060Ti，Core Ultra7 265KF，Linear/HDR；2560×1440。通过 Unity API 新建专用 SoftGlassUI_Renderer（仅新 Feature），加入 PC 与 Mobile RP rendererDataList 的 index1；原世界 renderer、SSAO 保留。TagManager 原空闲层8—11登记为 EUIBackdrop0—3；本工程确认原相机未使用这些层。

UI核心提供管线无关的采样租约和屏幕表面；URP独立asmdef，实现Base stack中的Overlay分段、AfterTransparents捕获、降采样/HV模糊、跨相机导入持久RT。未使用旧Compatibility或OpaqueTexture代替完整下层UI。

首轮错误：clearDepth只读、BlurShader缺URP Core宏；已修复。首次探针发现新Graphic未声明CanvasRenderer依赖；已加入RequireComponent，重新编译并重跑。当前MCP已确认新程序集MVID `39b17aaf-4fd0-44d5-8f65-734ae94962e6`，编译完成和域重载时间见 `evidence/s1/compile-state.json`，之后Console无产品编译错误；MCP自身WebSocket重连警告单独记录，不算产品错误。

`evidence/s1/frame-debugger.json`：同一渲染帧内，事件7为下层HUD，9—13为B1降采样/HV；14—16为Popup1前景；18—22为B2降采样/HV；23—25为Popup2及上层标记；27才最终resolve。暂停时节流时钟会使捕获不执行，因此此抓帧明确用Editor诊断回调Invalidate强制捕获，未把缺捕获的早期帧记为通过。

实际图片：`s1-single-fixed.png`、`s1-two-live-msaa4.png`；B1源包含动态世界/HUD，不包含Popup1/2或上层标记；B2源包含Popup1及其表面，不包含Popup2/上层标记。运行帧号文字持续更新。源纹理为640×360、B10G11R11_UFloatPack32、MSAA1；PC源切4×MSAA仍正常，之后已恢复1×。最终画面方向、世界上下色块及文字对应，未见倒置；未进行精确色度测量。MSAA4源图见 GLASS-B1-msaa4-source.png / GLASS-B2-msaa4-source.png。此为技术探针，不是正式控件/GM预览，不代替AC13。

## S2 与 S3 进行中

已接入Inherit=0/Solid/Blur、全局默认Blur、Binding旧schema tint保留、创建请求/快照往返字段、OnOpen后展示前解析、池会话释放、FullScreen首次捕获屏障。新增定向测试类：EUIBackdropPolicyEditTests、EUIBackdropLeaseEditTests、EUIGlassGeometryEditTests。首轮8/9，租约夹具缺Camera导致未获取；补齐Camera后9/9通过（0.875秒），见 evidence/s2-first-tests.json 与 s2-tests-passed.json。此后组活跃消费者检查又有改动，最终需要受影响复测；尚不能记Popup运行行为通过。

24目标已全部通过路径预检。EUI中心已创建18个正式Prefab（2骨架+16Items），资产/Binding契约回执 evidence/eui-build-receipt.json；Button/Close/Search/Extension四项通过中心SourceSkeleton形成Variant。第一次StandardPage误走展示分支因按钮尚未创建而停止，已修正制作器并通过Prefab API修复本任务新建骨架，保留GUID 42bb70bc27c31e54a8b1e3cc0f59da16，再显式resume。第二次工具响应为空，未盲重跑；确认磁盘回执18目标完整后才请求编译。真实Binding字段已读取，16份Business用户逻辑已接入；原生成空骨架备份在 evidence/generated-skeletons/，没有手写Binding字段。旧GM时间范围由真实Prefab核对为0.1—3、非整数、初始1。Center与Pages仍待下一批创建。

S3控件/公共弹窗、S4 GM/入口、S5质量性能/成品预览均未完成，后续继续。建议预算未由用户确认，未宣称移动或Player构建达标。无人工视觉签收、触摸真机验证或独立验收结论。

## S3/S4 当前增量与定向返工

已通过中心制作全部24目标、22 Binding与5新Page注册，独立SoftGlassLibrary与catalog已创建。维护工程外观配置显式选择SoftGlass，缺配置仍Legacy；后端安装配置独立，默认Popup效果仍Blur。GameInit仅替换GM选择调用，topdown试玩/输入接线保持。旧GM未改受管区。通过真实生成字段接入Item、对话facade、GM action registry和静态/动态Item示例。

24资产初次运行前MCP编译/域重载已完成，加载Assembly-CSharp MVID `44a0ac38-6676-4a78-9fd8-28518e102057`；Console为0错误，库24条。首轮Play已运行真实Showcase与GM，但图片 `evidence/runtime/` 是返工诊断，不能作为最终AC13预览。

运行发现：原嵌套Canvas的worldCamera会作用到根Canvas，原尝试按嵌套Canvas分派相机会错误采到旧Main；beginCameraRendering重新绑定的临时探针同样失败，已经随退出Play移除，不交付该路线。最终调整：独立采样边界必须是根Canvas；普通嵌套Graphic/Mask共享祖先Group，后端只分派根Canvas。GM所有卡片/入口共用根组；示例页通过SoftGlassBackdropAttachment临时借出已制作的背景Canvas成为根，释放后归还原父级、序位和Rect布局，不运行时生成UI、不改Prefab磁盘层级。与D1的嵌套分段提议有此实现偏差，R03的下层UI采样要求保持。该修复尚待本轮编译与再次Play实证。

另修复：竖Slider原生stretch导致手柄过宽、Panel标题在小尺寸溢出、圆形Item在流布局被拉成胶囊。GM几枚Unicode图标在现有字体缺字，正在替换为包内矢量线形Graphic，保留现有字体资产；不是通过对用户字体追加字形规避。正式预览与最终证据待上述返工完成后生成。
