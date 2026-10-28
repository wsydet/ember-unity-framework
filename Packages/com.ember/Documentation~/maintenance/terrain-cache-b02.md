# B02：脚本域重载后的规则瓦片缓存恢复

后续 Player 加载路径的独立修复与边界见 [B03](terrain-cache-b03.md)；本页保留 B02 当轮历史验收范围。

2026-10-27～28。归属：框架 LevelAuthoring Editor。状态：框架主动编译、8项定向测试、最终版本连续两次真实脚本域重载验收通过；消费端与其他加载路径未验，B02 尚未关闭。

## 证据与根因

已只读核对 Windward `docs/implementation/evidence/e05-closeout-20261027/` 的
`cache-before-domain.json`、`cache-after-domain.json`、`after-reopen.json` 及问题记录。
旧实现真实域重载后 12 格、1 个碰撞轮廓、持久 fallback 均存在，但原 Tilemap 的 GetSprite 为 null；
对同实例显式 Refresh 后恢复。正常退出重开 Unity 的对照成功。这些是消费端既有证据，不是本补丁的验收结果。

源码因果链：`EmberTerrainTile.OnDisable` 释放 HideAndDontSave 渲染 Sprite；`OnEnable` 准备新缓存；
只有 `GetTileData` 把新引用交给 Tilemap。旧实现没有在域重载完成后使已加载 Tilemap 重新求值的自动接线。

已核对的公开入口是 `EmberTerrainEditing.Refresh(Tilemap, BoundsInt)`、Unity 的 `Tilemap.RefreshAllTiles()`，
以及工作台“重建当前 Tilemap 接缝”。规则 Inspector 的应用按钮也显式刷新打开场景。
这些入口可以修复表现，但均不提供自动域重载恢复契约；无需给消费端再增加同义函数或复制恢复系统。

## 补丁与支持边界

新增内部 `EmberTerrainCacheLifecycle`：通过 `DidReloadScripts` 通知排队，
临时订阅 Editor update 等待编译/更新结束，扫描前即取消订阅，然后一次性枚举已加载 Tilemap，
包括 inactive 对象与有效、已加载的 Prefab Stage；磁盘 Prefab 资产与未加载场景排除。
用 GetUsedTilesCount/GetUsedTilesNonAlloc 判断是否使用 EmberTerrainTile，只对命中的图调用原生 RefreshAllTiles。
进入 Play、返回 Edit 也排队，重复通知合并。

不在 InitializeOnLoad 静态构造中访问场景。初稿曾使用资产后处理通知和 delayCall；真实验收失败，未交付为通过。
诊断记录证明重载通知触发1次、delayCall恢复执行0次；编辑器失焦的该路径不能依赖 Inspector 更新驱动 delayCall。
最终改为脚本重载通知加临时 update 等待；临时诊断代码已移除，最终版本重新完成两次独立真实重载。
[RefreshAllTiles](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Tilemaps.Tilemap.RefreshAllTiles.html)
负责重新取得渲染等 TileData；不创建新的占格。

保持原有缓存所有权和 OnDisable 清理，不保活旧 Sprite、不烘焙临时资产。Runtime、规则算法、源 Sprite/图集、
铺设事务、碰撞设置、Grid、Anchor、Prefab 关系均未改。混合图中的普通 Tile 也会随原生全图刷新重新求值，
但没有写 Tile 资产或 SetTile。纯普通 Tile 图跳过。Editor update 只在等待就绪期间订阅，就绪扫描前解除，没有常驻扫描，也不遍历稀疏 cellBounds；
每次生命周期恢复对命中图有一次与实际瓦片数量有关的重求值成本，大图耗时仍须现场测量，不能声称零卡顿。

此补丁针对 Editor 脚本域重载路径。Player 不包含这段 Editor 接线，不能据此推断 Player 成功或失败。
没有新增消费端接线要求；本轮不变更 Windward 当前包/模板、PackageCache、旧地图，也不发布、升版或部署。

## 定向自动用例

包内 `EmberTerrainCacheLifecycleTests.TwoCacheInvalidationsRestoreReferencesWithoutChangingTileState`：
模拟两轮 OnDisable/OnEnable，调用实际脚本重载通知并等待自动一次性恢复，随后先读取 GetSprite；
不在恢复前执行手动刷新。检查 fallback、右邻居规则、旋转镜像、自定义非方形尺寸/Anchor、源 Sprite 顶点/UV、
普通 Tile 引用、占格与 TileBase、颜色/矩阵、排序、碰撞 Bounds、场景 dirty 标志和缓存条目数。
检查每轮旧 Sprite 已销毁，避免仅重建新缓存掩盖泄漏。

该模拟用例不等于真实程序集重载；本轮另建持久源 Sprite、Tile、场景及 Prefab Variant 夹具完成真实两轮重载，
包括12格和1个 CompositeCollider2D 轮廓。源 SpriteAtlas 与大图耗时仍未实测，不能凭模拟测试关闭 B02。

建议只运行本类及 `EmberLevelAuthoringTests` 中以下直接回归：
AllEightNeighborMasksResolveDeterministically、ConflictingRulesAreReported、PaintEraseAndUndoPreserveOccupancy、
MoveUpdatesOldAndNewOccupancy、SpriteAndCollisionMatchNonSquareGridWithCustomPivot。
不默认执行全量程序集测试。

## 真实编译和消费端定向复验

1. 先在框架验证夹具中使用持久源 Sprite、规则 Tile、普通 Tile，保存包含 Grid/Tilemap 的场景及 Prefab Variant 实例。
   放置连续 12 格；另放有右邻居的命中规则格、无邻居 fallback 格；使用 quarterTurns=1/mirrorX=true、非方形尺寸及自定义 Anchor。
   首次建立基线后记录各格 TileBase GUID、Sprite 名称、规则选择、颜色/矩阵、排序、Grid/Anchor、碰撞轮廓、Prefab 来源/override、临时 Sprite 数量与场景 dirty 状态。
2. 保持场景及原实例打开，实际更改/删除临时 Editor 验收脚本，通过 Unity MCP 显式请求 compile=request。
   记录请求回执和目标工程；确认本轮程序集 MVID 变化或明确重载完成事件，确认 compiling/updating=false、scriptCompilationFailed=false，再读取 Console。
3. **先读取原实例 GetSprite，保存结果和 Scene/Game 截图，再做任何修复性刷新。** 不重开场景、不点击工作台按钮、不调用 Refresh。
   必须非空且可见，12 格与1个轮廓及上述语义保持。失败回执保存后才允许显式 Refresh 辅助诊断。
4. 再实际触发一次编译/域重载，重复先读结果，并比对临时 Sprite 数量。用相同资源、相同加载对象集合计数，区分其他工具预览缓存，旧对象须已释放，不能持续增长。
5. 分别保存并重开场景、正常退出重开 Unity、通过正式生命周期试玩/停止、Windows 构建、Player 首次加载。
   每条路径各记录 Sprite、占格、碰撞、画面与错误；Windows 构建与 Player 必须有独立产物/运行记录。
6. 按上述用例/现场操作验证铺设、扩展、擦除、邻域变化与 Undo/Redo，加入已有图集资源和大图恢复耗时检查。

Windward 复验目标仍为 `Assets/Game/Development/Windward/LevelAuthoring/Candidates/E05Mechanisms.unity`，
对象 `E05 windward.mechanism.7` 的既有 Variant 子 Tilemap，基线是12格/1轮廓、Grid=(1,1,1)、Anchor=(.5,.5,0)、walls21_11。
本轮维持其当前包意味着它尚未获得此工作区补丁，因此不能把框架通过写成 Windward 已关闭。
未来在明确授权且通过正式 UPM Manager 交付修复包后重复同样步骤；不修改 PackageCache、不重部署模板、不重铺地图。

## 本轮结果记录

| 路径 | 本轮结果 |
| --- | --- |
| 源码与消费端既有 JSON 证据核对 | 完成；因果解释与既有实测一致 |
| 最小补丁、定向用例静态检查 | 完成 |
| 主动 Unity 编译、域重载完成证据、Console | 框架6000.5.4f1通过；有本轮编译/重载完成时间，最终代码新程序集已加载，error/warning为0 |
| 新增定向测试及直接铺设回归 | 最终代码8/8通过（0失败/跳过），非全量测试 |
| 真实两轮脚本重载、已打开场景/Prefab Variant、Scene/Game | 框架隔离夹具通过；每轮先读取、再截图，无手动恢复性刷新 |
| 保存重开场景 | 未执行 |
| 正常重开 Unity | 本补丁未执行；旧实现消费端对照成功 |
| 正式生命周期试玩/停止 | 未执行 |
| Windows 构建 | 未执行 |
| Player 首次加载 | 未执行 |
| 源图集、大图耗时 | 未执行 |
| Prefab来源关系、Composite轮廓 | 两轮来源关系一致、12格/1轮廓；语义字段一致，原生渲染缓存override有差异，见下文 |

上一轮 MCP 连接失败并重试耗尽；本轮原会话连接仍初始化失败，但同一 MCP HTTP 服务可响应，建立新 MCP 会话后恢复。
所有自动 Unity 操作仍通过该 Unity MCP 服务，目标固定为 ember-unity-framework@b23e9b768e85ed30。
没有使用日志轮询、BatchMode或dotnet替代验证，也没有操作 Windward 的包、模板或场景。

## 2026-10-28 框架真实验收回执

完整回执和可重建夹具见仓库 [证据索引](../../../../docs/dev/evidence/terrain-cache-b02-20261027/README.md)。
目录名保留首次工作日期；本轮现场验收在北京时间2026-10-28完成。

- 最终代码 Editor MVID：`d7b390c0-effe-4916-9994-b480944fc9ba`。
- 临时 Editor 探针程序集 MVID：基线 `9e4407bd-ca20-4972-9b70-c80469068637` → 第一轮 `00486357-d496-4f57-b3ef-e171b3b9bae0` → 第二轮 `c23e1fce-912f-4d32-9abe-93bf7d2172d0`。
- 两轮保留同一个已打开 Variant 根实例和 Tilemap；首次 GetSprite 全部非空，fallback/命中规则/普通 Tile 名称分别正确。
- 格子及 TileBase、颜色/矩阵、flags、Grid、Anchor、排序、碰撞Bounds、1个Composite轮廓、Prefab来源均与基线一致；场景dirty=false。
- 临时 Sprite 总数3→3→3，每轮旧 Sprite ID均不再存在；两轮Game相机截图SHA256一致，Scene截图已检查。
- 允许变化的是临时渲染引用。原始Prefab modification列表还观察到原生 `m_ChunkCullingBounds` 和 `m_TileSpriteArray...m_Data` 缓存项差异，完整保留在比较回执中，不能声称override字节完全相同。场景、源Prefab、Tile与PNG的磁盘哈希在两轮后未变。
- 夹具创建完成后曾读到空Sprite，已单独保存，并在开始最终验收前按复现步骤显式初始化一次基线；两轮验收期间不刷新、不重开场景。
- 临时场景/资产已归档到fixture.zip并清理，原FrameworkScene恢复且dirty=false、仅1个场景加载；临时探针脚本及其已加载类型均已消失。清理后再次主动请求编译，确认本轮编译/域重载结束、failed=false、error/warning为0。

本轮仅确认框架隔离夹具的上述范围。Windward原实例、保存重开场景、正常重开Unity、正式生命周期试玩/停止、Windows构建和Player首次加载仍分别待验，B02保持未关闭。
