# 确认服务操作说明

本技能**随附**确认服务：`scripts/confirmation.py`（本机回执服务）与 `assets/confirmation.html`（页面）。
它把代理准备的**方案草案**渲染成本机可交互页面，并把用户的选择 / 草稿 / 确认 / 取消写成结构化回执。

它**不写 Unity 资产、不调用 Unity、不生成代码**。真正的实施在 `verify` 通过之后由代理完成。

> 与图片导入技能的区别：图片导入的 `confirmation.py` 是图片与配表专用的，**不能套用**。
> 本服务是通用「方案确认」，只依赖 Python 标准库（**不需要 Pillow**）。

## 1. 依赖与启动

依赖 Python 3.10+（只用标准库）。不要假设系统 `python` 已在 PATH；找不到时先定位解释器
（Windows 常见位置：`%LOCALAPPDATA%\Microsoft\WindowsApps\python.exe`）。

先由代理准备方案草案 JSON，再启动服务：

```powershell
& '<python.exe>' -B '<skill>/scripts/confirmation.py' serve `
  --project '<项目根>' --plan '<方案草案.json>' `
  --work '<项目根>/.utmp/vn-custom-node/<批次>' --mcp-verified
```

| 参数 | 说明 |
|---|---|
| `--project` | Unity 项目根目录（必须含 `Assets`） |
| `--plan` | 代理准备的方案草案；服务会把它存进批次的 `plan.json` |
| `--work` | 批次目录，必须在 `<项目根>/.utmp/vn-custom-node/` 下；省略时自动生成 `<planId>-<随机>` |
| `--suggestions` | 预填选择 JSON；**只是草稿，永远不构成确认** |
| `--mcp-verified` | 仅在实时 Unity MCP 身份检查通过后追加。这是代理的**声明**，不是自动连接器，也不是资产写权限。没有实时 MCP 时不得使用，页面也会禁用确认按钮 |
| `--port` | 默认 0（随机端口），只监听 `127.0.0.1` |

`serve` 是**持续进程**：

- 用可保留 session ID 的终端启动并保留会话；不要同步执行一直等进程结束。
- 需要后台启动时用 `Start-Process -WindowStyle Hidden`，把 stdout/stderr 重定向到本任务临时目录，**保留 PID 和日志**。
- stdout 与批次 `session.json` 都会给出实际 `url`。

批次目录布局：

```
.utmp/vn-custom-node/<批次>/
  plan.json      代理提交的方案草案（原样留档）
  batch.json     批次基线：项目指纹、nonce、batchId、方案
  draft.json     用户保存的草稿（永不授权实施）
  decision.json  只有页面明确确认或取消才生成
  session.json   服务地址与 PID
```

## 2. 打开页面

必须**实际打开返回的 URL 并观察**：标题「自定义节点方案确认」、阶梯单选、计划预览（文件 / 变量 / 剧情步骤 / UI）、
保存草稿、确认、取消按钮都正常，然后把可点击 URL 和批次目录交给用户。

**打开失败要报告具体错误并修复，不能只生成 HTML 文件就声称已展示。**
缺少运行依赖时报告实际缺项，保留只读方案，不降级为自动执行。

页面只保存选择与回执，不直接写 Unity 资产；方案文本按纯文本展示，不执行其中的 HTML 或脚本；
写请求用随机会话令牌校验，且只接受来自本机同端口 Origin 的提交，不开放跨域写入。

## 3. 方案草案格式

`--plan` 指向的 JSON（`schemaVersion=1`、`skillId=ember-vn-custom-node`）：

| 字段 | 说明 |
|---|---|
| `request` | 用户原始效果描述 |
| `tier` / `tiers` | 选中的阶梯与三级阶梯候选；每个候选有 `applicable` 与 `reason`，**被标记不可用的阶梯不能提交** |
| `summary` | 方案摘要 |
| `step` | 要插入的剧情步骤：`storyPath` / `chapterId` / `nodeId` / `insertAfter` / `commandKind` / `scriptId` / `scriptAssetPath` |
| `bridge` | 阶梯 `bridge` 时必填：`requestKey` / `payload` / `resultVariableId` / `variableScope` / `timeoutSeconds`（必须 > 0） |
| `script` | 阶梯 `script` 时必填：`scriptId` |
| `variables` | 新增变量：`id` / `type` / `scope` / `declaredIn` / `purpose` |
| `files` | 要新增 / 修改的文件：`path` / `action`(`create`\|`modify`) / `ownership`(`new`\|`project-owned`\|`template-owned`) / `purpose` |
| `ui` | UI 工作：`page` / `pageType` / `viaEuI`（必须为 true，否则不能勾选包含 UI） |
| `fingerprint` | `customStepIncluded` / `speakerVariableIncluded`，用于在页面上讲清旧档兼容结论 |
| `risks` / `verification` | 风险与验证方式，页面原样展示 |
| `fingerprintRoots` | 参与项目指纹的项目相对目录/文件；计划文件本身总是参与比对 |

服务在启动时会拒绝这些草案：阶梯不可用、`preset` 阶梯却列了文件、`bridge` 缺请求键或超时 ≤ 0、
`script` 缺 `scriptId`、`files` 缺 `purpose`、路径越界或含 `..`、
**`ownership=template-owned` 且 `action=modify`**（改模板自带文件）。

## 4. 选择与回执

用户可先保存草稿；代理读取后补全真实计划，再刷新同一页面供最终提交。

`decision.json` 只有两条路径会生成：

- 页面点「取消」→ `status=cancelled`（**终态**，本批不能再提交）。
- 页面点「确认」→ `status=confirmed`，含 `schemaVersion` / `batchId` / `skillId` / `projectRoot` /
  `status` / `choices` / 完整 `plan` / `planHash` / `reviewHash` / `decidedAt`。

选项语义：`tier`（三级阶梯之一）、`files`（勾选同意落盘的文件，必须是计划子集）、
`includeStep`（方案含剧情步骤时**必须显式选择**）、`includeUi`、`note`。
预览后改选项会使旧 `reviewHash` 失效，确认端会拒绝并提示重新预览。

## 5. 执行闸门

```powershell
& '<python.exe>' -B '<skill>/scripts/confirmation.py' verify --work '<本批目录>'
```

- 只有**退出码 0 且输出 `status=confirmed`** 才可开始实施；输出还带 `planHash` 与 `tier`。
- 否则退出码 2，stderr 给出原因（缺少回执 / 未确认 / 已取消 / 指纹已变化 / 回执内容不一致）。
- 复核内容：批次基线未被改动、**项目指纹与确认时一致**、`planHash` 与计划一致、
  `reviewHash` 与「批次+选项+计划」一致，并对计划再跑一遍路径与阶梯检查。

代理**不得**替用户点击提交、调用确认接口或伪造回执；草稿、取消、关闭页面、等待超时都不触发任何写入。
用户提交后回读回执、复核原有 MCP 与身份条件，再执行已选计划，**不另加一轮聊天许可**。
源数据、目标或方案变化时确认失效，回到页面展示新计划。

> 回执是本机工作流契约，**不是对任意有文件写权限程序的密码学认证**。测试请用隔离项目。

## 6. 维护验证

```powershell
& '<python.exe>' -B '<skill>/scripts/test_confirmation.py' -v
```

自测只用临时目录造一个假项目，不接触真实项目或 Unity，覆盖：未确认零写入、确认回读、
指纹失效、取消为终态、回执被改、以及各道闸门。

根目录默认取系统临时目录；若运行环境的系统临时目录不可写（受限沙箱），
用环境变量指到可写位置：

```powershell
$env:VN_CUSTOM_NODE_TEST_TMP = '<可写临时目录>'
```

（脚本刻意不使用 `tempfile.mkdtemp`：部分受限环境会把 `mkdtemp` 建出的目录设为不可写，
测试自身反而无法在其中造项目。）

除自动自测外，还要**在浏览器里实际验收**：打开页面、切换阶梯、勾选文件、保存草稿、
预览、提交、取消各走一遍；自动化点击不得针对真实待实施批次。
