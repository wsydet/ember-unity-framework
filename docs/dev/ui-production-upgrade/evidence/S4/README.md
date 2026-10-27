# S4 实施与验收记录

2026-10-26。已完成授权范围内的真实分层 PSD、Rename、UXP/BUI 导出和只读交接校验；没有进入 S5 制作器、S6 换肤或 S7 模板。以下“通过”限定为本试点与列出的支持范围，不能推导为完整 Photoshop 效果或 Unity 制作链已通过。

## 实际改动与归属

| 位置 | 内容与归属 |
| --- | --- |
| `Packages/com.ember/Extensions~/ember-psd-to-bui` | 框架分发的 UXP 源码、配置、BUI协议、原生PSD脚本、只读验证与相关测试 |
| `art/ui/S4` | 仓库外部美术工作资料；真实source/renamed PSD、BUI、PNG、状态预览、EUI映射与请求记录 |
| 当前目录、`10-progress.md`、PSD规范入口 | 实施证据与后续阶段交接 |

没有修改 Unity Prefab、Assets 业务代码、Binding、EmberManaged 或共享皮肤；没有写参考工程/PackageCache、Templates~ 或 ParentSnapshot~。基线 Git 状态与后置比较没有丢失既有状态项；原模板切换删除保持。本轮 Assets 无改动，故不重复普通保存：[template-save.json](template-save.json)。仍为 base0.8.4/framework0.22.0、585文件前次保存、contentHash c58c6c5ed41b18efc8fe38fd4999dd5f、versionedContentHash 936dcb1d411afefcacd961cbad1d1d0d。未 Bump/提交/推送/发布/消费升级。

## 真实输入与输出

仓库根为 `C:/Users/wuyu/My/ember-unity-framework`。相对路径与所有摘要在 [artifacts.json](artifacts.json)，S5入口为：

- [源稿](../../../../../art/ui/S4/EmberPilot.source.psd)：Photoshop原生创建，1280×720，保留中文原层名、8个可编辑文字层、组件/Item/状态分组，非扁平图。SHA256 `7254350f66baf66bdf89b47fa7c8395d6808603703b79aaa4e2eb0d1eefcb8a2`。
- [工作稿](../../../../../art/ui/S4/EmberPilot.renamed.psd)：原稿复制后按真实layerId改名，并仅在工作稿着色提高对比度。SHA256 `81ef4a3c69a9fa2ec710f06f3ccf877a59989dd71952d58fd3f7ab1e0519baa9`。
- [真实BUI](../../../../../art/ui/S4/EmberPilot.bui)：schema3，3 Target/24节点，SHA256 `4b28841b937af76cf728eafabc2d93054cf679a3d7564386860d92bdad2b6f48`。
- [必需EUI映射](../../../../../art/ui/S4/EmberPilot.eui-map.json)：绑定当前BUI/PSD与真实契约、公共目录、源逻辑及资产摘要。不是伪造的BUI字段，也不是S5执行结果。
- `EmberPilot_bui_assets`：当前BUI引用8张PNG。最终productFiles另有4张文字合成参考PNG，共12张；BUI单独记摘要。目录中保留前轮未引用图，不可按最新文件或目录全集替换BUI引用。
- [主界面预览](../../../../../art/ui/S4/previews/main-final.png)、[普通行](../../../../../art/ui/S4/previews/list-normal-final.png)、[选中行](../../../../../art/ui/S4/previews/list-selected-final.png)、[禁用外观](../../../../../art/ui/S4/previews/list-disabled-final.png)：由Photoshop临时复制文档切换可见性原生保存，原PSD未被预览步骤修改。

试点选择主界面两个按钮及独立动态列表/行模板，不重做完整设置页。外观变化为左对齐标题/按钮、S2背景、右侧九宫样本、1040宽动态行、图标/动态文字/进度与独立状态。列表PSD只有一个模板，标题中的3 items为可编辑设计占位，并非3个运行实例。Toolbar计数/重排/删除及Back真实控件保留于现有页面，PSD中的Back仅为标签对照。试点不是完整换肤候选。

素材均来自S2正式资源，见 [materials.json](materials.json)；本轮未生成新图片、未复用Burner产品美术。工作稿将白色Help/Checkmark着色为灰蓝/蓝，将Start文字改白；色值、逐层对应、切图与公开复用见 [rename-plan.json](rename-plan.json)。

## 版本、能力与消费者

参考工程实际manifest/lock解析 `com.burner.ui.prefabmaker 2.0.0-preview.17`，读取其Rename/Maker边界、UXP源码、Reader/schema/tests；并未按文档旧版本猜测。上游UXP3.0.14，Ember插件1.0.0，命名1.5.0，文件桥1，BUI3，EUI契约1/tool1.1。完整兼容表、安装、人工面板与桥接入口见 [插件说明](../../../../../Packages/com.ember/Extensions~/ember-psd-to-bui/README.md)，逐文件上游/适配摘要见其 `provenance.json`。

本轮主界面用正式EUIContractService.Export/Write重新读取为 [EUIMainPage.eui.contract.json](EUIMainPage.eui.contract.json)，Framework/MainPage/GUID和框架绑定保持，blockers为空；没有把S1历史资源快照当作当前资产。S3三份最终契约按原摘要引用，早期DynamicList快照不使用。原契约中未展开序列化/未登记嵌套语义仍保留；主菜单行为由真实Btn_Start/Btn_Settings与逻辑文件证明，不从画面猜测。

| 表达 | 已验证内容 | 下游限制 |
| --- | --- | --- |
| Page/Item | 主菜单、动态列表页、独立ListItem，真实layerId | BUI名称显式映射EUI类名，不改已有GUID/Framework/导航 |
| List/PrefabRef | List为Vertical；嵌套ListItem提取独立Target，父级GeneratedTarget引用；外部按钮GUID与label覆盖 | 必需dynamic-list-v1；完整容器/Item/Content/Viewport/dataBinding/owner/refresh/cacheCapacity来自S3契约；Item不得再注册固定UILogic |
| 公共槽位 | ActionButton.label；SecondaryButton的设计label映射真实Label/TMPEx | 不按同名猜源，不修改私有子节点或共享源 |
| 动态内容 | Text保持内容/样式；Icon为DynamicTexture无资源；进度独立图层 | Title/Icon/Progress绑定由代码明确提供；50%设计长度不等于运行时满长度；key=Id，index仅显示顺序 |
| 状态 | Selected/Disabled独立Group且active=false | EnableState/可见性不等于interactable；目标隐藏不等于运行生命周期；独立预览不证明点击/禁用行为 |
| 九宫与透明 | 256设计框→130PNG，64四边+2中心；四角逐像素差0；Checkmark透明裁切31×25 | 设计框与PNG框分开；禁止把压缩PNG尺寸当设计尺寸 |
| 文本/效果 | 8个原生可编辑文本；6个Text节点+2个公开文字覆盖；隐藏文本样式仍在BUI | 隐藏文字合成参考不可取，保留4条TEXT_REFERENCE_UNAVAILABLE；ArialMT不等于TMP映射；原生阴影/描边/遮罩/智能对象未实测，关键需求须单独验证或阻断 |

本阶段支持current-psd/BUI3；保留的上游retain/history/SubPage/adaptation能力不在Ember首版承诺中。未知关键标签、重复身份、缺失/变化PNG、无效公开槽位、未知必需能力均在只读消费门禁阻断；不会靠普通Group降级放行。完整通用JSON Schema引擎未运行，UXP自身也是轻量checklist；本轮实际运行的是明确的结构/引用/几何/PNG/语义检查，不宣称全schema认证。临时尝试安装jsonschema因环境下载/权限失败，未把它作为通过证据。

## P01–P07

| ID | 结果 | 实际证据与适用边界 |
| --- | --- | --- |
| P01 | 通过 | source PSD保留原层名；复制工作稿后才Rename；source-layers/首轮before/最终报告及摘要可追溯 |
| P02 | 通过（本试点） | 实际inspect识别3目标、隐藏状态与独立Item；未知关键语义/重复身份的阻断在本轮相关测试覆盖，未称真实PS负面稿测试 |
| P03 | 通过（BUI3/current-psd） | Photoshop27.5.0+实际加载Ember UXP1.0.0；最终请求e6736f3a6f1f4d4fb7ca32ae058e5402终态completed/productSaved=true/recordStatus=saved |
| P04 | 通过 | [validation.json](validation.json)：全部8张引用PNG及4张可用文字参考的存在、SHA、尺寸检查；3目标/24节点/List/公开引用/契约能力检查 |
| P05 | 通过（试点无原生阴影/描边） | [pixels.json](pixels.json)：alpha框与设计框、九宫四角零差；实际查看最终PS预览；原生效果及Unity/TMP渲染不在此次通过范围 |
| P06 | 通过（实际与夹具分列） | 真实5秒等待超时后query原ID完成；覆盖快照拒绝→修复；真实过期源摘要写入前失败；相同字节原ID重复调用返回同终态且BUI未变；部分写入/丢失终态用模拟宿主验证，不称真实PS故障注入 |
| P07 | 通过 | 原生Photoshop保存/重开源与工作PSD；可编辑文字、组件/Item/隐藏状态与S2来源齐全，实际UXP生成BUI和PNG |

相关测试见 [tests.txt](tests.txt)，仅15项S4门禁/桥接用例，非Unity全量。Unity本轮MCP显式compile=request，编译/域重载完成、Console错误警告0；时间戳见 [unity-compilation.json](unity-compilation.json)。未运行S3整套Play回归，因为无运行代码/Unity资产修改。

## 失败、修复与恢复

1. UDT文件框不接受正斜杠路径，使用Windows路径后成功加载。沙箱COM和桥接硬链接受限，通过授权本地执行入口成功；首次失败请求目录检查为空，未盲目重放未知写入。
2. 首次Rename导致Photoshop隐藏层显示；原稿不变。保存首轮BUI和Rename报告于 [first-export](first-export/rename-report.json)，脚本修复为改名后从子到父恢复visibility，重开核对。最终BUI中Selected/Disabled为false。
3. 首次视觉预览白图标对白底不清楚、Start文字偏暗。仅工作稿着色，真实重导出；旧引用文件保留，最终BUI精确引用新Checkmark_01。
4. 只读门禁首次报PUBLIC_SLOT_UNKNOWN，查实际catalog发现SecondaryButton槽ID差异；加入明确alias映射后通过，没有改库或降低校验。
5. 每次覆盖先真实返回OVERWRITE_DECISION_REQUIRED，按本轮产物范围核对snapshot/decisionSha256后新请求导出。所有真实回包在 [bridge](bridge/final-request.json)。过期输入请求640114ca92794b0e957497e4043714d7返回EXPORT_SOURCE_SHA_MISMATCH、partialOutputPossible=false，无对应产物。
6. [repeat-result.json](repeat-result.json)证明原请求重复调用保持原结果/BUI摘要。现无pending/unknown写入。若未来超时，先query原ID；claim无终态须保留现场，不新ID重放。

## S5接续

从最终BUI、EUI映射、其引用PNG、当前主界面契约与S3三份最终契约继续；先运行插件README的只读门禁，核对摘要和消费者能力。S5尚无Unity制作实现，不把传入能力字符串当成已实现支持。

保留原主菜单GUID/框架模式/绑定和导航；保留未替换控件，Back必须仍为ClosePage(Page)。动态Item只归宿主/lease所有；静态Dropdown/Tab/横向网格能力不升级。长文案不能用PS字形裁切框取代原运行时布局；字体、Effect、公开实例内部布局和实际运行状态须在S5独立核对。本阶段没有消费部署、CCX签名、完整人工面板交互、Unity外观还原或完整多语言/效果验证。当前没有阻止本试点交付的工具阻塞。
