# Odin 构建预处理器路径修正

日期：2026-10-17。范围为第三方Odin UPM包维护，不属于Ember运行时或platformer-2d玩法能力。保留前轮所有修改，不Bump、提交、推送或发布。

## 维护源与实证

- 当前消费基线：`https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2`，锁定Git哈希`470c394c8ad61e71ec95bf6e34d2e0dcf1a7e7e9`。
- 正确维护源：`C:/Users/wuyu/My/ember-thirdparty-upm/com.sirenix.odin-inspector`。仓库HEAD `e4b91ccb314d436d2657c0267f499c22e7f05186`，开始时工作区干净；包版本4.0.2，没有对应构建回调C#源文件，供应商以DLL提供。
- `AssemblyImportSettingsAutomation.ConfigureImportSettings`先取`SirenixAssetPaths.SirenixAssembliesPath`，用DirectoryInfo解析物理路径，再按是否位于项目目录内决定转换方式。
- MCP实测：虚拟路径`Packages/com.sirenix.odin-inspector/Assemblies/`的FullName为`Library/PackageCache/com.sirenix.odin-inspector@.../Assemblies/`。原分支裁掉项目根，生成缓存相对路径；AssetImporter.GetAtPath返回null。用虚拟包根拼回枚举相对路径则四个DLL均可找到PluginImporter。

## 修正

维护源中修改`Assemblies/Sirenix.OdinInspector.Editor.dll`及配套Portable PDB，保留.meta/GUID和包版本。`Maintenance~/Patch-OdinImportPaths.ps1`为可重复审计的Mono.Cecil维护脚本，`Maintenance~/README.md`说明范围和升级注意。

统一使用原方法已有的虚拟资产路径分支，并规范反斜杠。不改EditorOnly/AutomateBeforeBuild检查、DLL枚举、JIT/AOT判定、ApplyImportSettings或构建回调。没有关闭回调、设置全局例外、吞异常或修改Ember Manager。脚本要求原DLL SHA256及两个特定IL分支匹配，不支持的供应商版本/重复应用会拒绝；禁止PackageCache输入/输出。

原DLL SHA256 `f9008d4a0171e37d38ba398d4067d345893cdd030b2f76c3b892fb040c70c926`；修正后`a0bcad2d3e9cef986d05f6b95d0d33b9d7aff81e2c684248d4af4cde150a73e2`。Cecil逐方法比较7928个方法体，只有ConfigureImportSettings改变；原DLL未强名称签名。

## 验证安装

为验证缓存布局，通过Package Manager的add_package正式安装本机tgz：`.utmp/odin-path-fix-20261017/odin-path-fix.tgz`。新缓存`com.sirenix.odin-inspector@b7525d46477e`由UPM生成，未直接编辑PackageCache。Unity MCP触发刷新/程序集重载后确认实际OdinEditor程序集来自此缓存，Console零错误/警告。

MCP调用原ConfigureImportSettings返回成功；AutomateBeforeBuild=true、EditorOnly=false、IsInSourceCode=false，四个序列化/工具DLL的PluginImporter均非null。这只是预检查，最终以完整构建及两进程运行结果为准。此前等待重载时execute_code曾引用已被UPM移除的旧缓存程序集，该工具编译失败不算工程编译失败；重载后重新检查成功。

## 发行构建结果

经缓存包真实布局构建：Windows64、Mono、BuildOptions.None（非Development），BuildReport为Succeeded、0 errors、20 warnings；GUID `08554ba7ddbd44909682c49f5bf287f8`。输出`.utmp/odin-path-fix-20261017/player-build`。原PluginImporter错误消失。20条警告包括此前18条编译/Shader/Feel可选PostProcessing资源警告，及构建期间2条MCP WebSocket重连警告；完整消息已归档`build-messages.txt`，未屏蔽警告或误称零警告。

为了不让工程长期依赖可清理的.utmp验证包，缓存布局验证完成后，通过Package Manager将Odin切换到已修正的本地维护源`file:../../ember-thirdparty-upm/com.sirenix.odin-inspector`。这是未发布修正的本机消费接线；没有更改旧远端tag。后续共享工程需要保留兄弟目录，或在单独授权发布后换成新的固定Git版本，不能直接回旧tag并期待修正仍有效。

## 两进程运行

同一新构建，两个进程按先退出后启动顺序执行，共用独立evidence存档库：

| 阶段 | 时间 | PID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 2026-10-17 14:59:43 | 98160 | `120d1fca7640413194c58c3531de4c7c` | 16/16 | 0 |
| resume | 2026-10-17 15:00:11 | 146780 | `389e3c5925e94aaa9f964b69f1258763` | 19/19 | 0 |

两报告均PASSED、editor=false、development=false、errors=[]。正式Main→A→B→组合保存退出、B磁盘检查点续玩、世界平台和项目数据恢复、死亡恢复一次提交、相机加载/恢复/卸载均通过。本轮没有改动运行器或放宽断言。已查看新续玩主相机离屏PNG，玩家与地面可见；不冒充整窗口UI截图。

报告位于`.utmp/odin-path-fix-20261017/player-build/evidence`。SHA256：report-save.json=`128cee1c247ddb5ff97c7be8a13a2dc0f3305f80aad8cebe01adb8a315f6588a`；report-resume.json=`e55e210b3d7616f645a418ebed71c3f61db3ed9511e3e31f2a864053868cb940`。

本次只验证Windows64 Mono发行Player；没有新增IL2CPP/其他平台验证，也没有承诺其他供应商版本可使用此二进制补丁。历史18条构建警告保留为独立事项，原Odin路径错误已实测消除。

构建后Console另记录6条Unity原生Assert（GetApplication().MayUpdate / includeAutoRepaint），堆栈指向MCP execute_code中同步调用PlayerColdAcceptance.Build→BuildPipeline.BuildPlayer。它们不计入上述BuildReport的error/warning数量；不能把BuildReport零错误表述为全过程Console零诊断。原Odin异常确已消失，原回调和Player两进程均通过；该编辑器调度诊断单独保留。

## 最终安装及普通保存

本地源切换期间，Unity显示“Missing Signature”未签名包信息窗口，提供Open Package Manager和Close。读取实际窗口内容后关闭提示，没有更改签名校验设置。此前等待就绪超时没有被判为MCP不可用。重载完成后实测isCompiling=false、isUpdating=false，PackageInfo.source=Local，实际加载程序集位于上述兄弟仓库，SHA256与修正版一致。原ConfigureImportSettings在本地源再次执行成功。

此回调会正常调整PluginImporter；本地验证产生两个NoEditor DLL .meta的格式/平台设置变化，已备份并仅恢复本轮开始时的干净源内容。最终第三方仓库仅有Editor DLL、PDB及Maintenance~文件改动。构建产生的ProjectSettings批处理配置与PC_RPAsset预筛选变化也恢复本轮基线，原有工作区修改保留。

2026-10-17 15:10:15通过项目中心正式SaveTemplate普通保存1395文件；本轮新增模板差异仅LevelAcceptance.md证据说明。15:10:22复查错误0、差异0、警告1（已保存未版本封存）。FrameworkScene处于非Play且无未保存修改。校验文本归档于`.utmp/odin-path-fix-20261017/project-validation-after-save.txt`。

- 实际框架0.19.2，模板platformer-2d 0.4.1，模板frameworkVersion声明0.19.0，父base 0.8.3；Odin仍4.0.2。
- contentHash与编辑记录均为`7a2972f9d108f3c7472f3dbc90c0262c`。
- versionedContentHash仍`373437cccbe9f4ba6e93359da2c8b2fd`，parentContentHash仍`1270ce6dfcae3c01d050c09ecfd60337`。
- 无Bump、提交、推送、发布；没有直接修改Templates~或PackageCache。模板文件由正式保存生成，包缓存由UPM安装生成。

消费项目须分别接入模板能力和Odin修正；普通模板保存不包含本机第三方依赖。当前manifest/lock使用兄弟仓库本地路径，尚不能作为已发布的远端消费版本。若需追查剩余原生Assert，可在Unity中从正式构建入口执行同配置构建，对照BuildReport与Console完整堆栈；本轮没有声称这些Assert已经修复。

后续已修正验收工具的构建调度，并完成新的发行构建及两进程验收，详见[构建调度与Assert复核](07-build-scheduling-acceptance.md)。新路径0 Assert；原同步入口与显式SynchronizationContext对照也未复现，历史6条的确切触发条件仍未确定，不能倒推为已证实的MCP根因。
