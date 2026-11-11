# Ember 0.25.2

2026-11-11。基于 v0.25.1 的 UI 规范与模板整理补丁，发布引用为 annotated `v0.25.2`，目标为既有 origin/main。实际发行提交以远端 tag 的 peeled commit 为准。

## 交付变化

- [统一 UI 规范](../../Packages/com.ember/Documentation~/ui/naming-and-paths.md)集中命名、代码与资源目录、绑定节点、图片分类和迁移检查；文档建议与工具当前能力分别说明。
- 新增通用技能 [ember-ui-normalize](../../.agents/skills/ember-ui-normalize/SKILL.md)，面向消费项目整理自有 UI，保护 GUID、引用、动态地址及用户逻辑。公共技能由 11 项增加到 12 项；EUI 制作技能引用同一规范。
- 正式整理七个模板。固定大图归 StaticTexture，小说按数据选择的背景/立绘及皮肤素材归 DynamicTexture，Ownership 示例补齐 Module 目录并正式重新生成；同步制作默认路径、配表、导入技能和说明。
- 原有 GUID 全部保留，移动 PNG 内容不变。既有页面名称与皮肤旧键保持兼容，不缩图、不重制美术、不删除产品内容。细节及证据见[调整记录](ui-template-alignment-20261111.md)。

## 模板与依赖

| 模板 | 封存版本 | 父级基线 |
| --- | --- | --- |
| base | 0.10.2 | 根模板 |
| lane-defense-2d | 0.1.2 | base 0.10.2 |
| platformer-2d | 0.13.2 | base 0.10.2 |
| source3d-2p5d | 0.6.2 | base 0.10.2 |
| topdown-2d | 0.1.2 | base 0.10.2 |
| ui-game | 0.2.2 | base 0.10.2 |
| visual-novel | 0.22.3 | base 0.10.2 |

同 major/minor 补丁保留框架兼容声明 0.25.0，所有模板内容已封存，派生父快照与指针一致。57 项完整依赖声明及可选包目标沿用 0.25.1，只推进 com.ember tag。

发布声明：[release-0.25.2.json](../../Packages/com.ember/Dependencies~/release-0.25.2.json)、[manifest-0.25.2.json](../../Packages/com.ember/Dependencies~/manifest-0.25.2.json)。技能源先选择性提交，再通过正式构建器生成 bundle；sourceCommit、文件 hash 与 12 项技能清单以随包 bundle.json 为准。

## 验证与边界

- 本任务 Unity EditMode 增量 6/6 通过（job f4aa519763064f24a9eb427598ce8368），覆盖页面命名、显式 Module 路由、两项 Item 所有权及两种分辨率的小说图片切换/清理。
- 图片导入技能 17/17 Python 回归通过；三张小说表正式 BakeCurrent 成功、诊断为空。base 70 个 Prefab/25 个纹理、小说 39 个纹理、恢复后的 topdown 71 个 Prefab/25 个纹理回读通过。
- 通过 MCP 主动编译、确认本轮编译/域重载并读取 Console；七份模板磁盘 hash、封存 hash、父快照/指针和图校验通过。原 topdown 编辑现场与用户本地字体改动保留。
- 发行阶段再次主动请求 Unity 编译，finished=1794384300199、domain_reload_after=1794384399135；PackageInfo 和框架 API 均为 0.25.2，编辑器非编译/非更新，Console Error=0，活动 topdown-2d 0.1.2 保持。
- 干净受管检出由源码提交 `31618f43ee8744d46ea9be9c45e881ba9e2c79be` 创建；正式 bundle 构建/检查通过：12 技能、34 文件。生成内容按 manifest 逐文件验证后转入发行候选，保留主工作区尚未提交的其他技能改动。
- 同一干净检出的共享二进制及 core.autocrlf=false/true 干净检出检查、字体配置检查通过，技能文件头检查 55 项、0 错误。最终发布声明已人工核对新增技能清单/能力，manifest 仅 com.ember 从 v0.25.1 变为 v0.25.2，其他 56 项依赖逐项相同。
- 限定最终 index 为 13 个发行文件，空白检查通过；相对 v0.25.1 的 19 份非模板 Markdown 共 198 个本地文件链接无缺失。34 份 bundle 原始 Git blob 的 SHA256 均匹配 manifest，已提交源与 bundle 的 UTF-8 文本换行归一后逐项相同。
- 工作区的 AI 工作流改造、规则同步源码、共享字体缓存、活动 Assets/ProjectSettings 及历史临时成果不进入本次提交。未改变的生产实现复用本任务相应验证，不以无关工作区源码充当候选提交内容。

未执行全量玩法回归、Player 构建、图集打包、独立消费项目部署或实机验收；本轮为实施及发行自检，不是独立验收。未替用户升级消费项目。

## 消费项目接入

1. 备份并通过 Ember/UPM Manager 升级框架至 v0.25.2，确认 UPM 解析及编译。已有工程不手改 manifest/lock 升级。
2. 通过 AI Skill 安装/更新取得 ember-ui-normalize。该技能最低框架版本为 0.25.2；现有其他技能、本地修改和模板专属技能仍按各自预览/备份流程处理。
3. 包升级不会移动已部署 Assets。仅需整理自有 UI 时调用新技能；需要模板内容更新时使用正式三方预览，逐项核对字符串资源地址、配表源及自有改动。不要为目录整理完整重部署模板。
4. 本次 Resources 路径有调整，消费端保存的字符串地址、工程外源表、制作源和自定义工具须按实际用途同步。GUID 保留不替代地址兼容验证。回退前保留原模板基线、资源路径和业务修改备份。
