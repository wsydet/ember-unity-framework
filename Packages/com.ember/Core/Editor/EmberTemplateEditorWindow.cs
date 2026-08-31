// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Ember 模板编辑器 —— 框架开发仓库专用（embedded 安装）：
    /// ① 下拉选择目标模板（不再手输 id，消除误存/误建）；② 「加载模板」到业务层编辑；
    /// ③ 「保存当前业务层」写入选中模板；④ 模板独立版本号（主/次/补丁 +1）与元数据编辑。
    ///
    /// 消费者项目（git 安装）中本窗口只读——包位于 Library/PackageCache，无法持久化模板。
    /// 工作流：编辑 Assets/Game 等 → 保存为模板 X → 提交 → 发版 → 消费者一键部署。
    /// </summary>
    public class EmberTemplateEditorWindow : EditorWindow
    {
        #region 内部参数

        private const string TAG = LogTags.CoreEditor;

        /// <summary>目标模板 id（下拉选择，保存/删除/版本/元数据均作用于它）。</summary>
        private string _selectedTemplateId = "base";

        private string _metaName = "";
        private string _metaDesc = "";
        private string _metaForId = "";

        private string _newTemplateId = "";
        private string _newDisplayName = "";
        private string _newDescription = "";
        private bool _fromBase = true;
        private bool _busy;
        private string _lastResult;

        private static readonly string[] ChannelNames = { "stable（稳定）", "preview（预览/实验）", "deprecated（弃用，消费端隐藏）" };
        private int _channelIndex;

        /// <summary>版本输入框（可编辑回退，与选中模板同步）。</summary>
        private string _versionField = "";

        private static GUIStyle _warningStyle;

        /// <summary>防串模板警告样式（懒加载：避免静态初始化期访问 EditorStyles，域重载未就绪时抛 NRE，见测试问题-3）</summary>
        private static GUIStyle WarningStyle
        {
            get
            {
                if (_warningStyle == null)
                    _warningStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(1f, 0.6f, 0.2f) }
                    };
                return _warningStyle;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/Setup/模板编辑器", false, 102)]
        public static void ShowWindow()
        {
            var win = GetWindow<EmberTemplateEditorWindow>("Ember 模板编辑器");
            win.minSize = new Vector2(520, 520);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember 模板编辑器", EditorStyles.boldLabel);

            bool devMode = EmberProjectSetup.IsEmbeddedPackage();
            if (!devMode)
            {
                EditorGUILayout.HelpBox(
                    "当前为消费者安装（git 包）。模板保存/加载仅在框架开发仓库（embedded 安装）中可用。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "模板创作工作流：\n① 在项目中编辑业务层（Assets/Game 等）→「保存为模板」快照进包内 Templates~/。\n② 「加载模板」把某模板内容替换进项目业务层，编辑后再保存。\n保存时自动剥离 dev 测试对象（第三方/开发工具，不进模板）。",
                MessageType.Info);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("现有模板", EditorStyles.boldLabel);
            var templates = EmberProjectSetup.GetTemplates();
            foreach (var t in templates)
                DrawTemplateRow(t);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("目标模板（保存/版本/元数据/删除的作用对象）", EditorStyles.boldLabel);
            DrawTargetTemplateSection(templates);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("新建模板", EditorStyles.boldLabel);
            _newTemplateId = EditorGUILayout.TextField("模板 id（英文，如 base / platformer2d）", _newTemplateId);
            _newDisplayName = EditorGUILayout.TextField("显示名称", _newDisplayName);
            _newDescription = EditorGUILayout.TextField("描述", _newDescription);
            _fromBase = EditorGUILayout.Toggle("从基础模板开始（复制 base 内容作为起点）", _fromBase);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_busy && !string.IsNullOrWhiteSpace(_newTemplateId) && IsValidTemplateId(_newTemplateId);
            if (GUILayout.Button("创建新模板并开始编辑", GUILayout.Width(200)))
                CreateNewTemplate();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_busy)
                EditorGUILayout.LabelField("⏳ 正在执行...", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);
        }

        private void DrawTemplateRow(TemplateInfo template)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📦 {template.displayName}（{template.id}）", EditorStyles.boldLabel, GUILayout.Width(240));
            EditorGUILayout.LabelField("v" + template.version, EditorStyles.miniLabel, GUILayout.Width(80));

            GUI.enabled = !_busy;
            if (GUILayout.Button("加载到项目编辑", GUILayout.Width(140)))
                LoadTemplateIntoProject(template);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(template.description))
                EditorGUILayout.LabelField("    " + template.description, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        /// <summary>目标模板区：下拉选择 + 版本 bump + 元数据编辑 + 保存/删除。</summary>
        private void DrawTargetTemplateSection(List<TemplateInfo> templates)
        {
            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox("包内没有模板，请先在下方新建。", MessageType.Warning);
                return;
            }

            string[] options = templates.Select(t => $"{t.displayName}（{t.id}）v{t.version}").ToArray();
            int index = templates.FindIndex(t => t.id == _selectedTemplateId);
            if (index < 0) index = 0;
            index = EditorGUILayout.Popup("选择模板", index, options);

            _selectedTemplateId = templates[index].id;
            var selected = templates[index];

            // 切换选中模板时同步元数据编辑框与频道下拉
            if (_metaForId != selected.id)
            {
                _metaForId = selected.id;
                _metaName = selected.displayName;
                _metaDesc = selected.description;
                _channelIndex = System.Array.IndexOf(new[] { "stable", "preview", "deprecated" }, selected.channel ?? "stable");
                if (_channelIndex < 0) _channelIndex = 0;
                _versionField = selected.version;
            }

            var editing = EmberProjectSetup.GetEditingTemplate();
            if (editing != null && !string.IsNullOrEmpty(editing.templateId))
            {
                var editingTpl = templates.Find(t => t.id == editing.templateId);
                bool same = editing.templateId == selected.id;
                EditorGUILayout.LabelField(
                    (same ? "📝 当前正在编辑：" : "⚠ 当前正在编辑：") +
                    (editingTpl != null ? $"{editingTpl.displayName}（{editingTpl.id}）" : editing.templateId) +
                    $"（加载于 {editing.loadedAt}）" +
                    (same ? "，与目标模板一致" : "——与上方目标模板不一致！保存会写入目标模板"),
                    same ? EditorStyles.miniLabel : WarningStyle);
            }
            else
            {
                EditorGUILayout.LabelField("📝 尚未加载任何模板到业务层", EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模板版本（独立于框架版本）", GUILayout.Width(180));
            _versionField = EditorGUILayout.TextField(_versionField, GUILayout.Width(70));
            GUI.enabled = !_busy && IsValidVersion(_versionField);
            if (GUILayout.Button("应用版本", GUILayout.Width(80)))
                ApplyVersion(selected);
            GUI.enabled = !_busy;
            if (GUILayout.Button("主+1", GUILayout.Width(45))) BumpVersion(selected, 0);
            if (GUILayout.Button("次+1", GUILayout.Width(45))) BumpVersion(selected, 1);
            if (GUILayout.Button("补丁+1", GUILayout.Width(55))) BumpVersion(selected, 2);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            string fw = EmberProjectSetup.GetFrameworkVersion();
            bool fwOk = EmberProjectSetup.IsFrameworkCompatible(selected.frameworkVersion);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("框架版本", GUILayout.Width(60));
            EditorGUILayout.LabelField(
                $"v{selected.frameworkVersion}" + (fwOk ? $"（当前框架 v{fw} ✅）" : $"（当前框架 v{fw} ⚠ 不兼容，消费端将隐藏）"),
                EditorStyles.miniLabel);
            GUI.enabled = !_busy;
            if (GUILayout.Button("声明为当前框架版本", GUILayout.Width(160)))
                DeclareFrameworkVersion(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("频道", GUILayout.Width(60));
            _channelIndex = EditorGUILayout.Popup(_channelIndex, ChannelNames);
            GUI.enabled = !_busy;
            if (GUILayout.Button("应用频道", GUILayout.Width(100)))
                ApplyChannel(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            _metaName = EditorGUILayout.TextField("显示名称", _metaName);
            _metaDesc = EditorGUILayout.TextField("描述", _metaDesc);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (GUILayout.Button("应用元数据修改（不动版本/内容）", GUILayout.Width(240)))
                ApplyMetadata(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (GUILayout.Button($"保存当前业务层为「{selected.displayName}」", GUILayout.Width(240)))
                SaveProjectAsTemplate(selected);
            if (GUILayout.Button($"删除模板「{selected.displayName}」", GUILayout.Width(200)))
                DeleteSelectedTemplate(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private static bool IsValidTemplateId(string id)
        {
            return id.Length <= 40 && !id.Contains(" ") && !id.Contains("/") && !id.Contains("\\") && !id.Contains("~");
        }

        private void CreateNewTemplate()
        {
            if (!EditorUtility.DisplayDialog("创建新模板",
                $"将创建模板 [{_newTemplateId}]" + (_fromBase ? "（复制基础模板内容作为起点）" : "（空模板，从头开始）") + "，\n" +
                "并把其内容加载到项目业务层（替换 Assets/Game、Assets/Resources、Assets/Ember/Editor、Assets/Settings）。\n\n" +
                "⚠ 当前业务层未提交的改动会丢失！\n\n继续？",
                "创建并开始编辑", "取消"))
                return;

            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.CreateTemplate(_newTemplateId, _newDisplayName, _newDescription, _fromBase);
                int n = EmberProjectSetup.LoadTemplate(_newTemplateId);
                _selectedTemplateId = _newTemplateId;
                _metaForId = "";
                _lastResult = $"✅ 模板 [{_newTemplateId}] 已创建（初始版本 v0.1.0，框架版本已记录 v{EmberProjectSetup.GetFrameworkVersion()}）并加载到业务层（{(_fromBase ? "基于基础模板" : "空模板")}，{n} 文件）。\n编辑完成后在「目标模板」区点「保存当前业务层」写回。";
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 创建失败：" + ex.Message;
                EmberDebug.LogError(TAG, "创建模板失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void SaveProjectAsTemplate(TemplateInfo selected)
        {
            string mismatch = "";
            var editing = EmberProjectSetup.GetEditingTemplate();
            if (editing != null && editing.templateId != selected.id)
                mismatch = "\n⚠ 当前业务层内容来自模板 " + editing.templateId + "，保存将写入 [" + selected.id + "]！\n";

            if (!EditorUtility.DisplayDialog("保存为模板",
                $"将把当前项目业务层保存为模板 [{selected.displayName}]（{selected.id}）：\n" +
                "Assets/Game、Assets/Resources、Assets/Ember/Editor、Assets/Settings\n\n" +
                mismatch +
                "模板旧内容将被覆盖（模板版本保持 v" + selected.version + " 不变；框架版本将重新对准当前框架 v" +
                EmberProjectSetup.GetFrameworkVersion() + "）。\n继续？",
                "保存", "取消"))
                return;

            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                int n = EmberProjectSetup.SaveTemplate(selected.id, _metaName, _metaDesc);
                _lastResult = $"✅ 已保存为模板 [{selected.displayName}]（{n} 文件，已自动剥离 dev 测试对象，版本保持 v{selected.version}）。\n提交并打 tag 后消费者即可通过初始化窗口部署。";
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 保存失败：" + ex.Message;
                EmberDebug.LogError(TAG, "保存模板失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void LoadTemplateIntoProject(TemplateInfo template)
        {
            if (!EditorUtility.DisplayDialog("加载模板",
                $"将用模板 [{template.displayName}] 的内容替换当前项目业务层：\n" +
                "Assets/Game、Assets/Resources、Assets/Ember/Editor、Assets/Settings\n\n" +
                "⚠ 这些目录中未提交的改动会丢失！\n\n继续？",
                "加载并替换", "取消"))
                return;

            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                int n = EmberProjectSetup.LoadTemplate(template.id);
                _lastResult = $"✅ 模板 [{template.displayName}] 已加载（{n} 文件）。编辑完成后用「保存当前业务层」写回。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 加载失败：" + ex.Message;
                EmberDebug.LogError(TAG, "加载模板失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void BumpVersion(TemplateInfo selected, int field)
        {
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.BumpTemplateVersion(selected.id, field);
                var fresh = EmberProjectSetup.GetTemplates().Find(t => t.id == selected.id);
                _versionField = fresh != null ? fresh.version : _versionField;
                _lastResult = $"✅ 模板 [{selected.displayName}] 版本已更新为 v{(fresh != null ? fresh.version : "?")}。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 版本更新失败：" + ex.Message;
                EmberDebug.LogError(TAG, "模板版本更新失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private static bool IsValidVersion(string version)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(version ?? "", @"^\d+\.\d+\.\d+$");
        }

        private void ApplyVersion(TemplateInfo selected)
        {
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.SetTemplateVersion(selected.id, _versionField);
                var fresh = EmberProjectSetup.GetTemplates().Find(t => t.id == selected.id);
                _versionField = fresh != null ? fresh.version : _versionField;
                _lastResult = $"✅ 模板 [{selected.displayName}] 版本已设为 v{_versionField}（可用于误操作回退）。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 版本设置失败：" + ex.Message;
                EmberDebug.LogError(TAG, "设置模板版本失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void DeclareFrameworkVersion(TemplateInfo selected)
        {
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.DeclareFrameworkVersion(selected.id);
                _lastResult = $"✅ 模板 [{selected.displayName}] 已声明兼容当前框架 v{EmberProjectSetup.GetFrameworkVersion()}（模板自身版本不变）。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 声明失败：" + ex.Message;
                EmberDebug.LogError(TAG, "声明框架版本失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void ApplyChannel(TemplateInfo selected)
        {
            string[] channels = { "stable", "preview", "deprecated" };
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.SetTemplateChannel(selected.id, channels[_channelIndex]);
                _lastResult = $"✅ 模板 [{selected.displayName}] 频道已设为 {channels[_channelIndex]}。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 频道设置失败：" + ex.Message;
                EmberDebug.LogError(TAG, "设置模板频道失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void ApplyMetadata(TemplateInfo selected)
        {
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.UpdateTemplateMetadata(selected.id, _metaName, _metaDesc);
                _metaForId = ""; // 下次 OnGUI 按新元数据重新同步编辑框
                _lastResult = $"✅ 模板 [{selected.id}] 元数据已更新（版本/内容不变）。";
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 元数据更新失败：" + ex.Message;
                EmberDebug.LogError(TAG, "模板元数据更新失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private void DeleteSelectedTemplate(TemplateInfo selected)
        {
            if (!EditorUtility.DisplayDialog("删除模板",
                $"将删除模板 [{selected.displayName}]（{selected.id}），包括其全部内容。\n\n" +
                "⚠ 此操作不可恢复！\n\n继续？",
                "删除", "取消"))
                return;

            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                EmberProjectSetup.DeleteTemplate(selected.id);
                _selectedTemplateId = "";
                _metaForId = "";
                _lastResult = $"✅ 模板 [{selected.displayName}] 已删除。";
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 删除失败：" + ex.Message;
                EmberDebug.LogError(TAG, "删除模板失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        #endregion
    }
}
