# 引导编辑器转入 base：恢复记录

用户决定放弃当前 2.5D 模板开发副本，由用户手动切换 base 后，代理将本轮引导编辑器改动恢复到项目 Assets，再由用户通过模板面板保存和 Bump。

## 完成的改动

- GuideEditorWindow：独立左右分栏、步骤拖拽排序、新增、深复制、删除、撤销、当前资产保存、配置检查。
- GuideEditorFields / GuideEditorModel：全部 13 个步骤字段、中文事件/条件/执行器、14 种参数映射、嵌套 AND/OR、参数创建和只读诊断。
- GuideEditorPicker：按 GamePages 及 EUIBinding 中文用途选页面，带入页面类型和层级；按当前运行器实际节点名称选择控件。
- GuideDefineEditor：共用绘制，Inspector 打开窗口；双击 GuideDefine 也可打开。
- GuideEditorMenu：读取 GuideModule 的 EmberModuleAttribute.Enabled；false 隐藏顶部菜单，true 在编译重载后注册，无每帧轮询、无模块单例创建；Inspector/双击入口保留。
- Unity 6000.5 编译修复：OnOpenAsset 中使用 EditorUtility.EntityIdToObject((EntityId)instanceId)，移除 obsolete 的 InstanceIDToObject。
- Assets/Tests/Editor/GuideEditor：3 个 EditMode 用例，覆盖嵌套引用深复制、新建空步骤与 Undo、只读校验。测试目录不属于业务模板快照。
- 使用说明、设计文档、文档索引和 Unreleased 记录已更新。

## 恢复规则

1. 用户确认已切回 base 后，读取 Assets/Editor/EmberEditingTemplate.json，确认 templateId 为 base。不要代用户切换、放弃或保存模板。
2. 校验 zip 内每个文件的 SHA256 与 manifest.json；只恢复 manifest 中 category=editor 的文件到项目 Assets。保持 .meta GUID，恢复前检查目标是否为原 base 版本或与备份相同；出现其他改动先对照合并，不盲目覆盖。
3. category=tests 的文件若仍在工作区且 Hash 相同无需重写；缺失时可恢复；已有不同内容需先比较。
4. category=documentation-reference 是文档参考快照，按当前状态合并；不能整份覆盖后来追加的 CHANGELOG 或文档。恢复后更新设计说明中“当前开发副本 source3d-2p5d / 待转入 base”的描述。
5. 不恢复源 Assets/Editor/EmberEditingTemplate.json，不触碰 Templates~、ParentSnapshot~ 或任何模板 Hash；不携带 2.5D 业务或此前的 EUI Prefab 覆盖。
6. GuideModule 本轮未改动，仍为 Enabled=false；不自动启用引导。确认是否启用仍按用户需要，启用后完成编译顶部菜单才出现。
7. Unity MCP 不可用：此前只有静态检查，尚未完成 Unity 编译、3 个测试或窗口交互验收；EntityIdToObject 修复也尚未取得编译结果。恢复后依 CLAUDE.md 验证并明确提醒手动编译。
8. 用户验收后在模板面板保存/Bump base，再按父子同步流程更新派生模板；未授权自动发布新版本。

## 明确排除

动态字体缓存、slnx、Table 生成文件状态、2.5D 专属配置及已发布 0.12.6 内容均不属于此次恢复。未执行 git stash、restore、reset、模板切换或删除。

## 备份定位

- ZIP：`C:/Users/wuyu/My/ember-unity-framework/Library/EmberGuideEditorHandoff/20260912-154851/guide-editor-base-handoff.zip`
- SHA256：`31a3471fb5fc6a3cff29073bb826861d2fbac86f620dcf9a2bc17d5cdd9161f7`
- 文件数：23（含源代码、meta、测试及文档参考快照）。
- ZIP 内 SHA256 已逐文件回读验证。

## 恢复完成：2026-09-12 16:05:31

- 已确认当前编辑记录为 base 0.6.1。
- 备份 ZIP 及全部 23 个文件校验通过；恢复 11 个编辑器文件，保留 1 个相同 meta；7 个测试文件/目录 meta 原样保留。
- 恢复后代码与备份逐文件比对一致，包含 EntityIdToObject 修复和按 Enabled 控制菜单。
- 文档已更新为 base 待保存；模块开关仍为 false。
- 未改写 Templates~、父快照、模板元数据、当前编辑记录或 2.5D 业务文件。
- 下一步：用户手动编译和验收，随后在模板面板保存并 Bump base；Unity 编译及 3 个测试仍未验证。
