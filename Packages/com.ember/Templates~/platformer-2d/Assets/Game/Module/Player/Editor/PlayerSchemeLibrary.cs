using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>编辑器归属登记；引用使用 Unity 稳定资产身份，重命名不解除保护。</summary>
    internal sealed class PlayerSchemeLibrary : ScriptableObject
    {
        [SerializeField] internal PlayerScheme[] builtins = Array.Empty<PlayerScheme>();
        [SerializeField] internal UnityEngine.Object[] sharedArt = Array.Empty<UnityEngine.Object>();
    }

    [InitializeOnLoad]
    public static class PlayerSchemeOwnership
    {
        #region 内部参数
        private static HashSet<string> _protectedGuids;
        private static PlayerSchemeLibrary _library;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerSchemeOwnership()
        { EditorApplication.projectChanged += Invalidate; EditorApplication.delayCall += ProtectInspectors; }
        private static void Invalidate()
        {
            _protectedGuids = null; _library = null;
            EditorApplication.delayCall -= ProtectInspectors;
            EditorApplication.delayCall += ProtectInspectors;
        }
        private static void Load()
        {
            if (_protectedGuids != null) return;
            var ids = AssetDatabase.FindAssets("t:PlayerSchemeLibrary");
            if (ids.Length > 1) throw new InvalidOperationException("存在多个玩家方案归属登记，请保留唯一登记。");
            _protectedGuids = new HashSet<string>();
            if (ids.Length == 0) return;
            _library = AssetDatabase.LoadAssetAtPath<PlayerSchemeLibrary>(AssetDatabase.GUIDToAssetPath(ids[0]));
            _protectedGuids.Add(ids[0]);
            foreach (var item in _library.builtins.Cast<UnityEngine.Object>().Concat(_library.sharedArt))
                if (item != null) _protectedGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item)));
        }
        private static void ProtectInspectors()
        {
            Load();
            foreach (var guid in _protectedGuids)
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (item != null) item.hideFlags |= HideFlags.NotEditable;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static IReadOnlyList<PlayerScheme> Builtins { get { Load(); return _library == null ? Array.Empty<PlayerScheme>() : _library.builtins; } }
        public static bool IsReadOnly(UnityEngine.Object asset)
        {
            if (asset == null || !AssetDatabase.Contains(asset)) return false;
            Load(); return _protectedGuids.Contains(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
        }
        public static void RequireWritable(UnityEngine.Object asset)
        { if (IsReadOnly(asset)) throw new InvalidOperationException("内置方案或共享素材只读，请先创建自有副本：" + asset.name); }
        internal static void Reload() { Invalidate(); ProtectInspectors(); }
        #endregion
    }

    internal sealed class PlayerSchemeSaveProtection : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths) => paths.Where(path =>
            !PlayerSchemeOwnership.IsReadOnly(AssetDatabase.LoadMainAssetAtPath(path))).ToArray();
        private static bool IsOpenForEdit(string assetPath, out string message, StatusQueryOptions options)
        {
            bool locked = PlayerSchemeOwnership.IsReadOnly(AssetDatabase.LoadMainAssetAtPath(assetPath));
            message = locked ? "内置方案及共享素材只读，请先创建自有副本。" : "";
            return !locked;
        }
    }
}
