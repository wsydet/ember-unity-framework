using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    public sealed partial class NarrativeGraphWindow
    {
        #region 内部方法
        private void CreateStory()
        {
            if (!NarrativeGraphModel.IsTemplateActive(true) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string selected = EditorUtility.SaveFilePanelInProject("新剧情", "NewStory", "asset", "选择小说文件名；之后可自由重命名", "Assets/GameResource/Resources/Config/Narrative");
            if (string.IsNullOrEmpty(selected)) return;
            string path = System.IO.Path.ChangeExtension(selected, null) + "/" + System.IO.Path.GetFileName(selected);
            TryEdit(() => ShowStory(NarrativeStoryModel.CreateStory(path)));
        }
        private void ValidateStory()
        {
            if (!_story) return;
            LoadTables(); _errors = NarrativeAssetValidation.Validate(_story, null, _catalog);
            _hints = NarrativeAssetValidation.ValidateHints(_story, null);
            _message = _errors.Count == 0 ? (_hints.Count == 0 ? "剧情全部章节与跨章路线校验通过。"
                : "剧情校验通过，另有 " + _hints.Count + " 条编写提示。") : null; _inspector?.MarkDirtyRepaint();
        }
        private void DrawStoryInspector()
        {
            EditorGUILayout.LabelField(_story ? _story.DisplayName + " · 章节总览" : "章节总览", EditorStyles.boldLabel);
            if (!_story) { EditorGUILayout.HelpBox("选择或新建剧情，添加章节后双击章节卡进入章内流程。", MessageType.Info); return; }
            if (_storyContent == null || _storyContent.targetObject != _story)
            { _storyContent?.Dispose(); _storyContent = new SerializedObject(_story); }
            _storyContent.UpdateIfRequiredOrScript();
            using (new EditorGUI.DisabledScope(!NarrativeGraphModel.CanEdit(_story)))
            {
                if (!_selectedChapter)
                {
                    EditorGUILayout.PropertyField(_storyContent.FindProperty("_displayName"), new GUIContent("剧情名称"));
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(_storyContent.FindProperty("_storyId"), new GUIContent("剧情 ID"));
                    EditorGUILayout.PropertyField(_storyContent.FindProperty("_revision"), new GUIContent("剧情修订"));
                    EditorGUILayout.PropertyField(_storyContent.FindProperty("_entry"), new GUIContent("入口章节"));
                    EditorGUILayout.PropertyField(_storyContent.FindProperty("_globals"), new GUIContent("会话全局变量"), true);
                    EditorGUILayout.HelpBox("全局变量跨章保留。章节卡之间的条件仅使用全局变量；按顺序首个命中，否则走兜底。", MessageType.None);
                    EditorGUILayout.PropertyField(_storyContent.FindProperty("_customSteps"), new GUIContent("自定义节点脚本清单"), true);
                    EditorGUILayout.HelpBox("段内「自定义节点」步骤只保存脚本稳定 ID，实际脚本资产必须在这里登记："
                        + "它们随剧情一起加载，参与剧情校验与存档指纹。取消登记后，引用它的步骤会报「脚本未在本剧情登记」。", MessageType.None);
                    _existingChapter = (NarrativeChapterSO)EditorGUILayout.ObjectField("已有章节", _existingChapter, typeof(NarrativeChapterSO), false);
                    if (_existingChapter && GUILayout.Button("登记已有章节")) TryEdit(() => NarrativeStoryModel.AddChapter(_story, _existingChapter));
                }
                else
                {
                    EditorGUILayout.LabelField(_selectedChapter.DisplayName, EditorStyles.boldLabel);
                    if (GUILayout.Button("进入章节 →")) ShowChapter(_selectedChapter);
                    if (_overviewContent == null || _overviewContent.targetObject != _selectedChapter)
                    { _overviewContent?.Dispose(); _overviewContent = new SerializedObject(_selectedChapter); }
                    NarrativeContentGUI.Draw(_overviewContent, null, _catalog);
                    if (GUILayout.Button("设为剧情入口")) _storyContent.FindProperty("_entry").objectReferenceValue = _selectedChapter;
                    if (GUILayout.Button("从剧情移除章节…") && EditorUtility.DisplayDialog("移除章节", "将移除章节登记，断开它的全部跨章入链和出口配置，保留章节与节点资产。可撤销。", "移除并断开", "取消"))
                    {
                        var removing = _selectedChapter;
                        _storyContent.ApplyModifiedProperties();
                        TryEdit(() => NarrativeStoryModel.RemoveChapter(_story, removing));
                        _selectedChapter = null; _storyContent.Update();
                    }
                    if (_selectedChapter) DrawChapterExits();
                }
                _storyContent.ApplyModifiedProperties();
            }
            foreach (var error in _errors)
            {
                EditorGUILayout.HelpBox(error.ToString(), MessageType.Error);
                if (GUILayout.Button("定位问题章节"))
                {
                    LocateError(error);
                }
            }
            foreach (var hint in _hints)
            {
                EditorGUILayout.HelpBox("编写提示（不阻断运行）\n" + hint, MessageType.Warning);
                if (GUILayout.Button("定位此提示")) LocateError(hint);
            }
        }
        private void DrawChapterExits()
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("章节出口与条件路线", EditorStyles.boldLabel);
            var links = _storyContent.FindProperty("_exits"); Action pending = null; bool found = false;
            for (int i = 0; i < links.arraySize; i++)
            {
                var link = links.GetArrayElementAtIndex(i);
                if (link.FindPropertyRelative("_source").objectReferenceValue != _selectedChapter) continue;
                found = true; int index = i;
                var exit = link.FindPropertyRelative("_exit").objectReferenceValue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(exit ? exit.name : "失效出口", EditorStyles.boldLabel);
                    var routes = link.FindPropertyRelative("_routes");
                    for (int j = 0; j < routes.arraySize; j++)
                    {
                        var route = routes.GetArrayElementAtIndex(j); int ri = j;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("优先级 " + (j + 1));
                            using (new EditorGUI.DisabledScope(j == 0)) if (GUILayout.Button("↑", GUILayout.Width(24))) pending = () => routes.MoveArrayElement(ri, ri - 1);
                            using (new EditorGUI.DisabledScope(j == routes.arraySize - 1)) if (GUILayout.Button("↓", GUILayout.Width(24))) pending = () => routes.MoveArrayElement(ri, ri + 1);
                            if (GUILayout.Button("−", GUILayout.Width(24))) pending = () => routes.DeleteArrayElementAtIndex(ri);
                        }
                        EditorGUILayout.PropertyField(route.FindPropertyRelative("_text"), new GUIContent("路线说明"));
                        DrawGlobalCondition(route.FindPropertyRelative("_condition"));
                        EditorGUILayout.PropertyField(route.FindPropertyRelative("_target"), new GUIContent("下一章"));
                    }
                    if (GUILayout.Button("+ 条件路线")) pending = () => { _storyContent.ApplyModifiedProperties(); NarrativeStoryModel.AddRoute(_story, index); _storyContent.Update(); };
                    EditorGUILayout.PropertyField(link.FindPropertyRelative("_fallback"), new GUIContent("兜底章节（必填）"));
                    if (GUILayout.Button("移除此出口配置")) pending = () => links.DeleteArrayElementAtIndex(index);
                }
            }
            if (!found) EditorGUILayout.HelpBox("本章没有已配置的出口。先进入章节添加“章节出口”节点，再在这里连接下一章。结局节点会直接结束整部剧情。", MessageType.Info);
            pending?.Invoke();
        }
        private void DrawGlobalCondition(SerializedProperty condition)
        {
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("_junction"), new GUIContent("满足方式"));
            var predicates = condition.FindPropertyRelative("_predicates"); int remove = -1;
            for (int i = 0; i < predicates.arraySize; i++)
            {
                var item = predicates.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(item.FindPropertyRelative("_variableId"), new GUIContent("全局变量 ID"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("_comparison"), new GUIContent("比较"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("_value"), new GUIContent("比较值"), true);
                if (item.FindPropertyRelative("_scope").enumValueIndex != (int)NovelVariableScope.Global && GUILayout.Button("修正此条件为全局作用域"))
                    item.FindPropertyRelative("_scope").enumValueIndex = (int)NovelVariableScope.Global;
                if (GUILayout.Button("删除条件 " + (i + 1))) remove = i;
            }
            if (remove >= 0) predicates.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("+ 全局变量条件"))
            {
                int i = predicates.arraySize++; var item = predicates.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("_scope").enumValueIndex = (int)NovelVariableScope.Global;
                item.FindPropertyRelative("_variableId").stringValue = "";
                item.FindPropertyRelative("_comparison").enumValueIndex = 0;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void ShowStory(NarrativeStorySO story)
        {
            _story = story; _overview = true; _selectedChapter = null; _errors = Array.Empty<NarrativeError>();
            _hints = Array.Empty<NarrativeError>(); _message = null;
            RefreshGraph(); _storyGraph?.schedule.Execute(() => _storyGraph.FrameAll());
        }
        #endregion
    }
}
