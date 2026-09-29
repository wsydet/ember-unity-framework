---
name: ember-eui-build
description: 使用 Ember UI 开发中心的创建、绑定与生成接口，制作或修改 Unity 项目的正式 EUI Prefab，并基于实际生成结果编写用户逻辑。适用于批量创建 UI、按需求搭建控件、重新绑定及生成代码。
---

# Ember EUI 制作

把用户的 UI 需求落成正式 Prefab、真实 Binding 和用户逻辑。默认承担布局、组件、事件接线与生成；用户可在之后调整美术。遵守当前请求的项目范围，不把某次产品的界面固化进通用技能。

## 确认工程和接口

- 读取目标工程的代理规则、EUI 生成设置、既有页面和已生成代码。从消费项目发现的改动按实际包内 `Documentation~/maintenance/unityfarm-change-routing.md` 分流；产品界面留在项目。
- 以消费工程实际解析的 `com.ember` 源码为准，不能只看另一开发仓库的最新版本。读取 [接口说明](references/editor-api.md)，再核对实际签名。
- 本技能由 Ember 框架仓库统一维护，通过 `Ember/UPM Manager → AI Skill` 安装到当前项目。使用适配器前确认公开的 `EUIBindingCodeGenUtility.TryRegenerateCode` 已存在；旧框架先通过 UPM Manager 升级，不把新适配器复制到缺少该 API 的项目。已部署的旧适配器与业务调用按实际差异迁移，不重复定义同名类。
- 列清新建或修改的 Prefab、角色、类名、输出目录、绑定字段、嵌套所有权、默认状态。用户明确要求代做资源时，直接实施，不再要求用户逐个制作或批准同一动作。

## 编辑器制作流程

1. 新 UI 使用 `EUICreationService.TryBuildPlan` 预检，通过后调用 `Create`。所有目标先预检；同名资源转为检查/修改，不能删掉用户资源重建。业务界面使用 `Business`，可复用控件使用 `Item`。
2. 备份拟修改的资产、元数据和生成文件。通过 `PrefabUtility.LoadPrefabContents` 编辑正式资源，在 `finally` 中 `UnloadPrefabContents`。不用运行时 `Awake` 搭界面，不手写 Prefab YAML。
3. 用 Unity UI/TMP 组件建立控件及 Layout/ScrollRect/Dropdown 的实际引用；从项目现有资源取得字体和图片。再次生成只更新绑定，默认不重新执行美术布局。
4. 使用 `EUIBindingEditorUtility` 登记/核对真实对象和类型。保存并重新加载 Prefab 资源，通过公开的 `EUIBindingCodeGenUtility.TryRegenerateCode` 重新生成。可使用 [生成适配器](assets/EmberEuiSkillAdapter.cs)；项目已有适配器时先比较调用方，避免重复复制。新增目录要处于合适的 Editor 程序集内。
5. 保存实际绑定快照、生成文件路径及错误。生成成功后读取 `.Binding.cs` 与 ControlMap，才写 `.cs` 用户区或独立用户 partial。禁止手写生成字段、修改 `.Binding.cs` 或 EmberManaged。
6. 由 EUI 生命周期创建/释放 Item、事件与拖拽状态。通过 Unity MCP 做有界编译和必要验证；资源生成报告不能替代编译、运行或视觉验证。

## 易错边界

- Item 根使用 RectTransform、CanvasGroup、EUIBinding，不另建 Canvas、EventSystem 或 Page 注册。
- 嵌套 Item 若由 EUIItemFactory 管理，父级绑定其 `Extension / UnityEngine.RectTransform`，子级自行生成；不要同时登记 UILogic 自动创建第二份逻辑。
- RectTransform、TMP_Dropdown、自定义组件显式登记 Extension 和完整类型名。运行时逻辑只改状态/数据，保留用户字体、颜色、布局。
- 模板部署到消费项目的 Page 可能仍标为 Framework 模式；生成器会拒绝在消费端重新生成它。不要假装 embedded、切换模式造成路径迁移、修改包缓存或改写生成区。可在已有 Page 的真实 Binding 中追加业务根引用，用户 partial 从实际 ControlMap 取得它；新业务 Item 正常生成。若任务确实需要框架生成能力变化，先分流评估。
- 对既有绑定只追加/更新目标条目，保留其它条目的 `IsFramework` 等标记。通用 `SetBindings` 是整表替换，不应直接套在已部署 Page 上。

## 执行条件与证据

优先通过当前可用的 Unity 编辑器工具执行制作入口。工具缺失时仍可完成技能、Editor 制作脚本和静态检查，但不能声称已调用 Unity。

用户已授权直接制作资源时，可以提供一次性的 Editor 资源制作请求：只识别固定任务 ID，执行前消费请求，失败不自动重试，后续域重载不重复覆盖用户美术。它仅运行资源制作，不代替或声称自动编译验证；禁止靠轮询自动刷新或 Editor.log 证明编译成功。若没有实际生成报告，说明执行仍待完成，并给出一个具体的菜单入口。

交付时区分脚本已安装、Prefab/Binding 已生成、业务已接线和验证通过。需要手动操作时解释真实阻碍，并把用户操作压缩到必要的一次执行或错误反馈。
