# S0 工程与保护记录

日期：2026-10-26。工程：`C:/Users/wuyu/My/ember-unity-framework`。本轮完成 S0，不展开 S1。

## 身份与范围

- Unity 6000.5.4f1；MCP 固定实例 `ember-unity-framework@b23e9b768e85ed30`，未操作同时在线的 Windward。
- HEAD：`5119adc2c142295f0367fde05c4b01d25b672c91`。embedded `com.ember` 声明 0.22.0，不据此宣称已发布。
- `Ember.Core.Editor.EmberProjectSetup.GetEditingTemplate()` 与 `Assets/Editor/EmberEditingTemplate.json` 一致：base 0.8.4，loadedAt=`2026-10-26 16:53:30`，contentHash=`936dcb1d411afefcacd961cbad1d1d0d`。
- base 无父模板，存储 contentHash 与 versionedContentHash 相同。模板列表另有 platformer-2d 0.11.3、source3d-2p5d 0.5.1、visual-novel 0.21.3；本轮不改这些模板。
- 用户本轮说明：“我手动切换到base的，不用管之前的模板，他没有提交”。因此将切换前模板保护追查从本轮范围排除；这不是声称找到了原模板备份。本轮未调用 LoadTemplate，未重复覆盖 base。
- 活动 base 没有 `Assets/Game/Documentation/base/glossary.md`；使用公共术语字典。

## 当前工作保护

本轮开始时 Git 有 27 个修改、1136 个删除、16 个未跟踪文件，无暂存差异。大批删除属于已经发生的模板切换现场，不能清理或当成本轮删除成果。字体四个 SDF 与 slnx 已有改动继续保留。

备份绝对路径：`C:/Users/wuyu/My/ember-unity-framework/.utmp/ui-production-S0-20261026`。

- `status-before.txt`：完整 Git 状态，包含删除与未跟踪清单。
- `head.txt`、`unstaged.patch`、`staged.patch`：HEAD 与 binary diff，恢复已跟踪差异的依据。
- `files/`：五个模板受管根（Game、Resources、Ember/Editor、Settings、GameResource）、Assets/Editor 身份记录、本实施文档、SharedAssets/Fonts 和 slnx；保留 .meta。
- `manifest.json`：430 个文件、35,347,589 字节，每文件 SHA256。重新读取备份逐个校验，损坏项 0。
- `persistent-before/`：运行前的本工程 persistentDataPath 备份，未恢复覆盖用户存档；正常 Play 可能更新存储时间或数据。
- `status-after-runtime.txt`、`base-compare.txt`：运行后状态与 base 比对。base 快照中每个文件均在当前 Assets 找到且字节一致，差异 0；这是单向比对，不宣称已完成所有额外文件审计。

运行前 FrameworkScene 无未保存修改，无 Prefab Stage，所有打开窗口 hasUnsavedChanges=false。运行结束停止 Play，回到同一 FrameworkScene，sceneDirty=false。备份清单中非文档文件与现场再次逐字节核验，变化 0。

恢复时先核对后来修改，按 `manifest.json` 从 `files/` 选择性恢复文件及其 .meta；已跟踪删除及历史内容结合固定 HEAD、status 与 binary patch，在独立恢复目录先比对。不要整目录覆盖当前工作区，不改模板身份伪造加载，不用 reset/clean。需要恢复到其他模板时先保护当前 base，再通过项目中心正式操作。

## 编译与收尾

通过 MCP `refresh_unity(compile=request, mode=force, scope=all)` 主动请求编译。事件时间：开始 1793007204252、完成 1793007207037、域重载完成 1793007213117（Unix 毫秒）；随后 editor idle、ready_for_tools=true、非编译/非更新，Console 错误/警告均为 0。详见 [validation.json](validation.json)。

Play 结束后的整个 Assets 内容摘要查询未返回有效结果，随后出现两条 MCP WebSocket warning；按 30 秒间隔第一次重试即恢复。没有重放该查询，没有未决写入。最终只读身份查询成功、Play=false、sceneDirty=false；最终 Console 无 error，两条 warning 为上述连接断开。不要再次对整个 Assets 请求模板 hash，模板比较应限定受管内容。

本轮仅新增交接证据并更新进度，无 C#、Prefab 或正式资源编辑，无正式 UI 再生成。模板普通保存不需要，未执行；Bump、提交、推送和发布均未执行。四份 contracts 未修订。

## 验收

| 编号 | 结果 | 依据 |
| --- | --- | --- |
| A00 | 通过（按用户澄清后的范围） | 当前现场完整保护及摘要校验；切换前模板不再追查 |
| A01 | 通过（已有切换的现场续验） | 用户确认手动切换、中心 API 与编辑身份一致、base 文件一致 |
| A02 | 通过 | 本轮编译/域重载、正式启动与导航运行证据 |
| A03 | 通过 | 实际资产、有效类型、生成来源与候选清单 |

下一步从 [S1](../../01-components-and-binding.md) 开始；不要再次加载 base。新阶段开始仍需检查新增并发修改。
