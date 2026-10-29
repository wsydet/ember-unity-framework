# 可组合扩展本地交付记录（2026-10-29，未发布）

> 2026-10-29 发行续接：用户已授权发布当前框架及全部已保存模板。最终版本、模板封存 hash、验证与限制以 [Ember 0.25.0 发行记录](release-0.25.0.md) 为准；下方未发布/未 Bump 表述保留实施当时的历史语义。

消费项目正式入口：[包内迁移说明](../../Packages/com.ember/Documentation~/manual/composable-extensions.md)，含依赖/默认值、兼容范围、API 对应、Windward 保留/适配/替代/删除清单、序列化/GUID 策略、升级路径与回退。示例源码随包位于 `Documentation~/samples/WindwardRoomGraphAdapter.cs.txt`，不依赖本仓库绝对路径。

## 本轮实现归属

- Core.Editor：`EmberExtensionCatalog`（纯依赖/循环/兼容求解），`EmberExtensionProject`（正式配置及门禁），`EmberExtensionPanel`（项目中心组合草稿与关闭预览）。基础设施不做可选裁剪；没有新增 Manager/Module。
- 既有 FSM 蓝图：通用工具由正式组合启用，参数/观察源继续校验 owner/module；兼容旧 Register，但不让它绕过开关。保留布局候选、真实图、参数选择及域重载草稿。
- LevelAuthoring.Editor：统一入口、资产/地形/对象写入、适配器获取和试玩 Start/Retry 接入门禁；内部清理通道仍可 Stop/恢复；规则草稿和制作字段保留。
- 房间图：`EmberRoomGraphContracts/Rules/Service/Board/Window` 投影原资产；任意端口数量、可选十二槽预设、固定1:1画布、原比例预览、连接/排版校验、外部修改冲突、Undo/失败回滚。实际世界坐标、产品初态与加载策略由项目拥有。
- 当前 topdown 活动 Assets：新增 `Game/Config/EmberExtensionDefaults.json`；迁移 TopdownLevelAdapter、TopdownObjectAdapter、PlayerEditorAccess、PlayerDevelopmentWindow 的条件及 README。2026-10-29 后续已按用户授权通过项目中心普通保存，未 Bump。

## 验证证据

[验证 JSON](extensions-evidence/validation.json) 记录最后两批 26/26 与 13/13、已加载程序集 MVID 和编译/域重载事件。前期 46 用例范围最初 44 通过、两项错误提示 GUI 失败；提示移到可见区域后两项复验通过。Windows 原子替换短暂占用与草稿测试使用未保存引用的问题已定位并修正，最终无待修复失败。

[域重载字段比较](extensions-evidence/domain-preservation.json) 全部为 true，保留的前后 JSON 是临时测试图的窗口状态。原生窗口坐标因 Unity 浮动窗口布局发生变化，未把它计入业务草稿比较。

[保护清单摘要](extensions-evidence/protection-summary.json) 记录 10,960 个文件的开始基线指纹及最终差异：模板快照 10,322 文件零变化，既有活动 Assets 仅四个接线脚本及 topdown README 被本轮修改。已有 UI、资源、场景、GUID、其他模板草稿保留。大量已有工作区差异未重置；[原有部分包改动备份](extensions-evidence/pre-existing.patch) 仅为本轮修改前的辅助记录，不是提交清单。

## 已留出的交付边界

### 2026-10-29 platformer 与全链路隔离补充

用户进一步要求扩展遵循模板一样的版本启用隔离，并覆盖声明、工具、Skill 与专属文档。已移除按模板名称自动启用的兜底，增加 schema v2 精确模板版本、扩展契约版本/源码 SHA256（文本统一换行）、传递依赖绑定、窗口版本指纹、Skill 扩展/模块范围筛选与执行检查、失效发现副本收回、文档受控索引/API及文档 SHA256。配置和发现目录复用原事务，变更副本备份在 `.utmp`，源、资产及草稿保留。公共 glossary 已加入扩展/拓展和框架 Wiki 定义与导航。

platformer 由用户确认加载后只在活动 Assets 修改；普通保存 1955 文件，保存前18项预期接线差异，保存后错误/差异0。版本0.13.0，实时 hash `1cf67da87731c1b88d685a8d4e8a7e08`，封存 hash保持 `ff217ca17005f39519049c46a83c138b`，未 Bump。[保存回执](extensions-evidence/platformer-save-20261029.json)。原存储快照2647文件已备份并核对逐文件 hash，其他草稿未覆盖。

增量测试：8/8；相关扩大范围19/20，唯一失败为旧测试在非OnGUI环境调用真实模板绘制器，修复后相关5/5通过；全链路实测发现清单单项依赖误写字符串，修正为数组并增加拒绝检查后最后4/4通过。实际关闭/域重载后工具菜单、Skill发现/执行和专属文档均阻断，窗口草稿/身份8个字段逐项一致，[域重载证据](extensions-evidence/platformer-full-chain-domain.json)。临时窗口已关闭，原组合已恢复。最终 Console Error=0，两条既有FSM测试字段序列化警告保留。

base 已由用户确认正式加载，仅新增空组合声明及其目录/.meta，普通保存701文件，保存前3项差异、保存后错误/差异0。版本0.10.0，实时hash `e9d789311cad169a19b6a76331a2877a`，封存hash仍为 `61cbe49aa797b1604f293b9756674566`。8/8相关增量测试通过，编译/域重载与Console检查完成；真实菜单、公开API、Skill与专属文档全部关闭。[base回执与验证](extensions-evidence/base-save-20261029.json)。原699文件快照已另存Library备份，不修改父快照或其他模板。

ui-game 已由用户确认加载，仅新增空组合声明及目录/.meta，普通保存749文件，前3项差异、后错误/差异0。版本0.2.0，实时hash `43030af8571b29f5c1d2d0631faa1733`，封存hash保持 `cc6430af3e8cdb46ee0e034ac1a18fb0`。2/2相关增量测试、实际菜单/API/Skill/文档关闭检查通过，编译/域重载完成，Console Error=0；两条MCP WebSocket断连警告后连接已恢复。[ui-game回执](extensions-evidence/ui-game-save-20261029.json)。原1445文件快照已备份。

source3d-2p5d 已由用户确认加载，仅新增空组合声明及目录/.meta，普通保存766文件，前3项差异、后错误/差异0。版本0.6.0，实时hash `9be0d1035d4396e096a3eaa820765eed`，封存hash保持 `f98b85bf591042619e55fafd5cd88af4`。2/2相关增量测试和实际菜单/API/Skill/专属文档关闭检查通过，编译/域重载完成，最终Console Error=0。[回执](extensions-evidence/source3d-2p5d-save-20261029.json)。原1462文件快照已备份，父快照和其他模板保持。

visual-novel 已由用户确认加载，只新增独立空组合声明及目录/.meta，版本0.22.1，实时hash `437a3552583df64cf2ab3d1c26083864`，封存hash保持 `61155f5eb3958eacdbd0a9467ebf60c3`。原2320文件快照已备份。保存原调用返回空结果，未重复执行；通过正式身份、实时metadata和项目中心比较确认已完成、错误/差异0。保存后快照Assets共1624文件。2/2相关增量测试与实际扩展菜单/API/Skill/文档关闭检查通过；9个小说自有Skill保持有效且按新hash同步，编译/域重载完成，Console Error=0。[回执与确认](extensions-evidence/visual-novel-save-20261029.json)。

topdown 已在用户确认后重新加载，补迁schema v2、受控文档索引和窗口版本/hash；直接BeginTrial与域重载接线单独检查试玩扩展，正式试玩不依赖蓝图。最终普通保存1061文件，保存前13项预期差异，后错误/差异0；版本仍为0.1.0，实时hash `b3bf3acb890f00980fd6633f8765a93a`，封存hash保持 `61cbe49aa797b1604f293b9756674566`。[最终回执](extensions-evidence/topdown-schema2-save-20261029.json)。12项增量测试11通过，1项仅为未显示临时窗口的清理错误；修复后窗口用例2/2通过，最终编译/域重载完成、Console Error/Warning=0。

六个模板的独立声明和接线均已普通保存，实际正式加载确认全部由用户处理。六份声明的精确模板版本和当前扩展版本/hash全部匹配，仍未Bump、提交、发布或升级Windward。活动工程留在topdown-2d。

最终保护：[1053个原活动文件](extensions-evidence/topdown-active-protection-final.json)无缺失，只有五个预期现有文件发生本轮后续变化；[3490个父快照文件](extensions-evidence/parent-snapshots-final-20261029.json)逐项hash一致且无新增文件。其余原文件变化局限于通过项目中心已授权普通保存的Assets/metadata，[快照核对](extensions-evidence/template-protection-final-20261029.json)记录最终topdown保存前的检查点。

模板保护复核：对照开始时10,322个模板快照文件，既有内容的18项差异全部限于已授权普通保存的topdown/platformer的Assets及metadata；其他模板与所有ParentSnapshot零非预期变化。[保护证据](extensions-evidence/template-protection-20261029-platformer.json)。三个本轮文档入口的本地链接检查无断链。

1. 首轮topdown普通保存的历史记录：1053文件、8项差异，保存后错误/差异0，当时实时hash `549c696fccdd7614fd68ff6e9f787933`，[首轮回执](extensions-evidence/topdown-save-20261029.json)。最终schema v2接线以本文上方最终回执的 `b3bf3acb890f00980fd6633f8765a93a` 为准；版本仍0.1.0、封存hash保持 `61cbe49aa797b1604f293b9756674566`。首轮保存曾因备份读取期间目录占用失败，原快照未变；备份完成后正式重试成功。备份在项目Library/EmberExtensionWiring/20261029-followup，不通过复制备份替代模板保存。
   Bump、提交、发布和消费升级仍未执行，且当前用户明确要求暂不执行；当前包版本号不是本功能的新发布凭证。
2. 六个当前模板的接线与空组合已完成；今后新增模板仍应正式加载后在Assets声明并普通保存，禁止直接修快照。父级同步未执行，现有父基线保持原状。
3. Windward 只读参考。消费升级可只升级框架并增加项目适配器/门禁，无需重建房间或重新部署模板；其样例尚未在 Windward 编译，升级时重新核对实际源码。
4. 六模板正常切换已实际执行；人工端口拖线/删除/排版体验、产品截图、真实通行、真实手柄以及消费项目试玩停止/重试/现场恢复仍需签收；Player构建和全量测试不包含在本轮验证结论中。
5. 正式发布时补入真实框架版本/tag/commit，以及实际已保存/Bump模板的版本和 hash；没有执行时保持未发布标记。
