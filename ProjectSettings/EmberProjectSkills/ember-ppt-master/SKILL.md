---
name: ember-ppt-master
description: Use the project-installed official PPT Master workflow through the Ember Project Center entry when the user calls ember-ppt-master.
---

# Ember 项目中心的 PPT Master 入口

1. 通过 Unity MCP 确认连接的是当前任务的 Unity 项目。用以下代码同时取得项目根目录和项目中心的执行检查结果。

   ```csharp
   return new
   {
       ProjectRoot = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
       BlockReason = Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-ppt-master")
   };
   ```

   项目不匹配、Unity MCP 不可用、检查抛错或返回非空原因时停止执行，反馈具体原因，提示通过「模板与项目 AI Skill」预览并同步。

2. 用已核实的项目根目录拼出官方技能 `.agents/skills/ppt-master/SKILL.md` 和项目 Python `Tools/SkillMaintenance/.venv/Scripts/python.exe` 的绝对路径。读取官方 `SKILL.md`，将其所在绝对目录作为 `SKILL_DIR`。如技能或 Python 缺失，按项目根目录下的 `Tools/SkillMaintenance/README.md` 补齐。

3. 使用项目 Python 以 `-B` 执行 `SKILL_DIR/scripts/attribution_guard.py`，展开实际绝对路径。检查通过后，按官方 `SKILL.md` 的顺序读取 `workflows/routing.md` 并执行该请求对应的官方流程。上游文档中的 Python 命令统一使用已确认的项目解释器。

如果当前请求只验证入口，完整性检查通过后可执行官方 `scripts/svg_to_pptx.py --help`，记录退出状态。安装、上游更新和恢复使用 `Tools/SkillMaintenance/Update-PptMaster.ps1`。
