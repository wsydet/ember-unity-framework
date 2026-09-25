# Ember Framework 0.14.12 发布说明

2026-09-26。本次交付 visual-novel **0.10.0**；框架 API、base/source3d-2p5d 模板与第三方依赖不变。

> 本次能力：剧情里「主角先遇到一个人、后面才知道名字」时，揭晓前的姓名框显示 `？？？`
> （或其他临时称呼），揭晓后显示真名，且**玩家已有的已读存档不会被判失效**。

## 更新内容

### 说话人称呼 Key（表现层字段）

- `NovelCommand` 新增 `_speakerNameKey`（Inspector 显示为「说话人称呼 Key（留空用角色名）」）：
  只覆盖这一句的说话人**显示名**，不改变 `_characterId`。构造参数追加在参数列表末尾，
  `WithResolvedText` 的 `MemberwiseClone` 自动继承，`JsonUtility` 往返保留；旧资产反序列化后为空。
- 显示名解析收敛为一条链，共用入口 `NovelLocalization.SpeakerName(characterId, speakerNameKey, displayName)`：

  `_speakerNameKey` 命中内容表 → `character.〈角色键〉` → `novel_characters.displayName`

  运行期当前句（`NovelSession.Render`）、历史解析（`NovelSession.Reading.ResolveSpeaker`）、
  编辑器摘要（`NarrativeContentGUI.Summary`）与布局窗口节点预览（`NovelGameplayLayoutWindow.ApplyNodePreview`）
  四处都走它，不再各写一份回退规则。覆盖 Key 留空或查不到时按原链继续，**不显示空白、不报错**。
- **不替换角色键**，因此：`NovelCompatibility.Fingerprint` 不受影响（不写进指纹）、
  「角色强调 → Auto」仍按真实角色键匹配说话人、已读定位与自动强调都不受影响。
- 校验规则与既有 `_textKey` 一致：不做额外校验，Key 查不到就回退角色名。

### 历史与存档

- `NovelHistoryEntry` 增加 `SpeakerNameKey`：`RecordStableLine` 写入、`TryCapture` 复制、
  `History` 解析优先使用。历史是读时解析，所以回看揭晓前的旧句仍是 `？？？`，
  不会因为后面说了真名而串味，也不会因为补译文而失效。
- 旧档没有这个字段，反序列化后为空、解析退回角色名回退链，**行为与改动前完全一致**；
  恢复校验仍只做结构与规模检查，`SchemaVersion` 保持 **7**，没有新增迁移分支。

### 配表与编辑器

- 内容表新增 Key 家族 `speaker.〈语义〉`；模板已提供 `speaker.unknown`
  （zh_Hans / zh_Hant / ja = `？？？`，en = `???`），源 CSV 与烘焙产物同步。
- 对白 Inspector 复用现成的多语言 Key 字段（逐语言预览，未命中显示「缺条目 → 回退：〈角色名〉」）；
  Step 摘要显示覆盖后的名字；新建指令会清空该字段，不会从被复制的上一条指令继承。

### 技能

- `ember-vn-import-story`：说话人列新增约定——源表写 `？？？`、`神秘人` 这类临时称呼时，
  映射为「**真实角色键 + 称呼 Key**」，不再映射成空角色键，也不为此新建角色；
  判断不出真实角色键时按映射歧义让用户确认。`references/localization.md` 补上 Key 表与第 4.1 节。
- `ember-vn-export-story`：明确 `说话人` 列导出**真实角色名**（称呼 Key 以「（本句显示：？？？）」附注），
  **不得**把 `？？？` 当成说话人身份回写；称呼 Key 不写进 `多语言Key` 列，定位列仍导出真实角色键。
  核对后的现有行为不变，只是把这条写清楚。
- 两个技能的最低业务模板版本提升到 `0.10.0`。

## 验证

Unity MCP 编译通过（Refresh 后 Console 无 error/warning）。

- 新增 5 项 EditMode 用例 `NovelSpeakerNameTests` **5/5 通过**：
  ① 覆盖 Key 命中显示译文（并按语言切换）② Key 未命中/留空回退角色名 ③ 只改称呼 Key 时剧情指纹逐字节不变
  ④ 历史条目带覆盖 Key、旧档缺字段仍能恢复并退回角色名 ⑤ Emphasis Auto 仍按真实角色键匹配
  （说话人亮度 1、其他角色变暗 0.3，说明匹配真的在起作用）。
  用例里另含 `NovelCommand` 的 `JsonUtility` 往返与运行期文本副本继承断言。
- 相关组回归：过滤掉需要编辑器前台焦点的历史用例后，`Game.Narrative.Tests` 的 11 个类
  **228/228 通过**（含 `NovelSessionTests` 全部分片即 NovelLocalization / NovelText / NovelReading /
  NovelCheckpoint / NovelActor / NovelCamera / NovelScreen / NovelMedia / NovelStepPresentation /
  NovelVariableLogic、`NovelCheckpointTests`、`NarrativeRunnerTests`、`NarrativeStory*`、`NarrativeGraph*`
  与 `NarrativeAvailability/TemplateIdentity`）。既有指纹基线（E0 `7D8B67D3…`、M1 `FFBCEEA6…`）未变。
- 模板封存：`SaveTemplate` + 显式次版 Bump，visual-novel **0.9.2 → 0.10.0**，
  内容与封存 hash 一致 = `eaa11d4aa58981fc45bce818f83f710b`；父模板 base 0.6.4 本轮不变，
  `ParentSnapshot~` 实测 hash 与 `parentContentHash` 一致（`2257aea46640e1f91735ba006522507d`），
  编辑记录未过期。保存前的差异清单为本次 21 项预期改动，未夹带其他内容。
- 未做：`NarrativeLibraryTests.RenamingAndMovingStoryPreservesEntryAndStableLookup` 需要编辑器前台焦点，
  本环境未跑通；`NovelGameplayTests` 重场景用例在本环境无法启动（改动前同样如此）；
  未运行框架全量回归，未跑 Play Mode，未在消费项目验收。编辑器面板的实际排版观感需人工确认。
- 本机无关的动态共享字体与贴图导入器补的平台设置改动未纳入发布（保存模板前已还原为已发布状态）。

visual-novel 版本 0.10.0，父模板 base 0.6.4；最低框架兼容声明保持 0.14.1。

## 消费项目更新

1. 通过 `Ember/UPM Manager` 升级框架至 **v0.14.12**；不要直接改 `manifest.json` 或 `packages-lock.json`。
2. 模板从 0.9.2 到 0.10.0 是 **minor**（前两位变化），补丁增量不适用。两种做法任选：
   - **完整重新部署**：先备份本地剧情、资源与业务代码，再部署，随后把定制内容合回；
   - **人工迁移（推荐）**：本次新增内容很小，可只在消费项目里补三处——
     `NovelCommand._speakerNameKey` 与 `NovelLocalization.SpeakerName`、`NovelHistoryEntry.SpeakerNameKey`
     及相应解析点、以及内容表新增 `speaker.unknown` 行（改完记得烘焙）。
3. 技能最低业务模板版本已提到 0.10.0：按项目中心流程更新模板技能后再执行导入/导出。
4. 在消费项目重跑验收：造「未知说话人 → 自我介绍 → 真名」两句，确认揭晓前显示 `？？？`、
   揭晓后显示真名、历史里旧句仍是 `？？？`、**改动前后的存档都能读**、
   「角色强调 → Auto」仍按真实角色键高亮说话人。
