# Windows Player 退出 AV：原生窗口查询与清理顺序调查

日期：2026-10-28。承接 [B03／退出／字体独立记录](b03-player-exit-font-20261028.md)。

**状态：退出问题未修复、未验收通过。本轮取得原生调用栈、有效 dump 和两组成对稳定复现。没有修改框架运行代码或消费项目。**

## 已证实的故障路径

在实际 6000.5.4f1 Windows64 Development Mono、D3D11 二进制中，ScreenManager 已完成析构且其全局指针为零，随后 `RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy` 又查询窗口。查询函数未判空，读取地址 `0x138`，产生 `0xC0000005`。

这不是从退出码或某个函数名称猜测：本轮直接记录到 ScreenManager 两层析构入口、清空后的全局指针、迟到查询入口、首次及二次机会 AV 的寄存器和完整原生栈。查询在 Player 主线程的原生静态清理中发生。单次外部 UI Automation 查询结束、查询进程退出后，仍能触发该路径；无需退出期间继续轮询。

当前证据将**这条已复现路径**定位到 UnityPlayer 的无障碍 provider 清理与 ScreenManager 生命周期顺序。没有据此宣称全部历史 AV 都同根、所有 Unity 版本都有问题，或给环境软件归责。历史两次 Windward 失败中究竟哪个客户端首次请求 UIA provider，现有证据不能追溯。

## 符号、指令和原生栈

[符号清单](evidence/exit-native-20261028/symbol-inventory.json)确认两个符号共用 RVA `0x1dd2e0`：

- `ExternalGPUProfiler::GetGameViewWindowHandle`
- `PlatformAccessibilityTestUtilities::GetWindow`

因此原有 `ExternalGPUProfiler::GetGameViewWindowHandle + 0x9` 标签不能单独证明 GPU Profiler 或 RenderDoc 是调用者。[静态相对 CALL 清单](evidence/exit-native-20261028/static-callers.json)还包含 GetProviderForUIA、NotifyUIADestroy 和 RenderDoc 的不同调用点；本轮实际栈的返回地址对应无障碍 StaticDestroy。

```asm
; UnityPlayer.dll，RVA 0x1dd2e0
sub rsp, 28h
call GetScreenManager          ; RVA 0x2304540
mov rax, qword ptr [rax+138h]  ; RVA 0x1dd2e9，实际故障
add rsp, 28h
ret
```

GetScreenManager 从 RVA `0x52185c0` 读取全局指针。AV 时 `RAX=0`，异常参数为 `[0, 0x138]`，即读取空指针偏移。不是从 Sprite 地址释放记录推导出的 use-after-free。

Windward 原构建、不设软件断点的二次机会 AV，PID 73480、主线程 TID 75056：

```text
UnityPlayer!ExternalGPUProfiler::GetGameViewWindowHandle + 0x9
  [与 PlatformAccessibilityTestUtilities::GetWindow 地址相同]
UnityPlayer!RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy + 0x3b
UnityPlayer!RegisterRuntimeInitializeAndCleanup::ExecuteCleanup + 0xed
UnityPlayer!RuntimeCleanup + 0x27
UnityPlayer!UnityMainImpl + 0x12e7
UnityPlayer!UnityMain + 0xb
<Player 入口，未解析符号>
```

[原始栈、寄存器和日志阶段](evidence/exit-native-20261028/debugger-uia-once-dump/capture-1.json)。当时正式业务关闭已请求、输入系统 Shutdown、PlayerConnection Cleanup 均已记录。

原始 JSON 顶部 PC 重复是捕获器先写当前 PC、StackWalk64 再返回首帧所致，不代表递归。早期辅助器在无法解析最后一帧时采用“最近较低模块基址”回退，可能给最后一帧错误的 gdi32 模块标签／超范围 RVA；不能采用这一标签。本报告仅采用 DbgHelp 成功解析的 UnityPlayer 帧。交付脚本已给模块回退增加 SizeOfImage 边界；未改写历史回执。

## 原生时序证据

Windward 单次 UIA + 原生断点观察，所有关键事件均在主线程 TID 70844，时间为启动后的秒数：

| 时间 | 事件 | ScreenManager 全局指针 |
| --- | --- | --- |
| 40.068～40.159 | UIA GetProviderForUIA → PlayerMainWndProc 路径三次查询 | `0x221010c2490`，HWND有效 |
| 40.277 | 下发原有输入／返回／退出脚本 | — |
| 45.107 | ScreenManagerWin 析构入口 | `0x221010c2490` |
| 45.111 | ScreenManager 析构入口 | `0x221010c2490` |
| 45.173 | DestroyGfxDevice 入口 | `0` |
| 45.530 | StaticDestroy 再次调用窗口 getter | `0` |
| 45.538／45.541 | 首次／二次机会 AV，读取 `0x138` | `0` |
| 50.602 | 原生进程结束 | 退出 `0xC0000005` |

[完整事件流](evidence/exit-native-20261028/debugger-uia-once-trace/native-events.jsonl)。同样断点配置、未注入 UIA 查询的对照没有迟到 getter 命中，退出 0；[对照事件流](evidence/exit-native-20261028/debugger-trace-1/native-events.jsonl)。这两个退出0仅是对照，不能作为修复结果。

独立空闲框架 Player 也记录到同一顺序、同一 getter／StaticDestroy 栈和读取 `0x138`，主线程 TID 81908；[空闲原生栈](evidence/exit-native-20261028/idle-native-uia-once/capture-8.json)。正常 WM_CLOSE 仅一次，无业务异步退出重试。

## 成对复现与边界

使用本来就存在的 `Builds/B03/Protected/EmberRegression.exe`，不传 `-ember-regression-output`，使 Boot 中唯一的回归组件 Start 直接 yield break。不进入 CacheFixture，不创建动态 Sprite、截图 RenderTexture 或字体 UI，不启动 GameLauncher／Windward Flow。包级 RuntimeInitializeOnLoad 和第三方模块仍存在，因此这是**同运行环境的空闲框架 Player 对照，不是完全去框架的纯 Unity 空工程**。

四次均串行、D3D11、960×540窗口、未附加调试器、未截图，最后只通过正常 WM_CLOSE 请求关闭：

| 用例 | 主动 UIA 查询 | 查询进程在关闭前结束 | 实际退出 |
| --- | --- | --- | --- |
| [idle-visible-no-uia-1](evidence/exit-native-20261028/idle-visible-no-uia-1/process.json) | 无 | 不适用 | `0` |
| [idle-visible-uia-once-1](evidence/exit-native-20261028/idle-visible-uia-once-1/process.json) | 一次成功 | 是 | `0xC0000005` |
| [idle-visible-no-uia-2](evidence/exit-native-20261028/idle-visible-no-uia-2/process.json) | 无 | 不适用 | `0` |
| [idle-visible-uia-once-2](evidence/exit-native-20261028/idle-visible-uia-once-2/process.json) | 一次成功 | 是 | `0xC0000005` |

“无”只指本诊断未主动发起 UIA 查询，不能保证系统没有其他客户端。两次故障的 [Windows Application Error 1000](evidence/exit-native-20261028/idle-application-errors.json)均记录 `UnityPlayer.dll`、`0xc0000005`、偏移 `0x1dd2e9`，与调试器下结果相符。

UIA worker 使用标准 .NET UIAutomation：目标 PID 枚举 HWND，`AutomationElement.FromHandle`、读取 Name、`FindAll(Children, TrueCondition)`。一次“查询”是这一组标准只读访问，不能进一步断言其中某个单独方法是唯一充分条件。保存了真实窗口名／children数，不以 worker 启动成功当查询成功；见每例 `uia-events.jsonl`。

早期launch元数据中的 `uia:false` 仅记录连续轮询开关，没有包含 `--uia-once`；单次查询以 `uia-start`、真实 `uia-events.jsonl` 和进程回执为准。空闲原生trace的命令还带有一个多余的 `-windward-authoring-probe` 参数，框架夹具不处理该参数；四次无调试器对照均没有此参数且得到同样故障偏移。交付工具已修正这两处诊断元数据／命令组装问题，保留原始记录。

## 有效 dump 与失败尝试

Windward 无软件断点运行保留有效首次／二次机会 dump：

- `exception-0.dmp`（本机诊断文件，不进入发布提交），68,053,693 bytes。
- `exception-1.dmp`（本机诊断文件，不进入发布提交），68,054,733 bytes。
- [dump 独立结构校验和 SHA256](evidence/exit-native-20261028/debugger-uia-once-dump/dump-verification.json)：MDMP、17 streams、异常线程75056、AV读0x138、RAX=0、RIP符合。

空闲框架 Player 另有 `exception-8.dmp`（本机诊断文件，不进入发布提交）（68,052,687 bytes）和[独立校验](evidence/exit-native-20261028/idle-native-uia-once/dump-verification.json)，得到相同异常、RIP和空指针状态。

全部尝试见 [matrix.json](evidence/exit-native-20261028/matrix.json)，以下不能当通过：

- `debugger-uia-1`、`debugger-uia-once-no-breakpoints`：业务回执完成、记录过 Application.Quit，但100秒窗口内未观察到进程退出；两例是超时，不是AV复现，也不是退出通过。调试宿主未主动杀进程；超时脱离后生命周期未完整观察，不能宣称其最终自然退出状态。
- `idle-no-uia-1`：隐藏窗口首次控制尝试在20秒准备窗口内没有枚举到 HWND，未发送WM_CLOSE、没有退出码；不是有效退出对照。后续显式等待窗口、采用正常可见窗口，才得到四次完整成对结果。
- `debugger-uia-once-trace` 最早 dump 尝试文件为0 bytes：原生 API 结构没有按SDK的 pack(4)布局，写入失败。修正外部采集脚本后才产生上述有效 dump。早期 JSON 调用栈与错误回执保留，不把0字节文件称为有效转储。
- Windward `debugger-uia-once-dump` 等待首次退出15秒后仍停留在已请求Quit状态，记录并补发一次正常WM_CLOSE，之后才进入原生销毁并AV；[进程回执](evidence/exit-native-20261028/debugger-uia-once-dump/process.json)标记 `normalCloseRetry=true`。它证明无软件断点仍可捕获AV，**不证明首次单一业务退出链路通过**。无此重试的Windward trace和空闲框架对照也已复现AV。

未使用 TerminateProcess、立即退出、跳过清理、关闭错误日志或吞掉AV。调试器将非自身断点的异常按 DBG_EXCEPTION_NOT_HANDLED 转交；自然二次机会失败后接收 EXIT_PROCESS_DEBUG_EVENT。

## 与 B03、字体和异步关闭的关系

- 当前AV的直接故障对象是已经清空的原生 ScreenManager；空闲 Boot 未运行规则格或字体动态测试仍复现。没有证据将其与B03或共享字体改写合并同根。
- 实际迟到查询来自原生 StaticDestroy，不是本次退出后仍存活的 UIA polling worker。不能把“停止截图／停止自动化”作为修复；provider 在之前被创建后仍参与退出销毁。
- 未注入查询的正常退出与注入查询后的AV配对、并有相同栈和全局指针时序，支持本版本原生无障碍清理顺序存在问题；不支持归因显卡驱动、截图工具、RenderDoc、输入系统或某个特定辅助软件。
- Windward 首次退出有时未进入最终原生清理，另有 wantsToQuit／异步关闭交错的疑点。尚未抓到该停留的完整托管与原生等待栈，不能把它断言为框架缺陷或作为已解释的AV根因。保留独立阻断。
- 目前没有可用于本仓库的已验证正式API修复。未加原生内存补丁、未禁用无障碍、未变更引擎或窗口清理顺序。若要交付修复，仍需独立纯Unity最小工程或上游受支持修复的对照，并在保持UIA功能的条件下重复本矩阵及正式退出验收。

## 身份、交付与复验

框架HEAD仍为 `baf06869256ebe295667875b3f266a93142a4d22`，package仍0.24.2；工作区含前轮未发布候选及其他既有改动，不能用HEAD代替工作区源码身份。Unity `6000.5.4f1 (d550df8bd089)`。本轮复用了既有Player，不重新编译、构建或替换它们。

[identity.json](evidence/exit-native-20261028/identity.json)保存原Windward、框架和安装目录的UnityPlayer.dll哈希：三份相同 `0fcf6d820362361deb538a02142aafe6dd9343ea3bef90078e837e5e45123b82`。CodeView GUID `fa546238-6fe9-44fd-8c1a-7376b3667b2a`，age1；匹配本机对应Development Mono PDB。也记录托管载荷、场景数据与消费端manifest／lock／模板身份当前哈希。通用Player exe stub的相同哈希不能用来证明托管内容相同。本轮未写Windward工程或PackageCache，原构建只读运行，持久化／日志均使用框架仓库独立根；活动模板仍platformer-2d0.13.0。

本轮新增内容仅为此记录、原生诊断证据和 [可审查诊断脚本](evidence/exit-native-20261028/tools/)。无C#／程序集／场景／资源修改，因此没有制造一次新Unity编译，也没有用既有编译结果冒充本轮原生调查。未运行全量测试、未升级／重部署Windward、未提交／推送／发布。

定向复验步骤（保持原二进制及设置；从框架仓库根执行，Python和Windows PowerShell需可用）：

1. 审查 `tools/plain_control.py` 与 `uia_probe.ps1`：只启动现有框架Player、查询目标窗口、等待UIA进程结束并发WM_CLOSE。每个证据目录必须不存在以保护旧结果。运行脚本会按无查询／单次查询／无查询／单次查询串行执行，保留每例Player日志和真实退出码。
2. 需原生栈时运行 `python docs/implementation/evidence/exit-native-20261028/tools/debug_player.py --name <新的证据目录名> --idle-framework --uia-once --trace-window --timeout 100`。省略 `--trace-window` 可只捕获AV，不设置软件断点；仍会使用操作系统启动调试事件。
3. Windward原构建可省略 `--idle-framework`，使用交付 `input-command.json` 的原有正式输入／返回／退出链路。保留所有超时及 `normalCloseRetry` 信息，不把第二次关闭请求当首次退出验收。
4. 使用 `verify_dumps.py <证据目录>` 验证有效MDMP的异常流、寄存器与哈希。WinDbg打开二次机会dump后，用匹配PDB检查 `.ecxr`、`kv`、故障指令及ScreenManager全局指针；本报告没有声称运行过未安装的WinDbg。
5. 真正修复候选须在单次UIA读取、查询端已退出、持续UIA访问和无主动查询三种条件下重测正常退出；再分别验收Windward真实输入／机关／返回菜单／单一正常退出，不能用空闲Player或本轮退出0替代。

当前证据支持“已定位本版本中的一条稳定原生故障路径”，不支持“退出问题已修复”。
