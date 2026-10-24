# Ember 消费端依赖声明

当前发布：Ember **0.21.6**，修复 Play 脚本重载后的框架会话退出，提供生命周期和同步所有权中止契约。模板与第三方依赖不变；业务异步所有权适配及 Windward 消费验证待完成。

## 清单范围

完整 manifest 包含 56 项直接依赖（开发项目 55 项加 Feel），按需选用，不代表每个消费项目都需要全部开发工具。Odin、Feel、UniRx 分别使用 `odin-v4.0.3`、`feel-v5.4.1`、`unirx-v7.1.1`；其他三方包保留原标签，可选 Unity MCP 固定到官方 v10.3.0 标签。Unity/OpenUPM 及内置模块版本不变，间接依赖由 UPM 解析；本清单不是跨 Unity 版本的 lock。

## 已有消费项目升级

**所有消费端框架升级必须通过 `Ember/UPM Manager`。** 不得手改 manifest 的 URL/版本或 packages-lock 的提交 hash；无法操作升级器时，由用户在 Unity 中执行。升级后核对实际包版本并完成编译。

0.21.5候选包含 platformer-2d **0.11.2** 地面支撑修复；正式发布后，先升级框架，再对平台0.11.1→0.11.2按三方补丁预览保护定制。框架升级不会更新已部署 Assets，仅更新 Skill 不会安装实现代码。base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.21.2不变。其他历史版本和跨minor迁移按[改动回流规则](../Documentation~/maintenance/unityfarm-change-routing.md)处理。

## 首次安装与依赖配置

1. 先确认第三方仓库清单所列各包标签 和框架仓库 `v0.21.4` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。框架升级只更新 `com.ember`；可选包须逐项点击安装，不会自动装齐全部依赖。

可选包按钮使用 `release-0.21.4.json` 的 `optionalPackageInstallTargets`：Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector 使用各自独立版本标签，Feel 使用 `feel-v5.4.1`。Unity MCP 使用官方仓库 10.3.0 的固定标签，与完整 manifest 的 Unity MCP 地址一致。依赖沿用 0.20.0 的逐包声明。已安装包不会被安装按钮覆盖；MCP 旧官方稳定版可显式点击升级，具体来源保护见[安装与更新说明](../README.md#unity-mcp-的安装与更新)。

## 模板声明与验证

本地候选封存 platformer-2d **0.11.2**，内容与封存 hash 均为 `986ceff5bf74695b8c82a33347a48d7d`。base **0.8.3**、source3d-2p5d **0.5.0**、visual-novel **0.21.2**和父快照不变。同 major.minor 的框架补丁保留兼容声明 **0.21.0**。封存前已通过 MCP 核对平台实际内容、父快照和同步计划；全部模板的发布前核验状态见当版说明。

修复阶段76项相关回归通过，最后8组真实渲染帧率专项复测通过，保留实际FPS和失败记录；此次仅封存与准备版本，复用同内容证据，未全量重测。平台术语字典已纳入0.11.2。Windward接入、首台人工复验和发行构建待验收；发布准备编译状态见[0.21.5说明](../../../docs/dev/release-0.21.5.md)。

## 发布顺序

本版按 package.json、CHANGELOG、release/manifest、提交与 annotated tag 对齐发布 v0.21.6。生命周期修复和验证边界见 [0.21.6 发布说明](../../../docs/dev/release-0.21.6.md)。模板、通用技能和依赖沿用既有内容。
