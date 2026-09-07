---
name: ember-plugin-migrate
description: >-
  Use when the user requests migrating third-party Unity plugins to UPM,
  mentions 插件迁移、插件转包管理、扫描插件 or /plugin-migrate.
  Do not use for Ember framework code, game modules, ordinary dependency inventory, or unrelated package managers.
---

# 第三方插件迁移到 UPM

先读取 `CLAUDE.md`、`docs/user/package-inventory.md`、`docs/dev/upm-migration-plan.md` 和当前 manifest/lock。区分“评估迁移”与“执行迁移”：前者只出方案，后者在已明确的插件和目标范围内实施，范围或授权不清楚才询问。

## 核对与选型

- 用 rg 扫描 Assets 中的第三方目录，排除业务 Game、GameResource、项目配置和受控模板快照。识别源码、DLL、asmdef、Editor、Resources、StreamingAssets、安装脚本及硬编码路径。
- 对照 manifest、embedded package 和本项目的私有第三方仓库说明，先判断是否已经迁移。Odin/Sirenix 已通过私有 Git URL 引入，不能再按名称标为“一律不可迁移”。迁移可行性须以具体版本、路径约束和程序集依赖验证。
- 使用官方文档/仓库核实 registry 或 Git 包的版本、UPM 目录和兼容性；不把缓存的“最新版本”当现状。没有网络证据时只报告本地可确认的方案。
- 来源选择考虑授权、维护和版本固定：官方 UPM/OpenUPM、已验证 Git 包、私有包或 embedded；不把付费内容发布到公开仓库。Unity 官方包是否已安装以 manifest 为准，不能将 Addressables 一概称为已内置。

## 执行迁移

1. 保存源插件及设置的当前状态，列出所有源码/资源引用与原始 GUID。
2. 在目标包建立 `package.json`、Runtime/Editor 边界及所需 asmdef。保留资源、DLL 与文件夹 `.meta` GUID 和导入设置，检查路径变化及许可证保留要求。
3. 准备所有必要修改后执行路径切换，避免源和目标两套相同 GUID/程序集同时被 Unity 导入。Windows 文件操作使用同一 PowerShell，并核对解析后的路径在明确目标目录内。
4. 更新 manifest 和必要的程序集引用；绝不因为 `.asset` 含 `guid:` 就删除它。保留 GUID 的正确迁移可以维持引用；如引用无法保留，先定位并修复对应引用。
5. 通过 Unity MCP 刷新、编译并验证插件初始化/Inspector/运行时调用。失败时按备份恢复本轮迁移，不覆盖其他任务改动。MCP 不可用时明确未验证状态，不使用日志或 BatchMode 代替。
6. 核对迁移完成及所有引用后，清理源目录和对应 meta，更新包清单；许可证、付费包与项目设置保留。不要仅按文件数量决定能否迁移。

## 交付

报告来源、目标目录、版本/解析提交、manifest 和 asmdef 变化、GUID 检查、实际测试结果及未验证项。不自动提交、推送或公开发布。已安装包不重复导入，未完成验证的迁移不标为通过。
