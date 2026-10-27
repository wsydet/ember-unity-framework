# B02 框架验收证据

工作起始日2026-10-27；本轮实际验收北京时间2026-10-28。Unity6000.5.4f1，工程 `ember-unity-framework`。
最终结论：框架定向8/8通过、最终代码连续两次真实脚本重载通过；B02整体保持未关闭。

## 最终有效回执

- [最终代码主动编译状态](final-fix-compile-state.json)、[Console](final-fix-compile-console.json)、[首次读取](final-fix-first-read.json)。
- [8项定向测试](final-tests-result.json)：缓存失效恢复、256邻域选择、冲突规则、铺设擦除及Undo/Redo、移动，以及3组Sprite/碰撞几何。
- [最终基线](final-domain-before.json)，[第一轮首读](final-domain-1-first-read.json)，[第二轮首读](final-domain-2-first-read.json)。
- 两轮对应的 `final-domain-{1,2}-request/state/console.json` 保存主动编译请求、完成事件时间、Console；探针脚本实际从Generation1改为2，再改为3，各轮新程序集MVID不同。
- [语义/缓存比较](final-comparison.json)：同一根实例；两轮语义字段一致；Sprite全非空；3个临时Sprite总量稳定且旧ID消失；磁盘资产哈希不变。
- [第一轮Game相机画面](final-domain-1-game_view.png)、[Scene画面](final-domain-1-scene_view.png)；[第二轮Game相机画面](final-domain-2-game_view.png)、[Scene画面](final-domain-2-scene_view.png)。Game相机画面SHA256一致。
- [清理回执](cleanup.json)、清理后的 `cleanup-compile-request/state/console/loaded.json`。

每轮先取得编译/重载完成状态、读取Console，然后运行只读脚本取得原实例GetSprite；之后才截图。最终两轮没有手动Refresh、重开场景或重铺。
Prefab modification原始列表中保留了临时Sprite引用与自动culling bounds的原生缓存变化；不宣称全部override逐字一致。

## 夹具与复现材料

- [创建夹具脚本](create-fixture.cs.txt)，通过Unity MCP execute_code执行；资产目录存在时拒绝覆盖。
- [验收只读脚本](read-first.cs.txt)，不调用Refresh/SetTile/碰撞重建。
- [夹具归档](fixture.zip)，保存创建的Scene、Base/Variant Prefab、Tile和持久PNG及其meta。解压回框架Assets可复核；并非模板分发内容。
- [创建后基线刷新前首读](fixture-before-baseline-refresh.json)单独保存了创建时的空Sprite结果；[基线初始化](fixture-baseline-initialized.json)只发生在验收开始之前。
- [两轮前磁盘资产哈希](fixture-hashes-before.json)。

临时资产和探针现已清理；原FrameworkScene恢复。未修改任何Windward包、模板或关卡。

## 失败尝试与诊断

初稿的资产后处理+delayCall方案没有通过真实域重载。`domain-1-*`、`fix-recompile-*`及`diagnostic-*`保留了失败结果。
其中 [调度诊断](diagnostic-trace.json)记录脚本重载回调1次，但恢复执行0次。不能把这些历史失败写成最终通过，也不能删除失败首读后只展示刷新成功。
最终改为DidReloadScripts+临时Editor update等待，扫描前解除订阅；诊断插桩已移除，最终重新编译、跑8项测试并完成两轮重载。

## 仍未实测

Windward原实例（当前包/模板保持不变）、保存重开场景、正常重开Unity、正式生命周期试玩/停止、Windows构建、Player首次加载、实际SpriteAtlas、大图耗时均未由本轮实测。不得从本次框架重载通过推断这些路径通过。
