# Ember 0.25.1

2026-10-30。基于 `v0.25.0` 的兼容性补丁发行，发行引用为 annotated `v0.25.1`，推送目标为既有 `origin/main`。实际远端提交以该 tag 的 peeled commit 为准。本次不发布新的玩法模板，不升级消费项目。

## 交付变化

- 项目自有 Skill 使用独立的 `ProjectSettings/EmberProjectSkills` 源及 schema v3 catalog，声明稳定 projectId、Skill version、精确模板 ID、业务版本兼容范围、框架/API、扩展和模块条件。安装记录升级为 schema v4，复用原模板事务、预览、暂存、备份、回滚和执行前检查。
- 扩展/模块关闭、源/身份/版本/hash 失效及域重载后收回项目受管发现副本，保留维护源及修改副本备份；同名通用、模板或未管理目录不被接管。新正文须显式同步，不能把仅执行 guard 当成完整发现隔离。
- 修正模板 Skill 空 scope 的 null/空字符串重复同步误判；模板技能源独立升级后，项目 Skill 继续绑定实际业务模板版本/hash，重复同步无变化。
- 正式试玩重试额外核对重新发现的配置 GUID 和适配器身份；排队重试期间拒绝新的 Start。同步 StopTrial 异常取消重试并允许恢复编辑现场。
- 新增带 requestId 的 `Stop`、`Retry`、`ReportFailure` 重载，原签名保留。异步适配器须显式使用带所有权的重载，才能隔离迟到请求；旧签名仍操作明确的当前会话，不推断调用者身份。
- 已发布 `ember.level.room-graph`、相关 API、配置解析、窗口和草稿恢复保留兼容，停止扩大业务范围。关卡连接、端口/宽口、能力限制、解锁、路线进度、主题和产品新增关卡流程归消费项目。仅本机可访问源码未见调用，不代表外部没有依赖；真正删除另行评估发行边界及授权。

随包接口与迁移正文：[项目自有 Skill](../../Packages/com.ember/Documentation~/manual/project-ai-skills.md)、[关卡职责及实施记录](../../Packages/com.ember/Documentation~/manual/level-authoring-boundary-plan.md)、[可组合扩展](../../Packages/com.ember/Documentation~/manual/composable-extensions.md)。UPM 只处理通用身份和兼容结果，没有引用 Windward、Player 或 Entity，没有新增常驻 Manager/Module 或共同玩法父模板。

## 模板与依赖

七个模板、父基线和实际封存内容全部沿用 0.25.0。框架补丁处于相同 major/minor，兼容声明保留 `0.25.0`，没有保存、Bump、加载或覆盖当前模板草稿。

| 模板 | 内容版本 | 实际内容/封存 hash |
| --- | --- | --- |
| base | 0.10.1 | `e9d789311cad169a19b6a76331a2877a` |
| lane-defense-2d | 0.1.1 | `608dc159f0dd54fc09455ea2e4d0529e` |
| platformer-2d | 0.13.1 | `466b7434d6dc8bcf6fa55141b32ccc8f` |
| source3d-2p5d | 0.6.1 | `9be0d1035d4396e096a3eaa820765eed` |
| topdown-2d | 0.1.1 | `406816c22a27be1fd281be126732d93e` |
| ui-game | 0.2.1 | `43030af8571b29f5c1d2d0631faa1733` |
| visual-novel | 0.22.2 | `437a3552583df64cf2ab3d1c26083864` |

六个派生模板仍以 base 0.10.1 / `e9d789311cad169a19b6a76331a2877a` 为父基线。57 项完整依赖声明沿用 0.25.0，仅其中 com.ember tag 更新；包内必装依赖也不变。11 项通用 Skill 源与 bundle 不变，bundle sourceCommit 仍为 `5868837542df759baf6ffd9412c7ae10cf7ee66b`。项目 Skill 不进入公共 bundle。

正式声明：[release-0.25.1.json](../../Packages/com.ember/Dependencies~/release-0.25.1.json)、[manifest-0.25.1.json](../../Packages/com.ember/Dependencies~/manifest-0.25.1.json)。

## 验证及限制

- 同一会话实施阶段的相关 EditMode **64/64** 通过，job `98648e265025448e925e060a4919d52c`，约 26.3 秒：模板/项目 Skill 57 项、通用试玩协议 3 项、当前 topdown 试玩门禁 2 项、旧通用安装器兼容 2 项。测试夹具警告修正后受影响用例 **1/1** 复验通过，job `2e19d05f15664a90b3eff7a70cc15030`，约 3.1 秒。本轮发布未重复全量测试，生产实现没有后续修改。
- 实际项目 Skill 经生产预览/事务安装后 guard 放行；加入修改副本草稿并主动编译/域重载，模块未重新登记时自动退出发现，旧 ID 执行被拒绝，源及修改副本备份保留。临时源随后清理，原配置和安装记录逐字节恢复；SHA256 与备份一致。
- 在测试运行器外的已保存编辑现场使用无产品规则适配器，确认已有 owner 时并发 Start 被拒绝，StopTrial 注入同步异常后 Active=false 并恢复原 FrameworkScene，错误诊断保留；没有进入真实产品 Play。运行器内的停止用例只断言恢复已排队。
- 发行阶段再次通过 Unity MCP 主动请求编译：Unity **6000.5.4f1**，编译完成 `1793362195426`、域重载完成 `1793362214399`；PackageInfo 和框架 API 均为 **0.25.1**，非编译/非更新，原生 Console Error=0、Warning=0。试玩程序集 MVID `7a57d764-88dd-44b1-b3be-2bf7929c9b87` 与此前受测生产实现一致。
- 通过 MCP 重算七个模板的真实内容 hash 与六份 ParentSnapshot，全部匹配 metadata/封存 hash/父指针，谱系零问题。当前编辑记录仍为 topdown-2d 0.1.1，无模板切换。
- 候选 index 的共享二进制及 core.autocrlf=false/true 干净检出检查、字体配置检查通过。首次二进制检查遇到系统临时目录权限限制，改用本仓库 `.utmp` 临时目录后同一正式检查通过，不改检查器或资源。

公共 Skill bundle 与限定 index 检查见本页“发行候选检查”，不用主工作区的未提交字体缓存或模板 Skill 发现副本替代发布内容。

没有执行完整测试集、真实 Play 中的取消/临时存储排空、产品能力断言、新消费项目部署、Windward 升级、发行构建或实机验收。项目闭包或外部文件仍需项目自行纳入冻结；旧框架历史原生退出问题不因本补丁自动关闭。

## 消费接入与回退

1. 已有工程通过 `Ember/UPM Manager` 更新到 `v0.25.1`，核对实际解析包并完成 Unity 编译。包升级不会自动合并已部署 Assets；本补丁不要求整模板重部署。
2. 扩展源码指纹变化会使旧绑定失效。在项目中心预览并签收当前组合，按实际模板及模块条件同步 Skill，保留本地修改备份；不手改配置 hash。
3. 项目业务 Skill 按随包 schema v3 正文维护独立来源，使用 Core 的执行前检查入口。试玩异步回调捕获启动时的 requestId 并使用对应重载。产品图、能力及主题流程留在项目，不新增房间图依赖。
4. schema v4 记录不应由旧管理器降级写入。回退前保留项目源、安装记录和副本修改备份，用支持 v4 的项目中心撤出受管项目 Skill；不要直接删除其他来源目录或把项目正文改成公共技能。
5. 在消费项目完成自身关卡、临时存储、输入、存档和目标平台验收。此源码发行不代替消费项目验证。

## 发行候选检查

- 限定暂存区准确为23个文件，差异空白检查通过；八份候选C#与已编译工作区在换行归一后相同。没有纳入活动Assets、ProjectSettings、模板/父快照、共享字体缓存、通用Skill源或bundle。
- 11份本轮修改/新增Markdown的严格本地链接检查无缺失；接口及锚点另行按实际源码复核。发布manifest仅com.ember引用从v0.25.0变为v0.25.1，依赖及模板元数据逐项相同。
- 干净受管工作树基线`b9a8243739fa19e2e3982beeefe06633105b7dde`的正式`build-ai-skill-bundle.ps1 -Check`通过：11技能、33文件。候选index的通用源、bundle和检查脚本与该基线没有差异。bundle原始33份文件hash逐一匹配manifest；index文本及记录的sourceCommit与bundle仅有已严格验证的UTF-8换行差异，没有重生成bundle或伪造sourceCommit。
- 提交后继续针对同一发布内容复核并核验远端main/tag，不批量推送tag、不移动旧tag。远端推送结果以实际引用为准。
