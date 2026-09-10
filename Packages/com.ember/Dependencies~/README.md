# Ember 消费端依赖声明

当前已发布基线是 `0.12.0`；`manifest-0.12.0.json` 与 `release-0.12.0.json` 是配置表版本的正式发布声明。Table 测试、模板内容封存与模板兼容声明已完成；UnityFarm 的 UPM 更新和实际读表结果作为发布后消费验收单独记录。

## 这份清单的范围

- `manifest-0.12.0.json` 是完整开发环境的可移植依赖基线：当前项目 manifest 的 55 项直接依赖，加上 Feel，共 56 项。
- 框架固定为 `v0.12.0`；本次未改变第三方包，7 个第三方包继续固定到私有仓库已发布的 `ember-v0.11.1`；Unity MCP 固定到开发工程 lock 中实际解析的 commit，不使用浮动 `main`。
- Unity/OpenUPM 包与内置模块沿用当前项目直接声明的版本，间接依赖仍由 UPM 解析；这不是跨 Unity 版本可复现的 lock 文件。完整基线面向 Unity **6000.5.4f1**，尤其 Feel/URP 的版本不能只看框架主包的最低版本字段。
- `release-0.12.0.json` 记录第三方包版本、模板兼容声明和可按需省略的开发工具。完整清单包含开发工具，不意味着每个消费工程都必须使用全部工具。
- 0.12.0 Table V1 不增加第三方依赖；本版清单只把 `com.ember` 目标推进到 `v0.12.0`，其余来源与版本保持一致。

## 如何使用

1. 先确认第三方仓库 `ember-v0.11.1` 和框架仓库 `v0.12.0` 已真正发布，且消费机器有私有仓库访问权限及适用的插件授权。
2. 备份消费项目的 `Packages/manifest.json`、`packages-lock.json` 和现有插件/设置。
3. 将本清单的 `dependencies` 按包名合并到消费项目 manifest，将 `scopedRegistries` 按 registry URL 合并 scope。保留该项目其他依赖、registry、testables 及其他配置，不直接覆盖整份 manifest。
4. 已有同名 embedded 包或 Assets 插件时，先核对本地修改并制定迁移；特别是 Feel/MMTools/MMFeedbacks/NiceVibrations，不能让原 Assets 插件和新 UPM 包同时被导入。本文不授权自动删除旧内容。
5. 让 Unity 完成 UPM 解析、手动触发编译，并验证 MMF Player、Odin、DOTween、输入、场景与模板部署。保留生成的消费项目 lock，不复制开发机的本地路径。

这些 JSON 是发布声明，不是会自动执行的安装脚本。当前 `Ember/UPM Manager` 仍只升级 `com.ember`；单独升级框架不会自动读取本目录并装齐所有依赖。

## 模板声明

0.11.5 未修改模板 Assets：`base` 保持 `0.5.6`，`source3d-2p5d` 保持 `0.2.7`，内容 hash 与父基线不变；二者只将框架兼容声明推进到 `0.11.5`。0.11.4 的稳定 GUID、部署前冲突检查和完整重新部署能力继续保留。

## 发布顺序

第三方内容未变，复用已发布的 `ember-v0.11.1`；框架发布 `v0.12.0`。Table 是新的模板结构，已有项目升级包后仍需按需人工迁移 Table Module、Catalog 与数据目录，框架升级不会自动覆盖业务模板文件。

0.12.0 已完成用户侧 Unity 编译/EditMode、base 项目中心保存与 Bump、source3d-2p5d 父同步、兼容声明和内容 Hash 校验，并按 `package.json → CHANGELOG → release/manifest → commit → tag → push` 发布。UnityFarm 随后通过 UPM Manager 更新到 `v0.12.0`，再记录模板迁移、编译与实际读表结果。
