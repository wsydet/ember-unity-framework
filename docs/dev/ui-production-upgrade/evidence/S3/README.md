# S3 动态 Item 与容器交接

执行工程：C:/Users/wuyu/My/ember-unity-framework，Unity6000.5.4f1，embedded com.ember0.22.0，活动 base0.8.4。实例枚举与 projectRoot 均已核对，操作固定到 ember-unity-framework@b23e9b768e85ed30，没有路由至 Unity Farm。日期2026-10-26。

## 实际实现与资产

通用源码：UIExtension/Runtime 下 EUIDynamicListHost、EUIItemList、EUIItemLease，以及 UIExt/EUITextRowLayout。契约扩展位于 Editor/Pages/EUIContractService。新增相关测试 EUIDynamicListEditTests。正式使用、所有权表及最小调用示例见 [包内动态列表](../../../../../Packages/com.ember/Documentation~/ui/dynamic-lists.md)。

三项资产均为真正 Variant，完整路径/GUID见 [assets.json](assets.json)：

| 资产 | GUID | 来源 |
| --- | --- | --- |
| EUIDynamicListPage | d5d93f1625ea48d4ea449887c0370258 | EUIStandardPage |
| EUIDynamicListItem | 0236256f3df79bf47b176289da266e6b | EUIContentHostItem |
| EUIDynamicOptionRowItem | 31e58672571f8fc45a944d3915096d0f | EUIOptionRowItem，含 Binding 继承 |

资产在 Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs；用户代码和 Binding 由中心生成在 Assets/Game/UI/Runtime/Module/UIFoundation。实际原 OptionRow 位于 Module/UIFoundation，并非启动提示中的 Common；未移动或复制它。制作脚本 DynamicUIBuilder 在同模块 Editor，走 TryBuildPlan/Create、EUIBindingEditorUtility、TryRegenerateCode；没有手改 Binding.cs/EmberManaged。新增页面注册由生成器更新 GamePages.User.cs。

入口：Play 后 `EUIManager.Instance.ShowMainPage(GamePages.EUIDynamicListPage)`。独立页面提供0/1/3/20行、反转、删除末项、清空及 Back；不加入默认导航。原六页、OptionRow源和用户代码、ContentHost源、DefaultLibrary/catalog均保持，校验见 [preserved-sources.json](preserved-sources.json)。字体 DefaultLightSans 的动态字形/图集缓存因新增文本更新，保留真实 TMP 数据；图片全部复用已有 Help/Search/圆角/对勾，皮肤仍1.1.0。公共库仍37条，新 Variant 通过显式源 Prefab 复用，不宣称已登记新库条目。

策略：单一模板、整表重新绑定、有界闲置缓存，20行达到高水位后反复刷新不再创建逻辑。key 非空、Ordinal 唯一，重复拒绝并保留旧列表；index 每次重排重新给定。事件读取本次借用，取消与代次检查共同阻止迟到写入。回收先清理借用再 ResetForPool；退出先 Item.Dispose 再销毁实例。数据和业务归页面；Prefab 资源归外部提供者；实例/逻辑归列表；订阅/取消源归借用。Hide 保留借用。没有增量差分、多模板、页签、虚拟列表或预加载重构。

## D01–D05 结果

| 用例 | 已验证结果 | 证据 |
| --- | --- | --- |
| D01 | 0/1/3/20/3/1/0刷新，活动项/订阅=数据量；20实例和20逻辑高水位保持，空列表无活动节点 | runtime-counts.json |
| D02 | 重排点击一次命中当前 key/index，禁用项回调0；三次20行打开关闭，每轮创建/销毁20，回调1，结束后活动/缓存/订阅0 | open-close-uniform.json、runtime-counts.json |
| D03 | 同实例复借；文本空、图标空、选中false、interactable true、进度0、事件0、取消true；新借用订阅1；实际延迟300ms的旧图标回调被拒绝 | reuse.json、late-callback.json |
| D04 | 实际纵向ScrollRect/Viewport/Content/RectMask2D；长行93.51，短行72；内容1629.51高于视口509.60；Scroll事件改变归一位置，屏外行未被射线命中 | layout.json、twenty-top.png、twenty-bottom.png、final.png |
| D05 | 真正 Destroy 宿主后 Item已Dispose、实例已销毁、订阅/缓存/活动0、令牌取消；异常绑定与取消重入由相关测试覆盖 | host-destroy.json、tests-final.json |

三次开关前另有混合规模记录 open-close.json（首轮20、后两轮3），保留而不替代统一规模复测。手动调用组件状态、EventSystem PointerClick/Scroll与RaycastAll属于自动运行验证，不声称物理鼠标/触控或人工试玩。

真实资源路径为编辑器 EditorUIResourceProvider→AssetDatabase，页面/Item/图标均为真实资产。容器没有独立加载句柄，计数0；外部提供者 Release 为空实现，不声称物理卸载或引用计数归还。单元测试的 fakeHandles 仅验证租约清理回调，不等于资源加载全链路。独立图片加载器、Addressables、构建后Resources和消费项目部署未验证。

最终相关测试13/13，job36e1675d77e74abf97d8e5895630f3ed，8.88秒；没有全量或压力测试。后续只修正示例Back回调，主动重编译并以真实事件复验。编译/域重载时间和最终冒烟见 [validation.json](validation.json)，返回路径见 back-final.json。最终空错误Console与退出Play状态见 final-state.json。

## 失败、修复和边界

初次测试7/12：新增夹具未设置 childControlHeight，配置验证明确拒绝；补齐后5/5，再增加容量/单所有者覆盖，最终13/13。见 tests-initial.json、tests-corrected.json、tests-final.json。

首张截图 initial.png 暴露宿主没有控制行宽、Variant重复创建Icon；已启用宽度控制并复用原Icon，保留源Prefab。最终图见final.png。Back最初重新请求主菜单，已有页面不会因此退栈；改为 ClosePage(Page)并复验。临时查询脚本曾因 Object 歧义、internal类型直接访问编译失败，均只影响该查询，改为明确类型/反射后成功。域重载出现过 MCP WebSocket warning，目标连接仍可用，没有结果未知写入或重放测试。

初次普通保存发生在 stop 请求已返回、Play退出尚未完成的短暂窗口，服务成功且比较差异0；随后Back修复后等待明确Play=false，再执行最终普通保存。最终事实以template-save.json为准。没有直接编辑Templates~/ParentSnapshot~/PackageCache。

未覆盖：实体输入设备、完整键盘导航、全部多语言/CJK、所有比例/安全区、20行之外的性能、干净消费部署/打包、派生模板同步。缓存不是虚拟列表；所有权和布局验证不是任意自定义组件的无限布局循环证明。

## S4/S5 交接与保存

三个 *.eui.contract.json 是实际最终导出，连续导出diff0、blockers空。早期 DynamicList.eui.contract.json 保留为中途快照，不作为S4最终输入。schemaVersion1/toolVersion1.1、requiredCapabilities含dynamic-list-v1；lists保存List/PrefabRef与稳定GUID、接线、布局和所有权。S4使用最终页面/宿主/行三份契约及DefaultLight素材；不要烘焙动态文字、图标和进度为整图。S5可复用TryBuildPlan/Create、Variant源、绑定/生成服务以及 EUIDynamicListHost；不认识必需能力必须阻断，不能降级为静态Group。

项目中心普通保存结果见template-save.json，版本仍base0.8.4/framework0.22.0，versionedContentHash保持936dcb1d411afefcacd961cbad1d1d0d。无Bump、Git提交、推送、发布或消费升级。任务前状态和拟改文件保护在.utmp/ui-production-S3；原模板切换删除保持。正式base说明随Assets保存，工程证据不进入模板运行依赖。
