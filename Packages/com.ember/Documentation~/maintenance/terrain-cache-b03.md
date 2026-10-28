# B03：Player 场景首次加载的规则瓦片渲染引用

2026-10-28，随0.24.3交付。B02 的 Editor 恢复实现保持不变；退出崩溃与字体问题分别验收。

`EmberTerrainTile` 生成的渲染 Sprite 使用 HideAndDontSave，不会随场景序列化。
Tile 的 OnEnable 准备新 Sprite，只恢复了资产自己的字典；反序列化 Tilemap 的原生渲染数据仍可能引用缺失 Sprite，
不会因字典重建而自动重新请求 GetTileData。原始三格持久资源在 Windows Player 首次和重载均复现规则/fallback=null，
普通 Tile 正常；失败回执保存后显式 RefreshAllTiles 立即恢复。

0.24.2 没有正式公开的运行时缓存自动恢复 API。`EmberTerrainEditing.Refresh` 属于 Editor；
原生 `Tilemap.RefreshAllTiles` 是手动入口，不能替代框架加载保证。

新增内部 `EmberTerrainRuntimeLifecycle` 在 BeforeSceneLoad 幂等订阅 SceneManager.sceneLoaded，
每次只遍历本次加载场景的根及子 Tilemap（包括 inactive），检查使用的 Tile 类型，
只对含 EmberTerrainTile 的图执行一次原生 RefreshAllTiles。回调完成后、Start/首次渲染前取得有效引用。
不在资产 OnEnable 访问场景；不增加每帧全场扫描、不遍历稀疏 cellBounds、不复制通用恢复系统。
普通纯 Tile 图跳过，混合图普通 Tile 保持。规则算法、临时 Sprite 所有权/清理和源顶点/UV 均未改。

无需在 GameInit、Scope、参与者或每个关卡编写刷新代码；正式场景流程仍等待自己的加载和参与者准备。
此保证针对正常 SceneManager 场景加载，包括重入及卸载再加载；不承诺 Awake 内提前查询已恢复，
也没有验收加载完成后另行反序列化自定义 AssetBundle/Prefab 的所有加载器。

框架三格混合图与新建原生 15 格 Prefab Variant 已完成两轮真实域重载、Windows 首次/卸载再加载和画面检查。
Windward 的实际 AuthoringSecretWall/正式产品流程尚未安装该候选，不能以本夹具代替消费端签收。
SpriteAtlas 打包路径未覆盖，当前 SpritePackerMode=Disabled，不改变消费项目设置。

各项结果及失败回执见[本轮实施记录](../../../../docs/implementation/b03-player-exit-font-20261028.md)。
