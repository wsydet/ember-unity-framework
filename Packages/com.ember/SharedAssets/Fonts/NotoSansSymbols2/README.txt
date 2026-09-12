Noto Sans Symbols 2 — Ember 符号后备字体

用途
钉钉进步体源字体没有 ▶（U+25B6）等符号。共享的 DingTalk-JinBuTi SDF 已将
NotoSansSymbols2-Regular SDF 接入 Fallback Font Assets。主字体能显示的字保持
原字体；缺失且此字体包含的符号由 TMP 自动补齐，无需修改业务文本或模板。
这是字体资产自身的后备链，不修改消费项目的 TMP Settings。

覆盖
原版 TTF 有 2,955 个 Unicode 映射。以下字符已检查源字体覆盖：
▶◀▲▼▷◁△▽✓✔✕✖★☆●○◆◇■□⚠⏸⏹⏵⏴
这不是完整 Unicode / 彩色 Emoji 字体；未收录的字符仍需别的字体或 Sprite。
→←↑↓ 不在此符号字体中，由包含它们的主字体显示。

资产配置
Dynamic + Multi Atlas，90 pt，1024 × 1024，padding 9，SDFAA。
交付独立的材质和空白动态图集，按实际使用的字符生成字形；Include Font Data
开启，构建包含源 TTF，不依赖玩家电脑安装字体。SDF 使用现有 TMP 序列化结构，
FaceInfo 度量来自原 TTF；导入和实际排版仍须在 Unity 中验收。
TTF 原文件 1,233,128 字节，未裁剪、未修改。许可证与字体一同交付。

来源与许可证
字体：Noto Sans Symbols 2 Regular
版权所有：2022 The Noto Project Authors (https://github.com/notofonts/symbols)
许可证：SIL Open Font License 1.1，完整条款见同目录 OFL.txt。
分发来源：https://github.com/google/fonts/tree/8b0a1d0f5983c89bc2b93f1b5fb55f9e252744b5/ofl/notosanssymbols2
下载日期：2026-09-12
SHA256：7D5FB73B7CA67A6798101741F5D280A3D016A56A197AFCD4199DBB57B4B82A21

验证与消费
静态检查入口：scripts/check-shared-binaries.py 与 scripts/check-shared-font-config.py。
Unity PlayMode：SharedFontAtlasPlayModeTests.SymbolFallback_RendersMissingSymbolsAndKeepsChinesePrimary。
本字体随框架 0.12.8 发布，UnityFarm 通过 Ember/UPM Manager 升级；不需重新部署模板。
验收含 ▶ 的步骤文本、中文与符号混排、材质和行高；用户确认编译无报错，PlayMode 与消费端渲染尚未验收。
