# Ember 消费端依赖声明

当前发布基线为 **0.21.4**：[发布声明](release-0.21.4.json)、[完整 manifest](manifest-0.21.4.json)。Unity 开发基线为 **6000.5.4f1**。

## 清单范围

完整 manifest 包含 56 项直接依赖（开发项目 55 项加 Feel），按需选用，不代表每个消费项目都需要全部开发工具。Odin、Feel、UniRx 分别使用 `odin-v4.0.3`、`feel-v5.4.1`、`unirx-v7.1.1`；其他三方包保留原标签，可选 Unity MCP 固定到官方 v10.3.0 标签。Unity/OpenUPM 及内置模块版本不变，间接依赖由 UPM 解析；本清单不是跨 Unity 版本的 lock。

## 已有消费项目升级

**所有消费端框架升级必须通过 `Ember/UPM Manager`。** 不得手改 manifest 的 URL/版本或 packages-lock 的提交 hash；无法操作升级器时，由用户在 Unity 中执行。升级后核对实际包版本并完成编译。

本次交付 Loading／遮幕解耦、MCP 显式升级入口与公共规则，无需重新部署模板；同场景恢复的业务时序须按[Loading 接入契约](../Documentation~/ui/loading-sessions.md)单独接线验收。框架升级不会更新已部署 Assets。模板沿用 base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.21.2 与 platformer-2d 0.11.1；平台0.11.0→0.11.1按三方补丁预览保护定制，跨 minor 升级须先保护定制内容再迁移；仅更新 Skill 不会安装实现代码。详见[改动回流规则](../Documentation~/maintenance/unityfarm-change-routing.md)。

## 首次安装与依赖配置

1. 先确认第三方仓库清单所列各包标签 和框架仓库 `v0.21.4` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。框架升级只更新 `com.ember`；可选包须逐项点击安装，不会自动装齐全部依赖。

可选包按钮使用 `release-0.21.4.json` 的 `optionalPackageInstallTargets`：Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector 使用各自独立版本标签，Feel 使用 `feel-v5.4.1`。Unity MCP 使用官方仓库 10.3.0 的固定标签，与完整 manifest 的 Unity MCP 地址一致。依赖沿用 0.20.0 的逐包声明。已安装包不会被安装按钮覆盖；MCP 旧官方稳定版可显式点击升级，具体来源保护见[安装与更新说明](../README.md#unity-mcp-的安装与更新)。

## 模板声明与验证

沿用 base **0.8.3**、source3d-2p5d **0.5.0**、visual-novel **0.21.2** 和 platformer-2d **0.11.1**。同 major.minor 的框架补丁保留兼容声明 **0.21.0**；本轮未保存或 Bump 模板。四个模板实际内容、封存 hash、父快照与谱系已由 Unity MCP 只读核对一致。

本轮相关代码96个不同用例通过，发布前主动编译与域重载确认，Console无错误/警告。未全量重测，消费项目接入与发行构建待验收；专属字典仍在 Assets 维护源，未进入本版封存模板。详见[发布说明](../../../docs/dev/release-0.21.4.md)。

## 发布顺序

按 `package.json → CHANGELOG → release/manifest → commit → annotated tag → push` 发布 `v0.21.4`。框架技能来源对齐此 tag，共 11 项，内容与 sourceCommit 保持不变。
