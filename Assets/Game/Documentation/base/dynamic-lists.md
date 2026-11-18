# 动态列表中性示例

Play 后调用 `EUIManager.Instance.ShowMainPage(GamePages.EUIDynamicListPage)`，命名空间分别为 Ember.UI 和 Game.UI。默认主菜单不增加入口。页面支持0/1/3/20行、反转、删除末行、清空及返回主菜单；点击行后标题显示当前数据 key 和位置。

正式资源位于 `Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/`：EUIDynamicListPage 继承 EUIStandardPage；EUIDynamicListItem 继承 EUIContentHostItem，配置 ScrollRect、行模板和20个闲置缓存；EUIDynamicOptionRowItem 继承原 EUIOptionRowItem 的资源与 Binding，增加动态高度、图标和进度。图片复用 DefaultLight Help/Search，无新素材。

用户逻辑位于 `Assets/Game/UI/Runtime/Module/UIFoundation/`。EUIOptionRowData 的 Id 为稳定 key，Title/Icon/Selected/Interactable/Progress 为显示数据。业务规则不进入通用容器。

首次制作入口 `Game.UI.Editor.DynamicUIBuilder.Build()` 仅适用于活动 base 体系，调用正式创建/绑定/生成服务，已有资产跳过，不重新覆盖美术。编辑已有资产后通过 UI 开发中心重新生成。公共 API 和所有权详见包内 `Documentation~/ui/dynamic-lists.md`。

刷新采用整表重新绑定；缓存实例/逻辑，结束旧借用的订阅和取消源。页面 Close/Dispose 释放全部实例，宿主 OnDestroy 兜底。Hide 只是临时隐藏。没有虚拟列表、多模板或页签。动态模板不可同时登记父级 UILogic。

编辑器实际使用 EditorUIResourceProvider/AssetDatabase，Prefab 依赖由外部提供者持有，容器不创建独立加载句柄。引用计数卸载、Addressables 和消费构建全链路未在此示例中验证。
