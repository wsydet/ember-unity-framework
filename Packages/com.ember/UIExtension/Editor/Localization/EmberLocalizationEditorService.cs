using System;
using System.IO;
using System.Linq;
using Ember.Table.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    public static class EmberLocalizationEditorService
    {
        #region 内部参数
        public const string REGISTRATION_ROOT = "Assets/Ember/Editor/Localization";
        public const string OUTPUT_ROOT = "Assets/GameResource/Resources/Config/Localization";
        public static event Action SourcesSaved;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static EmberLocalizationSource[] Sources() => AssetDatabase.FindAssets("t:EmberLocalizationSource", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<EmberLocalizationSource>)
            .OrderBy(s => s.TableId, StringComparer.Ordinal).ToArray();

        public static EmberLocalizationSource Register(string id, string label, string path, EmberTableDefinition definition = null)
        {
            var existing = Sources().FirstOrDefault(s => s.TableId == id);
            if (existing)
            {
                if (AssetDatabase.GetAssetPath(existing.Source) != path)
                    throw new InvalidOperationException("表标识已被其他源文件使用：" + id);
                return existing;
            }
            if (string.IsNullOrEmpty(id) || id.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                throw new InvalidOperationException("表标识仅允许字母、数字和下划线。");
            Directory.CreateDirectory(REGISTRATION_ROOT);
            AssetDatabase.Refresh();
            var source = ScriptableObject.CreateInstance<EmberLocalizationSource>();
            source.TableId = id; source.DisplayName = label;
            source.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (!source.Source) { UnityEngine.Object.DestroyImmediate(source); throw new InvalidOperationException("找不到 UTF-8 CSV/TSV 源表：" + path); }
            source.Definition = definition;
            AssetDatabase.CreateAsset(source, REGISTRATION_ROOT + "/" + id + ".asset");
            return source;
        }
        public static EmberLocalizationSource InitializeCommon()
        {
            const string path = "Assets/GameResource/TableSources/common_ui_text.etable.csv";
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            if (!File.Exists(path)) File.WriteAllText(path,
                "key,zh_Hans,zh_Hant,ja,en\ncommon.confirm,确定,確定,確認,Confirm\ncommon.cancel,取消,取消,キャンセル,Cancel\n");
            AssetDatabase.Refresh();
            var source = Register("common_ui_text", "公共 UI 文案", path);
            Save(source, EmberTableSourceDocument.Load(path));
            return source;
        }
        public static void Validate(EmberLocalizationSource source, EmberTableSourceDocument document)
        {
            if (!source || !source.Source || document == null || document.Path != AssetDatabase.GetAssetPath(source.Source))
                throw new InvalidOperationException("草稿与所选源表不匹配。");
            if (string.IsNullOrWhiteSpace(source.TableId) || source.TableId.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                throw new InvalidOperationException("表标识仅允许字母、数字和下划线。");
            if (document.Column(source.KeyColumn) < 0 || source.Languages == null || source.Languages.Length == 0 ||
                source.Languages.Distinct().Count() != source.Languages.Length || !source.Languages.Contains(source.SourceLanguage) ||
                source.Languages.Any(l => l == source.KeyColumn || document.Column(l) < 0)) throw new InvalidOperationException("主键列、语言列或源语言配置无效。");
            var keys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var row in document.Rows)
            {
                string key = document.Get(row, source.KeyColumn);
                if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || !keys.Add(key))
                    throw new InvalidOperationException("空白或重复 Key：" + key);
            }
            foreach (var other in Sources().Where(s => s != source))
            {
                if (other.TableId == source.TableId) throw new InvalidOperationException("重复表标识：" + source.TableId);
                if (source.Output && other.Output == source.Output) throw new InvalidOperationException("两个源表不能共用同一个运行产物。");
                if (!other.Source) throw new InvalidOperationException("源表缺失：" + other.DisplayName);
                var data = EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(other.Source));
                if (data.Rows.Any(r => keys.Contains(data.Get(r, other.KeyColumn))))
                    throw new InvalidOperationException("与「" + other.DisplayName + "」存在同名 Key，请使用模块前缀区分。");
            }
            if (source.Definition && source.Definition.Source != source.Source)
                throw new InvalidOperationException("注册源表与配表 Definition 的源文件不一致。");
            if (!source.Output && File.Exists(OUTPUT_ROOT + "/" + source.TableId + ".asset"))
                throw new InvalidOperationException("产物路径已存在，请先在注册资产中绑定正确产物。");
            if (source.Output && !AssetDatabase.GetAssetPath(source.Output).StartsWith(OUTPUT_ROOT + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("多语言产物必须位于 Config/Localization 的 Resources 目录中。");
        }
        public static void Save(EmberLocalizationSource source, EmberTableSourceDocument document)
        {
            Validate(source, document);
            var entries = document.Rows.Select(r => new EmberLocalizationTable.Entry
            {
                Key = document.Get(r, source.KeyColumn), Values = source.Languages.Select(l => document.Get(r, l)).ToArray()
            }).ToArray();
            Directory.CreateDirectory(OUTPUT_ROOT);
            AssetDatabase.Refresh();
            string outputPath = source.Output ? AssetDatabase.GetAssetPath(source.Output) : OUTPUT_ROOT + "/" + source.TableId + ".asset";
            string sourceJson = EditorJsonUtility.ToJson(source);
            var previousOutput = source.Output;
            string outputJson = previousOutput ? EditorJsonUtility.ToJson(previousOutput) : null;
            string original = document.Original;
            string binaryPath = source.Definition ? source.Definition.RuntimeOutputPath : null;
            byte[] previousSource = File.ReadAllBytes(document.Path);
            byte[] previousBinary = binaryPath != null && File.Exists(binaryPath) ? File.ReadAllBytes(binaryPath) : null;
            bool committed = false;
            try
            {
                document.Save(source.Definition);
                committed = true;
                if (!source.Output)
                {
                    source.Output = ScriptableObject.CreateInstance<EmberLocalizationTable>();
                    AssetDatabase.CreateAsset(source.Output, outputPath);
                }
                source.Output.TableId = source.TableId; source.Output.SourceLanguage = source.SourceLanguage;
                source.Output.Languages = (string[])source.Languages.Clone(); source.Output.Entries = entries;
                foreach (var row in document.Rows)
                {
                    string key = document.Get(row, source.KeyColumn), text = document.Get(row, source.SourceLanguage);
                    var review = source.Reviews.FirstOrDefault(r => r.Key == key);
                    if (review == null) source.Reviews.Add(new EmberLocalizationSource.Review { Key = key, SourceText = text });
                    else if (review.SourceText != text) { review.SourceText = text; review.Pending = true; }
                }
                EditorUtility.SetDirty(source); EditorUtility.SetDirty(source.Output); AssetDatabase.SaveAssets();
            }
            catch
            {
                if (committed)
                {
                    File.WriteAllBytes(document.Path, previousSource);
                    if (binaryPath != null)
                    {
                        if (previousBinary != null) File.WriteAllBytes(binaryPath, previousBinary);
                        else AssetDatabase.DeleteAsset(binaryPath);
                    }
                    if (previousOutput) { EditorJsonUtility.FromJsonOverwrite(outputJson, previousOutput); EditorUtility.SetDirty(previousOutput); }
                    else AssetDatabase.DeleteAsset(outputPath);
                    EditorJsonUtility.FromJsonOverwrite(sourceJson, source); EditorUtility.SetDirty(source);
                    AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); document.Original = original;
                }
                throw;
            }
            EmberLocalization.Reload(); SourcesSaved?.Invoke();
        }
        #endregion
    }
}
