# Ember 0.24.2

2026-10-28。B02规则瓦片缓存生命周期补丁：脚本编译/域重载后，已打开场景中的 EmberTerrainTile Tilemap 自动重新取得临时渲染 Sprite，无需作者手动重建。

发行包为 `Packages/com.ember`，标签 `v0.24.2`。本次只提交 B02接线、定向测试、问题与验收回执及必要发布资料。保留其余 UI、FSM、字体、项目 Assets/ProjectSettings 等工作区修改。B01已关闭，不在本补丁中重新处理。

## 修复与边界

新增内部 `EmberTerrainCacheLifecycle`，通过 `DidReloadScripts` 和Play进出通知合并排队；临时订阅 Editor update 等待编译、导入结束，扫描前解除订阅。只对使用 EmberTerrainTile 的已加载 Tilemap 调用原生 RefreshAllTiles；包括inactive对象与有效Prefab Stage，不访问未加载场景或写入磁盘Prefab资产。

保留原有临时 Sprite 清理与规则算法，不重铺、不改写格子、Grid、Anchor、碰撞设置或Prefab来源。没有常驻每帧全场扫描。混合Tilemap会进行一次原生全图重新求值；大图耗时仍未实测。没有新增消费端调用要求。

初稿delayCall调度没有通过真实重载；回执显示重载通知触发但恢复回调未运行，最终改为临时update调度并重新完成验证。失败首读和诊断保留，见[证据索引](evidence/terrain-cache-b02-20261027/README.md)。

## 验证

- Unity6000.5.4f1框架工程通过主动MCP编译，确认本轮编译/域重载结束及最终程序集加载后读取Console，error/warning为0。
- 最终代码8项定向EditMode回归全部通过，覆盖缓存失效恢复、规则选择/冲突、铺设擦除、移动、Undo/Redo和旋转镜像几何，未全量重测。
- 已打开隔离场景的实际Prefab Variant实例连续两次真实脚本编译/域重载通过：探针程序集MVID各轮不同；每轮先读GetSprite，再截图，未手动刷新或重开场景。
- fallback、命中规则及普通Tile渲染正确，12格/1个Composite轮廓、TileBase、颜色/矩阵、Grid/Anchor、排序、Prefab来源保持一致。临时Sprite总量3→3→3，旧ID消失；两轮Game相机画面SHA256一致，Scene画面已检查。
- 原生Sprite缓存引用和自动culling bounds的Prefab modification列表有差异，不宣称所有override逐字一致；场景、源Prefab、Tile和图片的磁盘哈希保持不变。
- 发布门禁与模板实际内容/父快照校验见[发布检查记录](evidence/terrain-cache-b02-20261027/release/README.md)。复用同一最终源码的8项测试和两轮重载证据，纯版本/文档变化不重复全量测试。

## 未验收路径与B02状态

**B02暂不关闭。** Windward原实例、保存重开场景、正常重开Unity、正式生命周期试玩/停止、Windows构建、Player首次加载、实际SpriteAtlas和大图恢复耗时均未由本补丁实测。Game相机截图为Editor捕获，不替代正式Play生命周期或Player验收；不沿用0.24.0构建或旧实现重开对照冒充本版结果。

发布只交付已验证的Editor脚本域重载修复，不据此断言完整进程重开或Standalone存在/不存在同样故障。完整问题与步骤见[随包说明](../../Packages/com.ember/Documentation~/maintenance/terrain-cache-b02.md)。

## 模板、依赖与消费接入

仅框架patch：platformer-2d仍为0.13.0，其余模板内容、封存hash、父快照、0.24.0兼容声明、第三方依赖和技能bundle保持0.24.1基线。不保存/Bump/部署模板。

本轮不升级Windward的包或模板，也不修改PackageCache。未来获得消费升级授权后，通过 `Ember/UPM Manager` 安装0.24.2，再对E05原场景与原Variant实例执行先读结果的定向复验；无需为此重部署模板或重铺地图。回退通过同一UPM Manager选择0.24.1，原有手动刷新入口仍可用于诊断。
