# PPT Master 项目级安装与维护

技能位于项目根目录的 [`.agents/skills/ppt-master`](../../.agents/skills/ppt-master/SKILL.md)。来源记录保留官方仓库、上游目录、版本、标签、确切提交、安装时间和整树 SHA-256。这份技能独立于 Ember UPM 包与模板，可供开发仓库和消费项目的本地 AI 工作流使用。 [来源记录](ppt-master.source.json) · [上游仓库](https://github.com/hugohe3/ppt-master)

## 在 Codex 中使用

用 Unity 项目根目录开启一个新的 Codex 对话，输入 `$ppt-master` 并描述要做的演示文稿。例如「用 $ppt-master 把这份 Markdown 做成可编辑的 PPTX，使用本项目的 Tools/SkillMaintenance/.venv/Scripts/python.exe 执行脚本，图片和旁白都用本地素材」。更新或恢复后重新开启对话，使客户端读取当前技能。

也可以从项目根目录直接调用脚本。加 `-B` 可避免在安装目录生成 Python 字节码文件。上游支持 Python 3.10–3.14，用其中一个版本为项目创建独立虚拟环境。 [上游安装要求](https://github.com/hugohe3/ppt-master/blob/v6.9.0/README.md)

```powershell
python -B -m venv .\Tools\SkillMaintenance\.venv
$pptPython = '.\Tools\SkillMaintenance\.venv\Scripts\python.exe'
& $pptPython -B -m pip --cache-dir .\Tools\SkillMaintenance\.cache\pip install -r .\.agents\skills\ppt-master\requirements.txt
& $pptPython -B .\.agents\skills\ppt-master\scripts\attribution_guard.py
& $pptPython -B .\.agents\skills\ppt-master\scripts\svg_to_pptx.py --help
```

## 检查、更新、恢复

从 Unity 项目根目录执行以下命令。维护脚本需要 Git 和 Python，支持 Windows PowerShell 5.1 与 PowerShell 7。省略 `-Ref` 时，检查和更新选择官方仓库最高的稳定版本标签。指定标签会先解析到完整提交 SHA，也可直接使用完整 SHA。

```powershell
.\Tools\SkillMaintenance\Update-PptMaster.ps1 -Check
.\Tools\SkillMaintenance\Update-PptMaster.ps1 -Update -Ref v6.9.0
.\Tools\SkillMaintenance\Update-PptMaster.ps1 -Update -Ref ce92fb43e4ade9c701f4c869e286f13fce743c72
Get-ChildItem .\Tools\SkillMaintenance\.backups\ppt-master -Directory
.\Tools\SkillMaintenance\Update-PptMaster.ps1 -RestoreBackup '<上一步列出的目录名>'
```

`-Check` 只读。更新先核对本地文件，再从官方仓库按提交做临时稀疏检出，逐文件核对 Git blob 后才替换技能目录。发现增删、内容或换行变化时停止，可以用 `git status --short --untracked-files=all -- .agents/skills/ppt-master` 检查工作树，再手动保存需保留的编辑。备份保存在 `.backups/ppt-master/` 下，每份包含技能目录及其来源记录。恢复也会校验备份指纹，并为当前干净版本再建一份备份。替换失败时自动恢复原目录。若维护进程被强制结束，先确认它已退出，再检查备份与当前记录。进程遗留的 `.cache/ppt-master.lock` 可在确认无维护操作后移除。

更新后对照新版 `requirements.txt`，在本项目虚拟环境重新运行安装依赖的命令。虚拟环境、缓存和备份放在本维护目录的忽略路径内。把环境配置和密钥保留在本地忽略路径中，技能目录按官方文件原样管理。

## 消费项目安装

把本目录的 `Update-PptMaster.ps1`、`ppt_master_maintenance.py`、`ppt-master.source.json`、`README.md`、`.gitignore` 复制到消费项目根的 `Tools/SkillMaintenance/`。在**消费项目根目录**运行以下命令，默认按复制来的来源记录安装同一确切提交。如果已有同名目录，脚本会停止并保留它。

```powershell
.\Tools\SkillMaintenance\Update-PptMaster.ps1 -Install
```

然后按本页步骤创建消费项目自己的虚拟环境，打开该项目的新 Codex 对话调用 `$ppt-master`。如果要把技能提交到消费项目仓库，将本仓库 `.gitattributes` 中的 PPT Master 专用规则合并进去，保持 Git 检出后的字节指纹。密钥和本地缓存的忽略规则可按本仓库 `.gitignore` 中的对应条目合并。

### 在 Ember 项目中心提供入口

此项目已登记 **`$ember-ppt-master`**。维护源位于 `ProjectSettings/EmberProjectSkills/`，入口版本为 `1.0.0`，适用当前已验证的 `topdown-2d 0.1.4`。项目中心生成 `.agents/skills/ember-ppt-master/` 发现副本；源文件改动后通过项目中心重新同步。

当前安装器每个受管 Skill 最多接受 512 个文件、16 MiB。PPT Master 6.9.0 的官方目录有 13,090 个文件。项目中心同步的是小型入口，其执行检查通过后调用 `.agents/skills/ppt-master/` 和项目虚拟环境。官方文件继续使用本页的安装、更新和恢复命令。 [入口维护源](../../ProjectSettings/EmberProjectSkills/ember-ppt-master/SKILL.md) · [本项目清单](../../ProjectSettings/EmberProjectSkills/catalog.json) · [项目自有 Skill 规则](../../Packages/com.ember/Documentation~/manual/project-ai-skills.md)

#### 在消费项目登记入口

1. 确认消费项目的 Ember 版本为 `0.25.1` 或更新版本，并已有正式活动部署模板。按上文安装官方技能和项目 Python 环境。
2. 把 `ProjectSettings/EmberProjectSkills/ember-ppt-master/` 复制到消费项目同一路径。在该项目的 `ProjectSettings/EmberProjectSkills/catalog.json` 中登记 `schemaVersion: 3` 的项目自有条目。有现成清单时，把该条目合入 `skills` 数组。以本项目清单为样例，将 `projectId` 改为消费项目的稳定 ID，将 `templateId` 和模板兼容区间改为消费项目实际采用的模板及已确认版本。只验过一个版本时，上下限填同一版本。
3. 打开 **Ember → 项目中心 → 模板 AI Skill**，点击「预览当前模板技能更新（保留业务内容）」，确认出现「项目自有 · ember-ppt-master」，再点击「更新技能」。版本控制收录 `ProjectSettings/EmberProjectSkills/` 维护源，并可把生成的 `/.agents/skills/ember-ppt-master/` 加入忽略规则。
4. 以消费项目根目录打开新的 Codex 对话，输入 `$ember-ppt-master`。入口会用 Unity MCP 核对当前项目与已同步的身份，再调用本项目的官方 PPT Master。安装官方技能或更新其版本仍使用本页 PowerShell 命令。

#### 本项目验证 · 2026-11-18

- Unity 6000.5.4f1、embedded Ember 0.25.5、活动编辑模板 `topdown-2d 0.1.4`。通过项目中心窗口使用的同一预览和同步接口操作，预览只提出安装 `ember-ppt-master`。同步后复查为「已一致」，发现副本和维护源的 SHA-256 相同，安装记录为 schema v4，所有者为 `project`。
- 同步前执行检查返回缺少所有权记录，同步后返回空值。按同步后的入口实际调用官方完整性检查和导出脚本，官方 `v6.9.0` 目录的 13,090 个文件仍匹配来源指纹。
- 用项目 Python 导出并读回本地样例，SVG 质量检查及 PPTX 导出后检查均通过。样例含 1 页、3 个可编辑形状，无媒体或外部关系。当前项目的产物位于 `Tools/SkillMaintenance/.cache/smoke/ember-ppt-master-entry.pptx`。
