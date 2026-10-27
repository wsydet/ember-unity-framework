// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>只读交换模型；对象 ID 使用字符串，避免 JSON 整数精度损失。</summary>
    [Serializable]
    public sealed class EUIContract
    {
        public int schemaVersion = 1;
        public string toolVersion = "1.1", exportedUtc, prefabPath, prefabGuid, assetDigest;
        public List<string> requiredCapabilities = new();
        public string role, pageType, codeMode, className, pageName, baseBindingGuid, variantSource;
        public string generatedPath, generatedDigest;
        public string bindingScriptPath, bindingScriptDigest, pageDefinitionPath, pageDefinitionDigest, dependencyDigest;
        public string customSettingsType, customSettingsJson;
        public List<EUILibraryDescription> libraryEntries = new();
        public List<EUIContractList> lists = new();
        public List<string> generatedFields = new();
        public List<EUIContractResource> resources = new();
        public List<string> legacyNames = new();
        public List<EUIContractBinding> bindings = new();
        public List<EUIContractNode> nodes = new();
        public List<EUIContractProperty> properties = new();
        public List<string> dependencies = new();
        public List<string> blockers = new();
        public List<string> unresolved = new();
    }

    [Serializable]
    public sealed class EUIContractList
    {
        public string kind = "List", templateKind = "PrefabRef";
        public string hostId, hostPath, containerPrefab, itemPrefab, itemGuid, itemLogic, contentId, viewportId, dataBinding;
        public string owner = "host:instances+items;lease:events+cancellation;external-provider:prefab-resource";
        public string refresh = "replace-all-bindings;ordinal-unique-key;index-is-display-position";
        public int cacheCapacity;
    }

    [Serializable]
    public sealed class EUIContractBinding
    {
        public string ownerId, name, objectId, path, widgetType, declaredType, componentType, semanticId, behavior, owner;
        public bool isFramework;
    }

    [Serializable]
    public sealed class EUIContractNode
    {
        public string id, path, parentId, nestedSource;
        public int siblingIndex, layer;
        public bool activeSelf;
        public List<string> components = new();
        public List<string> ownedSlots = new();
    }

    [Serializable]
    public sealed class EUIContractResource
    {
        public string id, path, type, spriteBorder, digest;
        public int width, height;
    }

    [Serializable]
    public sealed class EUIContractProperty
    {
        public string objectId, componentType, name, category, value;
    }

    /// <summary>只登记语义，不复制资产结构。ownerId/name 对应实际 Binding。</summary>
    [Serializable]
    public sealed class EUIContractAnnotation
    {
        public string ownerId, bindingName, semanticId, behavior;
    }

    [Serializable]
    public sealed class EUIContractDifference
    {
        public string category, target, before, after;
        public bool blocksApply;
    }

    /// <summary>一次登记的语义补充，使用 GUID 定位；不存储层级和序列化事实。</summary>
    [Serializable]
    public sealed class EUIContractSupplement
    {
        public string prefabGuid;
        public List<string> legacyNames = new();
        public List<EUIContractAnnotation> annotations = new();
    }

    /// <summary>从已保存 Prefab 读取契约并比较，不修改 Prefab、绑定或生成文件。</summary>
    public static class EUIContractService
    {
        public static EUIContract Export(string prefabPath, IEnumerable<EUIContractAnnotation> annotations = null,
            IEnumerable<string> legacyNames = null)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var binding = root ? root.GetComponent<EUIBinding>() : null;
            if (!binding || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("契约输入必须是已保存且根节点带 EUIBinding 的 Prefab。", nameof(prefabPath));
            var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
            var result = new EUIContract
            {
                exportedUtc = DateTime.UtcNow.ToString("O"), prefabPath = prefabPath,
                prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath), assetDigest = Digest(prefabPath),
                role = binding.Role.ToString(), pageType = binding.PageType.ToString(), codeMode = binding.PathMode.ToString(),
                className = binding.ClassName, pageName = binding.PageName, baseBindingGuid = snapshot.BaseBindingGuid,
                generatedPath = EUIBinding.OnGetGeneratedPath?.Invoke(binding)
            };
            result.generatedDigest = Digest(result.generatedPath);
            var supplementPath = GetSupplementPath(result.prefabGuid);
            if (annotations == null && File.Exists(supplementPath))
            {
                var supplement = JsonUtility.FromJson<EUIContractSupplement>(File.ReadAllText(supplementPath));
                if (supplement == null || supplement.prefabGuid != result.prefabGuid)
                    throw new InvalidDataException("语义补充与资产 GUID 不匹配：" + supplementPath);
                annotations = supplement.annotations;
                legacyNames = legacyNames ?? supplement.legacyNames;
            }
            result.bindingScriptPath = Path.ChangeExtension(result.generatedPath, "Binding.cs");
            result.bindingScriptDigest = Digest(result.bindingScriptPath);
            if (binding.PageSettings != null)
            {
                result.customSettingsType = binding.PageSettings.GetType().FullName;
                result.customSettingsJson = JsonUtility.ToJson(binding.PageSettings);
                result.unresolved.Add("自定义 Settings 仅导出 Unity 可序列化字段；Odin 专有数据需类型专用适配。" );
            }
            if (File.Exists(result.bindingScriptPath))
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                    File.ReadAllText(result.bindingScriptPath), @"(?m)^\s*private\s+([\w.<>]+)\s+(\w+)\s*;"))
                    result.generatedFields.Add(match.Groups[1].Value + " " + match.Groups[2].Value);
            result.dependencyDigest = AssetDatabase.GetAssetDependencyHash(prefabPath).ToString();
            var implementation = CSharpLogicImplementationData.FindDefault();
            if (implementation && binding.IsPage)
            {
                result.pageDefinitionPath = binding.PathMode == EUIBinding.CodePathMode.Framework
                    ? Path.Combine(Path.GetDirectoryName(implementation.PageDefFile) ?? "", "GamePages.cs").Replace('\\', '/')
                    : implementation.PageDefFile;
                result.pageDefinitionDigest = Digest(result.pageDefinitionPath);
            }
            var variant = PrefabUtility.GetCorrespondingObjectFromSource(root);
            result.variantSource = variant ? Identity(variant) : "";
            if (legacyNames != null) result.legacyNames = legacyNames.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            var notes = annotations?.ToList() ?? new List<EUIContractAnnotation>();
            foreach (var group in notes.Where(x => !string.IsNullOrEmpty(x.semanticId)).GroupBy(x => x.semanticId))
                if (group.Count() > 1) result.blockers.Add("重复语义 ID: " + group.Key);
            foreach (var group in notes.GroupBy(x => x.ownerId + "/" + x.bindingName))
                if (group.Count() > 1) result.blockers.Add("重复语义映射: " + group.Key);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var node = new EUIContractNode { id = Identity(t.gameObject), path = PathOf(root.transform, t),
                    parentId = t == root.transform ? "" : Identity(t.parent.gameObject), siblingIndex = t.GetSiblingIndex(),
                    activeSelf = t.gameObject.activeSelf, layer = t.gameObject.layer };
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                node.nestedSource = source ? Identity(source) : "";
                foreach (var c in t.GetComponents<Component>())
                {
                    if (!c) { result.blockers.Add("Missing Script: " + node.path); continue; }
                    node.components.Add(c.GetType().FullName);
                    if (c is IEUIExposedChildProvider provider)
                        foreach (var slot in provider.GetOwnedChildren())
                            if (slot) node.ownedSlots.Add(Identity(slot));
                    ReadProperties(c, result);
                }
                node.components.Sort(StringComparer.Ordinal);
                node.ownedSlots.Sort(StringComparer.Ordinal);
                result.nodes.Add(node);
            }
            foreach (var b in root.GetComponentsInChildren<EUIBinding>(true))
            {
                var ownerId = Identity(b.gameObject);
                var map = new Dictionary<string, Component>();
                EUIBindingBridge.PopulateControlMap(b, map); // parentLogic 为 null：不构造嵌套逻辑。
                var validation = EUIBindingEditorUtility.ValidateBinding(b);
                foreach (var issue in validation.Issues)
                    if (issue.Severity == EUIBindingIssueSeverity.Error) result.blockers.Add(issue.Message);
                foreach (var entry in b.Bindings ?? Array.Empty<EUIBinding.BindingEntry>())
                {
                    map.TryGetValue(entry.Name ?? "", out var actual);
                    var nested = entry.GameObject ? entry.GameObject.GetComponent<EUIBinding>() : null;
                    var note = notes.FirstOrDefault(n => n.ownerId == ownerId && n.bindingName == entry.Name);
                    var item = new EUIContractBinding
                    {
                        ownerId = ownerId, name = entry.Name, objectId = Identity(entry.GameObject),
                        path = entry.GameObject ? PathOf(root.transform, entry.GameObject.transform) : "",
                        widgetType = entry.Type.ToString(), declaredType = entry.ClassName,
                        componentType = entry.Type == EUIBinding.WidgetTypes.UILogic ? nested?.ClassName : actual?.GetType().FullName,
                        isFramework = entry.IsFramework, semanticId = note?.semanticId ?? "", behavior = note?.behavior ?? "",
                        owner = entry.Type == EUIBinding.WidgetTypes.UILogic ? "parent-logic" : "binding"
                    };
                    result.bindings.Add(item);
                    if (!entry.GameObject) result.blockers.Add("空引用: " + item.name);
                    if (entry.GameObject && !entry.GameObject.transform.IsChildOf(root.transform) && entry.GameObject != root)
                        result.blockers.Add("绑定指向 Prefab 外部: " + item.name);
                    if (string.IsNullOrEmpty(item.componentType)) result.blockers.Add("无法解析关键类型: " + item.name);
                    if (entry.Type == EUIBinding.WidgetTypes.UIContainer || entry.Type == EUIBinding.WidgetTypes.TabLoader
                        || entry.Type == EUIBinding.WidgetTypes.End || !Enum.IsDefined(typeof(EUIBinding.WidgetTypes), entry.Type))
                        result.blockers.Add("未启用的关键绑定类型: " + item.name + "/" + entry.Type);
                    if (entry.Type == EUIBinding.WidgetTypes.UILogic)
                    {
                        var types = TypeCache.GetTypesDerivedFrom<Ember.UI.EUILogic>()
                            .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null
                                && (t.FullName == item.componentType || t.Name == item.componentType)).ToArray();
                        if (!nested || nested.Role != EUIBindingRole.Item || types.Length != 1)
                            result.blockers.Add("嵌套逻辑必须是唯一可构造的 Item 类型: " + item.name);
                        else item.componentType = types[0].FullName;
                    }
                    if (entry.Type == EUIBinding.WidgetTypes.Extension && (entry.ClassName == null || !entry.ClassName.Contains(".")))
                        result.blockers.Add("Extension 必须使用完整类型名: " + item.name);
                    if (string.IsNullOrEmpty(item.semanticId)) result.unresolved.Add("待登记语义: " + ownerId + "/" + item.name);
                    if (string.IsNullOrEmpty(item.behavior)) result.unresolved.Add("行为/数据来源待确认: " + ownerId + "/" + item.name);
                }
            }
            foreach (var group in result.bindings.GroupBy(x => x.ownerId + "/" + x.name))
                if (group.Count() > 1) result.blockers.Add("重复绑定: " + group.Key);
            foreach (var group in result.bindings.Where(x => x.owner == "parent-logic").GroupBy(x => x.objectId))
                if (group.Count() > 1) result.blockers.Add("重复逻辑所有者: " + group.Key);
            foreach (var note in notes)
                if (!result.bindings.Any(b => b.ownerId == note.ownerId && b.name == note.bindingName))
                    result.blockers.Add("语义映射目标不存在: " + note.ownerId + "/" + note.bindingName);
            result.dependencies = AssetDatabase.GetDependencies(prefabPath, true).OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => AssetDatabase.AssetPathToGUID(x) + ":" + x).ToList();
            result.nodes = result.nodes.OrderBy(x => x.id, StringComparer.Ordinal).ToList();
            result.bindings = result.bindings.OrderBy(x => x.ownerId + "/" + x.name, StringComparer.Ordinal).ToList();
            result.properties = result.properties.OrderBy(PropertyKey, StringComparer.Ordinal).ToList();
            result.resources = result.resources.OrderBy(x => x.id, StringComparer.Ordinal).ToList();
            result.unresolved.Add("动态资源、事件/本地化提供方需语义补充；未从静态资产推断。" );
            if (!root.GetComponentInChildren<UnityEngine.UI.CanvasScaler>(true))
                result.unresolved.Add("Canvas/参考分辨率由外部宿主提供；本 Prefab 未声明 CanvasScaler。" );
            if (string.IsNullOrEmpty(result.generatedDigest)) result.unresolved.Add("生成文件未找到: " + result.generatedPath);
            result.libraryEntries = EUILibraryService.DescribeDependencies(prefabPath);
            foreach (var host in root.GetComponentsInChildren<EUIDynamicListHost>(true))
            {
                if (!result.requiredCapabilities.Contains("dynamic-list-v1")) result.requiredCapabilities.Add("dynamic-list-v1");
                foreach (var issue in host.ValidateConfiguration()) result.blockers.Add(PathOf(root.transform, host.transform) + ": " + issue);
                var item = host.ItemTemplate;
                result.lists.Add(new EUIContractList {
                    hostId = Identity(host), hostPath = PathOf(root.transform, host.transform),
                    containerPrefab = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(host.gameObject)),
                    itemPrefab = AssetDatabase.GetAssetPath(item), itemGuid = item ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item)) : "",
                    itemLogic = item ? item.GetComponent<EUIBinding>()?.ClassName : "",
                    contentId = Identity(host.Content), viewportId = Identity(host.Scroll ? host.Scroll.viewport : null),
                    dataBinding = host.DataBinding, cacheCapacity = host.CacheCapacity });
                if (result.bindings.Any(b => b.owner == "parent-logic" && b.objectId == Identity(host.gameObject)))
                    result.blockers.Add("动态容器根不能同时登记父级 UILogic: " + host.name);
            }
            return result;
        }

        public static List<EUIContractDifference> Compare(EUIContract before, EUIContract after)
        {
            if (before == null || after == null || before.schemaVersion != 1 || after.schemaVersion != 1)
                throw new ArgumentException("仅支持 schemaVersion 1 的两个契约。");
            var changes = new List<EUIContractDifference>();
            void Add(string category, string target, string a, string b, bool block)
            { changes.Add(new EUIContractDifference { category = category, target = target, before = a, after = b, blocksApply = block }); }
            var oldLists = string.Join("\n", (before.lists ?? new List<EUIContractList>()).Select(x => JsonUtility.ToJson(x)));
            var newLists = string.Join("\n", (after.lists ?? new List<EUIContractList>()).Select(x => JsonUtility.ToJson(x)));
            if (oldLists != newLists) Add("list-contract", "lists", oldLists, newLists, true);
            if (before.prefabGuid != after.prefabGuid) Add("asset-identity", "prefabGuid", before.prefabGuid, after.prefabGuid, true);
            if (before.prefabPath != after.prefabPath) Add("asset-path", "prefabPath", before.prefabPath, after.prefabPath, true);
            if (before.role != after.role || before.codeMode != after.codeMode || before.className != after.className || before.baseBindingGuid != after.baseBindingGuid)
                Add("ownership", "root", before.role + "/" + before.codeMode + "/" + before.className + "/" + before.baseBindingGuid,
                    after.role + "/" + after.codeMode + "/" + after.className + "/" + after.baseBindingGuid, true);
            var oldLibrary = JsonUtility.ToJson(new EUILibraryCatalog { entries = before.libraryEntries });
            var newLibrary = JsonUtility.ToJson(new EUILibraryCatalog { entries = after.libraryEntries });
            if (oldLibrary != newLibrary) Add("library-contract", "library", oldLibrary, newLibrary, true);
            var matched = new HashSet<EUIContractBinding>();
            foreach (var old in before.bindings)
            {
                var current = after.bindings.FirstOrDefault(x => x.ownerId == old.ownerId && x.name == old.name);
                if (current == null)
                {
                    var candidates = after.bindings.Where(x => x.ownerId == old.ownerId && x.objectId == old.objectId && !matched.Contains(x)).ToList();
                    if (candidates.Count == 1) { current = candidates[0]; Add("binding-rename", old.name, old.name, current.name, true); }
                }
                if (current == null) { Add("binding-remove", old.name, old.componentType, "", true); continue; }
                matched.Add(current);
                if (old.componentType != current.componentType || old.widgetType != current.widgetType || old.declaredType != current.declaredType)
                    Add("binding-type", old.name, old.widgetType + "/" + old.componentType + "/" + old.declaredType, current.widgetType + "/" + current.componentType + "/" + current.declaredType, true);
                if (old.objectId != current.objectId) Add("object-identity", old.name, old.objectId, current.objectId, true);
                if (old.owner != current.owner || old.isFramework != current.isFramework || old.semanticId != current.semanticId)
                    Add("ownership", old.name, old.owner + "/" + old.isFramework + "/" + old.semanticId, current.owner + "/" + current.isFramework + "/" + current.semanticId, true);
            }
            foreach (var entry in after.bindings.Where(x => !matched.Contains(x))) Add("binding-add", entry.name, "", entry.componentType, true);
            foreach (var node in before.nodes)
            {
                var current = after.nodes.FirstOrDefault(x => x.id == node.id);
                if (current == null || current.path != node.path || current.parentId != node.parentId || current.siblingIndex != node.siblingIndex)
                    Add("hierarchy", node.id, node.path, current?.path ?? "removed", true);
                if (current != null && (current.activeSelf != node.activeSelf || current.layer != node.layer))
                    Add("state", node.id, node.activeSelf + "/" + node.layer, current.activeSelf + "/" + current.layer, true);
                if (current != null && (!node.components.SequenceEqual(current.components) || !node.ownedSlots.SequenceEqual(current.ownedSlots)))
                    Add("component-structure", node.id, string.Join(",", node.components.Concat(node.ownedSlots)), string.Join(",", current.components.Concat(current.ownedSlots)), true);
            }
            foreach (var node in after.nodes.Where(n => !before.nodes.Any(o => o.id == n.id))) Add("hierarchy", node.id, "", node.path, true);
            var oldProperties = before.properties.ToDictionary(PropertyKey);
            var newProperties = after.properties.ToDictionary(PropertyKey);
            foreach (var key in oldProperties.Keys.Union(newProperties.Keys).OrderBy(x => x, StringComparer.Ordinal))
            {
                oldProperties.TryGetValue(key, out var old); newProperties.TryGetValue(key, out var current);
                if (old?.value == current?.value) continue;
                var category = (current ?? old).category;
                Add(category, key, old?.value, current?.value, category != "appearance" && category != "resource");
            }
            foreach (var blocker in after.blockers) Add("validation", blocker, "", blocker, true);
            foreach (var resource in before.resources)
            {
                var current = after.resources.FirstOrDefault(r => r.id == resource.id);
                if (current != null && resource.digest != current.digest)
                    Add("resource", resource.id, resource.digest, current.digest, false);
            }
            if (before.customSettingsType != after.customSettingsType || before.customSettingsJson != after.customSettingsJson)
                Add("binding-config", "customSettings", before.customSettingsJson, after.customSettingsJson, true);
            return changes;
        }

        public static void Write(string outputPath, EUIContract contract)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, JsonUtility.ToJson(contract, true), new UTF8Encoding(false));
        }

        public static string GetSupplementPath(string prefabGuid)
        {
            if (string.IsNullOrEmpty(prefabGuid) || prefabGuid.Length != 32 || !prefabGuid.All(Uri.IsHexDigit))
                throw new ArgumentException("需要真实的 32 位 Prefab GUID。", nameof(prefabGuid));
            return "Assets/Ember/Editor/UIContracts/" + prefabGuid + ".json";
        }

        /// <summary>显式登记语义。Export 不调用此方法，不会隐式写入资产。</summary>
        public static void WriteSupplement(EUIContractSupplement supplement)
        {
            if (supplement == null) throw new ArgumentNullException(nameof(supplement));
            var path = GetSupplementPath(supplement.prefabGuid);
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(supplement.prefabGuid)))
                throw new ArgumentException("Prefab GUID 不存在。");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(supplement, true), new UTF8Encoding(false));
        }

        private static string PropertyKey(EUIContractProperty p) => p.objectId + "/" + p.componentType + "/" + p.name;
        private static string PathOf(Transform root, Transform target) => target == root ? "." : AnimationUtility.CalculateTransformPath(target, root);
        private static string Identity(UnityEngine.Object value)
        {
            if (!value) return "";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id)
                ? guid + ":" + id.ToString(CultureInfo.InvariantCulture) : "";
        }
        private static string Digest(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "";
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static void ReadProperties(Component component, EUIContract result)
        {
            using var so = new SerializedObject(component);
            var p = so.GetIterator();
            while (p.Next(true))
            {
                if (p.propertyPath == "m_Script" || p.propertyPath == "m_GameObject" || p.propertyType == SerializedPropertyType.Generic) continue;
                var category = component is RectTransform ? "layout" : "appearance";
                string value;
                if (p.propertyType == SerializedPropertyType.ObjectReference)
                {
                    category = "resource";
                    var reference = p.objectReferenceValue;
                    value = Identity(reference);
                    if (reference && !(reference is Component) && !(reference is GameObject) && !result.resources.Any(r => r.id == value))
                    {
                        var resource = new EUIContractResource { id = value, path = AssetDatabase.GetAssetPath(reference), type = reference.GetType().FullName };
                        resource.digest = string.IsNullOrEmpty(resource.path) ? "" : AssetDatabase.GetAssetDependencyHash(resource.path).ToString();
                        if (reference is Sprite sprite) { resource.spriteBorder = sprite.border.ToString("R"); resource.width = (int)sprite.rect.width; resource.height = (int)sprite.rect.height; }
                        if (reference is Texture texture) { resource.width = texture.width; resource.height = texture.height; }
                        result.resources.Add(resource);
                    }
                    if (reference && string.IsNullOrEmpty(value)) result.unresolved.Add("非持久引用: " + component.name + "/" + p.propertyPath);
#if UNITY_6000_3_OR_NEWER
                    if (!reference && !p.objectReferenceEntityIdValue.Equals(EntityId.None)) result.blockers.Add("丢失资源: " + component.name + "/" + p.propertyPath);
#else
                    if (!reference && p.objectReferenceInstanceIDValue != 0) result.blockers.Add("丢失资源: " + component.name + "/" + p.propertyPath);
#endif
                    if (reference is Component || reference is GameObject) category = "hierarchy-reference";
                }
                else
                {
                    switch (p.propertyType)
                    {
                        case SerializedPropertyType.String: value = p.stringValue; break;
                        case SerializedPropertyType.Boolean: value = p.boolValue.ToString(); break;
                        case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: case SerializedPropertyType.ArraySize: value = p.longValue.ToString(CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.Float: value = p.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.Color: value = p.colorValue.ToString("R"); break;
                        case SerializedPropertyType.Vector2: value = p.vector2Value.ToString("R"); break;
                        case SerializedPropertyType.Vector3: value = p.vector3Value.ToString("R"); break;
                        case SerializedPropertyType.Vector4: value = p.vector4Value.ToString("R"); break;
                        case SerializedPropertyType.Quaternion: value = p.quaternionValue.ToString("R"); break;
                        case SerializedPropertyType.Rect: value = p.rectValue.ToString("R"); break;
                        default: result.unresolved.Add("未展开序列化类型: " + component.GetType().FullName + "/" + p.propertyPath + "/" + p.propertyType); continue;
                    }
                }
                if (component is EUIBinding) category = "binding-config";
                else if (p.propertyPath.Contains("m_Interactable") || p.propertyPath.Contains("m_IsOn") || p.propertyPath.Contains("enableState") || p.propertyPath.Contains("m_OnClick") || p.propertyPath.Contains("m_OnValueChanged") || p.propertyPath == "m_Enabled") category = "state";
                result.properties.Add(new EUIContractProperty { objectId = Identity(component), componentType = component.GetType().FullName,
                    name = p.propertyPath, category = category, value = value });
            }
        }
    }
}

