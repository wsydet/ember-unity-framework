using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring
{
    /// <summary>场景反序列化结束后恢复不可序列化的地形渲染引用，早于场景内 Start。</summary>
    internal static class EmberTerrainRuntimeLifecycle
    {
        #region 内部参数
        private static readonly List<Tilemap> Maps = new();
        private static TileBase[] _usedTiles = Array.Empty<TileBase>();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Domain Reload 关闭时也保持单次订阅；不依赖业务 GameInit 或 Scope。
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, Maps);
                foreach (var map in Maps)
                {
                    int count = map.GetUsedTilesCount();
                    if (_usedTiles.Length < count) _usedTiles = new TileBase[count];
                    int used = map.GetUsedTilesNonAlloc(_usedTiles);
                    bool terrain = false;
                    for (int i = 0; i < used; i++)
                        if (_usedTiles[i] is EmberTerrainTile) { terrain = true; break; }
                    Array.Clear(_usedTiles, 0, _usedTiles.Length);
                    if (terrain) map.RefreshAllTiles();
                }
                Maps.Clear();
            }
        }
        #endregion
    }
}
