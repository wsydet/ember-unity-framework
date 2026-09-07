# Ember 0.11.0 消费端依赖声明

## 这份清单的范围

- `manifest-0.11.0.json` 是完整开发环境的可移植依赖基线：当前项目 manifest 的 55 项直接依赖，加上 Feel，共 56 项。
- 框架固定为 `v0.11.0`；7 个第三方包统一固定到私有仓库的 `ember-v0.11.0`；Unity MCP 固定到开发工程 lock 中实际解析的 commit，不使用浮动 `main`。
- Unity/OpenUPM 包与内置模块沿用当前项目直接声明的版本，间接依赖仍由 UPM 解析；这不是跨 Unity 版本可复现的 lock 文件。完整基线面向 Unity **6000.5.4f1**，尤其 Feel/URP 的版本不能只看框架主包的最低版本字段。
- `release-0.11.0.json` 记录第三方包版本、模板兼容声明和可按需省略的开发工具。完整清单包含开发工具，不意味着每个消费工程都必须使用全部工具。

## 如何使用

1. 先确认第三方仓库 `ember-v0.11.0` 和框架仓库 `v0.11.0` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。当前 `Ember/UPM Manager` 仍只升级 `com.ember`；单独升级框架不会自动读取本目录并装齐所有依赖。

## 模板声明

本轮经用户要求处理 0.11.0 声明：先复核 `base 0.5.5` 已封存、父模板内容与派生 ParentSnapshot 完全一致、`source3d-2p5d 0.2.6` 内容与编辑记录一致，再同步父子两份 `frameworkVersion` 为 `0.11.0`。
这是无内容变化的声明处理，不重写 Assets/ParentSnapshot、模板内容版本或 hash，不等于通过 Unity 运行验收。日常变更继续使用项目中心的根模板声明与父级同步流程。

## 发布顺序

先发布第三方仓库的 `ember-v0.11.0`（可同时发布 `feel-v5.4.0`），再发布框架 `v0.11.0`。不要覆盖旧 tag。
声明文件的准备不代表远程 tag 已存在；发布前须完成当前尚未执行的 Unity 编译、Feel 安装和消费端回归。
