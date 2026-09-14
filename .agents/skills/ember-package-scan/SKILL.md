---
name: ember-package-scan
description: >-
  扫描 Unity 项目的包依赖，与既有清单比较或生成/同步包清单文档。用于扫描包、更新包清单、同步依赖文档或 /package-scan；不安装、升级或迁移插件。
---

# Ember 包清单扫描与同步

读取项目 Packages/manifest.json、packages-lock.json（如果存在）和适用规则。优先找到现有包清单；用户指定路径优先，其次使用已有 docs/user/package-inventory.md。没有清单时，扫描请求输出报告；生成/同步请求可在该默认路径创建，不要求项目预先存在框架文档。

## 核对来源

- manifest 记录直接依赖和 scopedRegistries；lock 记录实际解析的 source、depth、version、hash。分开呈现声明与解析结果，不能把 Git 标签当作包内 version，也不能以缺失 lock 判断未安装。
- 枚举 Packages 下含 package.json 的真实子目录，不只匹配 com.* / dev.*；读取 name/version/displayName。file: 依赖按 manifest 相对路径解析，核对实际目标。外部本地目录仅按指定包读取。
- Registry/Git 包的实际版本以能定位到的 package.json 为证据；PackageCache 名称可能含提交 hash，不凭目录名猜版本，也不在多个缓存候选中随意取第一个。无法对应时标为未确认。
- 嵌入式包缺 version 时写“未声明”；不把 embedded 来源当成版本号。Unity 内置模块也按本次声明比较。
- 统计直接与传递依赖、可选开发工具和 registry 范围；缓存里的旧包不作为当前依赖。用途依据现有文档或包描述，无法确定时明确标记。

## 输出或更新

按新增、移除、声明变化、解析变化列出差异，保留项目清单已有的用途与说明；“移除”须有当前解析/声明依据，未解析不等于已卸载。生成文档可包含包名、声明、解析版本/提交、来源、用途、证据状态和 registry 表。

只扫描则只输出报告；已要求更新时修订项目自有文档。严禁为匹配文档而改 manifest/lock、安装插件或修改包缓存。记录不确定项与实际检查范围，不把文档同步当作依赖安装验收。
