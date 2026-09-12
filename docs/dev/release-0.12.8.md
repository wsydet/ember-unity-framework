# Ember Framework 0.12.8 发布说明

日期：2026-09-12。Tag：`v0.12.8`。归属：**框架升级**。

## 符号后备字体

UnityFarm 的步骤序号使用 `▶`（U+25B6），钉钉进步体源 TTF 不包含该字形，TMP 因而输出缺字警告并显示方框。增加图集容量不能解决源字体缺字。

共享 `DingTalk-JinBuTi SDF` 的 Fallback Font Assets 接入随包交付的 `NotoSansSymbols2-Regular SDF`。保留主字体 GUID、源字体、图集和材质；主字体已有字形继续使用主字体，缺失的受支持符号由后备字体补齐。已有项目的文本、Prefab 和 TMP Settings 不需要修改。

Noto Sans Symbols 2 使用独立 Dynamic + Multi Atlas 字体资产、材质和按需生成的图集，90 pt、1024×1024、padding 9。原版 TTF 为 1,233,128 字节，包含 2,955 个 Unicode 映射，检查覆盖 `▶◀▲▼▷◁△▽✓✔✕✖★☆●○◆◇■□⚠⏸⏹⏵⏴`。不依赖操作系统安装字体，不承诺覆盖所有 Unicode 或彩色 Emoji。

字体原件来自 Google Fonts 固定提交 `8b0a1d0f5983c89bc2b93f1b5fb55f9e252744b5`，未修改或裁剪；保留 Noto 作者声明和 SIL OFL 1.1 全文。来源、SHA256 和使用说明见 [字体说明](../../Packages/com.ember/SharedAssets/Fonts/NotoSansSymbols2/README.txt)。

## 发布范围

- 共享字体资产、原版 TTF、许可证、meta、二进制清单和静态检查。
- `SharedFontAtlasPlayModeTests` 增加实际中文/符号混排、后备字体选择和 TMP 子网格回归；测试隔离字体、纹理和材质，避免写入持久化动态图集缓存。
- 模板保持 `base 0.6.3` 与 `source3d-2p5d 0.3.5`，父基线、内容 Hash、0.12.0 兼容声明及 `GuideModule.Enabled=false` 不变。
- 完整依赖清单仍为 56 项，仅更新 `com.ember` 至 `v0.12.8`；第三方继续复用 `ember-v0.11.1`。新增字体随框架直接交付，不增加 UPM 依赖。
- 已有主字体缓存变动、生成的 slnx、Table 状态与本地模板交接记录不进入提交。主字体发布差异仅新增后备引用。

## 验证与升级

用户于本轮确认 Unity 编译没有报错；这是用户提供的手动验证结果，本会话没有独立的 Unity MCP 编译验证。新增 PlayMode 案例、构建及消费端的实际渲染尚未验收。

静态发布检查包括三份 TTF 的长度/SHA256、字体表校验和与 Unicode 覆盖、后备链/材质/源字体引用、许可证、暂存区版本/依赖一致性、模板未变及两种 autocrlf 的干净检出。

已有项目必须通过 `Ember/UPM Manager` 升级到 0.12.8；不需要重新部署或保存模板，不手改消费端 manifest/lock，也不修改 PackageCache。

升级后验收步骤列表中的 `▶`、中文与符号混排、材质、行高及 UI 显隐；在框架 Test Runner 的 PlayMode 执行 `SharedFontAtlasPlayModeTests.SymbolFallback_RendersMissingSymbolsAndKeepsChinesePrimary`。

当前 Unity MCP 不可用，本次未独立完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。用户此前确认的无编译报错结果已保留，不代表上述运行测试已执行。
