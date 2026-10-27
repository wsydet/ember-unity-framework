# 资源回归执行结果

通过 Unity MCP execute_code 执行同目录 `resource-roundtrip.cs.txt`，返回 success=true。

- 自定义尺寸 3.5×0.5 新增，更新为 4×0.5 并改变格锚点；GUID 不变。
- 地形、机关、背景、装饰各新增一次、原路径更新两次；GUID 不变，每个 ID 只有一个目录项。
- 0.5×0.5、1×1、0.5×10、10×0.5 的实际 TilemapCollider2D 世界 bounds 与标称宽高一致。
- Prefab 放置、复制、删除及 Undo/Redo；复制实例保留 Prefab 连接。
- 装饰旋转、镜像、排序后无 Collider2D；保存并重开场景，三个根对象保留。

资产位于 `Assets/Game/Development/LevelAuthoring/Acceptance`。这些是公开 API 的制作流程回归，不代表干净消费工程或人工绘制手感已验收。
