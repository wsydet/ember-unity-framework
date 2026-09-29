using System;
using System.Linq;
using Sirenix.Utilities.Editor;
using Ember.Table.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Ember.UIExtension.Editor
{
    public sealed class EmberLocalizationWindow : EmberSourceEditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private EmberLocalizationSource _source;
        [SerializeField] private bool _missingOnly, _pendingOnly, _translation, _onlyEmpty = true, _register;
        [SerializeField] private string _endpoint = "", _model = "", _context = "", _tableId = "", _label = "";
        [SerializeField] private TextAsset _newSource;
        [SerializeField] private int _sourceIndex, _targetMask = -1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private EmberLocalizationSource[] _sources = Array.Empty<EmberLocalizationSource>();
        private string _apiKey = "";
        private UnityWebRequest _request;
        private EmberTranslationRequest.Translation[] _suggestions;
        private string[] _before, _targets;
        private string _requestSource;
        private EmberTableSourceDocument.Row _requestRow;
        protected override string KeyColumn => _source ? _source.KeyColumn : "key";
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        protected override void OnEnable()
        {
            base.OnEnable(); titleContent = new GUIContent("多语言中心");
            Run(() =>
            {
                _sources = EmberLocalizationEditorService.Sources();
                if (Document == null && (_source || _sources.Length > 0)) Select(_source ? _source : _sources[0]);
            });
            EditorApplication.update += PollTranslation;
        }
        protected override void OnDisable()
        {
            base.OnDisable(); EditorApplication.update -= PollTranslation; CancelTranslation(); _apiKey = "";
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        protected override void DrawHeader()
        {
            SirenixEditorGUI.Title("多语言中心", Document == null ? "选择文案表，开始编辑" : $"{Document.Rows.Count} 个 Key · {(Document.Dirty ? "有未保存修改" : "已保存")}", TextAlignment.Left, true);
            using (new EditorGUILayout.HorizontalScope())
            {
                int index = Array.IndexOf(_sources, _source);
                int next = EditorGUILayout.Popup("文案表", index, _sources.Select(s => s.DisplayName + " · " + s.TableId).ToArray());
                if (next >= 0 && next != index && CanSwitch()) Run(() => Select(_sources[next]));
                if (GUILayout.Button("刷新表清单", GUILayout.Width(95))) Run(() => _sources = EmberLocalizationEditorService.Sources());
                if (_source && GUILayout.Button("表配置", GUILayout.Width(65))) Selection.activeObject = _source;
            }
            if (_sources.Length == 0)
            {
                EditorGUILayout.HelpBox("先初始化公共文案，或注册项目现有的 CSV/TSV 表。", MessageType.Info);
                if (GUILayout.Button("初始化公共 UI 文案")) Run(() => { Select(EmberLocalizationEditorService.InitializeCommon()); _sources = EmberLocalizationEditorService.Sources(); });
            }
            _register = SirenixEditorGUI.Foldout(_register, "注册已有文案表");
            if (_register)
            {
                _newSource = (TextAsset)EditorGUILayout.ObjectField("CSV / TSV", _newSource, typeof(TextAsset), false);
                _tableId = EditorGUILayout.TextField("唯一表标识", _tableId);
                _label = EditorGUILayout.TextField("显示名称", _label);
                if (GUILayout.Button("注册并打开") && CanSwitch()) Run(() =>
                {
                    Select(EmberLocalizationEditorService.Register(_tableId, _label, AssetDatabase.GetAssetPath(_newSource)));
                    _sources = EmberLocalizationEditorService.Sources(); _register = false;
                });
            }
        }
        private void Select(EmberLocalizationSource source)
        {
            var document = EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(source.Source));
            if (document.Column(source.KeyColumn) < 0 || source.Languages.Any(l => document.Column(l) < 0))
                throw new InvalidOperationException("源表与注册配置的列不匹配，请检查表配置。");
            CancelTranslation(); _suggestions = null; _source = source; Document = document;
            Selected = document.Rows.Count > 0 ? 0 : -1;
            _sourceIndex = Math.Max(0, Array.IndexOf(source.Languages, source.SourceLanguage));
            hasUnsavedChanges = false; Message = null;
        }
        protected override void Reload() { if (CanSwitch()) Select(_source); }
        protected override void DrawFilters()
        {
            _missingOnly = EditorGUILayout.ToggleLeft("只看缺译", _missingOnly);
            _pendingOnly = EditorGUILayout.ToggleLeft("只看待复核", _pendingOnly);
        }
        private bool Pending(EmberTableSourceDocument.Row row)
        {
            var review = _source.Reviews.FirstOrDefault(r => r.Key == Document.Get(row, KeyColumn));
            return review != null && (review.Pending || review.SourceText != Document.Get(row, _source.SourceLanguage));
        }
        protected override bool Matches(EmberTableSourceDocument.Row row) =>
            (!_missingOnly || _source.Languages.Any(l => string.IsNullOrEmpty(Document.Get(row, l)))) && (!_pendingOnly || Pending(row));
        protected override string ListLabel(EmberTableSourceDocument.Row row)
        {
            string text = Document.Get(row, _source.SourceLanguage).Replace('\n', ' ');
            return (Pending(row) ? "● " : "") + Document.Get(row, KeyColumn) + "\n" + (text.Length > 32 ? text.Substring(0, 32) + "…" : text);
        }
        protected override void DrawDetail(EmberTableSourceDocument.Row row)
        {
            if (Pending(row)) EditorGUILayout.HelpBox("源文案有变动，其他语言需要复核。", MessageType.Warning);
            foreach (string language in _source.Languages.OrderBy(l => l == _source.SourceLanguage ? 0 : 1))
            {
                SirenixEditorGUI.BeginBox(language + (language == _source.SourceLanguage ? " · 源语言" : " · 译文"));
                Set(row, language, EditorGUILayout.TextArea(Document.Get(row, language), GUILayout.MinHeight(65)));
                SirenixEditorGUI.EndBox();
            }
            if (GUILayout.Button("标记本条翻译已复核")) Run(() =>
            {
                // 先保存才能记录与正式源文案一致的复核状态。
                SaveChanges();
                var review = _source.Reviews.First(r => r.Key == Document.Get(row, KeyColumn));
                Undo.RecordObject(_source, "确认翻译复核"); review.Pending = false;
                EditorUtility.SetDirty(_source); AssetDatabase.SaveAssets();
            });
            _translation = SirenixEditorGUI.Foldout(_translation, "翻译助手 · 生成、复核与应用");
            if (_translation) DrawTranslation(row);
        }
        private void DrawTranslation(EmberTableSourceDocument.Row row)
        {
            EditorGUILayout.HelpBox("点击生成时会将当前文案、相邻文案和上下文发送至所填服务。密钥仅保留在窗口内存中。", MessageType.Info);
            _endpoint = EditorGUILayout.TextField("Chat Completions URL", _endpoint);
            _model = EditorGUILayout.TextField("模型", _model);
            _apiKey = EditorGUILayout.PasswordField("API Key", _apiKey);
            _sourceIndex = EditorGUILayout.Popup("本次源语言", Mathf.Clamp(_sourceIndex, 0, _source.Languages.Length - 1), _source.Languages);
            _targetMask = EditorGUILayout.MaskField("目标语言", _targetMask, _source.Languages);
            _onlyEmpty = EditorGUILayout.ToggleLeft("仅补齐空白；取消则生成已有译文的新版本", _onlyEmpty);
            EditorGUILayout.LabelField("补充上下文 / 术语约定");
            _context = EditorGUILayout.TextArea(_context, GUILayout.MinHeight(45));
            if (_request != null)
            {
                if (GUILayout.Button("正在翻译… 点击取消")) { CancelTranslation(); Message = "翻译已取消。"; }
            }
            else if (GUILayout.Button("生成译文预览")) Run(() =>
            {
                _targets = _source.Languages.Where((l, i) => i != _sourceIndex && (_targetMask & (1 << i)) != 0 &&
                    (!_onlyEmpty || string.IsNullOrEmpty(Document.Get(row, l)))).ToArray();
                _requestRow = row; _before = (string[])row.Values.Clone();
                _requestSource = Document.Get(row, _source.Languages[_sourceIndex]);
                string nearby = string.Join("\n", Document.Rows.Skip(Math.Max(0, Selected - 1)).Take(3)
                    .Select(r => Document.Get(r, KeyColumn) + ": " + Document.Get(r, _source.Languages[_sourceIndex])));
                _suggestions = null;
                _request = EmberTranslationRequest.Start(_endpoint, _model, _apiKey, _source.Languages[_sourceIndex], _requestSource,
                    _targets, _source.TranslationContext + "\n" + _context + "\n" + nearby);
            });
            if (_suggestions == null || row != _requestRow) return;
            EditorGUILayout.LabelField("译文预览 · 尚未应用", EditorStyles.boldLabel);
            foreach (var suggestion in _suggestions)
            {
                EditorGUILayout.LabelField(suggestion.language + " · 当前译文");
                EditorGUILayout.HelpBox(Document.Get(row, suggestion.language), MessageType.None);
                suggestion.text = EditorGUILayout.TextArea(suggestion.text, GUILayout.MinHeight(60));
            }
            if (GUILayout.Button("应用全部预览译文到草稿")) Run(() =>
            {
                if (!_before.SequenceEqual(row.Values)) throw new InvalidOperationException("请求发出后本条文案已变化，请重新生成，避免覆盖新修改。");
                foreach (var suggestion in _suggestions) EmberTranslationRequest.ValidateTokens(_requestSource, suggestion.text);
                foreach (var suggestion in _suggestions) Set(row, suggestion.language, suggestion.text);
                _suggestions = null; Message = "译文已应用到草稿，请复核后保存。";
            });
        }
        private void PollTranslation()
        {
            if (_request == null || !_request.isDone) return;
            Run(() => { try { _suggestions = EmberTranslationRequest.Read(_request, _requestSource, _targets); } finally { CancelTranslation(); } });
        }
        private void CancelTranslation() { if (_request == null) return; _request.Abort(); _request.Dispose(); _request = null; }
        protected override void Commit() => EmberLocalizationEditorService.Save(_source, Document);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [MenuItem("Ember/多语言中心", priority = 130)]
        public static void Open() => GetWindow<EmberLocalizationWindow>().Show();
        #endregion
    }
}
