using System;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    public enum EmberLevelMaterialKind { Terrain, Mechanism, Background, Decoration }

    public sealed class EmberLevelMaterial : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _adapterId;
        [SerializeField] private string _description;
        [SerializeField] private EmberLevelMaterialKind _kind;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private EmberTerrainPreset _terrain;
        [SerializeField] private bool _allowRotation;
        [SerializeField] private bool _allowScale;
        [SerializeField] private bool _allowMirror;
        [SerializeField] private bool _hasPhysics;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public string DisplayName => _displayName;
        public string AdapterId => _adapterId;
        public string Description => _description;
        public EmberLevelMaterialKind Kind => _kind;
        public GameObject Prefab => _prefab;
        public Sprite Sprite => _sprite;
        public EmberTerrainPreset Terrain => _terrain;
        public bool AllowRotation => _allowRotation;
        public bool AllowScale => _allowScale;
        public bool AllowMirror => _allowMirror;
        public bool HasPhysics => _hasPhysics;
        public void Configure(string id, string label, EmberLevelMaterialKind kind, GameObject prefab, Sprite sprite,
            EmberTerrainPreset terrain, string adapterId = "", bool rotation = false, bool scale = false, bool mirror = false,
            bool hasPhysics = false, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label)) throw new ArgumentException("素材需要稳定ID与名称。");
            if (kind == EmberLevelMaterialKind.Terrain && terrain == null || kind != EmberLevelMaterialKind.Terrain && prefab == null && sprite == null)
                throw new ArgumentException("素材引用缺失。");
            if (kind == EmberLevelMaterialKind.Mechanism && string.IsNullOrWhiteSpace(adapterId)) throw new ArgumentException("机关必须声明编辑适配器。");
            _id = id; _displayName = label; _kind = kind; _prefab = prefab; _sprite = sprite; _terrain = terrain;
            _adapterId = adapterId; _allowRotation = rotation; _allowScale = scale; _allowMirror = mirror;
            _hasPhysics = hasPhysics; _description = description;
        }
        #endregion
    }
}
