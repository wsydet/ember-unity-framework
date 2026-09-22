# 框架与模板文档归属

框架公共文档常驻；模板业务文档随模板加载、保存和部署。归属按实现的所有者判断，不按文档中是否出现“模板”二字判断。

跨模板共用的 [业务代码与资源目录规范](../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)
常驻包内公共文档。模板文档只补充自身模块、资源及实际迁移状态，不另立一套通用目录规则。

## 存放约定

| 内容 | 编辑位置 | 加载其他模板时 |
|---|---|---|
| 框架 API、架构、工具、发布和维护记录 | 仓库 `docs/` 或 Ember 包内 `Documentation~/` | 保留 |
| 基础业务骨架、基础模板可选模块说明 | `Assets/Game/Documentation/base/` | 随模板替换；派生模板继承 |
| 2.5D 玩家操作、场景气泡业务说明 | `Assets/Game/Documentation/source3d-2p5d/` | 仅加载该模板及继承它的模板时出现 |
| 以后新增模板的设计、实施与验收文档 | `Assets/Game/Documentation/<template-id>/` | 随所属模板替换 |
| 目录用途、生成产物约束、简短导航 | 对应业务目录的 `README.md` | 随所在业务目录替换 |
| 模板自有与继承的 AI Skill 源清单/正文 | `Assets/Game/Documentation/TemplateSkills/` | 随模板快照替换；发现副本由安装器与模板事务同步 |

`Assets/Game/Documentation/README.md` 是当前模板的文档入口。基础模板仅列基础文档；派生模板保留基础入口并追加自己的专属入口。完整说明只维护一份，模块目录 README 可以链接到该说明。

模板系统管理 `Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、`Assets/Settings`、`Assets/GameResource` 五个目录。文档进入 `Assets/Game/Documentation` 即可使用现有机制，无需增加运行时模块或文档加载器。

模板 Skill 同样复用这五个目录的保存、封存和继承机制，项目根目录 `.agents/skills` 只存发现副本。框架级 Skill 随包生成并首次安装，模板自有/继承 Skill 则按正式身份启用；详见 [框架与模板 AI Skill](../../Packages/com.ember/Documentation~/maintenance/template-ai-skills.md)。这是本地未发布能力，验证状态以该文档为准。

## 本次整理范围

| 原文档 | 所属模板与目标 | 处理原则 |
|---|---|---|
| `docs/dev/guide-module-design.md` | `base/guide-module-design.md` | 保留设计、扩展待办和历史验证边界 |
| `docs/user/引导编辑器使用.md` | `base/guide-editor-usage.md` | 与 Guide 设计同目录互链 |
| `docs/dev/player-control-module.md` | `source3d-2p5d/player-control-module.md` | 保留完整输入、边界和手动验收说明 |
| `Assets/Game/Module/SceneUI/README.md` | `source3d-2p5d/scene-ui-business.md` | 原位置保留导航，完整业务说明集中存放 |

以上目标均相对于 `Assets/Game/Documentation/`。原仓库文档位置在模板封存后只保留简短迁移入口，使旧链接仍能使用；不再维护第二份正文。

配置表目录 README 已随基础模板交付，且分别说明 Row、Definition、源数据及二进制目录，保留就近说明，由基础文档索引统一导航。
`scene-ui-module-design.md` 描述通用引擎和模块基类，仍属于框架；只有具体 World Channel、Example 气泡、场景物体等接线说明归 2.5D。
`guide-editor-base-handoff.md` 是开发仓库历史恢复记录，包含本机备份定位和历史操作约束，保留在仓库，不作为模板使用手册下发。
发布说明、CHANGELOG 和既有验证记录保留历史版本语义。

## 编辑、同步和交付

1. 在 `Ember/项目中心` 确认并加载目标模板。加载会替换五个受管目录，先保存或备份当前未保存内容。
2. 只在项目 Assets 中编辑文档，让 Unity 生成并维护 `.meta`。
3. 按模板保存规则预览差异，确认本次修改范围，保存后显式 Bump 封存版本。
4. 基础文档更新后，预览并同步派生模板；派生的专属目录保持不变。父子同时修改入口 README 时按实际内容合并，不丢弃任何一方的入口。
5. 在基础模板和派生模板间切换，核对文档出现、移除、恢复以及相对链接。
6. 模板随 `com.ember` 发布，交付消费项目时仍需新的框架发布 tag。仅本地保存和 Bump 不等于已经发布。

禁止直接编辑 `Templates~/*/Assets`、`ParentSnapshot~` 或手改 hash。仓库索引可以链接已封存快照供跨模板查阅，快照不是日常编辑位置。

模板内部优先使用相对链接指向同模板或继承的文档，不依赖消费项目不存在的仓库 `docs/`。跨到通用 Package 的说明可给出包内路径；不要把开发仓库特有的相对目录当作所有消费项目都存在。

新模板应先派生并加载，再建立自己的专属目录。单纯方案草稿可以先放仓库文档区；正式进入模板时迁移正文并处理旧链接。

实现依据：[模板体系](template-upgrade-system.md)、[项目中心源码](../../Packages/com.ember/Core/Editor/EmberProjectSetup.cs)、[模板事务](../../Packages/com.ember/Core/Editor/EmberTemplateTransaction.cs)。

## 2026-09-17 整理与验证记录

- 在 embedded 框架项目中通过 Unity MCP 调用项目中心同一套模板 API：base 保存并封存为 `0.6.4`；派生先同步至 `0.3.6`，再保存专属文档并封存为 `0.3.7`，父基线 `0.6.4`。框架包仍为 `0.13.2`，本批尚未发布。
- 已实际验证：加载 2.5D 后基础与专属文档同时存在，加载 base 后专属目录移除。最终停留 base，恢复原先打开的 FrameworkScene 与 MainScene，活动场景为 FrameworkScene。
- 模板 API 最终检查：base 工作副本差异为 0，模板谱系无问题，派生同步状态为 Synced。文件检查确认派生 ParentSnapshot 与 base 逐字节一致，模板变更仅为 Markdown、对应目录与 `.meta`，原有 252 个业务文件逐字节未变。
- 文档审计 `--mode framework --strict` 无坏链接；额外检查两份模板文档内部相对链接均有效。旧仓库正文已改成迁移入口，仓库索引和现有入链指向封存正文。
- 切换时曾被 Unity 二次确认弹窗阻塞，用户确认后恢复。额外连续切换过程中发生目录事务回滚未完整恢复，保留备份后恢复项目 Game 目录；核对 2.5D 工作副本零差异，再单独加载 base 成功。本轮没有修改模板事务实现，不能据此宣称已修复连续切换问题。
- MCP 最终可用，Editor 非编译状态、`EditorUtility.scriptCompilationFailed = false`，控制台 `error CS` 查询为 0。期间出现过一次 EUIBinding 缺失类型日志；本轮不将文档切换检查解释为运行功能全面通过，未执行 Play Mode 或全量测试。
- 整理前文件、两个模板备份、目录恢复副本及校验结果保存在本机 `.utmp/ember-doc-maintenance/`，不随模板发布。
