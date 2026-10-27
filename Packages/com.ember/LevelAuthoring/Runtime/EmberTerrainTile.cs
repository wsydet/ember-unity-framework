using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring
{
    public enum EmberTerrainPhysics { Solid, OneWay, Decoration }

    /// <summary>格子的语义与规则唯一来源。邻域位序为右、上、左、下、右上、左上、左下、右下。</summary>
    [CreateAssetMenu(menuName = "Ember/Level/Terrain Tile")]
    public sealed class EmberTerrainTile : TileBase
    {
        [Serializable]
        public struct Rule
        {
            public int required, forbidden, priority;
            public Sprite sprite;
            public int quarterTurns;
            public bool mirrorX;
        }

        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private string _connectionGroup;
        [SerializeField] private int _version = 1;
        [SerializeField] private Vector2 _cellSize = new(.5f, .5f);
        [SerializeField] private Vector3 _anchor = new(.5f, .5f, 0);
        [SerializeField] private EmberTerrainPhysics _physics;
        [SerializeField] private Sprite _fallback;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Rule[] _rules = Array.Empty<Rule>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [NonSerialized] private Dictionary<(Sprite, int, bool, Vector2, Vector3), Sprite> _renderSprites;
        private static readonly Vector3Int[] Offsets = { Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down,
            new(1,1,0), new(-1,1,0), new(-1,-1,0), new(1,-1,0) };
        public string Id => _id;
        public string ConnectionGroup => _connectionGroup;
        public int Version => _version;
        public Vector2 CellSize => _cellSize;
        public Vector3 Anchor => _anchor;
        public EmberTerrainPhysics Physics => _physics;
        public Sprite Fallback => _fallback;
        public Rule[] Rules => (Rule[])_rules.Clone();
        public override void OnDisable() { ClearRenderSprites(); base.OnDisable(); }
        public override void OnEnable() { base.OnEnable(); PrepareRenderSprites(); }

        private void PrepareRenderSprites()
        {
            // 缓存可重建，不修改源Sprite或格子占用。
            RenderSprite(_fallback, default);
            foreach (var rule in _rules) RenderSprite(rule.sprite, rule);
        }

        private void ClearRenderSprites()
        {
            if (_renderSprites == null) return;
            foreach (var sprite in _renderSprites.Values)
                if (sprite != null)
                {
                    if (Application.isPlaying) Destroy(sprite);
                    else DestroyImmediate(sprite);
                }
            _renderSprites.Clear();
        }

        private Sprite RenderSprite(Sprite source, Rule rule)
        {
            if (source == null) return null;
            _renderSprites ??= new Dictionary<(Sprite, int, bool, Vector2, Vector3), Sprite>();
            var key = (source, rule.quarterTurns, rule.mirrorX, _cellSize, _anchor);
            if (_renderSprites.TryGetValue(key, out var cached) && cached != null) return cached;
            Vector2 size = source.rect.size / source.pixelsPerUnit;
            bool swapped = (Math.Abs(rule.quarterTurns) & 1) != 0;
            var scale = new Vector3((swapped ? _cellSize.y : _cellSize.x) / size.x,
                (swapped ? _cellSize.x : _cellSize.y) / size.y, 1);
            if (rule.mirrorX) scale.x *= -1;
            var rotation = Quaternion.Euler(0, 0, rule.quarterTurns * 90);
            Vector3 center = (source.rect.size * .5f - source.pivot) / source.pixelsPerUnit;
            var offset = new Vector3(_cellSize.x * (.5f - _anchor.x), _cellSize.y * (.5f - _anchor.y), 0);
            // 归一化Sprite顶点，Tile变换保持单位矩阵，Grid碰撞不被PPU二次缩放。
            // 初始Bounds保守覆盖语义格；UV和三角形来自原图，保持图集映射。
            var texture = source.texture;
            float ppu = Mathf.Min(texture.width / _cellSize.x, texture.height / _cellSize.y);
            var extent = new Vector2(texture.width / ppu, texture.height / ppu);
            var pivot = new Vector2(.5f - offset.x / extent.x, .5f - offset.y / extent.y);
            var copy = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            copy.SetVertexCount(source.GetVertexCount());
            var sourceVertices = source.vertices;
            var transformed = new Vector3[sourceVertices.Length];
            for (int i = 0; i < transformed.Length; i++)
                transformed[i] = offset + rotation * Vector3.Scale((Vector3)sourceVertices[i] - center, scale);
            using (var vertices = new NativeArray<Vector3>(transformed, Allocator.Temp))
            using (var uvs = new NativeArray<Vector2>(source.uv, Allocator.Temp))
            using (var indices = new NativeArray<ushort>(source.triangles, Allocator.Temp))
            {
                copy.SetVertexAttribute(VertexAttribute.Position, vertices);
                copy.SetVertexAttribute(VertexAttribute.TexCoord0, uvs);
                copy.SetIndices(indices);
            }
            copy.name = source.name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            _renderSprites[key] = copy;
            return copy;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(string id, string group, Vector2 cellSize, Vector3 anchor, EmberTerrainPhysics physics,
            Sprite fallback, Color color, Rule[] rules, int version = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(group) || fallback == null)
                throw new ArgumentException("地形需要稳定ID、连接组和回退图片。");
            if (!(cellSize.x > 0 && cellSize.y > 0) || float.IsInfinity(cellSize.x) || float.IsInfinity(cellSize.y))
                throw new ArgumentException("格子尺寸必须为正有限值。");
            if (!float.IsFinite(anchor.x) || !float.IsFinite(anchor.y) || anchor.z != 0)
                throw new ArgumentException("锚点必须是有限二维坐标。");
            _id = id; _connectionGroup = group; _cellSize = cellSize; _anchor = anchor; _physics = physics;
            ClearRenderSprites();
            _fallback = fallback; _color = color; _rules = rules == null ? Array.Empty<Rule>() : (Rule[])rules.Clone(); _version = version;
            PrepareRenderSprites();
        }

        public int Evaluate(int neighbors, out bool conflict)
        {
            int best = -1, priority = int.MinValue; conflict = false;
            for (int i = 0; i < _rules.Length; i++)
            {
                var rule = _rules[i];
                if ((neighbors & rule.required) != rule.required || (neighbors & rule.forbidden) != 0) continue;
                if (best < 0 || rule.priority > priority) { best = i; priority = rule.priority; conflict = false; }
                else if (rule.priority == priority) conflict = true;
            }
            return best;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData data)
        {
            int mask = 0;
            for (int i = 0; i < Offsets.Length; i++)
                if (tilemap.GetTile(position + Offsets[i]) is EmberTerrainTile neighbor &&
                    !string.IsNullOrEmpty(_connectionGroup) && neighbor.ConnectionGroup == _connectionGroup) mask |= 1 << i;
            int index = Evaluate(mask, out bool conflict);
            bool fallback = index < 0 || conflict || _rules[index].sprite == null;
            var rule = fallback ? default : _rules[index];
            data.sprite = RenderSprite(fallback ? _fallback : rule.sprite, rule);
            data.transform = Matrix4x4.identity;
            data.color = _color; data.flags = TileFlags.LockAll;
            data.colliderType = _physics == EmberTerrainPhysics.Decoration ? Tile.ColliderType.None : Tile.ColliderType.Grid;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            // 原生调色板同样刷新依赖邻域，不新增任何占用格。
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) tilemap.RefreshTile(position + new Vector3Int(x, y, 0));
        }
        #endregion
    }
}
