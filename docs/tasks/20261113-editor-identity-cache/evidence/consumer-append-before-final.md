
## 框架方案 B 实施接续（2026-11-13）

框架工程已完成本批 Editor 身份/扩展缓存、稳定绑定复用、后台合并失效检测、菜单/项目中心/试玩检查批次，以及直接相关验证。状态仍为**框架工作区已实现，未发布，Windward 未升级，消费端性能待复测**。

- [框架实施回执及证据](../../../../../ember-unity-framework/docs/tasks/20261113-editor-identity-cache/README.md)
- [仅本任务实际差异](../../../../../ember-unity-framework/docs/tasks/20261113-editor-identity-cache/evidence/task-only.diff)
- [消费端接续说明](../../../../../ember-unity-framework/docs/tasks/20261113-editor-identity-cache/consumer-handoff.md)
- 最终对应框架 MCP 编译/域重载完成，Core Editor MVID `a0e4f035-a6dc-4d99-9044-bb651897d379`；定向测试 33/33 通过、0 skipped，最终 Console error/warning 过滤为 0。
- 稳态查询区间无新增 JSON 读取、模板枚举或 hash 构建；周期主线程检查 p95 约 0.189ms。启用态 prepared resolution 另以仅内存的待审阅配置验证；未保存/重新绑定任何项目组合。API 微基准不替代 Windward 帧采样或产品实际试玩。
- 框架现有 topdown 编辑身份 0.1.2 与旧扩展配置 0.1.1 不匹配，保持安全拒绝；没有为验证覆盖草稿或修改该绑定。真实模板切换、UPM 包来源切换和产品启用态 Play 尚未覆盖。

下一步分别授权框架提交、推送、正式发布，以及 Windward **Ember/UPM Manager** 升级；必要的扩展 hash 重新绑定先取得 Windward 实际正式预览，再独立授权。之后补齐原方案的同环境前台四条件与直接相关功能验证。原有限基线及样本限制继续有效，未将整体低帧率问题标为已修复。本次仅向此回执追加链接，没有修改 Windward 源码、PackageCache、manifest/lock、配置绑定或存档。
