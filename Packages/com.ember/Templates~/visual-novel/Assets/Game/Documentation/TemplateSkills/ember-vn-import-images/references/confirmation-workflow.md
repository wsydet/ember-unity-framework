# 本地确认服务

## 启动和打开

依赖 Python 3.10+ 与 Pillow。Codex Windows 可先调用 `load_workspace_dependencies` 取得随应用提供的 Python，再用该解释器运行 `-c "from PIL import Image; print(Image.__version__)"` 验证；不要假设系统 `python` 已配置。其他客户端使用已安装的 Python/Pillow。缺少依赖时说明缺项，不生成不能回传的替代静态页面。

在实际项目根目录运行（`<skill>` 是本次正式安装技能的绝对路径；模板作者测试时使用 Assets 中的源路径）：

```powershell
& '<python.exe>' -B '<skill>/scripts/confirmation.py' serve --project '<项目根>' --source '<指定图片文件或目录>' --work '<项目根>/.utmp/vn-image-import/<新批次>'
```

`serve` 是持续进程。使用可返回 session ID 的终端工具启动并保留会话；不可用同步执行一直等待进程结束。需要后台启动时使用 `Start-Process -WindowStyle Hidden`，将 stdout/stderr 重定向到本任务临时目录；保留 PID。服务创建独立批次目录，已存在则拒绝覆盖。仅监听 `127.0.0.1` 的随机端口。

先通过 Unity MCP 核对当前项目、执行 `GetTemplateSkillExecutionBlockReason` 且结果为空、确认实际 CSV/Definition 映射后，才能追加 `--mcp-verified`。此参数是代理记录前置检查的声明，**不是 MCP 自动连接器，也不是资产写权限**。没有实时 MCP 时不得使用。提交后执行阶段还必须复核 MCP，不能凭此启动参数跳过。

stdout 和批次 `session.json` 都有实际 `url`。Codex 用 `open_in_codex` 的 browser target 打开，或 `cua.createBrowserTab('iab', url, {visible:true})`；随后用浏览器观察实际页面，验证标题“图片导入确认”、缩略图和按钮。保留面向用户的页面（浏览器工具支持时 `markDeliverable`）并输出可点击 URL。不得只输出 HTML 文件路径或以提问工具调用代替打开。不要在正式素材批次自动点击最终确认；测试点击只用于隔离测试夹具。

服务扫描所指定目录及子目录，支持 PNG/JPEG/WebP/BMP/TGA/TIFF，展示解码尺寸、是否含透明像素、SHA256 重复提示。AI 仍需亲自查看图片内容。无法解码的图片会报错，应核对源图并新建批次，不暗中漏掉失败图片。

## 表路径、建议和草稿

默认使用映射文档中的三张 `.etable.csv`，精确核对列名。实际 Definition 改过路径时提供 `--tables <json>`：

```json
{
  "characters": "Assets/GameResource/TableSources/novel_characters.etable.csv",
  "portraits": "Assets/GameResource/TableSources/novel_portraits.etable.csv",
  "backgrounds": "Assets/GameResource/TableSources/novel_backgrounds.etable.csv"
}
```

可用 `--suggestions <json>` 预填完整 choices 数组；索引按源文件完整路径排序。所有建议仅写入草稿。示例：

```json
[
  {"index":0,"kind":"portrait","story":"MyStory","characterId":"hero","displayName":"主角","variant":"neutral","resourceId":"hero_neutral","conflict":"block"},
  {"index":1,"kind":"background","story":"MyStory","scene":"school","variant":"day","conflict":"block"},
  {"index":2,"kind":"other","story":"MyStory","scene":"title","otherUse":"TitleUI","variant":"default","conflict":"block"},
  {"index":3,"kind":"skip"}
]
```

`otherUse` 可选 TitleUI、CG、Icon、Mask、Other。已有角色显示名只读，复用角色 ID；新角色需填写显示名。资源 ID 留空时按角色/场景与变体生成。默认冲突阻断，`reuse` 仅允许相同目标文件内容和一致表行；不支持覆盖不同内容。其他用途不写小说表、不自动接入业务。

草稿 `draft.json` 记录 batchId 与 choices；同批页面刷新会恢复。服务退出后创建新批次可将旧草稿的 choices 提取为 suggestions，但必须重新核对源图顺序、指纹和完整计划；不可复制旧 decision 作为新批授权。

## 结果回读和执行闸门

- `batch.json`：实际项目、源范围、CSV 路径、源图/表/表定义/资源 SHA256 指纹与 batchId。
- `draft.json`：用户保存的 choices，或 AI 初始建议；永远不授权导入。
- `decision.json`：只有页面明确确认或取消才生成，状态为 confirmed 或 cancelled。确认包含 choices、完整 plan 和 reviewHash；取消是终态。

页面更改选择后使已预览计划失效，后端也验证选择与预览指纹一致。文件变化会阻止提交；执行前必须再次运行：

```powershell
& '<python.exe>' -B '<skill>/scripts/confirmation.py' verify --work '<本批目录>'
```

仅退出码 0 且输出 `status=confirmed` 后，读取该计划、复核实时 MCP 与技能身份，按 SKILL.md 备份 → 导入 → 配表 → 烘焙。计划/源图/配表变化均需新批次重新确认。不得把“verify 成功”说成“已经导入”；辅助脚本没有复制素材或修改配表的代码。

回执是本机工作流契约，不是对任意有文件写权限程序的密码学认证。代理不得自行编辑 batch/decision、伪造点击或调用 confirm；正式用户提交通过页面完成。测试使用隔离项目。

源范围、全体资源和三张表在首轮写入前核对；导入会改变这些指纹，因此不要在自己已写入一半后原样再次运行 verify。中途恢复遵循 SKILL.md 的本批输出指纹和备份规则。完成后保留回执、备份和逐图结果，停止本批服务；等待用户操作期间保持服务。服务不会自动触发 Codex 新一轮消息，用户提交后返回任务时读取文件即可可靠继续。

## 维护验证

```powershell
& '<python.exe>' -B '<skill>/scripts/test_confirmation.py' -v
```

该测试只用系统临时目录，验证取消/未确认零资产写入、确认回读、指纹失效与离线草稿。还需在浏览器实际验收打开、图片加载、批量和逐图修改、预览、提交和取消；自动化点击不得针对真实待导入批次。
