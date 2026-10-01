---
name: ember-vn-generate-save
description: 为 Ember 视觉小说生成指定节点的定位测试存档。按节点名称、台词或小游戏名称定位目标，普通节点直接生成，含自定义步骤等特殊节点回退到前置普通节点；使用默认变量并按用户要求调整，可写入空闲手动槽或导出实机存档。适用于“我要测试某个节点”“生成到小游戏前的存档”，不负责实现小游戏或修改剧情。
---

# 指定节点测试存档

复用项目内 `Game.Narrative.Editor.NovelTestSaveAutomation` 生成正式格式的定位档。
普通停留点是含台词且没有自定义步骤的对白节点，或选择节点。特殊节点只能回退到前置普通节点；
即使同一对白节点在自定义步骤之前还有台词，也不能直接把该节点选为存档点。

## 入口检查

读取目标项目的仓库规则，通过 Unity MCP 的实例列表核对项目路径并选中正确实例。
调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-vn-generate-save")`。
返回非空原因时停止写入，说明具体原因及项目中心的模板技能修复入口。
核对项目存在 `NovelTestSaveAutomation`，并处于编辑模式；不要为了生成存档擅自停止用户正在运行的游戏。
没有 Unity MCP 时可以定位源码和解释候选，但不能声称已经生成，不手工拼接存档 JSON 或校验摘要。

本技能需要 visual-novel 0.19.0 的业务工具。只有新版技能源、没有对应业务 API 时，说明需要先升级/迁移业务工具；
消费端框架升级走 Ember/UPM Manager，不能修改 PackageCache、manifest、模板快照或安装指纹。

## 定位与生成

1. 用 `NovelTestSaveAutomation.Search(query)` 搜索用户给出的节点名、ID、台词或小游戏脚本名。
   搜索结果带剧情资产路径、章节和节点 ID。多个匹配时结合用户上下文缩小，仍无法唯一确定时只询问候选，不能猜。
   `Truncated` 为 true 时缩小关键词。没有结果时可只读检索剧情资产与自定义脚本，找到真实 ID 后再调用工具。
2. 对确定的节点调用 `Inspect(storyAssetPath, chapterId, nodeId)`。
   使用返回的 `Candidates`，不要根据屏幕位置、资产文件排序或手写节点 ID 推断前驱。
   单一候选直接使用；多个前置普通节点、多个调用链且用户未指定时，请用户选择一次。
   没有候选时说明需要在特殊节点前放一个普通对白/选择节点，不自动修改剧情。
3. 默认采用声明的变量；用户指定的值只覆盖对应作用域与类型，不修改剧情资产的默认值。
   说明这是定位档：不补跑前置剧情，背景/立绘/音乐为空；停留台词前的赋值与演出也不重放。
   普通目标默认第一句，回退目标默认最后一句；用户明确指定台词时传返回的 `CommandId`。
4. 按 [调用契约](references/api.md) 构造请求并调用 `Generate(requestJson)`。
   默认 `Slot=-1`、`OverwriteExisting=false`，写入当前项目的首个空闲手动槽，无需再询问生成许可。
   槽位全满时优先导出到项目 `.utmp/novel-test-saves/<唯一目录>/VisualNovelSaves`，不要自动覆盖。
   用户要传到另一台设备或指定导出位置时设置 `OutputRoot`。仅在用户明确指定覆盖槽位时使用 `OverwriteExisting=true`。
5. 以工具返回的回读验证结果为准。失败时报告实际原因；剧情变动则重新 Inspect，不能绕过语义指纹、调用栈或合法选项校验。

## 交付

报告所选目标、实际存档点（如发生回退，明确说明）、手动槽编号（API 0–5 对应界面 1–6）、调整的变量与文件位置。
本机档说明“启动游戏 → 读取存档 → 对应手动槽”。导出档同时交付 `index.json` 和 payload 所在的完整目录，不能只给 payload。
远端设备应先关闭游戏并备份原目录，再放入该游戏的 `persistentDataPath/VisualNovelSaves`；不要替用户覆盖未授权的设备文件。
实机读档后正常推进到小游戏。生成/回读校验通过不等于小游戏在实机已测试通过。
