# S2 视觉复验与修正（2026-10-26）

用户指出对勾变形、圆角模糊和进度条九宫格不正确；此前 L07/L09 的“正常”结论过宽，以本次修正截图为准。

原因：12逻辑像素高的进度条复用了四边18像素的旧九宫格，端部纵向压缩、横向过宽；通用64像素几何用于不同尺寸，没有分别校准。选中标记使用字体字符，不适合状态图标。

已修改六个源 Prefab，保留 Prefab GUID 和 Binding 字段，嵌套/Variant 继承更新：ActionButton、ChoiceToggle、ContentHost、Progress、StandardPopupPage、OptionRowItem。没有改动 C# 或生成字段，仅通过 Unity API 编辑资产、MCP刷新及运行验证，无需重编译；本次没有重跑上轮56项测试。

- 新制 RoundedSurfaceHiDPI.png：256×256，PPU400，border64，无压缩/无mipmap；普通控件倍率4/3（12逻辑像素角），12高进度条倍率8/3（6逻辑像素端部）。是独立制作的确定性几何图片，不使用 Unity 内置 Sprite。小状态块倍率4。
- 新制 Checkmark.png：imagegen 内置工具生成，真实透明，源1280×1280，Unity导入Max256、无压缩、Bilinear、无mipmap；28×28等比图标框。旧 Selected TMP 清空并禁用，状态图由 Selected/Checkmark Image 表现；Toggle.graphic 和公开 selected 槽位同步。保留原节点身份与选择逻辑。
- 同步修正 OptionRowVariant 的 label 槽位：旧配置误指向 Selected TMP，现在明确指向 Label。首次生成目录发现缺少 label 后修复，最终8条目 Validate=[]、Binding均无错误、目录摘要一致。
- 皮肤资源修订为 EmberDefaultLight1.0.1；base仍0.8.4，未Bump/提交/发布。普通保存477文件，hash 597adad19e785452af360455243b8983。保存前13项差异均为本轮资产/配置/说明。

实际Play截图：[100%](visual-fix-full.png)、[50%](visual-fix-half.png)。Toggle点击后isOn=false、进度0；检查了真实Track250×12、半值Fill125×12。Console error/warning=0；退出Play。未重测4:3/实体触摸，本次不扩大验收结论。

新图位于 `Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/`。Checkmark最终来源 `C:/Users/wuyu/.codex/generated_images/01a177d4-e403-7dd3-a4f1-82c43c94341b/exec-045628e6-625d-4b61-b762-1da0ea47b559.png`，已复制入工程。首次生成边缘有杂点，第二次清理后导入并按实际小尺寸检查，不以大图返回等同可用。

生成提示词：Use case: logo-brand. Asset type: production Unity UI checkmark sprite for a neutral macOS-inspired light theme. One single clean white checkmark centered on a true transparent square canvas. Flat solid white fill, two arms with a clear angled elbow, short lower-left arm and longer rising upper-right arm, uniform moderately bold stroke, subtly rounded ends and join. Crisp vector-like silhouette, no texture, no shadow, no glow, no gradient, no enclosing box or circle, no text. The checkmark fills about 80 percent of canvas width, with balanced transparent margins. Designed to remain legible at 20 to 24 screen pixels; naturally wide checkmark, never tall/narrow or squashed. Transparent alpha, not a checkerboard.

修图提示词：Refine this checkmark sprite. Preserve the same white checkmark silhouette, balanced placement and true transparent background. Remove ALL stray speckles, paint texture, ragged fringe, fibers and artifacts around the contour, especially inside the elbow. It must have mathematically smooth clean anti-aliased vector-like edges, continuous uniform solid white interior, smoothly rounded caps. This is a polished tiny UI icon, not a painted brush stroke. No shadow, glow, backdrop or decoration.

后续制作偏好：不合适的图标制作新图片，尽量避免 Unity 内置图片；不能拿字体字符或低质量占位图冒充正式图标。九宫格仍按实际像素/PPU/控件厚度校准，不因高分辨率就自动正确。

备份 `.utmp/ui-production-S2-visual-fix/`；四份更新契约为 `*.visual-fix.contract.json`，保留上轮证据。
