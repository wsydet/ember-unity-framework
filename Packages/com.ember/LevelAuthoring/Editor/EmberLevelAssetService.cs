using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>按稳定ID原位更新；新文件失败删除，更新失败恢复原序列化值。磁盘生成不属于场景Undo。</summary>
    public static class EmberLevelAssetService
    {
        #region 外部方法
        public static T[] Find<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).ToArray();

        public static T Upsert<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !path.EndsWith(".asset", StringComparison.Ordinal))
                throw new ArgumentException("资源必须保存到项目 Assets 内的 .asset 路径。");
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'))) throw new ArgumentException("请先选择已存在的项目目录。");
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && existing is not T) throw new InvalidOperationException("目标已被其他资源占用。");
            var asset = existing as T;
            bool created = asset == null;
            if (created) asset = ScriptableObject.CreateInstance<T>();
            string before = EditorJsonUtility.ToJson(asset);
            try
            {
                configure(asset);
                if (created) AssetDatabase.CreateAsset(asset, path);
                EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
                return asset;
            }
            catch
            {
                if (created) { if (AssetDatabase.GetAssetPath(asset) == path) AssetDatabase.DeleteAsset(path); else UnityEngine.Object.DestroyImmediate(asset); }
                else { EditorJsonUtility.FromJsonOverwrite(before, asset); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                throw;
            }
        }

        public static EmberTerrainPreset SaveSize(string path, string id, Vector2 size, Vector2Int anchor, EmberTerrainTile terrain)
        {
            foreach (var other in Find<EmberTerrainPreset>())
                if (other.Id == id && AssetDatabase.GetAssetPath(other) != path) throw new InvalidOperationException("尺寸ID已登记到 " + AssetDatabase.GetAssetPath(other));
            return Upsert<EmberTerrainPreset>(path, value =>
            {
                if (!string.IsNullOrEmpty(value.Id) && value.Id != id) throw new InvalidOperationException("更新不能改变已有稳定ID。");
                value.Configure(id, size, anchor, terrain);
            });
        }

        public static EmberLevelMaterial SaveMaterial(string path, string id, Action<EmberLevelMaterial> configure)
        {
            foreach (var other in Find<EmberLevelMaterial>())
                if (other.Id == id && AssetDatabase.GetAssetPath(other) != path) throw new InvalidOperationException("素材ID已登记到 " + AssetDatabase.GetAssetPath(other));
            return Upsert<EmberLevelMaterial>(path, value =>
            {
                if (!string.IsNullOrEmpty(value.Id) && value.Id != id) throw new InvalidOperationException("更新不能改变已有稳定ID。");
                configure(value);
                if (value.Id != id) throw new InvalidOperationException("登记ID与资源ID不一致。");
            });
        }
        #endregion
    }
}
