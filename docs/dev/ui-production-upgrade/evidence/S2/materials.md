# 默认皮肤和素材来源

EmberDefaultLight 1.0.0，2026-10-26。几何、控件、文字为真实 Unity UI；没有使用整页效果图。背景和空态通过本轮 imagegen 技能生成，正式副本已经导入 Assets，运行和模板分发不引用生成缓存。

| 资产（相对 Assets/GameResource/Resources/UI/Common） | 来源与尺寸 | 导入/用途 |
| --- | --- | --- |
| Textures/DefaultLight/Background.png | imagegen，1672×941，不透明 | Sprite Single，PPU100，Max2048，Bilinear，无 mipmap、无压缩；页面背景，非大图 Atlas |
| Atlas/DefaultLight/EmptyState.png | imagegen，1536×1024，真实透明 alpha | Sprite Single，PPU100，Max2048，Bilinear，无 mipmap、无压缩；独立空态插画 |
| Atlas/DefaultLight/RoundedSurface.png | Unity 制作助手生成基本圆角几何，64×64 | Sprite Sliced，border 四边18；背景/卡片/按钮，不烘焙文字 |
| Fonts/DefaultLight/LiberationSans.ttf、OFL.txt | 复用本项目 TMP Essentials 已有 Liberation Sans 与授权 | 可分发源字体；没有下载/使用系统字体 |
| Fonts/DefaultLight/DefaultLightSans.asset | TMP_FontAsset.CreateFontAsset 建立动态字体及材质/Atlas 子资产 | 固定字体皮肤4；正文槽位1、标题槽位2；符号/CJK 回退框架 DingTalk |

主操作蓝色约 #0967F2，浅灰白底，次操作白底细边框；按下变暗、禁用降对比度，选中使用明确勾选而非只靠颜色。轻阴影/边缘为 Unity UI 效果与九宫格。图片像素检查见 [image-inspection.json](image-inspection.json)。字体依赖已有 TMP Shader/Essentials 和框架字体，均为工程/包资产，不依赖本机缓存。

背景原始提示词：

> Use case: stylized-concept. Asset type: Unity UI background texture, landscape 16:9. Create a macOS-inspired neutral very light abstract background, gray-white #F5F5F7 and extremely pale cool blue, soft minimal flowing layers near edges, quiet low-contrast center for native UI cards and dynamic text. Flat front-facing composition, opaque, crops gracefully. No text, buttons, windows, interface screenshot, logos, devices or border. This is only a background asset.

空态原始提示词：

> Use case: stylized-concept. Asset type: independent sprite for a neutral light Unity UI empty list. A small centered abstract arrangement of two blank rounded cards and a shallow collection tray. Refined minimal gray-white surfaces, pale blue accents, soft restrained dimensional shading, clear silhouette, macOS-inspired calm aesthetic. Generous transparent margin all around, entire subject visible. No text, numbers, buttons, logos, background panel or checkerboard. True transparent background with alpha.

原始生成文件位于 `C:/Users/wuyu/.codex/generated_images/01a177d4-e403-7dd3-a4f1-82c43c94341b/`：背景 `exec-68dc0a1a-9da7-495a-9f4d-ca4f4b373566.png`，空态 `exec-eb373da6-0ea1-4ce3-b320-3cd42c9b1d15.png`。这些路径仅为本轮来源追踪，正式来源是上述 Assets 文件。
