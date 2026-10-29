# Ember 0.25.0 发布准备

此页保留准备过程与验证证据。全部模板已封存，提交/tag/推送结果以 annotated `v0.25.0` 的实际远端提交为准。用户已授权一并发布当前框架及所有已保存模板变更；模板切换保留了项目中心人工弹窗确认。

## 范围与版本

从已发布 `0.24.3` 推进到 `0.25.0`。此次含公开可组合扩展、FSM 蓝图能力登记与版本门禁、关卡工具门禁、通用房间图适配工具，以及 topdown-2d、lane-defense-2d 两个新增玩法模板，采用 minor 版本。

第三方依赖沿用上一版声明；没有授权升级或重新部署 Windward。动态字体的本地字形缓存、模板切换产生的开发副本差异和已安装模板 Skill 副本不应自动并入包发布。

## 已完成

- `scripts/upm-bump-version.ps1 -Version 0.25.0 -Check` 与正式执行完成；包源码版本为 `0.25.0`。
- Unity 实际缓存版本已确认 `0.25.0`。显式请求编译后曾观察到 `isCompiling=true`，随后 `isCompiling=false/isUpdating=false`，Core.Editor 加载 MVID 为 `7f83ae6b-847b-4260-afe8-ca9c7c799d36`。
- Console 未见脚本编译错误，保留一次 NuGetForUnity 访问 `package.openupm.com` 的 ECONNRESET 解析错误，不能将本轮描述为依赖联网解析全部成功。
- 发布前完整模板备份位于 `Library/EmberReleasePreparation/0.25.0-before`；11,849 个模板文件逐文件 SHA256 比较，无遗漏、新增或内容差异。另保留正式编辑记录和 Git 状态；该备份不替代项目中心事务。
- 根模板 `base 0.10.1` 内容与封存 hash 为 `e9d789311cad169a19b6a76331a2877a`，已通过项目中心声明框架兼容版本 `0.25.0`。
- 发布元数据工具的 6 项隔离 Python 用例通过；初次运行被沙箱临时目录权限阻止，改用仓库 Library 下专用临时目录后通过。此结论不是 Unity 测试结果。
- 全部六个派生模板已通过 `ApplyParentSync` 正式同步至 base `0.10.1` / `e9d789311cad169a19b6a76331a2877a`，兼容声明统一 `0.25.0`，本次同步没有改变子模板内容。之后通过 MCP 计算七个实际内容 hash、六个 ParentSnapshot hash，均与对应 metadata 一致，`ValidateTemplateGraph()` 零问题。
- 发布相关增量测试最初 29 项中 26 项通过、3 项失败：两个测试夹具依赖当前活动模板已启用 FSM 蓝图。改为显式建立测试配置并逐字节恢复原配置后，主动请求编译，确认新测试程序集 MVID `ab900049-bab4-45b4-9f67-ff746d5c6a62` 及新 SetUp/备份字段已经加载，再读取 Console；相关 5 项复验全部通过。初始 job `21ed7e55d9f24adaaf5bae9dc65e331c`，复验 job `4fe189f52dce4789b271950524fdedfd`；两次范围有重叠，不能把它们简单相加为独立测试数。
- lane-defense-2d 的默认扩展声明已在活动 Assets 生成并绑定 `0.1.1`，正式普通保存 795 文件，再通过项目中心 Bump 封存为 `0.1.1`，实际/封存 hash 均为 `608dc159f0dd54fc09455ea2e4d0529e`。编辑记录匹配且未过期，配置只启用基础设施、没有问题；维护工程覆盖配置已通过正式 Save 绑定新版本。

## 当前续接点

用户确认 platformer 加载后，已通过 MCP 核对正式身份为 `platformer-2d 0.13.0`、编辑记录未过期、编辑器空闲。活动 Assets 中经 `CreateConfiguration` 生成扩展绑定，两份专属文档通过正式 API 核对内容指纹后对齐 owner 版本；正式保存 1955 文件、Bump 到 **0.13.1**，实际/封存 hash 为 `466b7434d6dc8bcf6fa55141b32ccc8f`。组合零问题、三项文档入口可用。显式编译后核对加载的 Player.Editor 程序集与新版配置，再读 Console；模板隔离回归 **4/4** 通过，[原始回执](release-0.25.0-platformer-tests.json)。

source3d-2p5d、ui-game、visual-novel 的已保存内容已通过项目中心 Bump 分别封存为 **0.6.1、0.2.1、0.22.2**，hash 分别为 `9be0d1035d4396e096a3eaa820765eed`、`43030af8571b29f5c1d2d0631faa1733`、`437a3552583df64cf2ab3d1c26083864`。三者没有启用的可选扩展，正式求解均仅基础设施生效且零问题；保留已保存的空声明，不加载重写内容。首次启用时应在中心审阅保存组合，签收新版 owner/源码身份。

用户进一步确认 topdown 加载后，已先核对正式编辑身份 `topdown-2d 0.1.0`、编辑副本有效及空闲状态。在活动 Assets 通过正式 API 生成扩展绑定并核对两份文档指纹，对齐 `0.1.1` 后普通保存1061文件，正式 Bump为 **0.1.1**，实际/封存 hash **406816c22a27be1fd281be126732d93e**。新版绑定后的隔离回归 **2/2** 通过，见[原始回执](release-0.25.0-topdown-tests.json)。显式编译后新 Player.Editor MVID 为 `f74763dc-c9da-4170-a807-761b0fbbc8b6`；编辑器空闲，正式记录匹配且未过期，扩展零问题，三项文档入口可用。

最终通过 MCP 分批核对全部七个实际内容/封存 hash、六份父快照及父指针，均一致，谱系零问题：[身份](release-0.25.0-unity-identity.json)、[hash A](release-0.25.0-hashes-a.json)、[hash B](release-0.25.0-hashes-b.json)。没有重复加载、绕过人工弹窗或直接编辑模板快照。

正式 release/manifest 0.25.0 已由技能工具先预览再生成，57项依赖沿用0.24.3。选择性暂存清单排除了开发 Assets、ProjectSettings、已安装模板 Skill 副本、产品策划及共享字体运行缓存；保留其工作区改动。候选 index 的共享二进制/字体检查通过，四份 TTF 和两种 autocrlf 干净检出通过，相关公共 C# 的 `git diff --cached --check` 无问题。正式 bundle 检查需在提交后的干净候选环境完成，不删除当前工作区副本来消除 dirty 检查。

## 正在核对与待完成

1. 父同步及内容/父快照核对已完成，保留各自扩展配置及既有 GUID，不重复同步。
2. 七个模板均已封存，实际版本与 hash 见上方记录及正式发布声明，不重复 Bump。
3. 扩展默认配置和专属文档索引绑定精确模板版本。尤其 platformer/topdown 启用了 FSM/关卡扩展，必须在活动 Assets 中核对新版身份和源码指纹，再通过项目中心保存/封存，不能仅 Bump 后留下失效默认组合。需要加载时保留人工弹窗。
4. 本轮测试结果及修正后复验已取得，见上方说明及 [5 项复验原始回执](release-0.25.0-tests-fixed.json)。保留对应源码的既有 lane-defense 35/35 与正式启动、放置、胜负、暂停/重开证据，明确其验证日期和范围，不作为本轮重跑。后续模板保存/切换仍须相应验证。
5. `release-0.25.0.json`、`manifest-0.25.0.json`、CHANGELOG、正式说明、lane-defense交接及当前版本入口已更新。
6. 选择性暂存发布范围，运行共享二进制、字体配置和技能 bundle 门禁；必要时使用干净候选提交环境检查，保留当前工作区。
7. 审阅提交内容、创建 annotated `v0.25.0`，向既有 origin 原子推送目标分支及这一个 tag，最后核对远端 branch/tag peeled commit 一致。禁止强推或批量推送全部 tag。

## 验证边界

当前没有完成此次版本的干净消费工程部署、完整测试集、发行构建或真实设备验收。上一版记录的 Windows 原生退出 `0xC0000005` 不因此次发版自动关闭。模板和框架升级为两个步骤，消费工程仍须保护本地定制、预览增量并单独验收。

本页为续接准备记录；正式发布完成后，以 `release-0.25.0.md`、发布声明、实际提交和远端 tag 为准。

## 静态门禁预检

在尚未暂存本次发布的阶段，对现有 index（`:`）及工作区执行共享二进制/字体预检：四份 TTF 的长度、SHA256、字体结构和 `core.autocrlf=false/true` 干净检出均通过；字体配置、fallback、GUID、动态模式、多 Atlas 和构建清空策略静态检查通过。这是未改变共享源资源的预检，最终候选提交仍需正式门禁，不能把它写成全部待发布文件已通过。

`build-ai-skill-bundle.ps1 -Check` 实际执行后因 `.agents/skills` 存在未提交变更而停止。目录含现有模板 Skill 发现副本变动与新发布技能；11 项公共分发源及 catalog 对 HEAD 没有差异。未删除、暂存或提交无关副本以绕过检查。另已核对 bundle 的 33 个原始文件均符合 manifest hash，声明源码提交为 `5868837542df759baf6ffd9412c7ae10cf7ee66b`；与该提交的源码字节差异逐项证明仅为 LF/CRLF 文本换行，UTF-8 文本一致。明细保存在 `Library/EmberReleasePreparation/bundle-byte-check.json`。这项补充静态检查不替代最终干净候选提交环境的正式 bundle 检查。
