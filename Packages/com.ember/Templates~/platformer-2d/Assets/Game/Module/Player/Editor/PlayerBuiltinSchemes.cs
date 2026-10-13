using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public static class PlayerBuiltinSchemes
    {
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
