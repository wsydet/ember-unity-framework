using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>域重载重建临时 Sprite 后，让已加载的 Tilemap 重新取得渲染引用。</summary>
    [InitializeOnLoad]
    internal static class EmberTerrainCacheLifecycle
    {
        #region 内部参数
        private static TileBase[] _usedTiles = Array.Empty<TileBase>();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberTerrainCacheLifecycle()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            QueueRestore();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
                QueueRestore();
        }

        internal static void QueueRestore()
        {
            // 合并同一轮重载与 Play 通知；不在资产 OnEnable 中访问尚未完成恢复的场景。
            // delayCall 依赖 Inspector 更新；编辑器失焦时也必须完成恢复。
            EditorApplication.update -= RestoreLoadedTilemaps;
            EditorApplication.update += RestoreLoadedTilemaps;
        }

        private static void RestoreLoadedTilemaps()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            // 仅在等待就绪期间订阅。扫描前解除，后续帧不会重复遍历场景。
            EditorApplication.update -= RestoreLoadedTilemaps;

            // 包括 inactive 对象和 Prefab Stage，排除磁盘 Prefab 资产与未加载场景。
            // 每个生命周期边界只扫描一次组件和使用的 Tile 类型，不遍历稀疏 cellBounds。
            foreach (var map in Resources.FindObjectsOfTypeAll<Tilemap>())
            {
                if (EditorUtility.IsPersistent(map) || !map.gameObject.scene.IsValid() || !map.gameObject.scene.isLoaded)
                    continue;
                int count = map.GetUsedTilesCount();
                if (_usedTiles.Length < count) _usedTiles = new TileBase[count];
                int used = map.GetUsedTilesNonAlloc(_usedTiles);
                bool terrain = false;
                for (int i = 0; i < used; i++)
                    if (_usedTiles[i] is EmberTerrainTile) { terrain = true; break; }
                Array.Clear(_usedTiles, 0, _usedTiles.Length);
                // 原生刷新重新求值 TileData；不 SetTile、不改 Grid/Anchor、不保存或标脏场景。
                if (terrain) map.RefreshAllTiles();
            }
        }
        #endregion
    }

}
