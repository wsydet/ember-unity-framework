# Ember Framework 0.12.5 发布说明

日期：2026-09-11。新 tag：`v0.12.5`。归属：**框架升级**。

## 改动

`Ember/UPM Manager → 检查更新` 原先在编辑器主线程同步读取 Git 输出并等待退出，网络慢时会冻结面板，查询中文字也可能无法刷新。

- 新增独立、零框架依赖的 `EmberUPMUpdateCheck`，同时异步读取 Git 标准输出和错误；编辑器 update 轮询完成状态，主线程更新版本列表。
- 面板显示循环移动的进度条、活动符号和已耗时。15 秒后提示响应较慢，60 秒自动超时；这只是等待指示，不表示下载百分比。
- 支持取消和重新检查。关闭窗口或脚本域重载导致 OnDisable 时，退订轮询并终止/释放当前 Git 查询；重新打开窗口后手动重试，不恢复旧查询。
- 查询期间禁止重复检查、升级框架和安装前置依赖，避免包切换中继续处理旧结果。
- 使用已有 Git 凭据，关闭后台交互式凭据输入；失败和超时会显示原因。
- 保留轻量/附注 tag 归一化与去重，重新检查先清空旧结果；没有可识别版本时显示错误，避免误报已是最新。

## 必需依赖与可选第三方包

保留 Odin Inspector 和 DOTween 的框架必需定位。可选区增加 Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector 和 Feel，显示 UPM 已安装版本、已检测到直接导入/自定义包插件或未安装。检测使用已注册包与插件类型，无第三方程序集硬引用；当前 Assets/ThirdParty/Feel 可通过 MMF_Player 类型识别，避免重复安装。

未安装的可选包提供按需安装按钮，固定到已核对存在的第三方标签 `ember-v0.11.1`，不自动批量安装或迁移现有插件。安装期间统一显示状态，启动失败恢复按钮；窗口支持滚动，保证小窗口仍可查看完整清单。

## 升级完成提示修复

根因是 DrawUpgradeOperation 在成功提示的每次绘制中清空 `_newerTags` 与 `_checkMessage`，导致下一次查询的结果被立即抹掉。现在提示绘制只读，新检查自动收起旧终态提示，无需手动关闭再检查；候选列表只显示高于实际安装版本的 tag。

## 交付范围

更新 UPM 编辑器、测试、package.json、CHANGELOG、`Dependencies~/manifest-0.12.5.json`、`release-0.12.5.json` 和发布索引。
依赖基线仍为 56 项，仅推进 com.ember tag；第三方继续复用 `ember-v0.11.1`。

模板保持 `base 0.6.0` 与 `source3d-2p5d 0.3.1`、框架兼容声明 0.12.0，内容/Hash/父快照不变，无需重新部署模板。
不提交工作区原有的 EmberDebugConfig、动态字体缓存和 slnx 改动；共享字体发布内容保持 v0.12.4。

## 验证

静态核对非阻塞读取、超时/取消退出路径、窗口关闭退订、按钮互斥、版本与依赖声明一致性；运行 Git diff 空白检查及既有共享字体配置、二进制完整性与干净检出检查。

新增 `Ember.UPMManager.Editor.Tests.EmberUPMUpdateCheckEditTests` 的 5 个 EditMode 案例，使用临时本地 Git 仓库，不访问远程网络：

1. 轻量和附注 tag 正确返回版本并去重，忽略非版本 tag。
2. 空仓库不会误报已是最新。
3. 不存在的仓库返回 Git 错误。
4. 取消/重复释放后不发布旧结果，允许重新查询。
5. 到期后报告超时，后续轮询和释放安全。

另增 `EmberUPMUpgradePromptEditTests.CompletedPrompt_RepaintsWithoutClearingNewCheckResults`：在独立测试窗口实际执行两次 IMGUI 重绘，断言升级完成提示保留新查询结果和消息，不修改用户的 UpgradeTracker/SessionState。共新增 6 个测试，尚未执行。

这些 Unity 测试尚未执行。当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

手动验收：在框架项目打开 UPM Manager，点击检查更新，确认查询期间可操作编辑器；检查进度动画、耗时、取消及再次查询；弱网情况下检查 15 秒提示与 60 秒超时；查询中关闭窗口/触发脚本重载后，重新打开可正常查询。确认正常结果包含 v0.12.5。保留升级完成提示直接再次检查，确认旧提示自动收起且可升级按钮保持显示；已安装和更旧版本不再列为候选。检查必需区与五项可选包状态，在当前工程确认 Feel 显示已检测到插件，未安装的可选项提供安装入口。Unity 交互结果、编译和 EditMode 结果均不能由静态检查替代。

## 消费端升级

已有项目通过 `Ember/UPM Manager` 选择 v0.12.5，由 Unity Package Manager 完成升级；代理不直接改消费端 manifest/lock。
本轮发布不代表已为任何消费项目安装或验证新版本。安装 v0.12.5 后，新的检查更新反馈才会生效。
