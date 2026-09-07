# 文档维护记录（2026-09-07）

当前框架正在准备 **0.11.0** 发布。本次按当前工作区源码维护文档；包版本仍为 0.10.0，未执行改版本、模板保存、提交、tag 或发布。

## 场景同步收口补记

2026-09-07 用户确认模板分支与场景对象级语义同步已通过 Unity 编译、相关 EditMode、真实 UnityYAMLMerge 和手工 O/N/C 验收。正式说明已并入 [模板体系](template-upgrade-system.md)、[当前进度](framework-progress.md)、[回归清单](framework-test-checklist.md)、[UPM 维护](upm-migration-plan.md) 与 [CHANGELOG](../../Packages/com.ember/CHANGELOG.md)；此前删除的临时方案和 handoff 不恢复。

非冲突合并后派生封存为 `source3d-2p5d 0.2.5 / preview`、父基线 `base 0.5.4`。最终只读复核时根模板已继续前进为 `base 0.5.5 / stable`，相对派生 ParentSnapshot 仅 `Game/Scenes/FrameworkScene.unity` 不同；当前编辑副本为 source3d-2p5d 0.2.5 且 hash 一致。本文档收口未修改 package.json、模板内容或 metadata，也未执行任何 Git 操作。

## 范围与结果

初始清单包含 106 个文档/文本文件，其中 72 个 Markdown。逐份处理项目文档，核对源码、配置、程序集、模板元数据、菜单与调用示例。缓存和构建物不在清单；第三方说明、许可和历史记录保持来源语义。`.claude/skills` 的 junction 不重复计数。

相对于本轮开始时保存的工作区版本：更新 **48** 份已有 Markdown，删除 **10** 份（8 份临时材料 + 2 份空白占位），其余 **14** 份保留。新增当前框架测试清单、本记录、文档维护 Skill 和配套检查脚本。

本轮修改前的 Markdown 与初始 git status 保存在本地忽略目录 `.utmp/doc-maintenance/`；这不是可随提交交付的备份，历史追溯仍使用 Git。原有 C#、场景、Prefab、模板快照和其他任务的未提交工作未由本轮修改。

## 主要纠正

- 框架代码已集中到 `Packages/com.ember`，`Assets/Ember/Editor/SOs` 仍是有效配置位置，不能一律替换。
- Core 引用 Basic、Odin 属性和 UniTask；可选 Module 按 Enabled/Phase 装配，业务查询不通过 `.Instance` 隐式创建。
- 当前 UI 使用 EUIManager/EUIViewEngine、Page/Logic/Binding/Item；旧 Router、MonoSingleton、协程钩子和控件示例已更新。
- 资源接口中的场景加载签名已修正；Resources 默认加载仍是同步包装，不再宣称真正异步或通用零分配。
- 模板父子同步、消费端补缺和未来升级向导分开说明；普通保存、Bump、兼容声明与场景合并的边界分别保留。
- 历史测试勾选注明日期和范围；当前发布清单从未执行开始。未实施的音频、重绑定、依赖解耦和独立升级器事项保留。

## 逐文件处置

| 文件（相对仓库根） | 处置 | 说明 |
|---|---|---|
| [.agents/skills/ember-commit-review/SKILL.md](../../.agents/skills/ember-commit-review/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-generate-doc/SKILL.md](../../.agents/skills/ember-generate-doc/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-odin-capture-style/SKILL.md](../../.agents/skills/ember-odin-capture-style/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-odin-inspector/SKILL.md](../../.agents/skills/ember-odin-inspector/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-package-scan/SKILL.md](../../.agents/skills/ember-package-scan/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-plugin-migrate/SKILL.md](../../.agents/skills/ember-plugin-migrate/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| [.agents/skills/ember-region-organizer/SKILL.md](../../.agents/skills/ember-region-organizer/SKILL.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [.agents/skills/ember-solution-design/SKILL.md](../../.agents/skills/ember-solution-design/SKILL.md) | 更新 | 修正工作区路径、现状或执行说明；保留对应 Skill 的职责边界 |
| `.agents/skills/template-skill/SKILL.md` | 删除 | 只有 TODO 的空白占位；编写指南保留示例结构 |
| `.claude/agents/template-agent.md` | 删除 | 只有 TODO 的未配置 Agent，占位内容无可执行用途 |
| [.githooks/README.md](../../.githooks/README.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Assets/Art/Icons/game-icon-pack-v1.4/README.md](../../Assets/Art/Icons/game-icon-pack-v1.4/README.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Assets/Art/README.md](../../Assets/Art/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [source3d-2p5d SceneUI 示例](../../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/SceneUI/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [CLAUDE.md](../../CLAUDE.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [docs/dev/api-doc-template.md](../../docs/dev/api-doc-template.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/audio-upgrade-plan.md](../../docs/dev/audio-upgrade-plan.md) | 更新 | 保留未实施方案，更新路径并说明 API 尚未落地 |
| [docs/dev/burner-architecture.md](../../docs/dev/burner-architecture.md) | 更新 | 保留历史研究，标明来源日期及未重新核验外部实现 |
| [docs/dev/contributing.md](../../docs/dev/contributing.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/editor-tools-test-checklist.md](../../docs/dev/editor-tools-test-checklist.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/ember-api-reference.md](../../docs/dev/ember-api-reference.md) | 更新 | 更新真实路径/签名/依赖，删除不实性能保证 |
| [docs/dev/ember-boot-sequence.md](../../docs/dev/ember-boot-sequence.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/ember-debug.md](../../docs/dev/ember-debug.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/ember-transition-block.md](../../docs/dev/ember-transition-block.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| `docs/dev/ember-ui-gap-analysis.md` | 删除 | 旧缺口表已失真；当前 UI 参考与框架进度承接 |
| [docs/dev/ember-vs-burner-ui-comparison.md](../../docs/dev/ember-vs-burner-ui-comparison.md) | 更新 | 旧覆盖率和缺失列表改为当前设计取舍 |
| [docs/dev/eui-reference.md](../../docs/dev/eui-reference.md) | 更新 | 更新 Page/Logic/Binding/Item、控件 API 与生命周期 |
| [docs/dev/framework-progress.md](../../docs/dev/framework-progress.md) | 更新 | 重写当前状态和待办，明确 0.11.0 发布准备 |
| [docs/dev/guide-module-design.md](../../docs/dev/guide-module-design.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/independent-updater-package-plan.md](../../docs/dev/independent-updater-package-plan.md) | 更新 | 保留独立升级器预案，区分 0.11.0 发布目标与现有包内实现 |
| [docs/dev/mcp-troubleshooting.md](../../docs/dev/mcp-troubleshooting.md) | 更新 | 移除失效协议/端口推断，按当前项目 MCP 规则排查 |
| [docs/dev/odin-panel-inventory.md](../../docs/dev/odin-panel-inventory.md) | 更新 | 重新扫描框架和业务 Odin 引用 |
| [docs/dev/odin-usage-notes.md](../../docs/dev/odin-usage-notes.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/player-control-module.md](../../docs/dev/player-control-module.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| `docs/dev/res-migration-plan.md` | 删除 | Handle/Slot/EventGroup 等已有实现；Resource/API/进度文档承接 |
| [docs/dev/scene-ui-module-design.md](../../docs/dev/scene-ui-module-design.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [docs/dev/skill-writing-guide.md](../../docs/dev/skill-writing-guide.md) | 更新 | 改为 .agents 真目录、.utmp 产物与已授权任务执行规则 |
| [docs/dev/skills-reference.md](../../docs/dev/skills-reference.md) | 更新 | 完整列出当前九个有效 Skill 与文档维护调用方式 |
| `docs/dev/TEMP-v0.11.0-scene-object-level-template-sync-plan.md` | 删除 | 场景合并方案已实现；正式模板文档保留约束、回退与验收 |
| [docs/dev/template-upgrade-system.md](../../docs/dev/template-upgrade-system.md) | 更新 | 汇总 schema v2、父子同步、场景合并、事务与消费端边界 |
| `docs/dev/ui-logic-backup.md` | 删除 | 六个页面逻辑已恢复；当前实现与 UI 文档接替备份 |
| `docs/dev/uibinding-odin-restore-plan.md` | 删除 | 恢复工作已完成；写法保留在 Odin 规范与当前 EUI 文档 |
| [docs/dev/uiextension-learning-path.md](../../docs/dev/uiextension-learning-path.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| `docs/dev/uiextension-migration-plan.md` | 删除 | 迁移完成；源码路线、测试记录与未完成事项已分流 |
| [docs/dev/uiextension-test-plan.md](../../docs/dev/uiextension-test-plan.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md) | 更新 | 已完成迁移计划改为安装、交付与 0.11.0 发布维护指南 |
| `docs/dev/v0.11.0-template-branch-handoff.md` | 删除 | 阶段交接已合并到模板体系、进度与发布验收清单 |
| `docs/dev/v0.8.0-framework-test-plan.md` | 删除 | 历史完成计划；当前回归清单承接，历史结果保留在 CHANGELOG |
| [docs/README.md](../../docs/README.md) | 更新 | 补全分层索引和维护入口 |
| [docs/user/package-inventory.md](../../docs/user/package-inventory.md) | 更新 | 对照 manifest/lock/embedded 包补齐 MCP 与当前来源 |
| [docs/user/UI开发参考.md](../../docs/user/UI开发参考.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/CHANGELOG.md](../../Packages/com.ember/CHANGELOG.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/Core/Editor/README.md](../../Packages/com.ember/Core/Editor/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Core/Runtime/Event/README.md](../../Packages/com.ember/Core/Runtime/Event/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Core/Runtime/Manager/README.md](../../Packages/com.ember/Core/Runtime/Manager/README.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/Core/Runtime/Service/README.md](../../Packages/com.ember/Core/Runtime/Service/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Core/Runtime/State/README.md](../../Packages/com.ember/Core/Runtime/State/README.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/Core/Runtime/Update/README.md](../../Packages/com.ember/Core/Runtime/Update/README.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/Documentation~/audio/README.md](../../Packages/com.ember/Documentation~/audio/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/camera/README.md](../../Packages/com.ember/Documentation~/camera/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/core/README-Editor.md](../../Packages/com.ember/Documentation~/core/README-Editor.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/core/README.md](../../Packages/com.ember/Documentation~/core/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/input/README.md](../../Packages/com.ember/Documentation~/input/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/resource/README.md](../../Packages/com.ember/Documentation~/resource/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/scene/README.md](../../Packages/com.ember/Documentation~/scene/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/scene-ui/README.md](../../Packages/com.ember/Documentation~/scene-ui/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/Documentation~/ui/README.md](../../Packages/com.ember/Documentation~/ui/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/LICENSE.md](../../Packages/com.ember/LICENSE.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ember/README.md](../../Packages/com.ember/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/SceneUI/README.md](../../Packages/com.ember/SceneUI/README.md) | 更新 | 核对并修正源码路径、当前 API、配置归属或历史状态说明 |
| [Packages/com.ember/UniTask/LICENSE.md](../../Packages/com.ember/UniTask/LICENSE.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |
| [Packages/com.ryanindiedev.inputdevicedetector/Documentation~/How to use.md](../../Packages/com.ryanindiedev.inputdevicedetector/Documentation~/How to use.md) | 保留 | 核对后保留；现有约定有效，或属于第三方/许可/历史资料 |

## 新增入口

- [框架测试清单](framework-test-checklist.md)：0.11.0 发布准备的当前验收项。
- [文档维护 Skill](../../.agents/skills/ember-doc-maintenance/SKILL.md)：下次调用 `$ember-doc-maintenance`。
- [检查脚本](../../.agents/skills/ember-doc-maintenance/scripts/audit_docs.py)：只读枚举文档与检查 Markdown 本地链接，`--strict` 在存在失效目标时返回非零。

## 其他文本清单

以下文本已纳入盘点，保留配置/上游用途，不按普通项目说明更新版本或删除：

| 文件 | 处理 |
|---|---|
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Anton OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Bangers - OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Oswald-Bold - OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - AFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - License.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Examples & Extras/Fonts/Unity - OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Resources/LineBreaking Following Characters.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/TextMesh Pro/Resources/LineBreaking Leading Characters.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/MMTools/Core/MMReorderableList/reorderable-list-licence.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/MMTools/Demos/MMTween/Fonts/Lato/OFL.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/HapticSamples/HapticPackCClicense.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/OlderVersions/v1.7/NiceVibrations-v-1-7-readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/OlderVersions/v2.0.1/NiceVibrations-v-2-0-1-readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/OlderVersions/v3.9/NiceVibrations-v-3-9-0-readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/Scripts/Components/Resources/nv-constant-template.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/Scripts/Components/Resources/nv-emphasis-template.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/Scripts/Components/Resources/nv-pattern-template.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/NiceVibrations/readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/license.txt` | 保留：上游说明/许可/资源附带资料 |
| `Assets/ThirdParty/Feel/readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.borodar.rainbow-folders/NOTICES.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.borodar.rainbow-folders/QuickStart.pdf` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.borodar.rainbow-hierarchy/NOTICES.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.borodar.rainbow-hierarchy/QuickStart.pdf` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.demigiant.dotween/readme.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.ember/SharedAssets/Fonts/7000汉字+符号+英文字符集.txt` | 保留：配置文本 |
| `Packages/com.ember/SharedAssets/Fonts/钉钉进步体/LICENSE.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.ember/SharedAssets/Fonts/阿里妈妈东方大楷/INSTRUCTION.txt` | 保留：字体上游使用说明 |
| `Packages/com.ember/SharedAssets/Fonts/阿里妈妈东方大楷/LICENSE.txt` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.flyingworm.consolepro/Documentation~/Editor Console Pro Documentation.pdf` | 保留：上游说明/许可/资源附带资料 |
| `Packages/com.neuecc.unirx/ReadMe.txt` | 保留：上游说明/许可/资源附带资料 |
| `ProjectSettings/ProjectVersion.txt` | 保留：配置文本 |

## 验证范围

- 本地 Markdown 链接目标：初始 60 处失效；最终扫描 99 个文档/文本文件，`audit_docs.py --strict` 返回 0，失效目标为 0。
- 检查脚本隔离样例通过：有效与失效目标、中文 URL 编码、空格路径、引用定义、多长度围栏、第三方/模板/缓存排除、退出码及无 manifest 错误。
- Skill 名称、目录名、description、长度与当前 YAML 子集校验通过。官方 `quick_validate.py` 在此 Python 环境缺少 PyYAML，未能运行完成；使用独立的字段校验，不将其称为官方验证器通过。
- 已搜索删除文件引用、旧路径和无效 API；`git diff --check` 通过。安装后的 Skill 文件与已校验草稿逐一按内容比对一致。
- 原文档维护轮次未运行 Unity 编译、EditMode/PlayMode 或消费项目验收；其后模板场景同步专项已按本页补记由用户确认通过。PlayMode、消费项目与整体发布验收仍未因此完成；未验证外部链接可用性。只读脚本不验证标题锚点与 API 语义，不代表完整发布验证。

0.11.0 发布时应同步 package.json、CHANGELOG 与模板兼容声明，并完成 [发布验收](framework-test-checklist.md)；文档维护不代替该步骤。
