using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    internal sealed class EmberExtensionPanel
    {
        #region 内部参数
        private EmberExtensionProject.Configuration _draft;
        private string _path, _error, _preview;
        private Vector2 _scroll;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Draw()
        {
            if (_path != EmberExtensionProject.ConfigurationPath) { _draft = null; _path = EmberExtensionProject.ConfigurationPath; _preview = null; }
            try { _draft ??= EmberExtensionProject.Load(); }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); return; }
            EditorGUILayout.LabelField("项目扩展组合", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("模板提供默认组合；保存后由本项目独立持有。启用自动包含依赖，关闭先预览影响。资产、场景与窗口草稿保留；运行能力不会热卸载。", MessageType.Info);
            EditorGUILayout.LabelField(_path, EditorStyles.wordWrappedMiniLabel);
            if (_draft.schemaVersion != 2 || _draft.ownerTemplateVersion != EmberExtensionProject.CurrentTemplateVersion)
                EditorGUILayout.HelpBox("该组合尚未签收当前模板版本/扩展 hash。审阅后保存将绑定当前源码版本，并同步 Skill 发现副本。", MessageType.Warning);
            if (!string.IsNullOrEmpty(EmberExtensionDiscovery.Error)) EditorGUILayout.HelpBox(EmberExtensionDiscovery.Error, MessageType.Error);
            var result = EmberExtensionCatalog.Resolve(EmberExtensionProject.All, _draft.enabled);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var definition in EmberExtensionProject.All)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool enabled = result.IsEnabled(definition.Id);
                    EditorGUILayout.LabelField(definition.DisplayName + " · " + (enabled ? "启用" : "关闭"), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(definition.Id + " · " + definition.Kind, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("契约版本 " + definition.ContractVersion + "（未发布工作区身份）", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("依赖：" + string.Join(", ", definition.Dependencies) + "\n兼容：" + definition.Compatibility, EditorStyles.wordWrappedMiniLabel);
                    if (result.Problems.TryGetValue(definition.Id, out var problem)) EditorGUILayout.HelpBox(problem, MessageType.Error);
                    if (definition.Kind == EmberExtensionKind.Infrastructure) continue;
                    if (!enabled && GUILayout.Button("启用（包含依赖）")) { _draft.enabled = _draft.enabled.Append(definition.Id).Distinct().ToArray(); _preview = null; }
                    if (enabled && GUILayout.Button("预览关闭影响"))
                    {
                        var affected = EmberExtensionCatalog.PreviewDisable(EmberExtensionProject.All, _draft.enabled, definition.Id);
                        var after = EmberExtensionCatalog.Resolve(EmberExtensionProject.All, _draft.enabled.Except(affected));
                        _preview = "将关闭：" + string.Join(", ", result.Enabled.Except(after.Enabled)) + "。内容资产与草稿保留。";
                        _draft.enabled = _draft.enabled.Except(affected).ToArray();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            foreach (var problem in result.Problems) EditorGUILayout.HelpBox(problem.Key + "：" + problem.Value, MessageType.Error);
            if (!string.IsNullOrEmpty(_preview)) EditorGUILayout.HelpBox(_preview + "\n尚未应用，保存配置后才生效；重新读取可放弃。", MessageType.Warning);
            if (GUILayout.Button("预览 Skill 发现与版本绑定"))
                try
                {
                    var skills = EmberProjectSetup.PreviewExtensionSkills(EmberExtensionProject.CreateConfiguration(_draft.enabled));
                    _preview = skills == null ? "无模板 Skill。" : string.Join("\n", skills.Differences);
                }
                catch (Exception error) { _error = error.Message; }
            EmberExtensionDocuments.DrawIndex();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || result.Problems.Count > 0))
                    if (GUILayout.Button("保存项目组合"))
                        try { EmberExtensionProject.Save(_draft); _error = null; _preview = null; }
                        catch (Exception error) { _error = error.Message; }
                if (GUILayout.Button("重新读取")) { _draft = null; _preview = null; }
            }
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
        }
        #endregion
    }
}
