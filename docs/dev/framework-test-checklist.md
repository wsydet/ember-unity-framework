# 框架回归与发布验收清单

> 当前用于 0.11.5 发布与消费端验收。模板 hash 和版本静态检查沿用 0.11.4 基线并重新核对兼容声明；Unity 编译、EditMode、UnityFarm 安装与 Play Mode 以本轮实际执行记录为准。

本轮状态：包与父子模板已声明 0.11.5；模板内容版本和 hash 未改。0.11.5 只新增随包的 UnityFarm 改动分流文档、代理强制入口和版本声明；不能由静态检查推断 UnityFarm 的安装或运行兼容已经通过。
> 旧 v0.8.0 临时计划记录过 2026-08-31 的 51 项通过和 Farm 冒烟，属于历史记录，不证明当前新功能通过。

## 记录方式

每次记录框架版本、commit/工作区差异、Unity 版本、模板版本、安装形态、执行日期和结果。
自动编译只用 Unity MCP；MCP 不可用就记录未验证，按 CLAUDE.md 交给用户手动编译。
模板变更测试在专用测试模板/消费工程内通过项目中心操作，不手改正式快照、hash 或部署记录模拟成功。

## 当前测试程序集

| 程序集 | 源码范围 |
|---|---|
| Ember.UI.Tests | `Packages/com.ember/Tests/EditMode`：UI、开发中心、模块目录模板、Core 模板与 UnityYAMLMerge 测试 |
| Ember.SceneUI.Tests | `Packages/com.ember/SceneUI/Tests/EditMode` |
| Ember.SceneUI.PlayModeTests | `Packages/com.ember/SceneUI/Tests/PlayMode` |
| Ember.UPMManager.Editor.Tests | `Packages/com.ember/UPMManager/Tests/Editor` |

测试数量以当次 Test Runner 结果为准，文件名或历史数字不作为通过证据。

## UI 与启动

- [ ] FrameworkScene → BootSplash → Init → Main；黑幕和背景就绪后才显示首页。
- [ ] Main → Settings → 返回、Main → Gameplay → 返回；退出状态清理其页面，Push/Pop 保留下层状态。
- [ ] Popup、FullScreenPopup、遮罩点击与 SettingsState.Pop 一致，没有页面关闭但状态滞留。
- [ ] Loading 从进入开始拦射线；Custom Exit 和方块全部退出后才广播 LoadingFadeOutComplete。
- [ ] 退出 Play 时取消尚未完成的异步过渡，无销毁后回调访问。
- [ ] 开发中心 Page/Item 创建预检、重复生成、UIUpdate 与可选 Popup 钩子、Settings、GamePages 路由正确。
- [ ] Item 不生成 PageDef、无独立 Canvas；首次使用、隐藏、回池、再借出与最终 Dispose 顺序正确。
- [ ] 模块目录模板仅补缺、预览重命名、冲突阻断、资源 GUID 保持。
- [ ] 删除 UI 的共享脚本、复杂注册条目和越界路径保护有效，孤儿清理只处理可确认对象。
- [ ] 标准 Page 的 CanvasScaler、公共 SafeArea、Animator、字体和绑定都有效。

增强控件与 GM 历史布局问题使用 [绑定回归](uiextension-test-plan.md)；其他工具见 [编辑器清单](editor-tools-test-checklist.md)。

## 模板与场景合并

- [x] 原双页项目中心的 embedded/consumer 分流已验收；新增项目校验页按下面的新批次重新验收。
- [x] 创建派生模板生成完整 Assets 和 ParentSnapshot，起始 0.1.0/preview；谱系循环、父缺失和重复 id 阻断。
- [x] 保存只更新 contentHash；Bump 封存；父未 Bump 阻止同步；同版本不能封存新内容。
- [x] 旧编辑副本或不同编辑模板不能覆盖目标；另存为保留明确目标。
- [x] 三方新增/修改/删除分类、双方相同修改、同名不同新增、父删子改和父改子删均符合规则。
- [x] 资产与 meta 成对选择，目录 metadata、大小写碰撞、GUID 变化/重复得到处理。
- [x] 未解决冲突、预览后文件变化、工具/规则变化、结果 hash 变化时零提交。
- [x] stage 提交任一步注入故障后 Assets、ParentSnapshot 与 metadata 全部恢复。
- [x] 仅 `.unity` 的合法并发修改进入 Smart Merge；缺工具/超时/无效 YAML/同属性冲突回退整场景选择。
- [x] 旧父场景上父改 GameBoot、子改相机，结果同时保留；包含 PrefabInstance/stripped 对象的场景导入正常。
- [x] 同步内容改变必须 Bump；当前正在编辑该子模板时面板重新加载，其他模板副本保持原状。
- [ ] 新消费工程首次部署 stable/preview 成功；同模板补缺保留已有用户内容；跨模板部署被阻止。
- [ ] 消费记录单条迁移/多条歧义处理、deprecated 隐藏、major.minor 兼容过滤正确。
- [ ] UI/脚本与 meta 完整落盘后再导入，不出现部署后随机 GUID 或 Missing Prefab。

专项记录（2026-09-07）：Unity `6000.5.4f1`。用户确认 Unity 编译、相关 EditMode 测试、真实 UnityYAMLMerge 集成测试，以及非冲突/同属性冲突两条手工 O/N/C 流程全部通过；非冲突合并后派生为 `source3d-2p5d 0.2.5 / preview`、父基线 `base 0.5.4`。收口时根模板已前进为 `base 0.5.5 / stable`，派生仍基于 0.5.4，当前这一待同步状态不记作再次应用通过。测试数量以当次 Test Runner 为准，本记录不补写未保存的数量。消费项目和下列其他发布项不包含在本次通过范围内。

## 项目中心校验与模板面板新批次（待 Unity 验收）

- [ ] 入口直接为 `Ember/项目中心`，无 Setup 中间层级；embedded 显示初始化/模板开发/项目校验，consumer 显示初始化/项目校验；旧校验菜单不再出现，项目校验页仍可执行检查。
- [ ] 新增 `EmberProjectValidationEditTests` 全部通过：只读零写入、业务差异不报错、meta 缺失/GUID 重复、管理区标记、保存场景剥离、历史基准说明和效果过期。
- [ ] 模板树选择不加载，当前编辑对象独立显示；新建面板不被刷新关闭；820×560 窗口内分区与滚动可用。
- [ ] 左右分隔条可拖动，最窄/最宽位置不挤掉操作区域；双击恢复默认，同一 Unity 会话内重新打开窗口保留宽度。长模板名、ID、状态能换行完整显示；两侧独立滚动，缩放窗口后布局仍可用。
- [ ] 内容差异能筛选、分页、定位并查看文本；保存前提示保存场景，取消后可先检查详细差异。
- [ ] 父级同步默认筛选冲突，隐藏的自动变更仍计入版本封存判断；未保存修改阻止自动重载。
- [ ] 正常业务修改只显示差异；UI 缺失脚本/空绑定/注册错误可定位；编辑/部署基准区分正确；失效报告提示重查。
- [ ] Game 视图截图和 PNG/JPEG 导入先记录草稿；保存封存后可确认版本效果，来源变化拒绝确认。
- [ ] 效果文件不进入部署、不改变模板 contentHash；图片被替换或模板版本/内容/兼容声明变化时有提示。
- [ ] 播放/编译/导入期间禁止保存、部署、同步与校验；退出播放后可继续操作。

## SceneUI、PlayerControl 与模块

- [ ] 禁用 Module 不构造、不登记、不更新；启用 Module 只有 OnInit 成功后接受帧更新。
- [ ] SceneUI 气泡位于业务 BubbleRoot，排序、静态手动刷新、动态目标跟随、相机切换与池复用正常。
- [ ] Gameplay 退出清理 Channel/订阅；重入后的旧 Handle 不影响新实例。
- [ ] PlayerControl 等完整 Loading 退出后才接受输入；从 UI 按下不会捕获世界拖动。
- [ ] WASD/拖动不能跨未连接区域；边界反向拖动无空行程；缩放和 SO 实时参数生效。
- [ ] 加载 base 后仍有公共 Input Manager，且无 PlayerControl/SceneUI 业务残留引用。

完整业务验收：[SceneUI](../../Packages/com.ember/Documentation~/scene-ui/README.md)、[PlayerControl](player-control-module.md)。

## UPM 与交付

- [ ] 包版本、目标 tag 与 CHANGELOG 一致；使用可访问的消费依赖，避免重复 UniTask。
- [ ] UPMManager 正确区分 embedded、已安装、远程目标；不接受重复并发升级。
- [ ] 域重载续接、网络/凭据失败、目标不存在和安装版本不符均能报告。
- [ ] 字体许可随包保留，共享资源 GUID 在新消费项目有效。
- [ ] 记录当次结果，恢复测试资产与临时修改，按当前模板状态保存；不把旧版本号硬写回 metadata。
