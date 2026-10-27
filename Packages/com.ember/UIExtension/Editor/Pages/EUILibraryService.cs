// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>精确引用、槽位验证与可重新生成的库目录；不做相似组件猜测。</summary>
    public static class EUILibraryService
    {
        #region 内部参数
        public const string DefaultPath = "Assets/Ember/Editor/UILibrary/DefaultLibrary.asset";
        public const string CatalogPath = "Assets/Ember/Editor/UILibrary/catalog.json";
        #endregion

        #region 外部方法
        public static EUILibrary LoadDefault() => AssetDatabase.LoadAssetAtPath<EUILibrary>(DefaultPath);

        public static List<string> Validate(EUILibrary library)
        {
            var errors = new List<string>();
            if (!library) { errors.Add("公共库配置不存在。"); return errors; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var prefabs = new HashSet<GameObject>();
            foreach (var entry in library.Entries)
            {
                if (entry == null) { errors.Add("空库条目。"); continue; }
                if (string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id)) errors.Add("重复或空 ID: " + entry.Id);
                if (!entry.Prefab || !EditorUtility.IsPersistent(entry.Prefab)) { errors.Add(entry.Id + ": 无效 Prefab"); continue; }
                if (!prefabs.Add(entry.Prefab)) errors.Add(entry.Id + ": Prefab 重复登记");
                if (string.IsNullOrWhiteSpace(entry.Family)) errors.Add(entry.Id + ": family 为空");
                var slotIds = new HashSet<string>();
                foreach (var slot in entry.Slots)
                {
                    if (slot == null) { errors.Add(entry.Id + ": 空 slot"); continue; }
                    var key = entry.Id + "/" + slot.Id;
                    if (string.IsNullOrWhiteSpace(slot.Id) || !slotIds.Add(slot.Id)) errors.Add(key + ": 重复或空槽位 ID");
                    if (!slot.Target) { if (slot.Required) errors.Add(key + ": 失效槽位"); continue; }
                    if (slot.Target.transform != entry.Prefab.transform && !slot.Target.transform.IsChildOf(entry.Prefab.transform))
                        errors.Add(key + ": 越过组件边界");
                    var type = Type.GetType(slot.ComponentType, false) ?? AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(slot.ComponentType ?? "", false)).FirstOrDefault(t => t != null);
                    if (type == null || !type.IsInstanceOfType(slot.Target)) errors.Add(key + ": 类型不兼容 " + slot.ComponentType);
                    using (var so = new SerializedObject(slot.Target))
                        foreach (var property in slot.Properties)
                            if (property != "children" && so.FindProperty(property) == null) errors.Add(key + ": 无效属性 " + property);
                }
                foreach (var role in entry.RequiredRoles)
                    if (!entry.Slots.Any(s => s != null && s.Target && s.Role == role)) errors.Add(entry.Id + ": 缺少角色 " + role);
                CheckCycles(entry.Prefab, new HashSet<string>(), new HashSet<string>(), errors);
            }
            return errors.Distinct().ToList();
        }

        public static bool ValidateCreationSource(EUICreationRequest request, out string error)
        {
            error = null;
            if (request.ReferenceResolution.x <= 0 || request.ReferenceResolution.y <= 0
                || !float.IsFinite(request.ReferenceResolution.x) || !float.IsFinite(request.ReferenceResolution.y)
                || !float.IsFinite(request.MatchWidthOrHeight) || request.MatchWidthOrHeight < 0 || request.MatchWidthOrHeight > 1)
                error = "参考分辨率或缩放配置无效。";
            if (!request.SourceSkeleton) return error == null;
            var source = request.SourceSkeleton;
            var binding = source.GetComponent<EUIBinding>();
            if (!EditorUtility.IsPersistent(source) || !binding || binding.Role != request.Role)
                error = "源骨架必须是相同角色的已保存 EUI Prefab。";
            else if (request.InheritBinding && binding.NoCodeGeneration)
                error = "结构骨架没有逻辑类，不能作为 Binding 基类。";
            else if (request.IsPage && (string.IsNullOrEmpty(request.ContentSlotPath) || !(source.transform.Find(request.ContentSlotPath) is RectTransform)))
                error = "内容插入槽位不存在: " + request.ContentSlotPath;
            else if (EUIBindingEditorUtility.ValidateBinding(binding).HasError)
                error = "源骨架的 Binding 验证失败。";
            var library = LoadDefault();
            if (library && library.Entries.Any(e => e.Prefab == source))
            {
                var errors = Validate(library);
                if (errors.Count > 0) error = string.Join("\n", errors);
            }
            return error == null;
        }

        public static EUILibraryDescription Describe(EUILibrary library, EUILibraryEntry entry)
        {
            var result = new EUILibraryDescription { id = entry.Id, family = entry.Family, category = entry.Category,
                prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.Prefab)),
                skinId = library.SkinId, skinVersion = library.SkinVersion, allowedOverrides = entry.AllowedOverrides };
            foreach (var slot in entry.Slots)
            {
                string guid = ""; long localId = 0;
                if (slot.Target) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(slot.Target, out guid, out localId);
                result.slots.Add(new EUILibrarySlotDescription { id = slot.Id, role = slot.Role,
                    targetId = guid + ":" + localId, path = slot.Target ? AnimationUtility.CalculateTransformPath(slot.Target.transform, entry.Prefab.transform) : "",
                    componentType = slot.ComponentType, state = slot.State, properties = slot.Properties });
            }
            return result;
        }

        public static List<EUILibraryDescription> DescribeDependencies(string prefabPath)
        {
            var library = LoadDefault();
            if (!library) return new List<EUILibraryDescription>();
            var paths = new HashSet<string>(AssetDatabase.GetDependencies(prefabPath, true));
            return library.Entries.Where(e => e != null && e.Prefab && paths.Contains(AssetDatabase.GetAssetPath(e.Prefab)))
                .OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => Describe(library, e)).ToList();
        }

        public static string Digest(EUILibrary library)
        {
            var text = EditorJsonUtility.ToJson(library);
            foreach (var entry in library.Entries.Where(e => e?.Prefab).OrderBy(e => e.Id, StringComparer.Ordinal))
                text += AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(entry.Prefab));
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }

        public static EUILibraryCatalog GenerateCatalog(EUILibrary library, string outputPath = CatalogPath)
        {
            var errors = Validate(library);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var result = new EUILibraryCatalog { inputDigest = Digest(library), entries = library.Entries.OrderBy(e => e.Id)
                .Select(e => Describe(library, e)).ToList() };
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(outputPath);
            return result;
        }

        public static bool IsCatalogCurrent(EUILibrary library, string path = CatalogPath)
        {
            if (!library || !File.Exists(path)) return false;
            try { var catalog = JsonUtility.FromJson<EUILibraryCatalog>(File.ReadAllText(path));
                return catalog != null && catalog.schemaVersion == 1 && catalog.inputDigest == Digest(library); }
            catch (Exception) { return false; }
        }

        /// <summary>在已验证的宿主矩形中嵌套精确组件；调用方负责保存并通过中心重新生成。</summary>
        public static GameObject Instantiate(EUILibrary library, string entryId, RectTransform parent)
        {
            var entry = ResolveExact(library, entryId);
            if (!parent || EditorUtility.IsPersistent(parent)) throw new InvalidOperationException("插入区域必须属于可编辑实例。");
            if (entry.Prefab.GetComponent<EUIBinding>()?.IsPage == true) throw new InvalidOperationException("Page 骨架应通过创建中心建立 Variant。");
            for (var ancestor = parent.transform; ancestor; ancestor = ancestor.parent)
                if (PrefabUtility.GetCorrespondingObjectFromSource(ancestor.gameObject) == entry.Prefab)
                    throw new InvalidOperationException("不能将组件嵌入自己的实例。");
            return (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, parent);
        }
        public static string[] FindConsumers(EUILibraryEntry entry, string searchRoot)
        {
            if (entry == null || !entry.Prefab || !AssetDatabase.IsValidFolder(searchRoot)) return Array.Empty<string>();
            var source = AssetDatabase.GetAssetPath(entry.Prefab);
            return AssetDatabase.FindAssets("t:Prefab", new[] { searchRoot }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != source && AssetDatabase.GetDependencies(path, true).Contains(source)).OrderBy(path => path).ToArray();
        }
        public static EUILibraryEntry ResolveExact(EUILibrary library, string id)
        {
            var errors = Validate(library);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            return library.Entries.SingleOrDefault(e => e.Id == id) ?? throw new InvalidOperationException("未找到精确条目: " + id);
        }

        public static EUILibraryEntry[] Match(EUILibrary library, string family, string[] roles)
        {
            if (!IsCatalogCurrent(library)) throw new InvalidOperationException("目录过期，请重新生成；禁止自动匹配。");
            var errors = Validate(library);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            return library.Entries.Where(e => e.AutoMatch && e.Family == family && roles.All(r => e.Slots.Any(s => s.Role == r))).ToArray();
        }

        /// <summary>只返回嵌套实例的槽位，拒绝写公共源以及非声明属性。</summary>
        public static Component ResolveInstanceSlot(EUILibrary library, string entryId, GameObject instance, string slotId, string property)
        {
            var entry = ResolveExact(library, entryId);
            if (!instance || EditorUtility.IsPersistent(instance) || instance == entry.Prefab)
                throw new InvalidOperationException("覆盖目标必须是实例，不能是公共源。");
            var sourcePath = AssetDatabase.GetAssetPath(entry.Prefab);
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance) != sourcePath
                || PrefabUtility.GetCorrespondingObjectFromSourceAtPath(instance, sourcePath) != entry.Prefab)
                throw new InvalidOperationException("实例来源与精确条目不一致。");
            var slot = entry.Slots.SingleOrDefault(s => s.Id == slotId);
            if (slot == null || !slot.Properties.Contains(property)) throw new InvalidOperationException("槽位或属性不允许覆盖。");
            foreach (var component in instance.GetComponentsInChildren<Component>(true))
                if (component && PrefabUtility.GetCorrespondingObjectFromSourceAtPath(component, sourcePath) == slot.Target) return component;
            throw new InvalidOperationException("实例槽位目标已丢失: " + slotId);
        }
        #endregion

        #region 内部方法
        private static void CheckCycles(GameObject prefab, HashSet<string> stack, HashSet<string> visited, List<string> errors)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            if (!stack.Add(path)) { errors.Add("Prefab 依赖循环: " + path); return; }
            if (visited.Add(path))
                foreach (var dependency in AssetDatabase.GetDependencies(path, false))
                    if (dependency != path && dependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    { var next = AssetDatabase.LoadAssetAtPath<GameObject>(dependency); if (next) CheckCycles(next, stack, visited, errors); }
            stack.Remove(path);
        }
        #endregion
    }
}



