# S2 编辑器面板与蓝图入口纠正（2026-10-26）

范围：用户要求公共预制体库面板使用 Odin，并明确选择“只在使用 FSM 蓝图的模板中显示；base 隐藏入口”。本轮修改框架 Editor、相关测试和文档，不进入 S3，不加载或覆盖其他模板。

结构化结果、测试失败堆栈及新增脚本 GUID 见 [验证记录](editor-panels-validation.json)。既有 Prefab GUID 无变化。

## 已实现

- `Packages/com.ember/UIExtension/Editor/Pages/EUILibraryPanel.cs` 改为窗口拥有的 Odin 浏览器实例。分类搜索、单项详情、允许覆盖、分页槽位、打开/定位/查询引用方；继续调用既有校验与目录服务。`EUIPrefabManagerWindow.cs` 管理刷新和 Dispose。详情数据只读；槽位禁止增删，但展开控件可用。Unity 有 AssetPreview 时显示预览，UI Prefab 无可用预览时通过“打开 Prefab”查看，不把缺失缩略图称为完成预览。
- `Packages/com.ember/Core/Editor/EmberFsmBlueprintAccess.cs` 新增模板能力登记。菜单为 `Ember/FSM/蓝图观察器`，移除旧 Tools 分支。参数/观察源登记支持 ownerTemplateId 与 enabled，回调执行前检查；公开 Open 和旧窗口操作同样检查。窗口保存精确活动模板身份，变化后停止观察、禁用编辑，保留图/预设/本机布局。
- 修改 BlueprintWindow 的 Runtime、Navigation、LayoutPreview 分部，参数与观察源服务；增加 BlueprintAccessTests，原布局测试用显式测试能力登记，不在生产代码中绕过 base 隔离。
- 纠正文档中“公共工具不受模板限制”的错误豁免。权威契约为包内 [FSM 蓝图能力与模板隔离](../../../../../Packages/com.ember/Documentation~/manual/fsm-blueprint-access.md)，根 CLAUDE、业务隔离规范、架构、FSM 和 API 索引均有入口。Odin 与公共库手册同步。

## 已验证

- 全部 Unity 操作锁定 `ember-unity-framework@b23e9b768e85ed30`，projectRoot=`C:/Users/wuyu/My/ember-unity-framework`；Unity6000.5.4f1、Odin4.0.3，真实活动模板为 base。
- 每批脚本修改主动请求 MCP compile 并核对完成及域重载。最终 started=`1793020213815`、finished=`1793020218314`、domainReloadAfter=`1793020225054`；编译后读取 Console 没有错误。过程中有两条既有 Core 测试 UAC1001 警告，以及临时截图脚本释放 active RenderTexture 的三条警告；截图脚本已修正恢复顺序，未改运行时代码，不把过程 Console 记为全程零警告。
- 相关用例共23项：蓝图能力4、条件菜单2、观察/布局2、正式模板身份及父链15。初次8项为6通过2失败，两个失败均在新测试调用未显示窗口的 Close 清理处；改为 DestroyImmediate 并编译后新类4/4通过。没有删除失败记录或全量重测。
- 初次 job=`ec2b69ad15fe462d956b38d4d376b37b`；能力复测=`c9bc2826669f4df894dd457968ac9556`；身份15项=`f0c5b80f274b492c89375ad1b1774165`。
- base 实际菜单检查：新旧菜单均不存在、IsAvailable=false、公开 Open 拒绝。临时测试能力登记后新菜单存在，关闭模块条件后菜单移除，释放登记后保持隐藏；未修改正式模板身份。
- 公共库真实读取37项，树中37个资源条目，Validate无问题，目录摘要当前。实际发送 Unity 窗口鼠标事件选择 AmountButton 后详情稳定 ID 变为 `ember.amountbutton`。最终界面见 [Odin 浏览器](editor-library-odin.png)，槽位展开见 [槽位详情](editor-library-slots.png)。搜索改为ID/Family/Prefab名/别名的忽略大小写子串匹配，slider准确返回Slider和SettingSliderRow两项，见 [搜索结果](editor-library-search.png)。
- 截图使用目标窗口自身的 GUIView 渲染。最初屏幕像素截图捕获了其他前台窗口，已替换，不作为证据；Unity CaptureEditorWindow 在150%缩放下裁切，最终按物理像素尺寸调用同一 GUIView.GrabPixels 与 Unity 的捕获材质得到完整窗口。未操作其他 Unity 工程。
- 浏览/编译前后 DefaultLibrary.asset、catalog.json 及各自 meta 的 SHA256一致；没有资源、GUID、Binding、用户逻辑或皮肤改动。本轮不生成图片，皮肤仍1.1.0。
- 文档 strict 全仓检查仍有65项其他历史/未加载模板链接问题；本轮修改的文档没有 missing_local_target。未把全仓检查称为通过。

## 保存与未验证边界

本轮只修改包内编辑器及仓库文档，无默认资源改动，因此没有再次保存 base。沿用上轮项目中心普通保存：567文件、base0.8.4、framework0.22.0、contentHash=`20a0b4a5bfb39b175a5ca567ed4229b9`，versionedContentHash=`936dcb1d411afefcacd961cbad1d1d0d`。未 Bump、提交、推送、发布或消费升级；未改 Templates~/ParentSnapshot~/PackageCache。

没有实际切换 platformer-2d/派生模板、消费部署或修改未加载模板接线。旧 Register 不传 owner/module 时仅捕获登记时正式身份，不能替模块表达禁用条件，也不能修复错误保留的业务接线；后续在对应模板加载后的 Assets 维护源迁移显式参数，不手改快照。身份单元测试不是跨模板完整菜单实测。

未完整遍历所有 Prefab 的 Unity 缩略图、所有槽位分页、不同停靠布局和不同操作系统。此次只验收编辑器浏览与隔离；S2 原资产/Play结果沿用各自证据，不重复宣称本轮全量运行验收。S3入口保持原记录，不新增容器或对象池。

工作区保护备份位于 `.utmp/editor-panels-20261026/`，完整 Git 状态记录在 `.utmp/editor-panels-final-status.txt`，文档审计在 `.utmp/editor-panels-doc-audit.json`。原有模板切换删除和其他工作区内容不恢复、不提交。
