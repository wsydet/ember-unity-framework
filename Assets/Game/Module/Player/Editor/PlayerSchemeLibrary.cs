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
        /// <summary>模板作者显式保存已登记内置方案；不改变消费端只读归属或项目默认。</summary>
        public static void SaveBuiltinAuthoring(PlayerScheme scheme) => PlayerSchemeSaveProtection.SaveBuiltinAuthoring(scheme);
        internal static void Reload() { Invalidate(); ProtectInspectors(); }
        #endregion
    }

    internal sealed class PlayerSchemeSaveProtection : AssetModificationProcessor
    {
        private static string _registrationPath;

        internal static void SaveBuiltinAuthoring(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !Ember.Core.Editor.EmberProjectSetup.IsEmbeddedPackage() ||
                Ember.Core.Editor.EmberProjectSetup.GetEditingTemplate() == null ||
                !PlayerSchemeOwnership.Builtins.Contains(scheme) || _registrationPath != null)
                throw new InvalidOperationException("内置保存仅允许模板作者在非Play状态显式执行，且不能重入。");
            PlayerSchemeWorkflow.Validate(scheme);
            string path = AssetDatabase.GetAssetPath(scheme);
            var assets = PlayerSchemeWorkflow.Configuration(scheme).Values;
            if (assets.Any(asset => AssetDatabase.GetAssetPath(asset) != path))
                throw new InvalidOperationException("内置配置必须保存在同一方案资产中，不能连带写入共享配置。");
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            var flags = objects.Select(asset => asset.hideFlags).ToArray();
            try
            {
                _registrationPath = path;
                for (int i = 0; i < objects.Length; i++) objects[i].hideFlags &= ~HideFlags.NotEditable;
                EditorUtility.SetDirty(scheme);
                AssetDatabase.SaveAssetIfDirty(scheme);
                if (objects.Any(EditorUtility.IsDirty))
                    throw new InvalidOperationException("内置方案未成功保存；保留内存草稿。");
            }
            finally
            {
                for (int i = 0; i < objects.Length; i++) objects[i].hideFlags = flags[i];
                _registrationPath = null;
            }
        }

        internal static void SaveRegistration(PlayerSchemeLibrary library)
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !Ember.Core.Editor.EmberProjectSetup.IsEmbeddedPackage() ||
                Ember.Core.Editor.EmberProjectSetup.GetEditingTemplate() == null)
                throw new InvalidOperationException("归属登记保存只允许在模板编辑的非Play状态执行。");
            var ids = AssetDatabase.FindAssets("t:PlayerSchemeLibrary");
            string path = AssetDatabase.GetAssetPath(library);
            if (library == null || ids.Length != 1 || AssetDatabase.GUIDToAssetPath(ids[0]) != path ||
                _registrationPath != null)
                throw new InvalidOperationException("归属登记必须唯一且不能重入保存。");
            foreach (var scheme in library.builtins) PlayerSchemeWorkflow.Validate(scheme);
            var flags = library.hideFlags;
            try
            {
                _registrationPath = path;
                library.hideFlags = flags & ~HideFlags.NotEditable;
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssetIfDirty(library);
                if (EditorUtility.IsDirty(library))
                    throw new InvalidOperationException("归属登记未成功保存；保留内存草稿，请检查保存限制。");
            }
            finally
            {
                library.hideFlags = flags;
                _registrationPath = null;
            }
        }

        private static string[] OnWillSaveAssets(string[] paths) => paths.Where(path =>
            path == _registrationPath || !PlayerSchemeOwnership.IsReadOnly(AssetDatabase.LoadMainAssetAtPath(path))).ToArray();
        private static bool IsOpenForEdit(string assetPath, out string message)
        {
            bool locked = assetPath != _registrationPath && PlayerSchemeOwnership.IsReadOnly(AssetDatabase.LoadMainAssetAtPath(assetPath));
            message = locked ? "内置方案及共享素材只读，请先创建自有副本。" : "";
            return !locked;
        }
    }
}
