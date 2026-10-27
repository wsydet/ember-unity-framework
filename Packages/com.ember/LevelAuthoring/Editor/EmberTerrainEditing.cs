using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>人工与自动化共用的地形事务；预检后整批提交，失败回滚。</summary>
    public static class EmberTerrainEditing
    {
        #region 外部方法
        /// <summary>沿Grid两条格轴测量世界宽高，保留父级缩放/镜像/平面旋转；不把旋转后的AABB当成印章尺寸。</summary>
        public static Vector2 GetWorldCellSize(Tilemap map)
        {
            if (map == null || map.layoutGrid == null) throw new ArgumentException("需要带Grid的Tilemap。");
            Vector3 origin = map.CellToWorld(Vector3Int.zero);
            Vector3 x = map.CellToWorld(Vector3Int.right) - origin, y = map.CellToWorld(Vector3Int.up) - origin;
            if (!float.IsFinite(x.sqrMagnitude) || !float.IsFinite(y.sqrMagnitude) || x.magnitude < .000001f || y.magnitude < .000001f ||
                Mathf.Abs(x.z) > .00001f || Mathf.Abs(y.z) > .00001f || Mathf.Abs(Vector3.Dot(x.normalized, y.normalized)) > .0001f)
                throw new InvalidOperationException("二维物理要求非退化、无剪切的XY平面网格；请显式修复该变换，不会自动规整。");
            return new Vector2(x.magnitude, y.magnitude);
        }

        public static Vector2Int GetCellCount(Vector2 size, Vector2 cell)
        {
            if (!(size.x > 0 && size.y > 0 && cell.x > 0 && cell.y > 0) ||
                float.IsInfinity(size.x) || float.IsInfinity(size.y) || float.IsInfinity(cell.x) || float.IsInfinity(cell.y))
                throw new ArgumentException("宽高与网格必须为正有限值。");
            Vector2 ratio = size / cell;
            if (ratio.x > 4096 || ratio.y > 4096 || ratio.x * ratio.y > 1000000 ||
                Mathf.Abs(ratio.x - Mathf.Round(ratio.x)) > .0001f || Mathf.Abs(ratio.y - Mathf.Round(ratio.y)) > .0001f)
                throw new ArgumentException($"尺寸 {size} 不能整除网格 {cell}，或超过单次100万格限制；请显式选择兼容尺寸/网格。");
            return new Vector2Int(Mathf.RoundToInt(ratio.x), Mathf.RoundToInt(ratio.y));
        }

        public static void Validate(Tilemap map, EmberTerrainTile terrain)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请停止试玩后编辑地形。");
            if (map == null || !map.gameObject.scene.IsValid() || terrain == null) throw new ArgumentException("请选择场景 Tilemap 与地形。");
            var grid = map.layoutGrid;
            if (grid == null || grid.cellLayout != GridLayout.CellLayout.Rectangle || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ)
                throw new InvalidOperationException("首版支持 XY 矩形网格。");
            if (Vector2.Distance(grid.cellSize, terrain.CellSize) > .0001f || map.tileAnchor != terrain.Anchor || grid.cellGap != Vector3.zero)
                throw new InvalidOperationException("网格尺寸、间隙或 Tile Anchor 与样式不兼容；不会自动转换原场景。");
            _ = GetWorldCellSize(map);
            var collider = map.GetComponent<TilemapCollider2D>();
            var effector = map.GetComponent<PlatformEffector2D>();
            if (terrain.Physics != EmberTerrainPhysics.Decoration && (collider == null || !collider.enabled || collider.isTrigger))
                throw new InvalidOperationException("实体地形需要启用的非触发 TilemapCollider2D。");
            if (terrain.Physics == EmberTerrainPhysics.OneWay && (effector == null || !effector.enabled || !effector.useOneWay || !collider.usedByEffector))
                throw new InvalidOperationException("单向平台需要实际 PlatformEffector2D 和 usedByEffector。");
            if (terrain.Physics == EmberTerrainPhysics.Solid && effector != null && effector.enabled && effector.useOneWay)
                throw new InvalidOperationException("实体样式不能写入单向层。");
        }

        public static void Stamp(Tilemap map, Vector3Int origin, EmberTerrainPreset preset, bool erase = false)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            Validate(map, preset.Terrain);
            var count = GetCellCount(preset.Size, GetWorldCellSize(map));
            origin -= new Vector3Int(preset.Anchor.x, preset.Anchor.y, 0);
            Paint(map, new BoundsInt(origin, new Vector3Int(count.x, count.y, 1)), preset.Terrain, erase);
        }

        public static void Paint(Tilemap map, BoundsInt region, EmberTerrainTile terrain, bool erase = false)
        {
            Validate(map, terrain);
            if (region.size.z != 1 || region.size.x <= 0 || region.size.y <= 0 || (long)region.size.x * region.size.y > 1000000)
                throw new ArgumentException("请选择有效的二维范围（最多100万格）。");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(erase ? "擦除地形" : "绘制地形");
            try
            {
                Undo.RegisterCompleteObjectUndo(map, "地形笔划");
                var tiles = new TileBase[region.size.x * region.size.y];
                if (!erase) for (int i = 0; i < tiles.Length; i++) tiles[i] = terrain;
                map.SetTilesBlock(region, tiles); Refresh(map, region);
                EditorSceneManager.MarkSceneDirty(map.gameObject.scene); Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void Move(Tilemap map, BoundsInt source, Vector3Int delta, EmberTerrainTile terrain)
        {
            Validate(map, terrain);
            if (source.size.z != 1 || source.size.x <= 0 || source.size.y <= 0 || (long)source.size.x * source.size.y > 1000000)
                throw new ArgumentException("请选择有效的二维范围（最多100万格）。");
            var positions = new List<Vector3Int>(); var tiles = new List<TileBase>();
            foreach (var p in source.allPositionsWithin)
            {
                var tile = map.GetTile(p);
                if (tile != null && tile != terrain) throw new InvalidOperationException("选区含其他样式；请明确选择单一样式后移动。");
                var target = p + delta;
                if (!source.Contains(target) && map.HasTile(target)) throw new InvalidOperationException("目标范围已被占用。");
                positions.Add(target); tiles.Add(tile);
            }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(map, "移动地形选区");
                foreach (var p in source.allPositionsWithin) map.SetTile(p, null);
                map.SetTiles(positions.ToArray(), tiles.ToArray()); Refresh(map, source); Refresh(map, new BoundsInt(source.position + delta, source.size));
                EditorSceneManager.MarkSceneDirty(map.gameObject.scene); Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void ReplaceStyle(Tilemap map, BoundsInt region, EmberTerrainTile from, EmberTerrainTile to)
        {
            Validate(map, to);
            if (region.size.z != 1 || region.size.x <= 0 || region.size.y <= 0 || (long)region.size.x * region.size.y > 1000000)
                throw new ArgumentException("请选择有效的二维范围（最多100万格）。");
            if (from == null || from.Physics != to.Physics || from.CellSize != to.CellSize || from.Anchor != to.Anchor)
                throw new InvalidOperationException("几何或碰撞语义不兼容，不能作为纯外观替换。");
            Undo.RegisterCompleteObjectUndo(map, "替换兼容地形样式");
            foreach (var p in region.allPositionsWithin) if (map.GetTile(p) == from) map.SetTile(p, to);
            Refresh(map, region); EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        }

        public static void Refresh(Tilemap map, BoundsInt changed)
        {
            var expanded = new BoundsInt(changed.position - new Vector3Int(1, 1, 0), changed.size + new Vector3Int(2, 2, 0));
            foreach (var p in expanded.allPositionsWithin) map.RefreshTile(p);
            var collider = map.GetComponent<TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges) collider.ProcessTilemapChanges();
        }

        public static string[] ValidateRules(EmberTerrainTile terrain)
        {
            var errors = new List<string>();
            if (terrain == null) return new[] { "地形样式缺失" };
            if (terrain.Fallback == null) errors.Add("缺少回退图片");
            var rules = terrain.Rules;
            for (int mask = 0; mask < 256; mask++)
            {
                int index = terrain.Evaluate(mask, out bool conflict);
                if (conflict) errors.Add($"邻域 {mask}: 同优先级规则冲突，使用回退");
                else if (index < 0 || rules[index].sprite == null) errors.Add($"邻域 {mask}: 缺少映射，使用回退");
            }
            return errors.ToArray();
        }
        #endregion
    }
}
