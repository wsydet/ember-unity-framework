using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ember.Table;
using Ember.UIExtension;
using Game.Narrative;
using Game.Table.Generated;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>皮肤窗口与自动化共用入口。素材写入目标皮肤目录，创建与批量导入失败会回滚。</summary>
    public static class NovelUISkinEditorService
    {
        #region 内部参数
        public const string ROOT = "Assets/GameResource/Resources/UI/Common/Atlas/NovelSkins";
        private const string ASSIGNMENTS = "Assets/GameResource/Resources/Config/Narrative/UISkinAssignments.asset";
        private static readonly HashSet<string> PAGES = new(StringComparer.Ordinal)
        {
            "EUIMainPanel", "EUINovelReaderPage", "EUINovelReadingMenuPage", "EUINovelHistoryPage",
            "EUINovelFontPage", "EUINovelChoiceItem", "EUINovelSavePage", "EUINovelSaveSlotItem", "EUINovelNameInputPage"
        };
        [Serializable] public sealed class Replacement { public string Key; public string File; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void Editable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("请在编辑模式并完成编译后修改皮肤。");
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static string OwnedFolder(NovelUISkin skin)
        {
            if (!skin) throw new ArgumentNullException(nameof(skin));
            string folder = ROOT + "/" + skin.Id;
            if (AssetDatabase.GetAssetPath(skin) != folder + "/Skin.asset")
                throw new InvalidOperationException("皮肤不在管理目录中，请先从它复制创建新皮肤。");
            return folder;
        }
        private static Sprite CloneSprite(Sprite source, string folder)
        {
            if (!source) return null;
            string path = AssetDatabase.GetAssetPath(source);
            string destination = folder + "/" + Guid.NewGuid().ToString("N");
            if (AssetImporter.GetAtPath(path) is TextureImporter)
            {
                destination += Path.GetExtension(path);
                if (!AssetDatabase.CopyAsset(path, destination)) throw new IOException("无法复制图片：" + path);
                AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
                var copyImporter = (TextureImporter)AssetImporter.GetAtPath(destination);
                var copy = copyImporter.spriteImportMode == SpriteImportMode.Single
                    ? AssetDatabase.LoadAssetAtPath<Sprite>(destination)
                    : AssetDatabase.LoadAllAssetsAtPath(destination).OfType<Sprite>().FirstOrDefault(s => s.name == source.name);
                if (!copy) throw new InvalidOperationException("复制后的图片没有对应 Sprite：" + source.name);
                return copy;
            }
            // Unity 内置图片也物化到皮肤内；GPU 回读不要求源纹理开启 Read/Write。
            Rect rect = source.textureRect;
            var texture = source.texture;
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                Graphics.Blit(texture, rt); RenderTexture.active = rt;
                pixels = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(rect, 0, 0); pixels.Apply();
                destination += ".png"; File.WriteAllBytes(destination, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
                if (pixels) UnityEngine.Object.DestroyImmediate(pixels);
            }
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = source.pixelsPerUnit; importer.spriteBorder = source.border;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
        }
        private static NovelUISkinImage CopyEntry(NovelUISkinImage source)
            => JsonUtility.FromJson<NovelUISkinImage>(JsonUtility.ToJson(source));
        private static NarrativeTableCatalog LoadLegacy(out EmberTableEngine engine)
        {
            engine = new EmberTableEngine(); var catalog = GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (!asset) throw new InvalidOperationException("缺少配表产物：" + entry.ResourcePath);
                bytes.Add(entry.TableId, asset.bytes);
            }
            if (!engine.Load(catalog, bytes).Succeeded) throw new InvalidOperationException("皮肤配表加载失败。");
            return new NarrativeTableCatalog(engine.Database);
        }
        private static string Value(object row, string field) => row.GetType().GetProperty(field)?.GetValue(row) as string;
        private static void ApplyLegacy(List<NovelUISkinImage> images, string id)
        {
            EmberTableEngine engine = null;
            try
            {
                var catalog = LoadLegacy(out engine);
                var rows = catalog.GetType().GetProperty("SkinSprites")?.GetValue(catalog) as IEnumerable;
                if (rows == null) throw new InvalidOperationException("当前项目未安装旧版皮肤表。");
                foreach (object row in rows)
                {
                    if (Value(row, "SkinId") != id) continue;
                    string key = Value(row, "Page") + "|" + Value(row, "Control") + "|" + Value(row, "Node");
                    var entry = images.FirstOrDefault(i => i.Key == key);
                    if (entry == null) throw new InvalidOperationException("旧皮肤目标不在可编辑 UI 清单内：" + key);
                    entry.Sprite = Resources.Load<Sprite>(Value(row, "SpritePath"));
                    if (!entry.Sprite) throw new InvalidOperationException("旧皮肤图片不存在：" + key);
                }
            }
            finally { engine?.Dispose(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string[] Prefabs() => AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameResource/Resources/UI" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => PAGES.Contains(Path.GetFileNameWithoutExtension(p)))
            .OrderBy(p => p, StringComparer.Ordinal).ToArray();

        public static NovelUISkin[] Skins() => AssetDatabase.IsValidFolder(ROOT)
            ? AssetDatabase.FindAssets("t:NovelUISkin", new[] { ROOT }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NovelUISkin>).Where(s => s).OrderBy(s => s.Id).ToArray()
            : Array.Empty<NovelUISkin>();

        public static Dictionary<string, string> LegacySkins()
        {
            var result = new Dictionary<string, string>(); EmberTableEngine engine = null;
            if (typeof(NarrativeTableCatalog).GetProperty("Skins") == null) return result;
            try
            {
                var catalog = LoadLegacy(out engine);
                if (catalog.GetType().GetProperty("Skins")?.GetValue(catalog) is IEnumerable rows)
                    foreach (object row in rows)
                    {
                        string id = Value(row, "SkinId") ?? Value(row, "Id");
                        if (!string.IsNullOrEmpty(id)) result.Add(id, Value(row, "DisplayName") ?? id);
                    }
            }
            finally { engine?.Dispose(); }
            return result;
        }

        public static List<NovelUISkinImage> CaptureBase()
        {
            var entries = new List<NovelUISkinImage>();
            foreach (string path in Prefabs())
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var binding = root.GetComponent<EUIBinding>();
                var bound = new Dictionary<Transform, string>();
                if (binding && binding.Bindings != null)
                    foreach (var item in binding.Bindings)
                        if (item.GameObject && !bound.ContainsKey(item.GameObject.transform)) bound.Add(item.GameObject.transform, item.Name);
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    // 剧情立绘、背景、震屏/遮罩等动态演出不是皮肤占位。
                    if (root.name == "EUINovelReaderPage" &&
                        image.transform.GetComponentInParent<EUIBinding>(true) != binding) continue;
                    Transform anchor = image.transform; string control = "";
                    while (anchor != root.transform && !bound.TryGetValue(anchor, out control)) anchor = anchor.parent;
                    if (anchor == root.transform) control = bound.TryGetValue(anchor, out var c) ? c : "";
                    string node = AnimationUtility.CalculateTransformPath(image.transform, anchor);
                    var entry = NovelUISkinRuntime.Capture(image);
                    entry.Page = root.name; entry.Control = control ?? ""; entry.Node = node;
                    if (entries.Any(e => e.Key == entry.Key)) throw new InvalidOperationException("图片目标不唯一：" + entry.Key);
                    entries.Add(entry);
                }
            }
            return entries;
        }

        public static NovelUISkin Create(string id, string displayName, NovelUISkin source = null, string legacyId = null)
        {
            Editable();
            if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z][a-z0-9_-]{0,47}$") ||
                Regex.IsMatch(id, "^(con|prn|aux|nul|com[1-9]|lpt[1-9])$"))
                throw new ArgumentException("皮肤标识须以小写字母开头，最多 48 个字母、数字、下划线或短横线。");
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("请填写皮肤名称。");
            if (source && !string.IsNullOrEmpty(legacyId)) throw new ArgumentException("只能选择一个来源。");
            string folder = ROOT + "/" + id;
            if (Directory.Exists(folder) || Skins().Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("皮肤标识已存在：" + id);
            if (!string.IsNullOrEmpty(legacyId) && !LegacySkins().ContainsKey(legacyId)) throw new ArgumentException("来源皮肤不存在。");
            var entries = source ? source.Images.Select(CopyEntry).ToList() : CaptureBase();
            if (!string.IsNullOrEmpty(legacyId)) ApplyLegacy(entries, legacyId);
            NovelUISkin skin = null;
            try
            {
                Folder(folder + "/Images");
                skin = ScriptableObject.CreateInstance<NovelUISkin>();
                skin.Id = id; skin.DisplayName = displayName.Trim(); skin.Source = source ? source.Id : legacyId ?? "基础外观";
                foreach (var entry in entries)
                {
                    entry.Sprite = CloneSprite(entry.Sprite, folder + "/Images"); entry.Pending = true;
                    skin.Images.Add(entry);
                }
                AssetDatabase.CreateAsset(skin, folder + "/Skin.asset"); AssetDatabase.SaveAssets();
                return skin;
            }
            catch
            {
                if (skin && !AssetDatabase.Contains(skin)) UnityEngine.Object.DestroyImmediate(skin);
                AssetDatabase.DeleteAsset(folder); throw;
            }
        }

        /// <summary>先验证整批目标和输入文件，再导入；失败时配置不变并删除本批新资产。</summary>
        public static void ReplaceImages(NovelUISkin skin, Replacement[] replacements)
        {
            Editable(); string folder = OwnedFolder(skin) + "/Images";
            if (replacements == null || replacements.Length == 0) return;
            var keys = new HashSet<string>();
            foreach (var item in replacements)
            {
                if (item == null || !keys.Add(item.Key) || skin.Images.Count(e => e.Key == item.Key) != 1)
                    throw new ArgumentException("图片目标为空、重复或不存在。");
                if (!File.Exists(item.File) || !new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd" }.Contains(Path.GetExtension(item.File).ToLowerInvariant()))
                    throw new ArgumentException("请选择有效的 PNG、JPG、TGA 或 PSD：" + item.File);
            }
            var paths = new List<string>(); var sprites = new List<Sprite>();
            string before = EditorJsonUtility.ToJson(skin);
            try
            {
                foreach (var item in replacements)
                {
                    string path = folder + "/" + Guid.NewGuid().ToString("N") + Path.GetExtension(item.File).ToLowerInvariant();
                    paths.Add(path); File.Copy(item.File, path, false);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!importer) throw new InvalidOperationException("图片无法导入：" + item.File);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!sprite) throw new InvalidOperationException("图片无法生成 Sprite：" + item.File);
                    sprites.Add(sprite);
                }
                Undo.RecordObject(skin, "替换皮肤图片");
                for (int i = 0; i < replacements.Length; i++)
                {
                    var entry = skin.Images.Single(e => e.Key == replacements[i].Key);
                    entry.Sprite = sprites[i]; entry.Pending = false;
                }
                EditorUtility.SetDirty(skin); AssetDatabase.SaveAssets();
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(before, skin); EditorUtility.SetDirty(skin);
                foreach (string path in paths) AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets(); throw;
            }
        }

        public static void SetAppearance(NovelUISkin skin, string key, Color color, Image.Type type, bool preserveAspect,
            float pixelsPerUnitMultiplier, Vector4 border)
        {
            Editable(); string folder = OwnedFolder(skin);
            var entry = skin.Images.Single(e => e.Key == key);
            if (!float.IsFinite(pixelsPerUnitMultiplier) || pixelsPerUnitMultiplier <= 0) throw new ArgumentException("九宫格比例须大于零。");
            if (!float.IsFinite(border.x + border.y + border.z + border.w) || border.x < 0 || border.y < 0 || border.z < 0 || border.w < 0)
                throw new ArgumentException("九宫格边距须为有限非负数。");
            if (entry.Sprite && border != entry.Sprite.border)
            {
                string path = AssetDatabase.GetAssetPath(entry.Sprite);
                if (!path.StartsWith(folder + "/Images/", StringComparison.Ordinal)) throw new InvalidOperationException("不能修改共享素材的导入设置。");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer || importer.spriteImportMode != SpriteImportMode.Single)
                    throw new InvalidOperationException("多 Sprite 图片请先替换为独立图片，再调整边距。");
                var size = entry.Sprite.rect.size;
                if (border.x + border.z > size.x || border.y + border.w > size.y) throw new ArgumentException("九宫格边距超过图片尺寸。");
                Undo.RecordObject(importer, "调整皮肤九宫格"); importer.spriteBorder = border; importer.SaveAndReimport();
            }
            Undo.RecordObject(skin, "调整皮肤显示");
            entry.Color = color; entry.Type = type; entry.PreserveAspect = preserveAspect; entry.PixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            EditorUtility.SetDirty(skin); AssetDatabase.SaveAssets();
        }

        public static void Assign(NarrativeStorySO story, NovelUISkin skin)
        {
            Editable();
            if (!story || !AssetDatabase.Contains(story)) throw new ArgumentException("请选择已保存的剧情资产。");
            if (skin) OwnedFolder(skin);
            var asset = AssetDatabase.LoadAssetAtPath<NovelUISkinAssignments>(ASSIGNMENTS);
            if (!asset)
            {
                Folder(Path.GetDirectoryName(ASSIGNMENTS).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<NovelUISkinAssignments>(); AssetDatabase.CreateAsset(asset, ASSIGNMENTS);
            }
            Undo.RecordObject(asset, "应用剧情皮肤");
            asset.Stories.RemoveAll(a => a == null || a.Story == story);
            asset.Stories.Add(new NovelUISkinAssignment { Story = story, Skin = skin });
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
        }

        public static string[] Validate(NovelUISkin skin)
        {
            if (!skin) return new[] { "未选择皮肤。" };
            var pages = Prefabs().ToDictionary(Path.GetFileNameWithoutExtension, p => AssetDatabase.LoadAssetAtPath<GameObject>(p));
            var issues = new List<string>(); var keys = new HashSet<string>();
            foreach (var e in skin.Images)
            {
                if (e == null) { issues.Add("存在空图片条目。"); continue; }
                if (!keys.Add(e.Key)) issues.Add("重复目标：" + e.Key);
                if (!pages.TryGetValue(e.Page, out var root) || !NovelUISkinRuntime.Resolve(root, e.Control, e.Node)) issues.Add("目标已失效：" + e.Key);
                if (!e.Sprite && !e.Pending) issues.Add("替换图片丢失：" + e.Key);
                if (e.Sprite && !AssetDatabase.GetAssetPath(e.Sprite).StartsWith(ROOT + "/" + skin.Id + "/Images/", StringComparison.Ordinal))
                    issues.Add("引用了皮肤外的共享图片：" + e.Key);
                if (e.Type == Image.Type.Sliced && e.Sprite && e.Sprite.border == Vector4.zero) issues.Add("九宫格未设置边距：" + e.Key);
            }
            return issues.ToArray();
        }
        #endregion
    }
}
