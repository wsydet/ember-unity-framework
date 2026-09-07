// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>UI 开发中心中的模块目录模板管理面板。</summary>
    internal sealed class EUIModuleTemplatePanel
    {
        private Vector2 _scroll;
        private EUIModuleTemplateSnapshot _snapshot;
        private EUIModuleTemplateSyncPlan _syncPlan;
        private EUIModuleTemplateRenamePlan _renamePlan;
        private EUIModuleTemplateDeletePlan _deletePlan;

        private string _selectedParentRelativePath = string.Empty;
        private string _newFolderName = string.Empty;
        private string _renameSourcePath;
        private string _renameName = string.Empty;
        private string _deleteConfirmation = string.Empty;

        public void Refresh()
        {
            _snapshot = EUIModuleTemplateService.Scan();
            _syncPlan = null;
            _renamePlan = null;
            _deletePlan = null;
            _deleteConfirmation = string.Empty;
            if (_snapshot?.TemplateRelativeDirectories.Contains(
                    _selectedParentRelativePath, StringComparer.OrdinalIgnoreCase) != true)
                _selectedParentRelativePath = string.Empty;
        }

        public void Draw(ref string lastResult)
        {
            _snapshot ??= EUIModuleTemplateService.Scan();
            DrawHeader(ref lastResult);
            if (_snapshot?.IsValid != true)
            {
                EditorGUILayout.HelpBox(_snapshot?.Error ?? "模块目录模板尚未扫描。",
                    MessageType.Error);
                return;
            }

            foreach (var warning in _snapshot.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling
                       || EditorApplication.isUpdating
                       || EUICreationCompilationContinuation.IsPending))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                DrawCreateSection(ref lastResult);
                DrawTemplateTree();
                DrawRenameSection(ref lastResult);
                DrawDeleteSection(ref lastResult);
                DrawSyncSection(ref lastResult);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawHeader(ref string lastResult)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模块目录模板", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(80f)))
            {
                Refresh();
                lastResult = _snapshot?.IsValid == true
                    ? $"模板扫描完成：{_snapshot.TemplateRelativeDirectories.Count} 个目录，"
                      + $"{_snapshot.ModuleDirectories.Count} 个业务模块。"
                    : _snapshot?.Error;
            }
            using (new EditorGUI.DisabledScope(_snapshot?.IsValid != true))
            {
                if (GUILayout.Button("定位模板", GUILayout.Width(90f)))
                    PingFolder(_snapshot.TemplateRoot);
            }
            EditorGUILayout.EndHorizontal();

            if (_snapshot?.IsValid == true)
            {
                EditorGUILayout.LabelField(_snapshot.TemplateRoot, EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(
                    "新增目录可通过下方批量同步补齐；重命名会先预览再同步现有模块；"
                    + "删除只把模板空目录移入系统回收站，永不删除既有模块目录。",
                    MessageType.Info);
            }
        }

        private void DrawCreateSection(ref string lastResult)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("新增模板目录", EditorStyles.boldLabel);

            var relativePaths = _snapshot.TemplateRelativeDirectories.ToArray();
            var labels = new[] { "模板根目录" }.Concat(relativePaths).ToArray();
            var selectedIndex = string.IsNullOrEmpty(_selectedParentRelativePath)
                ? 0
                : Array.FindIndex(relativePaths, path => string.Equals(path,
                    _selectedParentRelativePath, StringComparison.OrdinalIgnoreCase)) + 1;
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup("父目录", selectedIndex, labels);
            _selectedParentRelativePath = selectedIndex == 0
                ? string.Empty
                : relativePaths[selectedIndex - 1];

            EditorGUILayout.BeginHorizontal();
            _newFolderName = EditorGUILayout.TextField("文件夹名称", _newFolderName);
            if (GUILayout.Button("添加到模板", GUILayout.Width(110f)))
            {
                var result = EUIModuleTemplateService.CreateTemplateFolder(
                    _snapshot, _selectedParentRelativePath, _newFolderName);
                lastResult = result.Message;
                if (result.Success)
                {
                    _newFolderName = string.Empty;
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    Refresh();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("添加只修改模板；需要时在页面底部预览并执行批量同步。",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateTree()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("模板目录结构", EditorStyles.boldLabel);
            if (_snapshot.TemplateRelativeDirectories.Count == 0)
            {
                EditorGUILayout.HelpBox("模板中没有子目录。", MessageType.Warning);
                return;
            }

            foreach (var relativePath in _snapshot.TemplateRelativeDirectories)
            {
                var fullPath = $"{_snapshot.TemplateRoot}/{relativePath}";
                var protectedPath = string.Equals(relativePath,
                    EUIModuleTemplateService.RequiredPrefabDirectoryName,
                    StringComparison.OrdinalIgnoreCase);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Space(relativePath.Count(character => character == '/') * 18f);
                EditorGUILayout.LabelField(protectedPath ? $"🔒 {relativePath}" : relativePath,
                    GUILayout.MinWidth(220f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("定位", GUILayout.Width(55f))) PingFolder(fullPath);
                using (new EditorGUI.DisabledScope(protectedPath))
                {
                    if (GUILayout.Button("重命名", GUILayout.Width(65f)))
                    {
                        _renameSourcePath = fullPath;
                        _renameName = Path.GetFileName(fullPath);
                        _renamePlan = null;
                        _deletePlan = null;
                    }
                    if (GUILayout.Button("删除…", GUILayout.Width(65f)))
                    {
                        _deletePlan = EUIModuleTemplateService.BuildDeletePlan(
                            _snapshot, fullPath);
                        _deleteConfirmation = string.Empty;
                        _renameSourcePath = null;
                        _renamePlan = null;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRenameSection(ref string lastResult)
        {
            if (string.IsNullOrEmpty(_renameSourcePath)) return;
            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("重命名并同步", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_renameSourcePath, EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _renameName = EditorGUILayout.TextField("新名称", _renameName);
            if (GUILayout.Button("预览", GUILayout.Width(75f)))
                _renamePlan = EUIModuleTemplateService.BuildRenamePlan(
                    _snapshot, _renameSourcePath, _renameName);
            if (GUILayout.Button("取消", GUILayout.Width(75f)))
            {
                _renameSourcePath = null;
                _renamePlan = null;
            }
            EditorGUILayout.EndHorizontal();

            if (_renamePlan != null)
            {
                DrawPlanSummary(_renamePlan.BuildSummary(),
                    _renamePlan.CanExecute ? MessageType.Info : MessageType.Error);
                using (new EditorGUI.DisabledScope(!_renamePlan.CanExecute))
                {
                    if (GUILayout.Button("执行模板与模块重命名"))
                    {
                        if (EditorUtility.DisplayDialog("重命名并同步模块目录",
                                _renamePlan.BuildSummary() + "\n\n继续执行？",
                                "重命名", "取消"))
                        {
                            var result = EUIModuleTemplateService.ExecuteRename(_renamePlan);
                            lastResult = result.Message;
                            if (result.Success)
                            {
                                _renameSourcePath = null;
                                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                                Refresh();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDeleteSection(ref string lastResult)
        {
            if (_deletePlan == null) return;
            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("危险操作：删除模板目录", EditorStyles.boldLabel);
            DrawPlanSummary(_deletePlan.BuildSummary(),
                _deletePlan.CanExecute ? MessageType.Warning : MessageType.Error);
            EditorGUILayout.HelpBox(
                "这里只会把模板空目录移入系统回收站；所有现有模块中的对应目录都会保留。",
                MessageType.Warning);

            _deleteConfirmation = EditorGUILayout.TextField(
                $"输入“{_deletePlan.ConfirmationName}”确认", _deleteConfirmation);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("取消"))
            {
                _deletePlan = null;
                _deleteConfirmation = string.Empty;
            }
            var confirmed = _deletePlan.CanExecute
                            && string.Equals(_deleteConfirmation,
                                _deletePlan.ConfirmationName, StringComparison.Ordinal);
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.3f, 0.25f);
            using (new EditorGUI.DisabledScope(!confirmed))
            {
                if (GUILayout.Button("移入系统回收站"))
                {
                    if (EditorUtility.DisplayDialog("最后确认",
                            _deletePlan.BuildSummary()
                            + "\n\n此操作不会删除任何已有模块目录。",
                            "移入回收站", "取消"))
                    {
                        var result = EUIModuleTemplateService.ExecuteDelete(_deletePlan);
                        lastResult = result.Message;
                        if (result.Success)
                        {
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            Refresh();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSyncSection(ref string lastResult)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("批量同步现有模块", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"同步范围：{_snapshot.ModuleDirectories.Count} 个模块；只创建缺失目录，不删除或覆盖资源。",
                EditorStyles.miniLabel);
            if (GUILayout.Button("预览一键同步"))
                _syncPlan = EUIModuleTemplateService.BuildSyncPlan(_snapshot);

            if (_syncPlan != null)
            {
                DrawPlanSummary(_syncPlan.BuildSummary(),
                    _syncPlan.CanExecute ? MessageType.Info : MessageType.Error);
                using (new EditorGUI.DisabledScope(!_syncPlan.CanExecute
                                                   || _syncPlan.DirectoriesToCreate.Count == 0))
                {
                    if (GUILayout.Button(
                            $"创建 {_syncPlan.DirectoriesToCreate.Count} 个缺失目录"))
                    {
                        if (EditorUtility.DisplayDialog("批量同步模块目录",
                                _syncPlan.BuildSummary() + "\n\n只会创建目录，继续？",
                                "同步", "取消"))
                        {
                            var result = EUIModuleTemplateService.ExecuteSync(_syncPlan);
                            lastResult = result.Message;
                            if (result.Success)
                            {
                                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                                Refresh();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawPlanSummary(string summary, MessageType messageType)
        {
            EditorGUILayout.HelpBox(string.IsNullOrEmpty(summary) ? "—" : summary, messageType);
        }

        private static void PingFolder(string assetPath)
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath);
            if (!folder) return;
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
}
