# 天台示例素材记录

2026-09-21。工具：内置 `image_gen.imagegen`；参考输入为 `UI/Module/Narrative/Atlas/LastLight/Portraits/lin_evening.png`。输出复制为 `Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Portraits/zhou_evening.png`。未程序化裁剪或重绘，保留真实透明 alpha；Unity 导入 Sprite Single、FullRect、最大 2048、无 mipmap。

完整生成提示：

Create one new visual novel character sprite, matching the reference image's detailed painterly semi-realistic anime style, realistic dark fabrics, subtle warm dusk rim light, restrained emotional tone. New subject is a young adult East Asian man, short softly tousled dark hair, charcoal jacket over cream knit sweater, relaxed thoughtful expression, holding a small old portable recorder casually in one hand near waist, three-quarter pose facing slightly toward viewer's left to converse with the reference woman. Portrait composition from head to mid-thigh, entire hair and arms visible with ample transparent margins, comparable head scale to reference. Only this male character, no woman, no scenery, no floor, no text, no UI, no cast shadow backdrop, no glow cloud. Production-ready isolated sprite on genuinely transparent alpha background. Preserve clean alpha edges; do not paint a checkerboard. Vertical 2:3 canvas. Reference is style guidance, not a request to modify the woman.

语音：Windows System.Speech / Microsoft Huihui Desktop，Rate=-2。两句分别为“等这盏灯亮起来，我们就一起回去。”和“晚安，许老师。迟了一年，也还是想说给你听。”保存为 Audio/Narrative/LastLight/wan_wait.wav 与 wan_goodnight.wav。此为可替换合成演示，不是真人录音。

## 菜单背景与统一界面（2026-09-24）

工具：内置 `image_gen.imagegen`。输出原样复制为 `Assets/GameResource/Resources/UI/Common/Atlas/Novel/menu_evening.png`，用于主菜单、设置和存档页。Unity 导入为 Sprite Single，最大 2048，无 mipmap。文字、按钮、边框和滑块为正式 EUI 控件，未烘焙在图片中。

完整生成提示：

Create a production-ready 16:9 background artwork for a literary visual novel menu, no text, no buttons, no UI, no people. Anime cinematic painted realism, quiet school rooftop at blue hour after rain, distant mountains and city windows softly glowing amber, steel blue and charcoal palette, restrained warm ivory highlights. Composition: the left 45 percent is dark, low contrast mostly empty sky and gently shadowed rooftop, specifically to make warm-white menu typography readable. The right side holds a subtle roof railing and distant illuminated windows, with soft sunset fading on horizon. Refined atmospheric composition, understated, contemplative, no bright saturated colors, no fantasy effects, no logos. Full bleed landscape.
