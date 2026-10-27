# S1 命名迁移与生成往返

通过 AssetDatabase.MoveAsset 移动六个 Prefab；通过 PrefabUtility 保存根名及 Binding prefabName/pageName；对 Extension 条目按实际组件规范化为完整类型名。FrameworkScene 的六个根名 override 用 Unity API 更新。Framework/PageType/类名、布局、排序和遮罩保持原职责。

| 原名 → 规范名 | 保留的 GUID | PageType |
| --- | --- | --- |
| EUIBackgroundPanel → EUIBackgroundPage | 70cb3c02d6d5b064eb30f18186932548 | Background |
| EUIMainPanel → EUIMainPage | 9b27fbe343055e94fb5f3ae24a87ee96 | MainPage |
| EUIGamePlayPanel → EUIGamePlayPage | 27bddabca8ed57a489e17f0d937534bf | MainPage |
| EUISettingPanel → EUISettingPage | 13157152ce048b740bd1e740a35951af | FullScreenPopup |
| EUILoadingPanel → EUILoadingPage | 2ddab893e6d6e3248935b57e43ee4063 | TopMost |
| GMPanel → GMPage | bba4cad8012e3f64690d24af5becf55d | FreePage |

正式目录：Assets/GameResource/Resources/UI/Common/Prefabs。旧文件不存在；GM 内部 Panel_GM 未重命名。均通过 EUIBindingCodeGenUtility.TryRegenerateCode 更新 Binding/注册，生成器负责同步生成头，不手改生成结果。GamePages.cs 仍是框架注册，示例注册写入 GamePages.User.cs。

创建默认 NewPanel 改为 NewPage；新建 Page 的 Panel 后缀会在预检被拒绝。相关开发中心测试夹具已使用 Page 名，非 Page 的历史 Panel 测试不做全仓改写。Loading 测试和 docs/user/UI开发参考.md 的有效引用已同步。

主界面/设置页往返实测：正式保存资产 → 读取契约 → TryRegenerateCode → SaveAssets → 重新读取，用户 .cs 文件字节不变、Prefab SHA256 不变、契约差异 0。生成 `.Binding.cs` 的时间戳更新属于生成元数据。六页原有 .cs 相比 S1 备份只同步 prefab name 头；Framework 受管业务与块外用户逻辑保留。

新增示例由 EUICreationService.TryBuildPlan/Create 创建，再使用正式绑定/再生成 API：

- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIOwnershipExamplePage.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIOwnershipLeafItem.prefab
- Assets/Game/UI/Runtime/UIFoundation/Page/EUIOwnershipExamplePage.cs 与生成 Binding
- Assets/Game/UI/Runtime/UIFoundation/Item/EUIOwnershipLeafItem.cs 与生成 Binding

示例 Page 再生成后仍使用 Page，契约 diff=0，blockers=[]。UILogic 生成缺陷已修复：组件字段从 ControlMap 取，子逻辑从 GetChildLogic<T> 取，运行验证证明非空且只初始化一次。

旧六名范围扫描见 old-name-scan.txt，当前有效源码/Prefabs/场景/测试/用户文档无残留；仅 Assets/Game/UI/GamePages.cs.bak 的六条旧历史文本保留。S0 证据、迁移规范、语义旧名映射以及未同步的派生模板快照是明确历史/升级输入，不全仓替换。InventoryPanel 的少量 UIModuleEditTests 路径规范化/历史 API 测试不是正式页面；当前 GamePages.User.cs 示例注释已改 InventoryPage。

保护位置：.utmp/ui-production-S1-20261026；backup-diff.txt 列出本轮对原有 Assets 的变化。没有加载覆盖 base、回退原工作区、修改 PackageCache 或 Burner，也没有触及派生模板的快照。
