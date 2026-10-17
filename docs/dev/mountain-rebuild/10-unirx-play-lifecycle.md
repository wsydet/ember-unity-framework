# UniRx 关闭域重载的播放生命周期修正

日期：2026-10-17。承接 [构建警告维护](09-build-warning-fixes.md) 中发现的编辑器播放检测问题。

## 维护归属与实现

维护源为仓库内 embedded `Packages/com.neuecc.unirx`（7.1.0），不是 PackageCache，也不是 Ember Manager 或 platformer-2d 模板。保留此前 Unity 6.5 查找 API 修正。

旧 `ScenePlaybackDetector` 只在 DidReloadScripts 中将播放状态置为 true；关闭域重载后，Initialize 被提前拦截，Post 则误投编辑器队列。另外 Dispatcher 的 isQuitting 会从上一轮退出保留到下一轮，单独修正检测仍不足以恢复初始化。

- `ScenePlaybackDetector.cs`：编辑器状态事件及脚本重载后读取真实状态，移除全局 EditorPrefs 中的 AboutToStartScene 依赖；供工作线程读取的缓存使用 volatile，getter 不调用 UnityEditor API。退出播放的销毁阶段仍保留运行时路由，到 EnteredEditMode 才置 false。
- `MainThreadDispatcher.cs`：仅 Editor 的 SubsystemRegistration 在每次 Play 开始重置 instance、initialized、isQuitting、主线程标记，并在场景 Awake 前设置播放检测。cullingMode 和既有公开 API 保留；发行 Player 不编译该复位入口。

使用方仍调用 `MainThreadDispatcher.Initialize()` 和 `MainThreadDispatcher.Post(action, state)`。工作线程使用前，应先在主线程完成 Initialize；本修正没有新增业务 API。

## 编辑器实测

Unity 6000.5.4f1；enterPlayModeOptionsEnabled=true，options=DisableDomainReload（场景重载仍开启）。在临时默认场景（Camera、Directional Light）连续三轮进入/退出 Play，每轮均检查：

1. 检测为 true，Initialize 后恰好一个 Dispatcher，IsInitialized=true、isQuitting=false。
2. 禁用 Dispatcher 的 Update 后，从真实工作线程 Post。跨 MCP 调用、经过编辑器更新后计数仍为 0；恢复 Update 后计数为 1，证明没有误走 EditorThreadDispatcher。
3. 临时设 cullingMode=Disabled，新增两个重复实例；调用 CullAllExcessDispatchers 后，跨帧从 3 个变为 1 个，并恢复原 cullingMode。供应商重复实例警告照常保留。
4. 销毁唯一实例后计数 0、IsInitialized=false；再次 Initialize 恢复为 1。
5. 退出后检测 false、对象计数 0；编辑模式 Initialize 不创建对象。退出时 quit=true，下一轮实测已复位为 false。

三轮后的 Console 错误查询为 0。临时场景未保存，已恢复 FrameworkScene。原始结果及五段可复跑 MCP 探针位于 `.utmp/unirx-lifecycle-20261017/play-probe.txt` 与 `probe*.cs.txt`。按 Start→Cull→Check→Reinit→退出→Exit 的顺序执行，各段之间让 Unity 经过一帧；重复三次。

没有验证关闭场景重载、播放中重新编译或旧 Unity 版本，不将上述结果扩大到这些配置。

## 发行回归与交付状态

本轮 CleanBuildCache 非增量 Windows64 Mono 发行构建：Succeeded，0错误、0警告、0 Assert；GUID `897d925e69674192b62391cf81209b5f`，options原始值128，backend=Mono2x，耗时1分47.230秒。枚举ToString中的Il2CPP等别名不是实际后端，后端已单独查询。证据目录 `.utmp/unirx-lifecycle-20261017/player`，包含build-report、build-verification、build-entry-stack、build-diagnostics、build-messages。

| 阶段 | 时间 | PID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 15:54:43 | 67168 | `6a8c7720257f46f2aed008bcaf7d5555` | 16/16 | 0 |
| resume | 15:55:22 | 197436 | `ea1ca5c926bd425ab7bcdd13f9dc82d1` | 19/19 | 0 |

保存进程完全退出后才启动续玩进程。两报告均PASSED、editor=false、development=false、errors=[]，使用同一新buildGuid。正式Main→A→B→组合保存退出、磁盘合法检查点续玩（非空中位置）、项目先于玩家启动恢复、世界恢复、死亡单次提交、相机绑定/warp/Brain输出及卸载释放均通过；未修改既有验收器与断言。

- `evidence/report-save.json` SHA256：`cfe946e8e4823c0b216f016a400ee3630bd391a289194e0b88627fb8235920f2`。
- `evidence/report-resume.json` SHA256：`9eb82e37650ed957921d95d297672d11dd796c69d9f4e24c10e0e41e27d6b141`。

15:55:15项目中心检查0错误、0差异、1条既有已保存未封存提示，原文在同级project-validation.txt。本轮未改模板维护内容，不重复普通保存。platformer-2d仍0.4.1，最后普通保存15:27:48，contentHash `b0d2b8954453f5eefeb8f094854c24eb`，versionedContentHash `373437cccbe9f4ba6e93359da2c8b2fd`，父base0.8.3及父hash不变。Ember实际0.19.2，模板声明frameworkVersion0.19.0，UniRx7.1.0均未改版本。

构建自动改写的URP预筛选与Odin两个NoEditor meta已恢复本轮基线，项目设置hash也与基线一致；保留其他已有修改。最终Console错误查询0，FrameworkScene已恢复。未Bump、提交、推送或发布；普通模板保存不会分发此embedded包补丁，其他消费项目仅安装注册表UniRx7.1.0不会获得修正，后续需另行按包维护流程接入/发布。
