// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Linq;
using Ember.UPMManager.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>项目中心拥有模板生命周期；UPM Manager 仅导航到此入口。</summary>
    public sealed class EmberTemplateSkillsWindow : EditorWindow
    {
        #region 内部参数
        private EmberAISkillInstaller.TemplatePreview _preview;
        private string _message;
        private bool _current;
        private Vector2 _scroll;
        private int _selected;
        private TemplateInfo[] _templates = Array.Empty<TemplateInfo>();
        #endregion

        #region 生命周期
        private void OnEnable() { _templates = EmberProjectSetup.GetTemplates().ToArray(); }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("模板专属 AI Skill", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("源：Assets/Game/Documentation/TemplateSkills\n发现副本：.agents/skills/<id>\n"
                + "消费项目升级框架后，在此独立更新技能，保留剧情、配表、图片、场景和业务代码。"
                + "\n编辑源文件后使用当前模板预览。通用 Skill 仍由 UPM Manager 独立管理。", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (_templates.Length > 0)
                {
                    _selected = EditorGUILayout.Popup("包内模板", Mathf.Clamp(_selected, 0, _templates.Length - 1),
                        _templates.Select(t => t.id + " " + t.version).ToArray());
                    if (GUILayout.Button("查看所选模板技能（只读）"))
                        Preview(false);
                }
                if (GUILayout.Button("预览当前模板技能更新（保留业务内容）")) Preview(true);
                using (new EditorGUI.DisabledScope(!_current || _preview == null || _preview.Errors.Count > 0 || !_preview.HasChanges))
                    if (GUILayout.Button(_preview?.NeedsBackupConfirmation == true ? "备份并更新技能" : "更新技能"))
                    {
                        try
                        {
                            if (_preview.NeedsBackupConfirmation && !EditorUtility.DisplayDialog("保留本地技能修改",
                                    "将旧技能源、目录 .meta、发现副本和技能记录备份到 .utmp/ember-ai-skills，再更新技能。业务部署记录及业务内容保持不变。", "备份并同步", "取消")) return;
                            EmberProjectSetup.SyncCurrentTemplateSkills(_preview, _preview.NeedsBackupConfirmation);
                            _preview = null;
                            _message = "技能已更新，业务内容未改动；备份见 .utmp/ember-ai-skills。请重新加载 AI 会话。";
                        }
                        catch (Exception ex) { _message = ex.Message; }
                    }
            }
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_preview != null)
            {
                EditorGUILayout.LabelField(_preview.TemplateId + " · " + _preview.TemplateVersion, EditorStyles.boldLabel);
                if (_preview.IsIndependentUpdate) EditorGUILayout.LabelField("业务部署 " + _preview.BusinessTemplateVersion + " → 保持；技能源 " + _preview.TemplateVersion);
                if (!_current) EditorGUILayout.HelpBox("只读分发预览。必须先通过正式模板加载/部署流程，才能启用；包内可读不代表已启用。", MessageType.Info);
                foreach (string error in _preview.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                foreach (string difference in _preview.Differences) EditorGUILayout.SelectableLabel(difference,
                    EditorStyles.wordWrappedLabel, GUILayout.Height(36));
                if (_preview.Differences.Count == 0) EditorGUILayout.LabelField("此模板没有专属技能，也没有待移出的受管技能。");
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("这是分发和发现隔离，不是保密权限。AI 客户端可能需要重新加载会话；已经载入会话的内容不能即时撤回。技能执行前仍须检查正式模板身份。", MessageType.Info);
        }
        #endregion

        #region 内部方法
        private void Preview(bool current)
        {
            _preview = null; _message = null; _current = current;
            try
            {
                _preview = current ? EmberProjectSetup.PreviewCurrentTemplateSkills()
                    : EmberProjectSetup.PreviewSelectedTemplateSkills(_templates[_selected].id);
            }
            catch (Exception ex) { _message = ex.Message; }
        }
        #endregion

        #region 外部方法
        public static void Open() => GetWindow<EmberTemplateSkillsWindow>("模板 AI Skill").Show();
        #endregion
    }
}
