using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public static class PlayerBuiltinSchemes
    {
        /// <summary>模板编辑中的增量内置登记；保留既有登记和默认方案，不重建资源。</summary>
        public static void RegisterAdditional(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !Ember.Core.Editor.EmberProjectSetup.IsEmbeddedPackage() ||
                Ember.Core.Editor.EmberProjectSetup.GetEditingTemplate() == null)
                throw new InvalidOperationException("新增内置方案登记只允许在模板编辑的非Play状态执行。");
            PlayerSchemeWorkflow.Validate(scheme); PlayerSchemeOwnership.RequireWritable(scheme);
            string path = AssetDatabase.GetAssetPath(scheme);
            if (!path.StartsWith("Assets/GameResource/Resources/Config/Player/BuiltinSchemes/", StringComparison.Ordinal))
                throw new InvalidOperationException("内置方案必须位于模板内置方案目录。");
            var ids = AssetDatabase.FindAssets("t:PlayerSchemeLibrary");
            if (ids.Length != 1) throw new InvalidOperationException("需要唯一既有方案登记。");
            var library = AssetDatabase.LoadAssetAtPath<PlayerSchemeLibrary>(AssetDatabase.GUIDToAssetPath(ids[0]));
            if (EditorUtility.IsDirty(library) || library.builtins.Contains(scheme))
                throw new InvalidOperationException("归属登记存在草稿或方案已登记。");
            var resources = new HashSet<UnityEngine.Object> { scheme, scheme.Graph, scheme.Animation.Controller };
            foreach (var slot in scheme.Animation.Slots)
                foreach (var item in new UnityEngine.Object[] { slot.Key, slot.Clip, slot.MovingRecoveryClip })
                    if (item != null) resources.Add(item);
            foreach (var segment in scheme.Feel.Attack.Segments)
                foreach (var frame in segment.Visual.Frames) resources.Add(frame);
            foreach (var resource in resources)
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(resource)))
                    if (EditorUtility.IsDirty(item)) throw new InvalidOperationException("先显式保存待登记资源草稿：" + item.name);
            Undo.RecordObject(library, "登记新增内置方案");
            library.builtins = library.builtins.Concat(new[] { scheme }).ToArray();
            library.sharedArt = library.sharedArt.Concat(resources.Where(r => r != scheme)).Distinct().ToArray();
            PlayerSchemeSaveProtection.SaveRegistration(library); PlayerSchemeOwnership.Reload();
        }
        public static void Install()
        {
            if (PlayerSchemeOwnership.Builtins.Count != 0) throw new InvalidOperationException("内置方案已安装，不重复生成或覆盖。");
            const string folder = "Assets/GameResource/Resources/Config/Player/BuiltinSchemes";
            const string catalog = "Assets/GameResource/Player/Editor/PlayerSchemeLibrary.asset";
            if (AssetDatabase.IsValidFolder(folder) || System.IO.File.Exists(catalog)) throw new InvalidOperationException("目标已存在，停止安装。");
            var created = new List<string>();
            var source = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme;
            var template = UnityEngine.Object.Instantiate(source);
            PlayerSchemeLibrary library = null;
            try
            {
                AssetDatabase.CreateFolder("Assets/GameResource/Resources/Config/Player", "BuiltinSchemes"); created.Add(folder);
                if (!AssetDatabase.IsValidFolder("Assets/GameResource/Player/Editor"))
                { AssetDatabase.CreateFolder("Assets/GameResource/Player", "Editor"); created.Add("Assets/GameResource/Player/Editor"); }
                var schemes = new List<PlayerScheme>();
                foreach (var style in new[] { "HollowKnightStyle", "CelesteStyle" })
                {
                    using (var data = new SerializedObject(template))
                    { data.FindProperty("_feel").objectReferenceValue = Resources.Load<PlayerFeelProfile>("Config/Player/Feel/" + style + "Profile"); data.ApplyModifiedPropertiesWithoutUndo(); }
                    var name = style == "HollowKnightStyle" ? "空洞骑士风格" : "蔚蓝风格";
                    var scheme = PlayerSchemeWorkflow.Copy(template, folder + "/" + name + ".asset");
                    AssetDatabase.SaveAssetIfDirty(scheme); schemes.Add(scheme);
                }
                library = ScriptableObject.CreateInstance<PlayerSchemeLibrary>(); library.builtins = schemes.ToArray();
                var art = new HashSet<UnityEngine.Object> { source.Animation.Controller };
                foreach (var slot in source.Animation.Slots) { art.Add(slot.Key); art.Add(slot.Clip); }
                foreach (var clip in art.OfType<AnimationClip>().ToArray())
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding)) if (key.value != null) art.Add(key.value);
                library.sharedArt = art.ToArray();
                AssetDatabase.CreateAsset(library, catalog); created.Add(catalog); AssetDatabase.SaveAssetIfDirty(library);
                PlayerSchemeOwnership.Reload();
            }
            catch
            {
                for (int i = created.Count - 1; i >= 0; i--) AssetDatabase.DeleteAsset(created[i]);
                if (library != null && !AssetDatabase.Contains(library)) UnityEngine.Object.DestroyImmediate(library);
                throw;
            }
            finally { UnityEngine.Object.DestroyImmediate(template); }
        }
    }
}
