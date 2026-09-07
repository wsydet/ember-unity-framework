// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>效果图的来源凭据，独立于可部署 Assets 和模板内容 hash。</summary>
    [Serializable]
    internal sealed class EmberTemplatePreviewRecord
    {
        public string templateId;
        public string templateVersion;
        public string frameworkVersion;
        public string contentHash;
        public string projectFingerprint;
        public string capturedAt;
        public string imageFile;
        public string imageHash;
        public bool draft = true;
    }

    /// <summary>真实截图的导入、Game 视图捕获和版本归档；不会加载其他模板来预览。</summary>
    internal sealed class EmberTemplatePreviewPanel : IDisposable
    {
        #region 内部参数

        private readonly EmberSetupWindowContext _context;
        private readonly List<EmberTemplatePreviewRecord> _records = new();
        private string _templateId;
        private Texture2D _texture;
        private int _index;
        private string _message;
        private string _capturePath;
        private TemplateInfo _captureTemplate;
        private string _captureFingerprint;
        private double _captureDeadline;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberTemplatePreviewPanel(EmberSetupWindowContext context) { _context = context; }

        public void Dispose()
        {
            EditorApplication.update -= CompleteCapture;
            if (_texture != null) UnityEngine.Object.DestroyImmediate(_texture);
            _texture = null;
        }

        internal void Refresh() { _templateId = null; }

        internal void Draw(TemplateInfo selected, EditingTemplateRecord editing)
        {
            if (_templateId != selected.id) Load(selected);
            EditorGUILayout.LabelField("模板效果", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selected.description, EditorStyles.wordWrappedLabel);
            if (_records.Count == 0)
                EditorGUILayout.HelpBox("此模板尚未记录效果图。加载并运行模板后，可捕获 Game 视图，或导入真实截图。截图先作为草稿保存。", MessageType.Info);
            else
            {
                int next = EditorGUILayout.Popup("效果记录", _index,
                    _records.Select(item => $"{item.capturedAt} · v{item.templateVersion} · {(item.draft ? "草稿" : "版本效果")}").ToArray());
                if (next != _index) { _index = next; TryOperation(() => LoadTexture(selected)); }
                var record = _records[_index];
                bool current = IsCurrent(record, selected);
                EditorGUILayout.HelpBox(record.draft ? "当前显示截图草稿，尚未确认为模板版本效果。"
                    : current ? $"模板 v{record.templateVersion} · 兼容框架 {record.frameworkVersion} 的已确认效果。"
                    : "模板版本、内容或兼容声明已变化，此效果图需要更新。", current && !record.draft ? MessageType.Info : MessageType.Warning);
                if (_texture != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(100, 240, GUILayout.ExpandWidth(true));
                    GUI.DrawTexture(rect, _texture, ScaleMode.ScaleToFit);
                }
                using (new EditorGUI.DisabledScope(_context.OperationsBlocked || !record.draft || editing?.templateId != selected.id))
                    if (GUILayout.Button("核对内容并确认为模板版本效果"))
                        TryOperation(() => Confirm(selected, record));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_context.OperationsBlocked || editing?.templateId != selected.id))
                    if (GUILayout.Button("导入真实截图（草稿）")) TryOperation(() => Import(selected));
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _capturePath != null || editing?.templateId != selected.id))
                    if (GUILayout.Button(_capturePath == null ? "捕获 Game 视图（草稿）" : "正在捕获…"))
                        TryOperation(() => StartCapture(selected));
            }
            EditorGUILayout.LabelField("草稿记录绑定捕获时的项目文件；内容保存并封存后，再确认版本效果。", EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("当前项目演示", EditorStyles.boldLabel);
            bool loaded = editing?.templateId == selected.id;
            EditorGUILayout.LabelField(loaded ? "演示运行的是当前项目，包含尚未保存到模板的修改。"
                : "请先加载此模板，再打开它在当前项目中的演示场景。", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(_context.OperationsBlocked || !loaded))
            {
                foreach (var scene in EmberProjectSetup.GetTemplateScenes(selected.id))
                {
                    using (new EditorGUI.DisabledScope(!File.Exists(scene)))
                        if (GUILayout.Button("打开 " + Path.GetFileNameWithoutExtension(scene)))
                            TryOperation(() =>
                            {
                                RequireLoaded(selected.id);
                                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                    EditorSceneManager.OpenScene(scene);
                            });
                }
                if (GUILayout.Button("从框架入口运行验证")) TryOperation(() =>
                {
                    RequireLoaded(selected.id);
                    const string scene = "Assets/Game/Scenes/FrameworkScene.unity";
                    if (!File.Exists(scene)) throw new InvalidOperationException("框架入口场景缺失，请先检查项目。");
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    EditorSceneManager.OpenScene(scene);
                    EditorApplication.isPlaying = true;
                });
            }
        }

        internal static bool IsCurrent(EmberTemplatePreviewRecord record, TemplateInfo template)
        {
            return record != null && template != null && record.templateId == template.id
                && record.templateVersion == template.version && record.contentHash == template.contentHash
                && record.frameworkVersion == template.frameworkVersion;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void Load(TemplateInfo template)
        {
            _templateId = template.id;
            _index = 0;
            _records.Clear();
            if (_texture != null) UnityEngine.Object.DestroyImmediate(_texture);
            _texture = null;
            try
            {
                var folder = Folder(template.id);
                if (!Directory.Exists(folder)) return;
                foreach (var path in Directory.GetFiles(folder, "*.json").OrderByDescending(path => path, StringComparer.Ordinal))
                {
                    var record = JsonUtility.FromJson<EmberTemplatePreviewRecord>(File.ReadAllText(path));
                    if (record == null || record.templateId != template.id || !SafeImageName(record.imageFile)) continue;
                    _records.Add(record);
                }
                LoadTexture(template);
            }
            catch (Exception ex) { _message = "效果图读取失败：" + ex.Message; }
        }

        private void LoadTexture(TemplateInfo template)
        {
            if (_texture != null) UnityEngine.Object.DestroyImmediate(_texture);
            _texture = null;
            if (_records.Count == 0) return;
            var record = _records[_index];
            var path = Path.Combine(Folder(template.id), record.imageFile);
            if (!File.Exists(path)) { _message = "效果图文件缺失。"; return; }
            var bytes = ReadImageBytes(path);
            if (Ember.Basic.CryptographyUtils.GetMD5(bytes) != record.imageHash)
            { _message = "效果图内容与记录不一致，请重新导入。"; return; }
            _texture = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            if (!_texture.LoadImage(bytes)) throw new InvalidOperationException("无法解码截图。");
        }

        private void Import(TemplateInfo template)
        {
            var path = EditorUtility.OpenFilePanel("导入当前模板的真实截图", string.Empty, "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return;
            if (EmberProjectValidationService.HasUnsavedProjectContent())
                throw new InvalidOperationException("请先保存场景和资源，再导入截图以记录正确的内容来源。");
            var report = EmberProjectValidationService.CompareEditingTemplate(template.id);
            SaveDraft(template, ReadImageBytes(path), report.ProjectFingerprint, Path.GetExtension(path).ToLowerInvariant());
        }

        private void StartCapture(TemplateInfo template)
        {
            var report = EmberProjectValidationService.CompareEditingTemplate(template.id);
            _captureTemplate = EmberTemplateInheritanceEngine.Clone(template);
            _captureFingerprint = report.ProjectFingerprint;
            var directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "EmberPreviewCaptures");
            Directory.CreateDirectory(directory);
            _capturePath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".png");
            _captureDeadline = EditorApplication.timeSinceStartup + 15;
            ScreenCapture.CaptureScreenshot(_capturePath);
            EditorApplication.update += CompleteCapture;
        }

        private void CompleteCapture()
        {
            byte[] bytes = null;
            try
            {
                if (File.Exists(_capturePath))
                {
                    var candidate = ReadImageBytes(_capturePath);
                    // PNG 的 IEND 块写完后才读取；避免截图异步落盘期间导入半个文件。
                    if (candidate.Length >= 12 && candidate[candidate.Length - 8] == (byte)'I'
                        && candidate[candidate.Length - 7] == (byte)'E'
                        && candidate[candidate.Length - 6] == (byte)'N'
                        && candidate[candidate.Length - 5] == (byte)'D') bytes = candidate;
                }
            }
            catch (IOException) { /* 截图文件仍在写入，等待本次捕获的有界截止时间。 */ }
            if (bytes == null && EditorApplication.timeSinceStartup < _captureDeadline) return;
            EditorApplication.update -= CompleteCapture;
            try
            {
                if (bytes == null) throw new IOException("截图超时或文件不完整，请保持 Game 视图可见并取消暂停后重试。");
                SaveDraft(_captureTemplate, bytes, _captureFingerprint, ".png");
                File.Delete(_capturePath);
            }
            catch (Exception ex) { _message = "捕获失败：" + ex.Message; }
            finally { _capturePath = null; _context.Repaint(); }
        }

        private void SaveDraft(TemplateInfo template, byte[] bytes, string fingerprint, string extension)
        {
            var check = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (!check.LoadImage(bytes)) throw new InvalidOperationException("请选择有效的 PNG/JPEG 图片。");
            }
            finally { UnityEngine.Object.DestroyImmediate(check); }
            string name = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
            var record = new EmberTemplatePreviewRecord
            {
                templateId = template.id, templateVersion = template.version,
                frameworkVersion = template.frameworkVersion, contentHash = template.contentHash,
                projectFingerprint = fingerprint, capturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                imageFile = name + extension, imageHash = Ember.Basic.CryptographyUtils.GetMD5(bytes)
            };
            WriteRecord(template.id, record, bytes);
            _message = "截图草稿已保存；退出运行并保存、封存模板后，可核对并确认版本效果。";
            Refresh();
            _context.Invalidate();
        }

        private void Confirm(TemplateInfo selected, EmberTemplatePreviewRecord record)
        {
            var template = EmberProjectSetup.GetTemplates().Find(item => item.id == selected.id)
                ?? throw new InvalidOperationException("模板不存在。");
            if (EmberProjectValidationService.HasUnsavedProjectContent())
                throw new InvalidOperationException("场景或资源有未保存内容，请先保存并核对。");
            var comparison = EmberProjectValidationService.CompareEditingTemplate(template.id);
            if (template.contentHash != template.versionedContentHash || comparison.DifferenceCount != 0
                || comparison.ProjectFingerprint != record.projectFingerprint)
                throw new InvalidOperationException("模板必须已保存并封存，且当前项目内容与截图来源一致。内容变化后请重新截图。");
            var bytes = ReadImageBytes(Path.Combine(Folder(template.id), record.imageFile));
            if (Ember.Basic.CryptographyUtils.GetMD5(bytes) != record.imageHash)
                throw new InvalidOperationException("截图内容与记录不一致，请重新导入。");
            var updated = JsonUtility.FromJson<EmberTemplatePreviewRecord>(JsonUtility.ToJson(record));
            updated.templateVersion = template.version;
            updated.frameworkVersion = template.frameworkVersion;
            updated.contentHash = template.contentHash;
            updated.draft = false;
            WriteRecord(template.id, updated, bytes);
            _message = "已将真实截图关联到当前封存版本。";
            Refresh();
            _context.Invalidate();
        }

        private static void WriteRecord(string templateId, EmberTemplatePreviewRecord record, byte[] imageBytes)
        {
            if (!EmberProjectSetup.IsEmbeddedPackage()) throw new InvalidOperationException("仅开发环境允许记录模板效果。");
            if (!SafeImageName(record.imageFile)) throw new InvalidOperationException("无效的截图文件名。");
            var stage = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "EmberPreviewStage", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(stage);
                File.WriteAllBytes(Path.Combine(stage, record.imageFile), imageBytes);
                string json = Path.GetFileNameWithoutExtension(record.imageFile) + ".json";
                EmberTemplateTransaction.WriteJson(Path.Combine(stage, json), record);
                EmberTemplateTransaction.CommitPreparedTargets(new[]
                {
                    new TemplateTransactionTarget(Path.Combine(stage, record.imageFile), Path.Combine(Folder(templateId), record.imageFile)),
                    new TemplateTransactionTarget(Path.Combine(stage, json), Path.Combine(Folder(templateId), json))
                });
            }
            finally { EmberTemplateTransaction.CleanPath(stage); }
        }

        private static string Folder(string templateId) => Path.Combine(Path.GetDirectoryName(EmberProjectSetup.GetTemplateAssetsPath(templateId)), "Preview~");
        private static bool SafeImageName(string name) => !string.IsNullOrEmpty(name) && name == Path.GetFileName(name)
            && name.IndexOfAny(new[] { '/', '\\', ':' }) < 0
            && new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(name).ToLowerInvariant());

        private static byte[] ReadImageBytes(string path)
        {
            if (new FileInfo(path).Length > 20 * 1024 * 1024) throw new IOException("请选择小于 20 MB 的截图。");
            return File.ReadAllBytes(path);
        }

        private static void RequireLoaded(string templateId)
        {
            if (EmberProjectSetup.GetEditingTemplate()?.templateId != templateId || EmberProjectSetup.IsEditingCopyStale(templateId))
                throw new InvalidOperationException("项目编辑模板已变化或过期，请刷新状态并重新加载目标模板。");
        }

        private void TryOperation(Action action)
        {
            try { action(); }
            catch (Exception ex) { _message = ex.Message; }
        }

        #endregion
    }
}
