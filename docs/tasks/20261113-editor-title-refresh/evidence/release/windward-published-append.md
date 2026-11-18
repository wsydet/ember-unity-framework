
## 标题刷新修复正式发布：Ember 0.25.5（2026-11-13）

框架已复审并正式发布 annotated `v0.25.5`。远端 main 和 tag peeled commit 均为 `f3ea76bd2e45f57d1412f7fcdf540751d55f0abf`，原子推送成功。[发布说明](../../../../../ember-unity-framework/docs/dev/release-0.25.5.md)、[正式发布证明](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/evidence/release/remote-proof.json)。

发布目标版本在框架 MCP 确认编译/域重载与新代码加载；相同标题定向测试 8/8、0 skipped，七模板/六父快照真实 hash 与谱系一致，二进制/字体/未变技能 bundle 检查通过。候选只包含本轮标题修复、测试和必要发布资料；较早 solution WIP 差异已排除，框架其他本地改动保留。

**用户明确要求 Windward 暂不升级，本轮未执行消费接入、扩展绑定或复测。** 后续按[消费接续](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/consumer-handoff.md)独立授权，通过 Ember/UPM Manager 接入；如内容 hash 改变，先预览再独立授权绑定。原前台四条件及 GUI/GC 尾帧问题仍待接续，整体性能问题保持开放。
