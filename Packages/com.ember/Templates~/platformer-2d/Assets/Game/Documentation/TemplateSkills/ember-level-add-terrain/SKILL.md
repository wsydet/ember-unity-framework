---
name: ember-level-add-terrain
description: 为 platformer-2d 及派生项目新增或更新白盒尺寸、地形连接规则和素材登记，并验证铺设、邻域刷新、碰撞与撤销。
---

# 地形制作

维护源属于 platformer-2d。执行前通过 Unity MCP 调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-level-add-terrain")`；非空时停止业务写入并报告原因。确认已加载 `Ember.LevelAuthoring.Editor.EmberTerrainEditing` 与 `EmberLevelAssetService`，不要仅按版本号假定有API。最低交付组合为Ember 0.24.0与platformer-2d 0.13.0，版本/验收边界见 `Assets/Game/Documentation/platformer-2d/LevelAuthoring.md`。

读取实际 Grid、Tilemap、样式和已有尺寸预设。输入包含宽×高世界单位、格锚点偏移、兼容网格、实体/单向/无碰撞、连接组；美术输入还需八邻域映射、允许变换及回退Sprite。缺图逐项列出，不合成用户未允许的镜像。尺寸不能整除网格时报告具体比值，不改Grid。

尺寸始终为世界单位。目标有缩放、镜像或平面旋转时，用 `EmberTerrainEditing.GetWorldCellSize(tilemap)` 测量两条格轴，再用 `GetCellCount` 预检；不要直接以样式的局部CellSize计算世界占格。保留原Transform和Tile Anchor，不把旋转后的世界AABB当作印章宽高。非整除、退化或剪切网格阻止并报告。

人工入口：`Ember/Level/2D 关卡制作工作台` → 地形 → 制作尺寸预设。自动化使用同一公开服务：

地形使用原生Grid碰撞；框架将源Sprite顶点归一化到语义格并保留UV，避免PPU缩放连带改变碰撞。不同图片Pivot或Tile Anchor也应核对实际视觉边界。更新图源/规则后显式Configure并刷新选定区域，不能只凭Inspector尺寸宣称一致。

```csharp
var terrain = UnityEditor.AssetDatabase.LoadAssetAtPath<Ember.LevelAuthoring.EmberTerrainTile>("Assets/Game/Development/LevelAuthoring/Solid.asset");
var preset = Ember.LevelAuthoring.Editor.EmberLevelAssetService.SaveSize(
    "Assets/MyLevels/Platform.asset", "my.platform", new UnityEngine.Vector2(3.5f,.5f), UnityEngine.Vector2Int.zero, terrain);
Ember.LevelAuthoring.Editor.EmberLevelAssetService.SaveMaterial("Assets/MyLevels/PlatformMaterial.asset", "my.platform",
    m => m.Configure("my.platform", "短平台 3.5×0.5", Ember.LevelAuthoring.Editor.EmberLevelMaterialKind.Terrain, null, null, preset));
```

先创建/选择项目自有目录，查找同ID再更新原路径；不得另生成同ID资产。更新已有预设保留ID/GUID，影响后续放置；已放置格子只在用户指定选区显式变更。规则资产用 `Upsert<EmberTerrainTile>` + `Configure`，同组邻域位序为右/上/左/下/右上/左上/左下/右下。规则检查用 `ValidateRules`；它覆盖256种邻域，冲突和缺图使用回退。规则变更不批量打开并改写关卡。

用 `Stamp`、`Paint`、`Move`、`ReplaceStyle` 操作已选择Tilemap；纯外观替换必须物理类型、网格与锚点相容。样例位于 `Assets/Game/Development/LevelAuthoring`。在候选关验证矩形、L形、内洞、孤立、窄条和异组边界；追加/擦除/移动及Undo/Redo不增加未选格子。读取实际TilemapCollider2D，单向需实际Effector。保存重开核对占用/视觉/碰撞。新增与更新各执行一次，再重复更新确认路径和ID不变。

新资产不是场景Undo的一部分；服务创建失败只删除自己创建的文件，更新失败恢复原序列化值。多资产工作先记录新建路径，登记失败时只回退本次新增文件，不删既有用户资产。按项目Unity规范编译和增量验证。交接列出路径、ID、尺寸/占格/物理类型、实际通过与失败及未验证项。
