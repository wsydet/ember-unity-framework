# Unity MCP 连接排查

最后核对：2026-09-17。项目依赖为 `com.coplaydev.unity-mcp`，来源见 [manifest](../../Packages/manifest.json)；具体解析提交见 [packages-lock](../../Packages/packages-lock.json)。不要用历史服务版本推断当前协议或端口。

## 消费端安装入口（0.13.2 起）

0.13.2 为 `Ember/UPM Manager → 可选第三方包` 增加 **Unity MCP（AI 编辑器连接）**。
此前只有完整依赖清单包含它，面板没有对应安装项。消费端需先通过 UPM Manager 升级到 0.13.2 或更高版本，再使用此入口。

- 未安装时点击“安装 v10.1.2”，由 Unity Package Manager 下载、解析并维护 manifest/lock；已有安装显示实际版本和来源，不被强制覆盖或降级。
- 安装来源为官方 `CoplayDev/unity-mcp` 仓库的 `/MCPForUnity` 子目录，固定到 `4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50`，与既有发布依赖清单一致，不跟随浮动 main。
- 提供复制安装地址和刷新安装状态。安装完成后等待导入与编译，再在 `Window > MCP for Unity` 配置服务与客户端。仅安装 Unity 包不会替用户配置 Python/uv、启动服务或连接 AI 客户端。
- 这是可选编辑器工具，不增加框架 Runtime 依赖，也不需要保存或部署模板。

已核对 [固定提交的官方包信息](https://github.com/CoplayDev/unity-mcp/blob/4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50/MCPForUnity/package.json) 与本地包信息；本轮未实际安装，Unity 编译及新增 EditMode 回归尚未验证。

## 排查顺序

1. 确认 Unity 打开的是本仓库，等待编辑器完成启动，在 MCP 插件面板确认服务/桥接已启动。
2. 在当前 AI 客户端检查可用的 Unity MCP 工具与连接状态，确认实例对应正确项目。安装了包不等于当前会话已连接。
3. 对照插件面板与当前客户端配置中的 URL、端口和 transport；不要照抄固定的 `127.0.0.1:8080`。
4. 配置变更后按客户端要求重连或重载，再通过只读工具确认项目和场景。不要同时向多个 Unity 实例发修改命令。
5. 请求失败时保留工具返回的错误、连接地址和客户端名称。仅凭 GET 返回 400、`text/event-stream` 或空日志文件，不能判断具体根因。

VSCode、Claude Code 和其他客户端各自读取自己的 MCP 配置；先确认当前使用的客户端，再检查其实际配置。不要把另一个客户端的设置文件直接搬过去，也不要在报告中输出访问令牌。

## 编译和测试边界

按 [项目规则](../../CLAUDE.md)，编译验证只走 Unity MCP：刷新/请求编译后，通过 MCP 读取 Console，并在需要时运行相应测试。MCP 不可用时，记录无法验证并由用户在 Unity 中检查；不改用 Editor.log、BatchMode 或 dotnet 作为替代编译通道。

仅修改 Markdown 或维护脚本时，运行链接检查和脚本验证即可，不宣称 Unity 编译或 PlayMode 通过。

## 历史案例

2026-08-07 曾出现客户端配置的 transport 与服务不匹配，修正配置并重载后恢复。这个案例说明需要核对客户端和服务两端配置；旧记录中的服务版本、协议日期和固定端口不作为当前配置依据。
