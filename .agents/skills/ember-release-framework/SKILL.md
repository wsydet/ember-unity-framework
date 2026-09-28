---
name: ember-release-framework
description: 准备、检查并发布 Ember 框架的新版本，协调包版本、模板封存与父级同步、发布声明、验证、提交和远端 tag。用于发布新版框架、框架发版或 /release-framework；不用于消费项目升级、第三方插件发布或仅创建发布技能。
---

# Ember 框架发布

仅用于 Ember 框架源码仓库。技能本身放在 `.agents/skills`，不加入面向消费项目的 `catalog.json`。

## 先确定范围

1. 读取仓库 `AGENTS.md`、`CLAUDE.md`、`docs/dev/upm-migration-plan.md`。涉及 UnityFarm 回流时，完整读取 `Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md` 并先分类。
2. 检查工作区、暂存区、未跟踪文件、当前分支、upstream、远端 tag 和最近发布声明。识别已有发布进度，避免再次升版或重复提交。保留无关改动。
3. 用户要求“发布”即授权完成相关检查、选择性提交、创建 annotated tag 并推送到已配置的发布远端，不为同一操作反复询问。“只检查”保持只读；“准备发布”完成本地准备，停在提交与推送之前。范围或目的仓库确实不明确才澄清。
4. 从实际变更及仓库版本规则选择版本，说明选择依据。不得写死版本号、分支或远端。消费项目或 PackageCache 不是发布工作区；转到正确源码仓库后再操作。

## 准备版本和模板

1. 先确认本次代码/资源已完成，查阅 [发布操作细节](references/release-workflow.md)。复用仍对应当前改动的验证证据，禁止复制旧发布的测试结论充当本次结果。
2. 用 `scripts/upm-bump-version.ps1 -Version <版本> -Check` 预览，再按该脚本正式更新包版本。不要采用它可能输出的批量 `--tags` 推送建议。
3. 模板内容走“加载 → 修改项目 Assets → SaveTemplate → 显式 Bump”。已完成保存与封存的模板不重复 Bump；不得重新加载以覆盖尚未保存的 Assets。禁止直接改模板快照、ParentSnapshot 或 hash。
4. 框架跨 major/minor 时核对根模板兼容声明，再通过正式父级同步更新派生模板。只改变兼容声明且内容不变时不强制升模板内容版本。同 major/minor 的补丁可保留兼容声明的旧 patch。实际内容、父快照、父版本指针和封存 hash 必须一致。
5. 更新 `CHANGELOG.md`、当版发布说明及当前版本指针；历史发布文件保持不可变。第三方依赖未改变时保留原声明。技能源有变化时按维护文档先提交源再生成 bundle，不能伪造 sourceCommit。

## 生成发布声明

在仓库根运行本技能的 `scripts/release_metadata.py`。默认只输出预览；`--write` 才新建目标文件。它不升版、不保存模板、不运行 Unity、不提交或推送。

```powershell
python .agents/skills/ember-release-framework/scripts/release_metadata.py --repo . --baseline <上一发布版本> --version <目标版本> --coverage-file <本次范围.txt> --validation-file <本次验证.txt> --status <真实发布状态>
```

审阅后加 `--write`。该工具要求包版本已经更新、模板 metadata 已封存且谱系一致；从基线复制不变的依赖，并从当前模板与 bundle 取值。对已有不同内容文件拒绝覆盖。若依赖、技能目录、发布 schema 或 Unity 基线改变，需要按真实状态调整生成声明并复核；不能无条件继承旧值。

## 验证与发布

1. 检查准确的变更清单和发布说明。使用 Unity MCP 验证正确项目的编译、相关测试及模板实际内容/父快照。遵守 CLAUDE 的有界查询及失败停止规则；不可用时如实记录限制并给出规定的手动编译提醒，不绕道编译。存在已知编译错误、未封存模板或未完成的必要谱系同步时停止发布。
2. 对本次拟发布文件逐个暂存，审阅 `git diff --cached`。无关暂存内容不能混入提交，也不能被清空；采用限定路径的提交或隔离 index。运行仓库要求的共享二进制、字体、技能 bundle 检查，命令见参考文档。仅文档/技能变更不必启动 Unity。
3. 提交后核对实际提交内容，创建 `v<版本>` annotated tag。已有 tag 先比较 peeled commit；相同则续做未完成步骤，不同则停止，禁止移动或强推旧 tag。
4. 推送实际目标分支及这一个 tag，优先 `git push --atomic <remote> HEAD:refs/heads/<branch> refs/tags/v<版本>`。不用 `--force` 或 `--tags`。推送失败先查远端，保留现有提交/tag，再决定是否重试，不重新升版。
5. 用 `git ls-remote` 确认目标分支和 tag 的 peeled commit 均为发布提交。UPM tag 发布不依赖 GitHub Release 页面或 gh 登录；用户额外要求页面时另行完成。
6. 最终报告版本、提交、发布说明、实际验证结果和未验证事项。框架升级与消费项目模板迁移是两步；未经请求不要替用户升级消费项目。
