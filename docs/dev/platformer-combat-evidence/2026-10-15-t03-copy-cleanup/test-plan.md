# T03 独立框架测试副本

源项目：C:/Users/wuyu/My/ember-unity-framework

副本：C:/Users/wuyu/My/ember-unity-framework/.utmp/t03-framework-copy

Unity：6000.5.4f1。副本复制当前 Assets、Packages、ProjectSettings；不共享可写目录，不复制 Library、原用户窗口或 SessionState。不是消费项目，不代表消费身份验收。

## 约束

- 仅在标题为 t03-framework-copy、Application.dataPath 指向本目录的 Unity 内操作。
- 保持项目默认方案；测试候选使用已有“通用攻击模板1-体验调校”，只改内存草稿第一刀时长为 0.47，不应用、不保存。
- 不 Bump、不提交、不发布。不对原项目切模板或改草稿。
- 首次 MCP 查询失败/超时后停止自动 Unity 验证，不使用日志、BatchMode 或 dotnet 代替。

## 跨模板验证顺序（基线取得后才执行）

1. 由代理核对副本项目路径、Options=1、Player 启用及 Console；建立副本窗口测试候选、导出窗口和 SessionState 草稿、默认配置与素材引用。
2. 代理明确通知后，用户在副本打开 Ember → 项目中心 → 模板开发，选择 base，点击“加载到项目”，确认“加载并替换”。不要退出 Unity；SessionState 随 Editor 进程退出会消失。
3. 等待导入完成，连接该副本 MCP，代理验证菜单隐藏、Skill 拒绝、旧窗口不可操作及草稿缓存保留。先取得此阶段证据，不立即切回。
4. 代理通知后，在副本同一面板选择 platformer-2d，点击“加载到项目”并确认。待代理核对恢复草稿全部字段、引用及默认配置；不要点击重新读取或应用候选。
5. 全部取证后只清理本次测试候选，恢复副本窗口基线。原项目不参与。

## 尚不能标记通过的项目

- 合法派生：正式 CreateTemplate 调用 EnsureParentCanBeBranched；父 contentHash 与 versionedContentHash 不同即拒绝创建。当前 platformer-2d 为保存后未 Bump 状态，不绕过也不修改 hash，因此暂不创建派生模板。
- 缺失/无效身份：已有 EmberTemplateIdentityEditTests 可验证纯身份判定，但不代替真实菜单及旧窗口交互。不得通过手写正式身份记录伪造通过；具体隔离故障注入方式仍待确认。
- 消费身份：需要独立消费项目，本副本不覆盖。

本文件是操作计划，不是通过证据。首次导入及 MCP 基线未确认前，不执行步骤 2。
