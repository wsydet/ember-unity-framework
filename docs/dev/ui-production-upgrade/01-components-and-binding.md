# 阶段 S1 完善组件与绑定契约

目标是在现有 EUI 体系上统一页面命名，并建立可稳定读取、重新生成和替换的控件接口。前置是 S0 已完成并正式加载 base；实现前完整阅读 [EUI 信息契约](contracts/eui-contract.md)与[命名和迁移规范](contracts/ui-naming.md)。本阶段负责实际迁移及核心信息导出。

## 工作范围

优先处理基础控件、绑定边界和接口差异，不一次性启用全部 Burner 参考实现。保留原有页面行为与公开 API，发现缺陷按最小影响修复。通用代码归框架；base 示例接线归 Assets。

| 优先级 | 内容 | 本阶段动作 |
| --- | --- | --- |
| P0 | base 页面命名、生成来源和引用 | 按迁移表将页面 Panel 后缀统一为 Page，保留 GUID 和原行为 |
| P0 | EUIBinding、生成器、嵌套逻辑、EUIItem | 核对现有契约，修补实际缺口，保证唯一所有者 |
| P0 | TMPEx、ButtonEx、ImageEx、ToggleEx、ToggleGroupEx | 核对状态、槽位与编辑器支持，接入准确描述与验证 |
| P0 | ContentSizeFitterEx、安全区 | 核对自适应和设计区域信息，保留已有能力 |
| P1 | ScrollRect、列表和页签 | 原生组件可先使用；完整容器放 S3 |
| 后续 | 跑马灯、高级图文、图片字、合并图片等 | 列入候选，出现明确需求再实现 |

## 源码入口

- [EUIBinding](../../../Packages/com.ember/UIExtension/Runtime/EUIBinding.cs)与[EUIBindingBridge](../../../Packages/com.ember/UIExtension/Runtime/EUIBindingBridge.cs)。
- [Binding 编辑器服务](../../../Packages/com.ember/UIExtension/Editor/Pages/EUIBindingEditorUtility.cs)与[代码生成](../../../Packages/com.ember/UIExtension/Editor/EUIBindingCodeGenUtility.cs)。
- [Item 工厂](../../../Packages/com.ember/UIExtension/Runtime/EUIItemFactory.cs)与[EUIItem 生命周期](../../../Packages/com.ember/UI/Runtime/EUIItem.cs)。
- [EUI 制作技能](../../../.agents/skills/ember-eui-build/SKILL.md)及其接口参考；技能仅在实际制作时执行。

读取实际签名后再调用。导出的快照未必覆盖所有需要保留的元数据，应核对 IsFramework 等条目标记、代码模式、页面设置与继承信息；不能假定现有快照天然支持无损往返。

## 实施步骤

### 0 先统一页面命名

按命名规范执行 S0 的逐资产迁移表。覆盖 base 的 Background、Main、GamePlay、Setting、Loading 和 GM 页面，使用 Unity 正式资源移动/重命名接口保留 GUID，再同步根名、Binding、生成来源和实际消费者。

先修正创建中心与生成模板的默认命名，再重新生成注册和 Binding；不能仅将 Prefab 改名后留下旧资源路径。Framework 模式、页面类型、排序层、遮幕和状态机行为均保持原职责。

初次契约导出使用迁移后的规范名，同时保留历史名称映射。代码、配置、Skill 和当前文档中的正式引用一起迁移；模板快照与历史记录按规范处理，不全仓替换 Panel 文本。

### 1 建立组件能力表

为 P0 控件记录实际类型、现有封装、可用状态、公开槽位、绑定识别方式、默认值、序列化字段、编辑器替换入口和相关测试。逐个查看有效代码，注释参考代码只作为研究来源。

例如 EUIButtonEx 的 EnableState 控制显示节点，与原生 interactable 的交互含义需要分别记录。不要在信息导出时将两者合并成一个“已禁用”。如果确需统一，先评估现有调用方并定义兼容迁移。

### 2 固定组件封装边界

Button/Toggle 的标题优先通过已有 Label 槽位暴露；自动收集遵循 IEUIExposedChildProvider，避免页面再次直接绑定同一内部文本。复合组件对外公布内容和事件，内部节点仅供组件维护。

嵌套 Binding 由父逻辑创建子逻辑时，动态 Item 工厂不得再介入同一对象。对两种用法分别提供可生成的最小示例和运行生命周期记录。

### 3 实现只读契约导出

从 Prefab、Binding、生成文件和 S0 行为基线读取信息。保存源资产身份、绑定与对象映射、页面角色、代码模式、资源依赖和未解决项。行为说明无法静态证明时保留待确认，不虚构自动分析成功。

导出默认不改 Prefab、不触发代码生成、不自动补齐丢失资源。语义 ID 缺失时可以给出待登记建议；写入语义配置是独立应用步骤。

### 4 实现绑定差异与验证

至少区分外观改变、层级改变、资源引用改变、绑定新增、绑定删除、改名、类型变化、所有权变化和资产身份变化。自动应用前阻止空引用、重复绑定、重复语义 ID、未知关键类型和重复逻辑所有者。

验证实际组件与声明类型相符。Extension 使用完整类型名；不能仅因同名类存在就认定运行时可创建。

### 5 验证生成往返

选 base 主界面和设置页，记录生成前后 Binding、PageDef、用户区和资源引用。通过公开服务重新生成，保证用户逻辑和用户美术不会被无关重建。

Framework 模式页保留其模式和受管区域。需要修正生成内容时修改真实生成源/配置后再生成，不手改 .Binding.cs 或 EmberManaged 区。整表替换绑定前必须保留全部元数据，避免无关条目丢标记。

### 6 补必要控件缺口

只有影响上述契约、公共组件或试点的缺口才纳入本阶段。更改现有序列化字段时提供兼容方案，保留资源 GUID。将运行代码与 Editor 检查拆开，避免运行程序集引用 UnityEditor。

## 交付物

- P0 组件能力表，包含已实现与尚未启用的边界。
- 实际命名迁移表、保留 GUID 的证据、引用扫描结果，以及新建/重复生成不会恢复旧名的检查。
- 可实际调用的契约导出和绑定差异入口，记录真实菜单/API/参数。
- 主界面和设置页的实际契约文件及行为补充。
- 两种嵌套所有权示例与生成往返记录。
- 新增或修复的相关测试、实际编译与 Console 结果。

正式字段定义与使用说明进入包内 UI 文档。本目录保留设计决策和实施证据，不建立第二份长期 API 权威正文。

## 验收和恢复

执行 B01 至 B07。重点证明页面命名与引用一致、重复生成不会恢复 Panel、不破坏用户区、外观调整不会被误判成接口变更、删除绑定会被准确阻止、每个 Item 仅创建一份逻辑。

失败时保留报告，恢复本次触及的 Prefab/元数据及生成文件，先核对文件是否又被用户修改。不要回滚整份工作区。模板 Assets 变化按阶段执行授权经中心普通保存，Bump 单列状态。

将真实 API 名称、模型版本、未解决字段及 S2 可以依赖的能力写入[交接记录](10-progress.md)。
