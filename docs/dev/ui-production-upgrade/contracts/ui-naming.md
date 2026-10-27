# EUI 命名规范与旧页面迁移

用户已明确要求页面统一使用 Page，解决现有 Prefab 用 Panel、逻辑用 Page 的混用。本规范由 S0 盘点、S1 实际迁移、S2 以后持续执行。迁移完成后，正式资产、生成入口和当前使用说明都采用统一名称。

## 按角色命名

| 对象 | 规则 | 示例 |
| --- | --- | --- |
| Page 角色的正式页面 | Prefab 文件名、根对象名、Binding 的 PrefabName/PageName、页面逻辑类以同一 Page 名称对应 | EUIMainPage.prefab、根 EUIMainPage、逻辑 EUIMainPage |
| 弹窗页面 | 同样以 Page 结尾；Popup 或 FullScreenPopup 由 PageType 表达 | EUISettingPage，PageType 为 FullScreenPopup |
| 专用弹窗骨架 | 可以在职责名中含 Popup，仍以 Page 结尾 | EUIStandardPopupPage.prefab |
| 管理器持有的局部子页 | 以 Page 结尾，并明确实际页面类型 | EUIInventorySubPage |
| 宿主持有的轻量单元 | 以 Item 结尾，不因为美术叫子页就注册为 Page | EUIOptionRowItem、EUISettingsContentItem |
| 控件家族与库条目 | family 和显示名称按职责命名，具体 Prefab 仍服从其 Page/Item 角色；不夹带皮肤配色或产品名 | family 为 ActionButton，带 Item 绑定的资产可为 EUIActionButtonItem |
| 内部布局节点 | 使用 Content、Header、Footer 等语义；必要时可使用 Panel，但不冒充独立页面 | Content、InfoPanel |

Page 与 Popup 不是两个平级运行角色。所有被 EUIManager 按页面管理的对象都遵循 Page 命名，层级和开关策略由 PageType 决定。非页面的 EUISafeArea、ButtonEx 等不应机械追加 Page。

本轮保持已有逻辑类名和大小写，例如 EUIGamePlayPage，不顺手扩展为全仓风格重命名。GMPage 已有正式类名时沿用 GMPage，不额外增加 EUI 前缀制造第二轮迁移。

基础骨架不生成业务逻辑时，没有对应类的字段不必伪造；它的资产名仍表达 Page/Item 职责。界面显示标题可用中文，其变化不改变代码身份。

## base 已知旧名与目标名

以下是编制时存在的旧名与本轮目标；执行前通过真实 GUID、Binding 和代码再次核对。如果目标名已存在，先判断是否已经迁移，不能删除冲突资源。

| 旧 Prefab | 目标 Prefab | 逻辑和注册标识 |
| --- | --- | --- |
| EUIBackgroundPanel.prefab | EUIBackgroundPage.prefab | EUIBackgroundPage |
| EUIMainPanel.prefab | EUIMainPage.prefab | EUIMainPage |
| EUIGamePlayPanel.prefab | EUIGamePlayPage.prefab | EUIGamePlayPage |
| EUISettingPanel.prefab | EUISettingPage.prefab | EUISettingPage |
| EUILoadingPanel.prefab | EUILoadingPage.prefab | EUILoadingPage |
| GMPanel.prefab | GMPage.prefab | GMPage |

这批页面原则上只改资产命名及相应路径、生成来源，不改变 PageType、层级、遮幕、状态机或打开关闭行为。Loading 和 GM 虽不是首次美术重做对象，也纳入命名统一及相关回归。

## S0 只读盘点

建立逐对象迁移表：旧路径/根名、GUID、Binding 信息、目标名、生成来源、页面注册、资源加载键、引用方和冲突。除静态 Unity 依赖外，搜索字符串路径、测试夹具、默认值、创建工具、生成模板、Skill 与当前文档。

精确区分正式文件、备份、历史交付记录和模板快照。历史记录可保留旧名，必须明确是旧版本；.bak 不作为正在使用的代码；模板快照只读。不要全仓替换 Panel 字符串，Unity PanelSettings、无关 Editor 面板和内部容器不属于本迁移。

## S1 实际迁移顺序

1. 核对正式编辑 base，保护受影响的 Prefab、.meta、生成来源与用户文件，记录迁移前 GUID 和相关行为。
2. 先预检目标冲突、资源路径依赖和生成配置；修正中心创建/生成的默认命名，使后续重新生成不会恢复旧的 Panel 名称。
3. 使用 Unity AssetDatabase 的正式移动/重命名能力处理 Prefab 和元数据，保留 GUID。按计划同步根对象名和 Binding 中相关属性，不修改无关条目标记。
4. 对生成资产路径、注册和受管代码修改其真实生成来源，然后通过 EUI 中心重新生成；不直接编辑 .Binding.cs、EmberManaged 或生成注册区。
5. 处理非生成用户代码、资源键、配置、当前文档和技能中的实际引用。GUID 引用通常随资产移动保持，但字符串加载路径和动画绑定必须单独检查。
6. 保存、重新加载资产，验证根名、文件名、Binding、类名与 PageDef 对应一致。每个页面只保留一个正式资产，不能留下同 GUID 副本或两个有效注册。
7. 通过 MCP 完成本轮编译和相关运行验证，确认主界面、设置、Gameplay、Loading 与 GM 的原有行为保持。
8. 将旧名到新名的结果及保留 GUID 记录交接，通过项目中心普通保存 base；派生模板按正式父级同步/迁移流程处理，不手改其快照。

若已有消费项目使用旧字符串路径，记录受影响版本与项目。默认在明确迁移范围中更新引用；确需临时兼容映射时注明删除条件，不能永远维护两套正式页面名称。

## 长期校验与工具行为

创建中心应优先从统一逻辑名推导 Prefab 与页面标识，并在允许特殊情况时显式说明原因。新建 Page 使用 Panel 后缀应提示改正；实际 API 的既有调用方式通过迁移或兼容保持可用。

EUI 契约增加规范名、原名映射及迁移状态。阶段内旧名只用于定位历史来源；S2 到 S7 的新示例和生成结果必须采用新名。不能因文件名正确就跳过 Page/Item 与 PageType 验证。

## 验收

B06 验证迁移资产 GUID 保持、生成来源和运行引用一致；B07 验证新建及重复生成不会恢复 Panel 名称。旧名残留扫描只允许明确历史、备份、迁移记录或确有用途的内部布局节点，每项剩余例外有解释。
