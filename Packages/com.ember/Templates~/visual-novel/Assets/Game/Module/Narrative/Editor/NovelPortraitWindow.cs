using System;
using System.IO;
using System.Linq;
using Ember.Table.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    public sealed class NovelPortraitWindow : EmberSourceEditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private EmberTableDefinition _definition;
        [SerializeField] private bool _missingOnly;
        [SerializeField] private float _zoom = 1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private string[] _characters = Array.Empty<string>();
        protected override string KeyColumn => "id";
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        protected override void OnEnable()
        {
            base.OnEnable(); titleContent = new GUIContent("立绘管理");
            Run(() => { if (Document == null) LoadPortraits(); else LoadCharacters(); });
        }
        protected override void OnGUI()
        {
            if (!NarrativeEditorAvailability.Enabled)
            { EditorGUILayout.HelpBox("小说模块未启用。", MessageType.Info); return; }
            base.OnGUI();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void LoadPortraits()
        {
            _definition = EmberTablePipeline.FindAllDefinitions().FirstOrDefault(d => d.TableId == "novel_portraits");
            if (!_definition) throw new InvalidOperationException("未找到 novel_portraits 表定义。");
            LoadDocument(AssetDatabase.GetAssetPath(_definition.Source));
            LoadCharacters();
        }
        private void LoadCharacters()
        {
            var characters = EmberTablePipeline.FindAllDefinitions().FirstOrDefault(d => d.TableId == "novel_characters");
            _characters = characters ? EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(characters.Source)).Rows.Select(r => r.Values[0]).ToArray() : Array.Empty<string>();
        }
        protected override void DrawHeader() => EditorGUILayout.LabelField("立绘管理", EditorStyles.largeLabel);
        protected override void Reload() { if (CanSwitch()) LoadPortraits(); }
        protected override void DrawFilters() => _missingOnly = EditorGUILayout.ToggleLeft("只看图片缺失", _missingOnly);
        protected override bool Matches(EmberTableSourceDocument.Row row) => !_missingOnly || !LoadImage(row);
        protected override string ListLabel(EmberTableSourceDocument.Row row) => Document.Get(row, "id") + "\n" +
            Document.Get(row, "characterId") + " · " + Document.Get(row, "expression");
        protected override void InitializeRow(EmberTableSourceDocument.Row row)
        {
            if (_characters.Length > 0) row.Values[Document.Column("characterId")] = _characters[0];
            row.Values[Document.Column("expression")] = "neutral";
        }
        private Texture2D LoadImage(EmberTableSourceDocument.Row row) => Resources.Load<Texture2D>(Document.Get(row, "resourcePath"));
        protected override void DrawDetail(EmberTableSourceDocument.Row row)
        {
            string character = Document.Get(row, "characterId");
            int index = Array.IndexOf(_characters, character);
            int selected = EditorGUILayout.Popup("角色", index, _characters);
            if (selected >= 0 && selected != index) Set(row, "characterId", _characters[selected]);
            if (index < 0) EditorGUILayout.HelpBox("角色不存在：" + character, MessageType.Warning);
            Set(row, "expression", EditorGUILayout.TextField("表情", Document.Get(row, "expression")));
            Set(row, "resourcePath", EditorGUILayout.TextField("资源路径", Document.Get(row, "resourcePath")));
            var image = LoadImage(row);
            var picked = EditorGUILayout.ObjectField("绑定图片", image, typeof(Texture2D), false) as Texture2D;
            if (picked != image) Run(() =>
            {
                if (!picked) { Set(row, "resourcePath", ""); return; }
                string path = AssetDatabase.GetAssetPath(picked);
                const string marker = "/Resources/";
                int start = path.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0 || !path.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new InvalidOperationException("请选择项目 Resources 目录中的立绘图片。");
                path = path.Substring(start + marker.Length);
                Set(row, "resourcePath", path.Substring(0, path.Length - Path.GetExtension(path).Length));
            });
            if (!image) { EditorGUILayout.HelpBox("图片缺失，请绑定图片或修正资源路径。", MessageType.Warning); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("定位图片")) EditorGUIUtility.PingObject(image);
                if (GUILayout.Button("适应窗口")) _zoom = 1;
            }
            _zoom = EditorGUILayout.Slider("预览缩放", _zoom, .25f, 2f);
            EditorGUILayout.LabelField($"{image.width} × {image.height}", EditorStyles.miniLabel);
            Rect rect = GUILayoutUtility.GetRect(180, Mathf.Max(240, position.height * .5f * _zoom), GUILayout.ExpandWidth(true));
            const int size = 16;
            for (int y = 0; y < Mathf.CeilToInt(rect.height / size); y++)
                for (int x = 0; x < Mathf.CeilToInt(rect.width / size); x++)
                    EditorGUI.DrawRect(new Rect(rect.x + x * size, rect.y + y * size, Mathf.Min(size, rect.width - x * size), Mathf.Min(size, rect.height - y * size)),
                        (x + y) % 2 == 0 ? new Color(.25f, .25f, .25f) : new Color(.35f, .35f, .35f));
            GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit, true);
        }
        protected override void Commit()
        {
            foreach (var row in Document.Rows)
            {
                if (!_characters.Contains(Document.Get(row, "characterId"))) throw new InvalidOperationException("角色不存在：" + Document.Get(row, "characterId"));
                if (!LoadImage(row)) throw new InvalidOperationException("立绘图片缺失：" + Document.Get(row, "id"));
            }
            Document.Save(_definition);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open() { NarrativeEditorAvailability.RequireEnabled(); GetWindow<NovelPortraitWindow>().Show(); }
        #endregion
    }
}
