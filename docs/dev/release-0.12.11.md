# Ember Framework 0.12.11 发布说明

日期：2026-09-14。Tag：`v0.12.11`。归属：**框架升级**。

## 可选第三方包状态与安装

UPM 管理器的可选包区域明确显示已安装/未安装；已注册的 UPM 包显示实际版本及 Git、本地、内嵌或 Registry 来源。
兼容直接导入 Assets 或自定义包的已加载插件；发现脚本但尚未编译时标为待确认，并禁止重复导入。
未安装项提供“安装 v版本”按钮，通过 Unity Package Manager 的 Client.Add 下载和安装对应固定 Git URL。
安装前再次检查，已有插件不会被按钮覆盖。每项提供复制安装地址，面板提供刷新安装状态。

打开或返回窗口、项目变化、包注册和安装完成时请求刷新；检测结果在 Layout 阶段应用，避免异步更新改变重绘布局。

| 可选包 | 安装版本 | Git 标签 |
|---|---|---|
| Rainbow Folders | 2.4.5 | rainbow-folders-v2.4.5 |
| Rainbow Hierarchy | 2.6.5 | rainbow-hierarchy-v2.6.5 |
| Console Pro | 3.9.81 | consolepro-v3.9.81 |
| InputDeviceDetector | 1.0.0 | inputdevicedetector-v1.0.0 |
| Feel | 5.4.0 | ember-v0.11.1 |

以上按钮共用 ember-thirdparty-upm 仓库并指定各自包目录；Feel 暂无独立发布标签，继续使用已有标签。
完整地址记录在随包 release-0.12.11.json 的 optionalPackageInstallTargets。
历史发布 JSON 保持不变，新版完整 manifest 只推进 com.ember，仍保留原第三方依赖基线。
升级框架不会自动安装任何可选包，用户在面板中逐项按需安装。

## 范围与验证

- 已只读确认上述远端标签存在，配置版本与本地第三方镜像 package.json 一致；未执行实际插件安装。
- 新增 8 个 EditMode 案例，覆盖固定地址、实际版本/来源、已加载类型保护、待编译脚本和包注册时保留布局快照。
- 静态发布检查覆盖 package/manifest/release 一致性、56 项依赖范围、UPM 程序集零框架/Odin 引用、meta 唯一性与 Git 空白。
- 执行共享字体配置、三份字体二进制完整性与两种 autocrlf 干净检出检查；发布字体保持原状。
- 模板保持 base 0.6.3、source3d-2p5d 0.3.5，父快照、Hash、0.12.0 兼容声明与 GuideModule.Enabled=false 均不变。

当前 Unity MCP 不可用，本次未完成 Unity 编译验证。新增测试及消费端安装交互尚未执行，未沿用其他版本的编译确认。

## 升级与验收

已有项目通过 **Ember/UPM Manager** 升级至 0.12.11，无需保存或重新部署模板。
升级后验证已有 UPM 包的版本/来源、Assets 插件保护、缺包安装及完成后的状态刷新；安装使用消费机器现有 Git 仓库访问凭据。
请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。
