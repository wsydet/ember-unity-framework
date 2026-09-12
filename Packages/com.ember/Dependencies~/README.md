# Ember 消费端依赖声明

0.12.8 为共享钉钉进步体增加 Noto Sans Symbols 2 后备字体，补齐 ▶ 等缺失符号。用户确认 Unity 编译无报错；本会话无 Unity MCP 验证，新增 PlayMode 案例及消费端渲染待验收。

当前发布基线是 `0.12.8`；`manifest-0.12.8.json` 与 `release-0.12.8.json` 是正式发布声明。模板保持 base 0.6.3 与 source3d-2p5d 0.3.5。

## 这份清单的范围

- `manifest-0.12.8.json` 是完整开发环境的可移植依赖基线：当前项目 manifest 的 55 项直接依赖，加上 Feel，共 56 项。
- 框架固定为 `v0.12.8`；本次未改变第三方包，7 个第三方包继续固定到私有仓库已发布的 `ember-v0.11.1`；Unity MCP 固定到开发工程 lock 中实际解析的 commit，不使用浮动 `main`。
- Unity/OpenUPM 包与内置模块沿用当前项目直接声明的版本，间接依赖仍由 UPM 解析；这不是跨 Unity 版本可复现的 lock 文件。完整基线面向 Unity **6000.5.4f1**，尤其 Feel/URP 的版本不能只看框架主包的最低版本字段。
- `release-0.12.8.json` 记录第三方包版本、模板兼容声明和可按需省略的开发工具。完整清单包含开发工具，不意味着每个消费工程都必须使用全部工具。
- 0.12.8 不增加第三方依赖；本版清单只把 `com.ember` 目标推进到 `v0.12.8`，其余来源与版本保持一致。

## 已有消费项目升级

**禁止直接修改项目的 manifest 文件来升级，所有的消费端升级都必须通过 `Ember/UPM Manager`。**
不得手改 manifest 的 URL/版本或 packages-lock 的提交 hash；无法操作升级器时，由用户在 Unity 中执行。
升级后只读核对实际包与锁定提交，再手动编译并验证 ▶ 等符号与中文混排、材质和行高。无需重新部署模板。
正式规则见 [升级规则 §7.1](../Documentation~/maintenance/unityfarm-change-routing.md#71-仅框架变化)。

## 首次安装与依赖配置

1. 先确认第三方仓库 `ember-v0.11.1` 和框架仓库 `v0.12.8` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。当前 `Ember/UPM Manager` 仍只升级 `com.ember`；单独升级框架不会自动读取本目录并装齐所有依赖。

## 模板声明

用户已通过模板面板保存并封存 `base 0.6.3` 与 `source3d-2p5d 0.3.5`；派生父版本与 ParentSnapshot 对齐 base 0.6.3。两份模板均包含新版引导编辑器且 GuideModule.Enabled=false。两份模板保持框架兼容声明 0.12.0，按 major.minor 规则适配本补丁。升级框架不会覆盖已部署 Assets；既有项目采用新模板内容需另行通过项目中心的模板流程处理并核对业务改动。

## 发布顺序

第三方内容未变，复用已发布的 `ember-v0.11.1`；框架发布 `v0.12.8`。已有项目升级包后不会覆盖业务模板文件。

0.12.8 按 `package.json → CHANGELOG → release/manifest → commit → tag → push` 发布。此次发布共享符号后备字体、原版 TTF、OFL 许可证及相关回归；模板和第三方依赖不变。
