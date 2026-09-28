# 发布操作细节

以下路径相对仓库根；API 调用前按当前源码确认签名，Unity 自动化需读取 unity-mcp-orchestrator 技能。

## 发布资料

- 正式流程：`docs/dev/upm-migration-plan.md`
- 模板路由与版本规则：`Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md`
- API：`docs/dev/ember-api-reference.md`
- 技能源与分发：`Packages/com.ember/Documentation~/maintenance/ai-skills.md`
- 包：`Packages/com.ember/package.json`、`Packages/com.ember/CHANGELOG.md`
- 发布声明：`Packages/com.ember/Dependencies~/release-<版本>.json` 与 `manifest-<版本>.json`
- 发布说明：`docs/dev/release-<版本>.md`
- 当前指针：检查包 README、Dependencies README、`docs/dev/framework-progress.md` 等实际引用，勿全仓替换历史版本。

## Unity 模板事务

先从 MCP instances 和 project info 核对 projectRoot，必要时切 active instance。编辑模板前先查询当前编辑记录，保护项目 Assets。

`Ember.Core.Editor.EmberProjectSetup` 提供：

- `GetTemplates()`、`GetEditingTemplate()`：确定当前模板、description 和封存状态。
- `SaveTemplate(id, displayName, description)`：保留原完整描述，通过正式事务保存。
- `BumpTemplateVersion(id, field)`：0/1/2 分别为 major/minor/patch。只有内容需要发布且尚未封存时调用。
- `GetFrameworkVersion()`、`DeclareFrameworkVersion(id)`：声明只能从根/独立模板开始。若 package.json 已更新但 PackageInfo 缓存仍旧，可请求一次 `UnityEditor.PackageManager.Client.Resolve()`，再有界确认，不能改 hash 绕过。
- `ComputeParentSyncPlan(id)`：检查 `CanApply`、`HasConflicts`、`WillChangeChildContent`、实际内容 hash 和父快照。
- `ApplyParentSync(plan, choices, versionBumpField)`：正式应用；仅兼容声明变化且内容无变更可传空选择与 null bump。有冲突按差异处理，不默认 AcceptParent 覆盖子模板。

模板 metadata 自洽不等于磁盘内容真实。发布前通过正式模板校验确认实际内容 hash、父快照、父指针及编辑记录。跨版本同步失败不能用手写 JSON 修复。

## 验证候选提交

```powershell
python scripts/check-shared-binaries.py --revision : --checkout
python scripts/check-shared-font-config.py --revision :
powershell -NoProfile -File scripts/build-ai-skill-bundle.ps1 -Check
```

提交后对 HEAD 运行仓库规定的相应检查。出现新变更或失败才扩大/重复验证。

bundle 的 sourceCommit 必须对应已经提交的源。构建器可能因 `.agents/skills` 下无关未跟踪目录而拒绝；不要删除或提交无关文件来过关。先调查，必要时使用干净候选提交环境。CRLF 导致指纹差异时，必须证明 bundle 目标原始字节符合 manifest，源提交与工作区文本仅换行不同；不能对二进制归一化或仅凭猜测忽略失败。检查限制应如实记录。

Unity MCP 不可用时按 CLAUDE 停止编译尝试；最终必须写：

> 当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 恢复中断的发布

检查 package/release 版本、当前 HEAD、工作区、local tag 与 remote branch/tag。已经创建的正确提交或 tag 直接复用；远端已一致则报告已完成。网络错误不意味着推送没成功，先 `git ls-remote`。tag 对比使用 peeled commit（`refs/tags/v<版本>^{}`），而非 annotated tag 对象 SHA。禁止 force，禁止批量推送所有本地 tag。

元数据脚本仅生成候选声明，不证明编译、真实模板 hash、bundle 字节、依赖兼容或远端发布成功。覆盖范围、验证证据、status 必须由当前工作填写，不能把历史通过结果沿用。
