# Ember Framework 当前状态与后续工作

> 核对日期：2026-09-11。实现以当前工作区与发布 tag 为准；消费项目验收单独记录。
> 2026-09-07 模板分支与场景语义同步批次已由用户确认通过 Unity 编译、相关 EditMode、真实 UnityYAMLMerge 和手工 O/N/C 验收；未列明的 PlayMode、消费项目与整体发布项仍按未验证处理。

## 当前基线

0.12.5 将 UPM 检查更新改为非阻塞查询，增加循环进度、耗时、取消与 60 秒超时，并增加五项可选第三方包体检、修复升级完成提示清空新查询结果。Unity MCP 不可用，Unity 编译、6 个新增 EditMode 测试与面板交互验收待手动完成。详见 [0.12.5 发布说明](release-0.12.5.md)。

0.12.4 启用共享钉钉 SDF 多图集，并补充单张溢出、材质/子网格、SceneUI 显隐与池复用的 PlayMode 测试。静态检查通过；Unity MCP 不可用，修正首次测试编译错误后尚未取得编译和运行结果。详见 [0.12.4 发布说明](release-0.12.4.md)。

0.12.3 修复两份共享 TTF 的 Git 换行损坏，增加长度/SHA256、字体结构与干净检出回归；Unity MCP 不可用，编译和运行验收未完成，见 [0.12.3 发布说明](release-0.12.3.md)。

0.12.2 修复 Table 生成脚本与导入器 BOM 冲突及接线编码误判，新增 10 个 Editor 案例；用户已确认修复后的框架项目测试通过。该结论不是本会话 MCP 验证，未提供本轮 XML 或精确案例数。升级与消费回归步骤见 [0.12.2 发布说明](release-0.12.2.md)。

| 对象 | 当前值 | 证据 |
|---|---|---|
| Unity | 6000.5.4f1 | `ProjectSettings/ProjectVersion.txt` |
| 框架包 | 已发布 `0.12.5` | UPM 检查更新动态进度与非阻塞查询 |
| 发布状态 | `v0.12.5` | 静态检查完成；Unity 编译、EditMode 与交互回归待确认 |
| 根模板 | `base 0.6.0 / stable` | 内容与封存 Hash 均为 `506baffc678f37d420308607c5f70e42`；已通过项目中心声明框架 `0.12.0` |
| 派生模板 | `source3d-2p5d 0.3.1 / preview` | 父基线 `base 0.6.0`，ParentSnapshot 与父 Hash 一致；已通过父同步继承框架 `0.12.0` |
| 模板开发副本 | `source3d-2p5d 0.3.1` | `Assets/Editor/EmberEditingTemplate.json` 记录 Hash 与当前模板一致，不代表消费项目验收通过 |

0.11.5 新增随包的 UnityFarm 改动回流规则，明确项目、框架、模板及两者联动的判定、版本和消费流程；根 `AGENTS.md` 与 `CLAUDE.md` 提供强制阅读入口。模板 Assets 未改变，继续保留 0.11.4 的稳定 GUID、冲突预检与完整重新部署能力。
Rainbow 两包、Console Pro、InputDeviceDetector、Feel 统一纳入私有第三方仓库交付；前四包已核对镜像一致，Feel v5.4 已备好 5.4.0 UPM 封装，尚未切换安装或验收。依赖自动同步仍待实现。
随包 `Dependencies~` 已提供 56 项直接依赖的可移植 manifest 和 0.12.5 正式发布声明；本补丁不增加第三方依赖，7 个第三方包继续固定到已发布的 `ember-v0.11.1`。消费端框架升级必须通过 Ember/UPM Manager；第三方依赖按实际需要配置，详见 [0.12.5 发布说明](release-0.12.5.md)。
部署记录和开发编辑记录用途不同，不能根据 `EmberDeployedTemplates.json` 的旧 base 记录推断当前编辑的是 base。

## 已有能力

框架通过一个 `Packages/com.ember` 包交付，内部用 asmdef 划分职责。业务通过
“框架基础 + 必备 Managers + 按需选装 Modules”组成游戏。

| 子系统 | 当前实现 | 维护入口 |
|---|---|---|
| Core | 启动、状态机、Manager 发现、Module 阶段生命周期、统一 Update、时间和 Timer | [Core](../../Packages/com.ember/Documentation~/core/README.md)、[启动时序](ember-boot-sequence.md) |
| Basic / Extensions | 集合池、数据结构、STTask、JSON、存储、日志、通用扩展 | [API 速查](ember-api-reference.md)、[日志](ember-debug.md) |
| Resource | Provider、资源/文件 Handle、去重加载槽；默认 Resources 后端 | [Resource](../../Packages/com.ember/Documentation~/resource/README.md) |
| UI / UIExtension | EUIManager + EUIViewEngine、Page/Item、Binding、遮罩、过渡、SafeArea、开发中心 | [UI 使用](../user/UI开发参考.md)、[EUI API](eui-reference.md) |
| Scene | 经 Resource 加载场景、状态机场景桥接、Loading 拦截 | [Scene](../../Packages/com.ember/Documentation~/scene/README.md) |
| Audio | BGM / SFX、Mixer、临时音效 AudioSource | [Audio](../../Packages/com.ember/Documentation~/audio/README.md) |
| Camera / Input | 相机注册与霸占栈、InputAction 读取、重绑定契约 | [Camera](../../Packages/com.ember/Documentation~/camera/README.md)、[Input](../../Packages/com.ember/Documentation~/input/README.md) |
| SceneUI | 普通 Engine、业务 Module 基类、EUI Item 宿主、投影/显隐/池化/诊断 | [设计](scene-ui-module-design.md)、[使用](../../Packages/com.ember/Documentation~/scene-ui/README.md) |
| Table | 不可变 Row、CSV/TSV 校验、ETBL V1、强类型 Binding/Catalog、事务 Engine 与 ModuleBase；Editor 声明/数据浏览、代码提示和单表导出 | [使用](../../Packages/com.ember/Table/Documentation~/table/README.md)、[方案](table-system-development-plan.md) |
| 2.5D 业务示例 | Gameplay 输入门控、WASD、世界锚点拖动、缩放、连续区域边界 | [PlayerControl](player-control-module.md) |
| 模板与编辑器 | 项目中心、schema v2、父子三方同步、场景语义合并、事务回滚、消费端完整模板部署 | [模板体系](template-upgrade-system.md)、[包维护](upm-migration-plan.md) |

`InitState` 先发现并构造启用 Module，再启动全部 Manager，最后激活 Global。
Gameplay 的进入/退出驱动 Gameplay Phase。Main 或自定义 Phase 仍需业务状态显式接线。
禁用或尚未装配的 Module 不能通过 `.Instance` 偷偷创建，业务查询使用 Collector。

## 尚未完成的工作

| 优先级 | 事项 | 当前边界 |
|---|---|---|
| P0 | UnityFarm 0.12.4 消费验收 | 通过 Ember/UPM Manager 升级，确认实际版本和多图集配置；手动编译，执行容量/材质/SceneUI 回归并验收六个正式标记 |
| P1 | 消费端模板升级向导（P-B） | 已有所有权标记，尚无把新模板安全合并进已有用户工程的完整向导；父子模板同步不等于该能力 |
| P1 | 新消费项目完整回归 | 使用 `v0.12.1` 验证首次部署、模板替换部署、GUID 冲突阻断、场景引用和 Play 链路；不阻塞 UnityFarm 按既定 UPM 流程升级 |
| P1 | Preview 模板生命周期（P-C） | 第二模板已存在；转 stable、deprecated 和消费项目完整演练仍需验收 |
| P1 | Audio 多分类与池化 | [Audio 升级方案](audio-upgrade-plan.md) 尚未实施 |
| P1 | Resources 后端真正异步 | 当前 `Resources.Load<T>` 为同步回调包装；Handle 不意味着后台异步或引用计数缓存 |
| P1 | 输入重绑定实现 | 仅有 `IEmberInputRebindingService` 契约，交互监听、Overrides 持久化由后续服务接入 |
| P2 | GM ScrollRect 布局复测 | 历史记录中 Viewport 尺寸异常；见 [绑定回归第 6.4 节](uiextension-test-plan.md)，不能把隐藏节点视为修复 |
| P2 | 通用预制体池、本地化、存储异步 API | SceneUI 已有专用 View 池，通用 GameObject 池与 DataSaver 异步扩展需分别设计 |
| P2 | UI 高级能力 | 通用虚拟列表、Tab/挂件资源生命周期、可等待的页面打开 API、按模块拆分页面注册表；按真实需求推进 |
| P2 | UI 编辑器增强 | 对尚未激活的 Preview / Bake / Validation 等工具逐项评估，不再按旧迁移文件总数估算完成度 |
| P2 | 公开分发前的依赖解耦 | 当前 Odin 仍是硬程序集依赖；无 Odin 变体或条件编译方案尚未实施，保留原 UPM 方案中的此项后续工作 |
| P2 | GM 与引导增强 | GM 快捷操作；Guide 正式美术、状态机测试、更多业务条件；见 [Guide](guide-module-design.md) |
| 远期 | 可视化与网络 | 状态流转图、节点编辑器、联网状态同步、YooAsset/Addressables Provider、Wwise；当前无默认实现 |
| 预案 | 独立升级器 | [独立升级器方案](independent-updater-package-plan.md) 保留，当前仍使用包内 UPMManager |

UI 的 SubPage 排序、分帧操作预算、挂起操作、CanvasScaler 适配、预加载、加载计时、Profiler、
订阅追踪、FullScreenPopup、视图显隐、Loading 预制体和过渡动画已有代码，不再作为“从零实现”的待办。
一般页面的预加载不等于资源引用计数缓存；高级列表、资源后端和池化仍需分别验收。

后续想法保留为需求候选：通用条件、功能解锁、任务系统、图片/纹理管理、2D/3D 与平台模板。
不直接迁入 Burner 的服务器鉴权、加密协议、内部反射补丁、自研 Tween 或整套重型控件包装。

## 验证与历史

- 当前回归入口：[框架验收清单](framework-test-checklist.md)、[编辑器检查](editor-tools-test-checklist.md)、[绑定回归](uiextension-test-plan.md)。
- 2026-09-07 已完成模板分支与场景语义同步专项验收：相关 EditMode、真实 UnityYAMLMerge、非冲突与真实冲突 O/N/C 手工流程均通过；非冲突合并后派生封存为 `source3d-2p5d 0.2.5`、父基线 `base 0.5.4`。收口时根模板已继续封存为 `base 0.5.5`，派生尚未推进父指针。
- C# / asmdef / 场景 / 资源变更遵循 [CLAUDE.md](../../CLAUDE.md) 的 Unity MCP 编译规范；MCP 不可用时记录未验证并由用户手动编译，不改用 dotnet 或 BatchMode。
- 版本历史保留在 [CHANGELOG](../../Packages/com.ember/CHANGELOG.md) 与 Git。本文只维护当前状态和未完成工作，避免历史阶段表与现状互相冲突。
