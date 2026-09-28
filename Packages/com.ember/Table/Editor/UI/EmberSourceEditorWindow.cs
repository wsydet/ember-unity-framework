using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>Key 列表/详情窗口公共交互，序列化草稿在域重载后继续保留。</summary>
    public abstract class EmberSourceEditorWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeReference] protected EmberTableSourceDocument Document;
        [SerializeField] protected int Selected = -1;
        [SerializeField] private string _search = "", _newKey = "";
        [SerializeField] private float _split = .33f;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        protected string Message;
        private Vector2 _listScroll, _detailScroll;
        private bool _drag;
        private GUIStyle _rowStyle;
        protected virtual string KeyColumn => "key";
        protected EmberTableSourceDocument.Row Current => Document != null && Selected >= 0 && Selected < Document.Rows.Count ? Document.Rows[Selected] : null;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        protected virtual void OnEnable()
        {
            if (Document != null && string.IsNullOrEmpty(Document.Path)) Document = null;
            hasUnsavedChanges = Document != null && Document.Dirty;
            _rowStyle = null;
            minSize = new Vector2(780, 520);
            Undo.undoRedoPerformed += Repaint;
            EditorApplication.projectChanged += ProjectChanged;
            saveChangesMessage = "源表还有未保存修改，是否保存？";
        }
        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            EditorApplication.projectChanged -= ProjectChanged;
        }
        protected virtual void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorGUILayout.HelpBox("请退出运行模式后编辑。", MessageType.Info); return; }
            DrawHeader();
            if (!string.IsNullOrEmpty(Message)) EditorGUILayout.HelpBox(Message, MessageType.Info);
            if (Document == null) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("刷新 Key", EditorStyles.toolbarButton, GUILayout.Width(80))) Run(Reload);
                if (GUILayout.Button("保存并更新预览", EditorStyles.toolbarButton, GUILayout.Width(140))) Run(SaveChanges);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField("新 Key", _newKey);
                if (GUILayout.Button("添加", GUILayout.Width(65))) Run(() =>
                {
                    Undo.RecordObject(this, "添加 Key");
                    var row = Document.Add(_newKey, KeyColumn); InitializeRow(row);
                    Selected = Document.Rows.Count - 1; _search = ""; _newKey = "";
                });
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * _split)))
                {
                    DrawFilters();
                    _rowStyle ??= new GUIStyle(EditorStyles.miniButton)
                    {
                        fixedHeight = 0,
                        fixedWidth = 0,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                        padding = new RectOffset(10, 10, 6, 6),
                        stretchWidth = true
                    };
                    _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                    for (int i = 0; i < Document.Rows.Count; i++)
                    {
                        var row = Document.Rows[i];
                        if (!Matches(row) || (!string.IsNullOrEmpty(_search) && !row.Values.Any(v =>
                            (v ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0))) continue;
                        var previous = GUI.backgroundColor;
                        if (Selected == i) GUI.backgroundColor = new Color(.45f, .75f, 1f);
                        string label = ListLabel(row);
                        int lineCount = 1 + label.Count(c => c == '\n');
                        float rowHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, _rowStyle.lineHeight) * lineCount
                            + _rowStyle.padding.vertical;
                        if (GUILayout.Button(new GUIContent(label, label), _rowStyle,
                                GUILayout.Height(rowHeight), GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
                        { Selected = i; GUI.FocusControl(null); }
                        GUI.backgroundColor = previous;
                    }
                    EditorGUILayout.EndScrollView();
                }
                Rect divider = GUILayoutUtility.GetRect(5, 5, GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(divider, new Color(.2f, .2f, .2f));
                EditorGUIUtility.AddCursorRect(divider, MouseCursor.ResizeHorizontal);
                if (Event.current.type == EventType.MouseDown && divider.Contains(Event.current.mousePosition)) _drag = true;
                if (Event.current.type == EventType.MouseUp) _drag = false;
                if (_drag && Event.current.type == EventType.MouseDrag)
                { _split = Mathf.Clamp(Event.current.mousePosition.x / position.width, .22f, .55f); Event.current.Use(); Repaint(); }
                using (new EditorGUILayout.VerticalScope())
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    if (Current != null)
                    {
                        EditorGUILayout.SelectableLabel(Document.Get(Current, KeyColumn), EditorStyles.boldLabel, GUILayout.Height(20));
                        if (GUILayout.Button("复制 Key", GUILayout.Width(85))) EditorGUIUtility.systemCopyBuffer = Document.Get(Current, KeyColumn);
                        DrawDetail(Current);
                    }
                    else EditorGUILayout.HelpBox("从左侧选择 Key，或添加新 Key。", MessageType.None);
                    EditorGUILayout.EndScrollView();
                }
            }
            hasUnsavedChanges = Document.Dirty;
            EditorGUILayout.LabelField($"{Document.Rows.Count} 个 Key · {(hasUnsavedChanges ? "未保存" : "已保存")} · {Document.Path}", EditorStyles.miniLabel);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ProjectChanged()
        {
            if (Document == null || Document.Dirty) return;
            Run(() => LoadDocument(Document.Path));
        }
        protected bool CanSwitch() => Document == null || !Document.Dirty || EditorUtility.DisplayDialog("保留修改", "当前表有未保存修改。放弃后切换？", "放弃修改", "继续编辑");
        protected void LoadDocument(string path)
        {
            var next = EmberTableSourceDocument.Load(path);
            string key = Current == null ? null : Document.Get(Current, KeyColumn);
            Document = next;
            Selected = Document.Rows.FindIndex(r => Document.Get(r, KeyColumn) == key);
            if (Selected < 0 && Document.Rows.Count > 0) Selected = 0;
            hasUnsavedChanges = false;
        }
        protected void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { Message = ex.Message; }
            Repaint();
        }
        protected void Set(EmberTableSourceDocument.Row row, string column, string value)
        {
            int index = Document.Column(column);
            if (index < 0 || row.Values[index] == value) return;
            Undo.RecordObject(this, "编辑源表"); row.Values[index] = value; hasUnsavedChanges = true;
        }
        protected virtual void Reload() { if (CanSwitch()) LoadDocument(Document.Path); }
        protected virtual bool Matches(EmberTableSourceDocument.Row row) => true;
        protected virtual void DrawFilters() { }
        protected virtual string ListLabel(EmberTableSourceDocument.Row row) => Document.Get(row, KeyColumn);
        protected virtual void InitializeRow(EmberTableSourceDocument.Row row) { }
        protected abstract void DrawHeader();
        protected abstract void DrawDetail(EmberTableSourceDocument.Row row);
        protected abstract void Commit();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void SaveChanges()
        {
            Commit();
            base.SaveChanges();
            Message = "源表已保存，运行数据与预览已更新。";
        }
        public override void DiscardChanges() { Document = null; base.DiscardChanges(); }
        #endregion
    }
}
