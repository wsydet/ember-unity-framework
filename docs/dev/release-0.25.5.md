# Ember 0.25.5

2026-11-13。基于 v0.25.4 的 Editor 窗口标题周期刷新性能修复补丁。发布目标为 origin/main 与 annotated `v0.25.5`，实际发布提交以远端 peeled tag 为准。

窗口标题每 5 秒同步查询两次 Git 曾导致约 149–171ms 的主线程长任务；旧 0.25.3 样本也有命中，因此本版处理既有热点，不将它定性为已证实的 0.25.4 回归。

## 交付

Git 分支查询和正式身份文件读取改为后台单次在途快照。周期、手动和事件请求合并；主线程只接收已完成结果并构建一次标题，Unity descriptor 与内部 API 失败后的原生 fallback 复用该字符串，不再重复查询。保留项目名、子路径、分支、模板、构建目标与 Git 根目录格式。

查询同时排空 stdout/stderr，退出和 EOF 共享 2 秒期限；超时/取消终止进程，后台有界回收。失败显示 `?` 并保留诊断，不逐帧重试或刷屏。工程/来源与代次变化、域重载和退出取消旧请求，迟到结果不能覆盖另一工程或已释放会话。支持子目录工程、`.git` 目录/文件、detached HEAD、无 Git 和分支切换。

embedded 只取正式编辑身份，consumer 只取活动部署身份；缺失/损坏不跨模式回退。该缓存只服务标题显示，不用于资格判断。Basic.Editor 未增加 Core.Editor/UPMManager.Editor 反向引用。手动菜单、Play、projectChanged 和周期回调仍保留；macOS 原生调用也放入有界后台路径。

## 复审与验证

发布前重新阅读全部相关源码、程序集依赖和测试，未发现阻止发布的问题；生产 C# 与前轮最终验证指纹一致。详细[复审记录](../tasks/20261113-editor-title-refresh/evidence/release/review.md)、[实施与最终证据](../tasks/20261113-editor-title-refresh/README.md)。

直接相关 EditMode 8/8 通过，覆盖 descriptor 连续回调、单次在途/合并、迟到项目/代次、Dispose、身份模式/损坏、路径格式、隔离 Git 切分支/detached/`.git` 文件、失败/超时/取消和双路大量输出排空。框架最终代码经对应 Unity MCP 主动编译、域重载及新程序集核对。发布版本编译与增量回归结果另见任务 evidence/release。

在正式目标版本 0.25.5 下再次主动编译并确认域重载、新程序集和 PackageInfo/API 版本，随后同一类 **8/8 通过，0 skipped，用例耗时 4.9006 秒**。七模板实际内容/封存 hash、六份父快照/父版本指针及谱系全部一致，无模板写入。Console 无编译错误，保留两条 MCP WebSocket 断连警告。发布候选随后逐项检查源码范围、共享二进制、字体和未变技能 bundle。

保留前轮最终代码的自然周期 CPU 证据：16.107 秒、3 次查询调度，148 个主线程标题回调均小于 1ms，最大 0.3191ms；嵌套 BuildMainWindowTitle 最大 0.0475ms，不与外层相加。采样发生于框架后台 EditMode，不能替代 Windward 的 4K 前台四条件、游戏 FPS 或 GPU 验收；p95 包括无到期工作的 update 回调。

未执行全量、Player 构建、macOS、真实项目/UPM 来源切换、退出 Editor 或原生标题视觉验收。Windows fallback 已调用且不增加查询，但 Mono 未提供可用窗口标题读回值，未标为视觉验证通过。`.git` 文件使用 separate-git-dir 隔离夹具，未创建新 worktree。普通入口项目中心 46.74ms inclusive 重绘及其中 21.76ms GC 继续独立承接，不据此扩大重构。

## 发布范围与消费接续

复用已存在且干净的发布 checkout，从 v0.25.4 的 c5a2fe90b45ea5343a66f086c2663d35c6f6eac6 建立候选。只提交本轮标题实现、测试/meta、solution 项目引用、任务记录与必要的版本/发布资料；其他工作流、规则同步、字体缓存及历史任务改动留在原开发工作区。模板版本/hash/父快照/兼容声明、第三方依赖与 12 技能/34 文件公共 bundle 沿用 v0.25.4，不重建、不保存或 Bump 模板。

**Windward 本轮不升级。** 后续经独立授权通过 Ember/UPM Manager 接入，核对新包解析及编译；如扩展内容 hash 改变，先展示真实预览并独立授权绑定，再按[消费接续](../tasks/20261113-editor-title-refresh/consumer-handoff.md)做原有限范围复测。不手改消费端 manifest/lock、不改 PackageCache 或复制补丁，整体性能问题保持开放。
