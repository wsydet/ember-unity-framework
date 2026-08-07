# MCP 连接问题排查指南

> 踩坑实录，避免每次从头排查。
> 最后更新：2026-08-07

## 配置文件速查

| 文件 | 作用 | 生效范围 |
|------|------|----------|
| `%APPDATA%\Code\User\mcp.json` | VSCode 用户级 MCP 配置 | VSCode 扩展（MCP Gateway） |
| `<project>\.mcp.json` | 项目级 MCP 配置 | Claude Code 扩展 |
| `~\.claude\settings.json` | Claude Code 全局设置（`mcpServers` 字段） | Claude Code |

**关键区别**：`mcp.json` 由 VSCode 内置 MCP Gateway 处理，`.mcp.json` 由 Claude Code 扩展直接读取。同一个服务器可能在两边都有配置，排查时要搞清楚是哪个客户端在连接。

## 排查流程

### 第一步：确认服务器是否在运行

```bash
curl -s http://127.0.0.1:8080/health
# 或
curl -s http://127.0.0.1:8080/status
```

无响应 → 服务器没启动，去 Unity 里启动 MCP 桥接。

### 第二步：确认 MCP 协议握手是否正常

```bash
curl -s -D /tmp/mcp_headers.txt -X POST http://127.0.0.1:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
```

正常应返回 200 + `text/event-stream` + `mcp-session-id` header + JSON-RPC result。

返回 400 → 传输协议可能不匹配，继续看第三步。

### 第三步：确定服务器使用的传输协议

这是最常见的坑。MCP 有两种 HTTP 传输协议：

| 协议 | MCP 版本 | 握手流程 | `.mcp.json` type |
|------|----------|----------|-------------------|
| **SSE（旧版）** | 2024-10 之前 | GET 建立 SSE 流 → 从 endpoint 事件获取 sessionId → POST 发消息 | `"sse"` |
| **Streamable HTTP** | 2024-11-05 | POST `/mcp` initialize → 从 response header 获取 sessionId → 后续请求带 `Mcp-Session-Id` header | `"http"` |

**判断方法**：

```bash
# 方法 1：GET 请求（旧 SSE 的握手方式）
curl -s -w "\n%{http_code}" -X GET http://127.0.0.1:8080/mcp \
  -H "Accept: text/event-stream"
# → 200 = 旧 SSE 协议，type 写 "sse"
# → 400 "Missing session ID" = Streamable HTTP 协议，type 写 "http"

# 方法 2：看服务器 initialize 返回的 protocolVersion
# "2024-11-05" → 大概率是 Streamable HTTP
# "2024-10-07" → 旧的 SSE
```

### 第四步：检查 `.mcp.json` 的 type 字段

```json
// Streamable HTTP（mcp-for-unity-server v3.x 用这个）
{
  "mcpServers": {
    "mcp-for-unity": {
      "type": "http",          // ← 注意是 "http" 不是 "sse"
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}
```

### 第五步：查看 VSCode/Claude Code 日志

**VSCode MCP Gateway 日志**（VSCode 内置的 MCP 基础设施）：
```
%APPDATA%\Code\logs\<最新目录>\mcpGateway.log
%APPDATA%\Code\logs\<最新目录>\<windowN>\mcpServer.mcp.config.usrlocal.<serverName>.log
```
- 如果文件是 0 字节 → VSCode MCP Gateway 从未尝试连接（通常是 type 写错了）
- 如果有错误日志 → 看具体报什么错

**Claude Code 扩展日志**（Claude Code 自己的 MCP 连接）：
```
%APPDATA%\Code\logs\<最新目录>\<windowN>\exthost\Anthropic.claude-code\Claude VSCode.log
```
搜索关键字 `MCP` / `mcp`，关键日志行：
- `MCP server "<name>": <type> transport initialized` — 配置已读取
- `SSE Connection failed` / `HTTP Connection failed` — 连接失败，看 error 信息
- `MCP server "<name>" Connection failed (<status>)` — 失败状态码

### 第六步：修改配置后必须重载

改完 `mcp.json` 或 `.mcp.json` 后：
- **最可靠**：关闭 VSCode 窗口重新打开
- **快捷**：`Ctrl+Shift+P` → `Developer: Reload Window`

不行就两个都试试（偶有重载不生效的情况）。

## 常见错误速查

| 错误 | 原因 | 解决 |
|------|------|------|
| `SSE error: Non-200 status code (400)` | `.mcp.json` type 写了 `"sse"` 但服务器是 Streamable HTTP | 把 type 改成 `"http"` |
| 日志文件 0 字节 | VSCode MCP Gateway 根本没尝试连接 | 检查 `mcp.json` 的 type 和 url 是否正确 |
| `/mcp` 显示 0 servers | 没有检测到任何 MCP 配置 | 检查 `.mcp.json` 是否存在、JSON 格式是否正确 |
| `/mcp` 显示 1 not connected | 检测到配置但连接失败 | 按上面流程逐步排查 |
| `Missing session ID` | 用 GET 请求 Streamable HTTP 服务器 | 正常现象，说明服务器是 Streamable HTTP |

## 本次问题回顾（2026-08-07）

**现象**：`/mcp` 显示 `0 connected, 1 not connected`

**根因**：`mcp-for-unity-server` v3.4.6 使用 MCP 2024-11-05 Streamable HTTP 协议，但 `.mcp.json` 的 `type` 字段写了 `"sse"`（旧版 SSE 协议）。Claude Code 的 SSE 客户端先发 GET 请求尝试建立 SSE 流，服务器返回 400（"Missing session ID"——因为 Streamable HTTP 需要先 POST initialize 获取 session）。

**修复**：`.mcp.json` 中 `"type": "sse"` → `"type": "http"`，重载窗口即可。

**经验**：`"type"` 字段不是随便写的。`"http"` 在 Claude Code 里对应的是 Streamable HTTP 传输（MCP 2024-11-05 新标准），`"sse"` 是旧版 SSE 传输。不要看见服务器返回 `text/event-stream` 就以为是 SSE —— Streamable HTTP 也会返回这个 Content-Type。
