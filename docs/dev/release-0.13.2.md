# Ember Framework 0.13.2 发布说明

日期：2026-09-17。Tag：`v0.13.2`。归属：**框架升级**。

## Unity MCP 按需安装

此前完整依赖清单包含 Unity MCP，但 UPM Manager 的可选包列表未包含它，消费项目无法从该面板安装。
现在 `Ember/UPM Manager → 可选第三方包` 提供 **Unity MCP（AI 编辑器连接）**，显示安装状态、实际版本及来源。
未安装时点击“安装 v10.1.2”，由 Unity Package Manager 下载、解析和维护依赖；提供复制地址及刷新安装状态，保留已有插件的重复安装保护。

可选包条目支持独立安装地址。Unity MCP 使用官方仓库的固定提交，与已有完整依赖基线一致：

```text
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50
```

安装后等待导入和编译，在 `Window > MCP for Unity` 配置服务与 AI 客户端。
安装状态不等于连接状态，本入口不安装 Python/uv、不启动服务、不改 AI 客户端配置。

## 发布范围

- 框架版本、CHANGELOG、release/manifest 对齐 0.13.2；完整 manifest 的 56 项依赖仅推进 com.ember。
- 发布声明的 optionalPackageInstallTargets 新增 Unity MCP，其余 5 项地址保持不变。框架 Runtime 不强制依赖 MCP。
- base 0.6.3、source3d-2p5d 0.3.5、框架兼容声明 0.13.0、Hash、父快照及 GuideModule.Enabled=false 均不变，无需重新保存或部署模板。
- 9 个技能保持原状，独立技能来源继续使用 v0.13.1。
- 本地 EmberDebugConfig 与生成的解决方案改动不纳入发布。

## 验证边界与消费验收

静态检查覆盖版本与依赖一致性、固定安装 URL 与现有 manifest/lock 的一致性、官方及本地包版本 10.1.2、插件类型探针、原有安装地址未变、UPM 编辑器程序集独立性、Git 空白和共享字体配置/二进制及两种 autocrlf 干净检出。
新增官方仓库、包目录和固定提交的 EditMode 回归；未执行实际插件安装或消费端 manifest/lock 修改。

当前 Unity MCP 不可用，本次未完成 Unity 编译验证；新增测试及消费端安装、状态刷新和连接验收尚未执行。
已有消费项目通过 **Ember/UPM Manager** 升级至 0.13.2，再安装 Unity MCP。检查缺包时可安装、已有安装不被覆盖，以及安装完成后的状态刷新。
请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。
