# UPM Manager：MCP 推荐版本检查与显式升级

日期：2026-10-23。范围为框架公共 Editor 工具，适用于所有模板及未部署模板的项目。不涉及模板快照、版本 Bump、Git 提交、发布或消费项目升级。

## 实现与使用

入口：`Ember > UPM Manager > 可选第三方包 > Unity MCP`。

- `EmberUPMOptionalPackages` 保留现有安装检测，增加实际版本与推荐版本比较、来源判定及 `CanUpgrade`。当前推荐值来自随包目录，为官方 10.3.0，未发起远程最新版本查询。
- 官方 HTTPS 仓库 `CoplayDev/unity-mcp.git?path=/MCPForUnity`，使用默认分支、`main` 或 `vX.Y.Z` 标签，且为项目直接依赖时，较旧稳定版本显示“升级到推荐 v10.3.0”。用户点击才调用既有 `InstallPackage` / `Client.Add`，固定目标标签。相同版本不重装，较新版本不降级。
- 本地、内嵌、Registry、Assets 导入、分叉仓库、自定义分支、固定提交和间接依赖保留来源。未知、非标准或预发布版本交给人工确认。面板显示实际版本、推荐版本和保护原因。
- 点击升级时重新查询注册包，并读取当前磁盘 manifest。必须与已解析的来源一致，否则拒绝替换。重复、转义或无法安全识别的依赖声明采取保守拒绝；解析器仅识别标准的无转义平面 dependencies 字符串映射，以保持 UPM Editor 程序集无框架、无第三方 JSON 引用。
- MCP 尚未注册但 manifest 已有声明时，安装入口也拒绝覆盖。其他可选包原安装行为保持不变。
- 复用既有安装互斥和 UPM 请求完成/失败提示；包注册、窗口重新获得焦点及手动刷新重读实际状态。UPM 请求成功不宣称编译、域重载或 MCP 连接已完成。未修改 Python 服务端、端口、客户端配置。

消费说明的权威入口见[包 README](../../Packages/com.ember/README.md#unity-mcp-的安装与更新)。以后其他项目安装包含此功能的正式 Ember 版本后获得该入口，仍需明确点击 MCP 升级。当前已发布 0.21.3 不会因本地改动改变。

## 增量验证

环境：框架工程 Unity 6000.5.4f1；MCP 插件 10.3.0，官方 `#v10.3.0`。本轮未修改 Windward、Mountain、第三方仓库、PackageCache、模板或项目依赖清单。

只运行 `Ember.UPMManager.Editor.Tests.EmberUPMOptionalPackagesEditTests`：原有 9 项，加本轮 33 项，共 **42/42 通过**，0 失败、0 跳过。最终测试执行耗时约 1.28 秒，未全量重测。

覆盖较旧/相同/较新/未知/预发布版本、数值版本比较、官方分支与标签、固定提交与自定义来源、间接依赖、陈旧注册信息与磁盘声明不符、缺失/重复/转义声明、只向推荐地址派发一次、原有可选包安装地址及注册后刷新。

编译按公共规则主动触发两次 `refresh_unity(compile=request)`：

1. 首轮程序集 MVID 从 `a39b0a6a-81e4-4b28-b5df-2079f33ad300` 变为 `ac7e5a95-d2d3-450d-9f19-566db2817219`，已加载 `TryUpgrade`；非 compiling/updating，Console 0 错误/警告。41/41 通过。
2. 复查补充歧义声明和待解析安装保护后，再次主动编译。最终 MVID `5a61a5f0-f15e-4ad5-8567-f85c071574a5`，已加载 `TryReadManifestDependency`；非 compiling/updating，Console 0 错误/警告，再运行得到 42/42。

实际打开 UPM Manager，读取窗口已生成的 MCP 状态：`已安装 v10.3.0（UPM · Git）；框架推荐 v10.3.0（已达推荐版本）`，CanInstall=false、CanUpgrade=false。最终 Console 仍为 0 错误/警告。

## 首次结果、修正与边界

本轮没有编译或测试失败。首轮通过后静态复查发现，简单提取指定键无法排除转义同名声明，也未保护“manifest 已声明但尚未注册”的首次安装情况；补充保守声明校验与安装保护后重新编译和验证。

尝试通过 Unity `ReadScreenPixel` 留存窗口图，但多显示器/DPI/前台窗口条件下取得的图未覆盖目标窗口，已丢弃该无效图；不把它作为视觉验收。实际窗口状态与无 IMGUI Console 异常已核对，未宣称人工视觉签收。

旧版本与本地/自定义来源使用 PackageInfo 测试夹具，派发动作替换为记录回调，不写项目 manifest、不下载包。当前已是推荐版，未为验证而降级再升级。因此本轮没有新增真实旧项目的网络下载、UPM 失败或 MCP 重连端到端演练；这不等于消费项目升级签收。UPM 异步安装沿用现有实现，域重载后以重新读取的实际版本判断状态，不自动重试安装。

证据：

- [首轮测试](evidence/upm-mcp-upgrade-20261023/tests-first.json)
- [最终测试](evidence/upm-mcp-upgrade-20261023/tests-final.json)
- [新程序集与编译状态](evidence/upm-mcp-upgrade-20261023/compile-final.json)
- [最终 Console](evidence/upm-mcp-upgrade-20261023/console-final.json)
- [实际窗口状态](evidence/upm-mcp-upgrade-20261023/window-state.json)

`git diff --check` 对本轮相关源码和 README 检查通过。工作区保留之前所有未提交工作；本轮没有提交、推送或发布。
