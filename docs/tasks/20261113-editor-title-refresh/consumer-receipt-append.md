
## 第二批框架标题刷新修复接续（2026-11-13）

框架本地已实现窗口标题周期刷新修复：后台单次在途 Git/身份显示快照，主线程 descriptor 复用同一标题字符串，保留周期/手动/Play/项目变化与原生 fallback。没有改动 Windward 源码、PackageCache、manifest/lock、模板快照/hash 或绑定。**本轮未提交、推送或发布；整体性能任务继续开放。**

- [框架实施回执及边界](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/README.md)
- [本轮实际差异](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/evidence/task-only.diff)
- [最终定向测试](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/evidence/testResult3.json)：8/8、0 skipped；框架 MCP 已确认本轮编译/域重载及新 Basic.Editor MVID `a846df9e-3b58-41c7-b056-bcd9561e3b31`。
- [最终框架周期采样](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/evidence/periodic-summary.json)：16.107 秒、3 次查询调度，148 个主线程标题回调样本最大 0.3191ms、均低于 1ms；嵌套 BuildMainWindowTitle 最大 0.0475ms。此为框架后台 EditMode 定向证据，不能替代 Windward 的 4K 前台四条件复测或直接换算 FPS。
- [消费接续](../../../../../ember-unity-framework/docs/tasks/20261113-editor-title-refresh/consumer-handoff.md)：后续按独立授权正式发布，经 Ember/UPM Manager 接入；如扩展 hash 变化，先展示真实预览并独立授权绑定，然后再按原有限范围复测。

首批 0.25.4 已发布、Windward 已授权接入及重新绑定的事实以 [post-upgrade 回执](post-upgrade/README.md)为准。本轮修复的是该回执新定位的既有标题热点，不定性为已证实的 0.25.4 回归。普通打开项目中心的 46.74ms 重绘/其中 21.76ms GC 继续单独承接，本轮不扩大实现；inclusive 耗时不相加，也没有据缺失 GPU 数据判断 GPU 瓶颈。
