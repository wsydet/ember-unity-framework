# B03、退出 AV 与共享动态字体：三项独立定位和定向回归

2026-10-28。**本轮不能报告三项全部修复完成：B03 与字体可写资源迁移路径在框架夹具通过；退出 0xC0000005 尚未解决。**
未提交、推送、升版或发布；未修改 Windward PackageCache，未升级或重部署 Windward。

## 身份、范围与保护

- 仓库 HEAD `baf06869256ebe295667875b3f266a93142a4d22`，package.json 仍 0.24.2；本报告覆盖其上未提交的本轮工作区候选。
- Unity 6000.5.4f1 / d550df8bd089，Windows64 Development、D3D11，SpritePackerMode=Disabled。
- MCP 目标为 ember-unity-framework@b23e9b768e85ed30；主动请求编译、读取本轮完成状态及已加载程序集 MVID 后才验证。
- 改动归属为框架代码、共享源配方、直接测试与文档。字体需要消费项目定向生成/引用迁移；本轮没有模板快照、保存、Bump 或全量部署。
- 仓库原有大量修改保留。四份包字体在开始时已有磁盘差异，备份位于 `.utmp/b03-20261028/fonts-before`。
  本轮诊断/旧兼容测试造成的源缓存变化依据该备份恢复，仅保留源上的 ClearDynamicDataOnBuild: 1→0；没有用 Git 还原用户字体。

## 一、B03

**已证实根因**：场景中不保存 HideAndDontSave 渲染 Sprite；Tile.OnEnable 重建自己的字典，不会让反序列化的
Tilemap 原生缓存重新请求 GetTileData。不存在既有正式自动运行时恢复 API；Editor Refresh/恢复类不能覆盖 Player。

原消费三格资源直接复制到独立框架夹具，保留 PNG、Tile、meta/GUID。原版独立 Player 两次加载均规则/fallback=null、
普通 Tile 有 Sprite；记录失败后才执行显式诊断 RefreshAllTiles，立即恢复，进程退出 0。
证据：[原版加载与刷新对照](evidence/b03-20261028/baseline-fixture/samples.txt)。

最小修复只有新增内部 `EmberTerrainRuntimeLifecycle`：BeforeSceneLoad 订阅 sceneLoaded，对本次场景含 EmberTerrainTile
的 Tilemap 一次刷新（包括 inactive）。不改变 GetTileData、规则、Sprite 生成/销毁、碰撞或业务 Scope/参与者；
不新增公开同义刷新函数，不要求 GameInit/关卡接线。准确保证为 sceneLoaded 回调完成之后、Start/首次渲染之前。

直接回归夹具为 `Assets/Game/Development/EmberRegression/B03/`：三格混合图，另建真实 Unity Prefab Variant 的15格/1轮廓实例。
该 Variant 是独立框架夹具，不是 Windward 的 AuthoringSecretWall。混合图尺寸、Anchor、quarterTurns=1/mirrorX=true 保持。

- 同一已打开 Variant Stage 和混合图连续两次真实域重载，先 GetSprite 再截图，无诊断刷新。
  探针 MVID `35563a60-f3a8-454e-8fc6-1f9a825a38db` → `dba56567-5b67-464f-ac6a-8335000cb0b4` → `51fc053a-20cb-4db7-bb9c-ab15cb2906f6`。
- 所有已占格 Sprite 非空，旧 Sprite 已释放，缓存2→2→2，源顶点/UV、格子/TileBase、颜色/矩阵/flags、Grid/Anchor、排序、Prefab来源、1个轮廓及 dirty=false 保持。
  [比较](evidence/b03-20261028/editor-reload-comparison.json)、[第二轮 Scene 图](evidence/b03-20261028/editor-stage-reload-2.png)。
- 框架 Player 首次/卸载再加载、规则/fallback/普通 Tile 和独立 Variant 的画面及正常退出分别检查。
  原始黑帧截图保留为失败，不用于可见性通过；后改用正式 URP 单相机 render request 取得实际渲染图。

## 二、退出 0xC0000005

**未证实根因、未修复。** 不预设 B03 或字体与它同根，没有修改退出清理、强杀、跳过关闭或关闭错误记录。

已只读取得 Windows Application Error 1000 和 WER 签名。历史两次故障均为 UnityPlayer.dll + **0x1dd2e9**。
本机安装的 Windows64 Development Mono UnityPlayer.dll 与失败产物的 DLL SHA256 相同：
`0FCF6D820362361DEB538A02142AAFE6DD9343EA3BEF90078E837E5E45123B82`。
用匹配的本机 PDB/DbgHelp 解析到 **ExternalGPUProfiler::GetGameViewWindowHandle + 0x9**。
这定位了故障符号，不能单凭此推出调用者、空指针来源、引擎缺陷或外部软件责任；现存 WER 没有保留转储。
[Windows 事件](evidence/b03-20261028/windows-crash-events.json)、[符号及哈希](evidence/b03-20261028/native-symbol.json)。

同一个未改动 Windward.exe SHA256 为 `3F0C17085D75EFA926E91C6097DB4E05532B5986F4E3F7927E6A6413BC8C35F2`。
EXE是Unity启动壳，这个哈希本身不能区分项目脚本版本；旧/新关键Managed DLL和场景载荷另存于
[Player载荷身份](evidence/b03-20261028/player-payload-identity.json)。
本轮低完整性沙箱与普通 Windows 权限的隔离运行均实际移动约6.14、跳高约1.80、真实秘密墙触发/淡出、返回Menu、业务错误为空，退出码0。
[普通权限对照](evidence/b03-20261028/original-medium/summary.json)、[退出码](evidence/b03-20261028/original-medium/process.json)。
原始失败的两次 -1073741819 回执保留，新的退出0只表明未稳定复现，不能作为修复验收。
首次本轮启动等待不足、未送达输入命令的尝试也保留，不计为有效输入/退出对照。

下一步需要在能再次触发历史崩溃的会话保留原生崩溃转储/调用栈，核对故障时的图形分析连接与原生调用者。
不自动变更安全软件、驱动、图形 API 或 Unity 版本，不用猜测性的延时/立即退出补丁掩盖问题。

## 三、共享动态字体

**已证实机制**：包内动态 TMP_FontAsset 被直接用作可变字形/Atlas 状态。对源补“验”后字形0→1、Atlas宽1→1024；
Font Asset dirty 仍 false，**Atlas 子资源 dirty 变 true**，材质 dirty=false，当次源文件哈希未变。
已捕获 TMP RegisterResourceForUpdate 调用事件/堆栈。[实测](evidence/b03-20261028/font-source-dirty-probe.json)。
随后对这个已标脏源执行 Force Import，Unity 实际将“验”及1024² Atlas写入包源文件，文件从46,508字节变为2,144,192字节。
这给出了从TMP修改/标脏到AssetDatabase导入持久化的负对照，不是只推测误报。
[落盘回执](evidence/b03-20261028/font-source-probe-after-import.json)。首次仅用Import恢复并不充分；
检测到源树有1处差异后，按本轮开始备份精确恢复该源（仅保留构建清理标志差异），再重新验证dirty=false/Atlas宽1/字形0。
这一负对照后的中间构建不计入源资产保护通过，之后另做恢复基线后的构建。
所以不能按 Font Asset dirty=false 把警告当成误报。TMP_PreBuildProcessor 又会遍历动态字体并清空启用了
ClearDynamicDataOnBuild 的字形/Atlas；旧清空实验单独留存。没有取得原历史警告的完整落盘调用栈，不宣称已逐条还原历史写盘时序。

最小候选修复：

1. 四份包源只将 ClearDynamicDataOnBuild 关闭，保留 Dynamic、源 TTF、字形能力及所有原始引用/配方；避免全工程预处理清空包源。
2. 新增 Editor-only `EmberWritableFonts.EnsureProjectFonts`，接入既有 CreateProjectCatalog/SaveAndGenerate。
   默认在 `Assets/GameResource/Resources/UI/Common/Fonts/EmberGenerated/<源GUID>/<原文件名>` 共享一份字体，复制 Atlas/材质并递归重定向 fallback/字重。
   不复制 TTF，不按 UI 复制完整字库，已有项目生成字体不覆盖。
3. `MakePrefabsWritable(folder)` 带 Prefab/meta 备份，只迁移指定目录的字体/材质引用；组件 FileID、Binding/InputField 引用及文本/版式保持。
   自定义材质保留参数，并使用项目 Atlas。
4. TMPEx 在 TMP Awake/OnEnable 准备字形前选择映射字体。相同字体保留原自定义材质；固定字体遵循其明确引用。

项目生成字体仍保持动态补字、Multi Atlas 及 TMP 原有构建清理；运行内存由 Unity/TMP 资源生命周期所有，
没有新建字体渲染器或并行缓存管理系统。包源不能继续作为动态显示目标。
**只升级代码/关闭源清空不足以完成旧工程修复**：必须迁移项目库及直接源引用。无项目库、场景内固定文本、TMP Settings 全局 fallback 和自定义字体调用方需定向核对。

框架验证：三种中文字体及 NotoSansSymbols2 的所有权、动态补字、共享 fallback、幂等生成、固定字体 Prefab/FileID/InputField/材质参数通过。
Editor Play/Stop 与卸载再加载均缺字0；▶实际走项目 NotoSansSymbols2，三种中文字形/样式已检查实际图。
资源 Force Import 后四份项目字体均重新动态补字成功，材质/纹理属于项目且绑定一致。
[资源重载](evidence/b03-20261028/font-owned-resource-reload.json)。

从保护基线开始覆盖 Editor Play/Stop、Windows构建、Player 与资源重载：包源四份哈希不变，font/atlas/material dirty状态另有回执，
构建0错误0警告。源字形诊断是单独负对照，结束后立即恢复源状态，不混入保护验收时间段。
[哈希比较](evidence/b03-20261028/font-protected-run-comparison.json)、[字体状态](evidence/b03-20261028/font-after-editor-play.json)。

## 最小改动索引

| 文件 | 改动 |
| --- | --- |
| Packages/com.ember/LevelAuthoring/Runtime/EmberTerrainRuntimeLifecycle.cs | SceneManager加载边界恢复；不改Sprite生成/清理 |
| Packages/com.ember/UIExtension/Editor/UIExt/EmberWritableFonts.cs | 项目可写字体生成与指定Prefab目录迁移 |
| Packages/com.ember/UIExtension/Editor/UIExt/EmberFontSkinWindow.cs | 既有创建/保存生成入口各增加一次生成器调用 |
| Packages/com.ember/UIExtension/Runtime/UIExt/TMPEx.cs | 字体选择提前至TMP准备之前，相同字体保留自定义材质 |
| Packages/com.ember/SharedAssets/Fonts 下四份 SDF.asset | 相对本轮开始仅关闭源上的构建动态清理 |
| Packages/com.ember/Tests/EditMode/EmberWritableFontsTests.cs | 2项直接生成/所有权/字形/Prefab/材质回归 |
| Assets/Game/Development/EmberRegression/B03 | 独立混合图、原生Variant、字体及显式运行夹具，不登记正式构建路线 |
| 包内字体/B02/B03文档与本记录 | API、迁移、支持边界和各自验收 |

## 测试、画面、失败与消费端复验

最终串行定向 EditMode **9/9**（非全量）：EmberTerrainCacheLifecycleTests、EmberFontSkinEditTests、EmberWritableFontsTests。
[完整结果](evidence/b03-20261028/tests-final.json)。首轮7/9及后续符号重复添加断言失败均保留；
Unity原生纹理用原生身份比较，避免不同托管包装误判；TMP 对已存在字形的重复 TryAddCharacters 返回false，改为先查已有字形再验新增。

Editor Play 第一次因夹具不在全局 build list 失败；修正测试夹具的 Editor 专用加载入口，不修改正式 build list。
黑帧截图、超时尝试、测试失败及原消费失败均保留，不用 completed/READY 代替图像或进程退出码。
本轮没有运行全项目测试、SpriteAtlas打包、重做编辑器/字体系统/模板架构或新增玩法。

最终覆盖所有已占格的回执：

| 验收 | 实际结果 |
| --- | --- |
| 最终Windows构建 | Development、D3D11；42.22秒、236.90MB，0错误0警告；[构建](evidence/b03-20261028/build-protected-final.json) |
| Player首次及卸载再加载 | 3格混合图+15格Variant，共36次已占格Sprite采样，null=0；[采样汇总](evidence/b03-20261028/protected-player-all-cells/summary.json) |
| Player画面 | 实际渲染图已检查，命中规则/fallback/普通Tile/15格Variant及三种字体可见；[首次图](evidence/b03-20261028/protected-player-all-cells/cycle-0.png) |
| Player字形 | 两轮中文与真实Symbols2 fallback，缺字0；[字体回执](evidence/b03-20261028/protected-player-all-cells/fonts.txt) |
| Player退出 | 正常进程退出0；[进程](evidence/b03-20261028/protected-player-all-cells/process.json)。这是框架夹具结果，不能关闭Windward历史AV |
| 最终保护基线 | 恢复后的构建/Player前后四源资产哈希相同；[比较](evidence/b03-20261028/font-all-cells-build-comparison.json) |
| 字体完整源树 | 对照开始时备份共34文件，除四资产预期标志修改外0差异（包括TTF/meta）；[源树](evidence/b03-20261028/font-tree-protection-final.json) |
| Editor Play/Stop | 所有36个已占格Sprite非空，中文/符号正常，无本轮错误/警告回执；[日志](evidence/b03-20261028/editor-play-all-cells/samples.txt) |
| 清理后Editor | FrameworkScene单场景、dirty=false、非Play/编译/更新、scriptCompilationFailed=false、playModeStartScene恢复null；四源Font/Atlas/材质dirty全false；[状态](evidence/b03-20261028/final-editor-state.json) |

单独负对照导致的中间1处源树变化回执保留于 `font-tree-protection.json`，不是最终保护通过结果。

Windward 后续应在明确交付后通过正式 UPM Manager 升级；本轮未发布，尚无可安装新版本。
保持 platformer-2d0.13.0 部署身份，只生成/迁移字体项目资源和必要引用，无需重部署：

1. 保留现有 B02Candidate 与 AuthoringSecretWall Variant，连续两次真实域重载，先读 GetSprite/截图再做诊断刷新；按原记录比较旧 Sprite、UV/顶点与语义字段。
2. 项目字体皮肤窗口保存生成，核对三个皮肤/槽位和项目字体/fallback引用；对实际 UI Prefab 目录定向迁移固定字体/普通TMP，审阅备份和具体diff。
3. 按原11场景 Windows64 Development D3D11 构建，SpritePackerMode继续Disabled、实验Atlas排除；比较源 .asset/TTF/meta哈希及Font/Atlas/材质dirty。
4. 用新隔离根从原 B02BuildBoot/正式Scope/参与者进入，首次及重入/卸载再加载时核对混合图和真实秘密墙；实际输入、跳跃、秘密墙触发/淡出、返回Menu分别留证。
5. 显式正常退出并记录真实退出码；若再次0xC0000005，保留转储、Player日志和Windows事件。**退出未定位修复前不整体签收。**

本轮框架夹具不替代 Windward 已安装/通过。B01/B02既有消费通过记录保留，未在本轮重新执行其全部制作/保存/重开链路。

后续独立原生调查：见 [Windows退出窗口查询与清理时序记录](exit-native-20261028.md)。已取得有效dump、无断点原生栈和无调试器成对复现，定位到无障碍StaticDestroy在ScreenManager清空后查询窗口；仍未修复或签收退出。该后续证据不将B03、字体与AV合并同根，也不将本记录的退出0当修复结果。
