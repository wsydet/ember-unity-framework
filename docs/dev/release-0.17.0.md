# Ember Framework 0.17.0

发布日期：2026-09-29。发布标签：`v0.17.0`。本次为框架 + 模板升级。

## 交付内容

新增通用字体皮肤库与稳定 ID 枚举生成；TMPEx 支持每套皮肤独立字体、固定字体及多选混合值。思源宋体及许可证保留 GUID 迁入 Package，三套默认预设为宋体、钉钉进步体与阿里妈妈东方大楷。小说 UI 接入 TMPEx，多语言 Key 保留。

整理 Odin 编辑工作区、小说参数和预览分区，修复项目中心页签切换后恢复旧选择。图片统一由“图片资源管理”管理立绘、背景及皮肤，移除独立立绘入口。

| 模板 | 内容版本 | 兼容框架 | 内容 hash |
|---|---|---|---|
| base | 0.7.0 | 0.17.0 | 274ae24260525ab08b7264fc66625b57 |
| source3d-2p5d | 0.4.0 | 0.17.0 | 0ad733aaf3f71e7f77625ba7f814ddb9 |
| visual-novel | 0.17.3 | 0.17.0 | 81d0bb8ef1549a126ede75b3500e8805 |

小说模板复用已完成的 SaveTemplate/Bump 封存，未重复升版；根模板正式声明后，通过 ApplyParentSync 同步两个派生模板，内容无变化。第三方依赖及消费端技能内容不变，仍使用既有依赖基线与 bundle sourceCommit。

## 升级边界

通过 `Ember/UPM Manager` 升级框架，不手改消费项目 manifest/lock。小说从 0.16.0 跨 minor 到 0.17.3，先备份并保护业务定制，再按模板迁移流程部署、恢复和合并；“补齐缺失”不能替代升级。

思源宋体源文件、TMP 资源和许可证已从模板 Assets 移入 Package，GUID 保持不变。旧项目必须协调移除或迁移原 Assets 中同 GUID 的字体，不能让两份资源并存；先保留完整备份，核对业务引用后按[字体皮肤说明](../../Packages/com.ember/Documentation~/manual/font-skins.md)迁移。升级框架不自动迁移已部署模板。本次未操作消费项目。

## 本次验证

- 正确框架实例 Unity 6000.5.4f1：请求编译后域重载完成，编辑器就绪，无编译错误；控制台有一条 MCP WebSocket 初始化警告。
- 16/16 EditMode 回归通过：EmberFontSkinEditTests、EmberLocalizationEditTests、EmberLocalizationSourceEditTests。任务 ID：`e88d2f67d5d744c4bfbcf9e269320cab`。
- 三个模板实际内容 hash 与封存一致，父快照对应 base 0.7.0，谱系无错误，当前小说编辑记录一致。
- 迁移宋体与 v0.16.0 已发布模板原始字体逐字节一致，sfnt 表校验通过；新路径加入共享二进制清单。
- 暂存候选的共享二进制检查通过（包括 core.autocrlf=false/true 干净检出），共享字体 Dynamic/Multi Atlas、材质与后备字体配置检查通过。
- 技能 bundle 的 29 个文件原始 SHA256 均符合 manifest，内容对应记录的 sourceCommit 和当前已提交源。标准 `build-ai-skill-bundle.ps1 -Check` 因 5 个工作区源文件仅 CRLF/LF 差异未通过；逐文件比对确认差异仅为换行，未修改或重新生成 bundle。

完整 PlayMode、所有 Odin 面板交互/分辨率、消费端全新安装及升级仍未验证。历史批次渲染与导航验收不替代本次完整验收。

发布声明见 [release-0.17.0.json](../../Packages/com.ember/Dependencies~/release-0.17.0.json)，完整依赖基线见 [manifest-0.17.0.json](../../Packages/com.ember/Dependencies~/manifest-0.17.0.json)。
