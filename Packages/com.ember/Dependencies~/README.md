# Ember 消费端依赖声明

当前发布基线为 **0.21.2**：[发布声明](release-0.21.2.json)、[完整 manifest](manifest-0.21.2.json)。Unity 开发基线为 **6000.5.4f1**。

## 清单范围

完整 manifest 包含 56 项直接依赖（开发项目 55 项加 Feel），按需选用，不代表每个消费项目都需要全部开发工具。Odin、Feel、UniRx 分别使用 `odin-v4.0.3`、`feel-v5.4.1`、`unirx-v7.1.1`；其他三方包保留原标签，Unity MCP 固定到既有提交。Unity/OpenUPM 及内置模块版本不变，间接依赖由 UPM 解析；本清单不是跨 Unity 版本的 lock。

## 已有消费项目升级

**所有消费端框架升级必须通过 `Ember/UPM Manager`。** 不得手改 manifest 的 URL/版本或 packages-lock 的提交 hash；无法操作升级器时，由用户在 Unity 中执行。升级后核对实际包版本并完成编译。

框架升级不会更新已部署 Assets。本次交付 base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.21.2 与 platformer-2d 0.11.1；平台0.11.0→0.11.1按三方补丁预览保护定制，跨 minor 升级须先保护定制内容再迁移；仅更新 Skill 不会安装实现代码。详见[改动回流规则](../Documentation~/maintenance/unityfarm-change-routing.md)。

## 首次安装与依赖配置

1. 先确认第三方仓库清单所列各包标签 和框架仓库 `v0.21.1` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。框架升级只更新 `com.ember`；可选包须逐项点击安装，不会自动装齐全部依赖。

可选包按钮使用 `release-0.21.1.json` 的 `optionalPackageInstallTargets`：Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector 使用各自独立版本标签，Feel 使用 `feel-v5.4.1`。Unity MCP 使用官方仓库 10.1.2 的固定提交，与完整 manifest 的 Unity MCP 地址一致。依赖沿用 0.20.0 的逐包声明。已安装的包（包括直接导入的插件）不会被安装按钮覆盖。

## 模板声明与验证

交付 base **0.8.3**、source3d-2p5d **0.5.0**、visual-novel **0.21.2** 和 platformer-2d **0.11.0**。同 major.minor 的框架补丁保留兼容声明 **0.21.0**。platformer-2d 已通过项目中心保存/Bump，内容 hash 为 `48deb2fc08b00e1c7ee279890b289dad`；父模板 base 0.8.3，父 hash 为 `1270ce6dfcae3c01d050c09ecfd60337`。其余模板内容不变。

本次载具定向回归 114/114、到期/普通节奏/单向平台回归 49/49，正式唯一 PlayerModule 流程 8/8；已有 821 项结果保留，未重跑全套。实际 FPS 90.82–118.82，目标 120，不代表稳定 120。框架侧 P01/P04 修复已交付；Windward 消费与场景验收待完成。详见[发布说明](../../../docs/dev/release-0.21.1.md)。

## 发布顺序

按 `package.json → CHANGELOG → release/manifest → commit → annotated tag → push` 发布 `v0.21.1`。框架技能来源对齐此 tag，共 11 项，内容与 sourceCommit 保持不变。
