// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>Ember 项目中心：初始化、项目校验及 embedded 开发仓库的模板开发。</summary>
    public class EmberSetupWindow : EditorWindow
    {
        #region 编辑器面板参数

        [SerializeField] private int _selectedTab;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private static readonly string[] DevTabs = { "项目初始化", "模板开发", "项目校验" };
        private static readonly string[] ConsumerTabs = { "项目初始化", "项目校验" };

        private EmberSetupWindowContext _context;
        private EmberProjectSetupPanel _projectSetupPanel;
        private EmberTemplateDevelopmentPanel _templateDevelopmentPanel;
        private EmberProjectValidationPanel _projectValidationPanel;
        private Vector2 _scroll;
        private OdinMenuTree _navigation;
        private bool _navigationDevMode;
        private int? _pendingTab;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/项目中心", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<EmberSetupWindow>("Ember 项目中心");
            window.minSize = new Vector2(1000, 560);
            window._selectedTab = 0;
            window._navigation = null;
            window._pendingTab = null;
            window.Show();
        }

        /// <summary>供旧模板编辑器兼容入口跳转到统一窗口。</summary>
        public static void ShowTemplateDevelopment()
        {
            var window = GetWindow<EmberSetupWindow>("Ember 项目中心");
            window.minSize = new Vector2(1000, 560);
            window._selectedTab = EmberProjectSetup.IsEmbeddedPackage() ? 1 : 0;
            window._navigation = null;
            window._pendingTab = null;
            window.Show();
        }

        /// <summary>面板操作及兼容 API 共用的项目校验入口，无独立校验菜单。</summary>
        public static void ShowProjectValidation(bool runScan = false)
        {
            var window = GetWindow<EmberSetupWindow>("Ember 项目中心");
            window.minSize = new Vector2(1000, 560);
            window.EnsurePanels();
            window._selectedTab = 2;
            window._navigation = null;
            window._pendingTab = null;
            if (runScan) window._projectValidationPanel.RequestScan();
            window.Show();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnEnable()
        {
            EnsurePanels();
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            _templateDevelopmentPanel?.Dispose();
            _templateDevelopmentPanel = null;
        }

        private void OnProjectChanged()
        {
            _context?.Invalidate();
        }

        private void OnFocus()
        {
            _context?.Invalidate();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            OnProjectChanged();
        }

        private void OnInspectorUpdate()
        {
            if (_context != null && _context.OperationsBlocked)
                Repaint();
        }

        private void OnGUI()
        {
            EnsurePanels();
            bool devMode = EmberProjectSetup.IsEmbeddedPackage();
            if (Event.current.type == EventType.Layout && _pendingTab.HasValue)
            {
                _selectedTab = _pendingTab.Value;
                _pendingTab = null;
            }
            if (!devMode && _selectedTab == 1) _selectedTab = 0;

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember 项目中心", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(devMode ? "框架开发仓库 · 可保存与封存模板" : "项目工作区 · 初始化与校验", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(175)))
                {
                    if (_navigation == null || _navigationDevMode != devMode)
                    {
                        _navigationDevMode = devMode;
                        _navigation = new OdinMenuTree(false);
                        _navigation.Add("项目初始化", 0);
                        if (devMode) _navigation.Add("模板开发", 1);
                        _navigation.Add("项目校验", 2);
                        _navigation.MenuItems.FirstOrDefault(i => i.Value is int value && value == _selectedTab)?.Select();
                        var navigation = _navigation;
                        navigation.Selection.SelectionChanged += change =>
                        {
                            if (!ReferenceEquals(_navigation, navigation)) return;
                            if (navigation.Selection.SelectedValue is int selected)
                            {
                                _pendingTab = selected;
                                Repaint();
                            }
                        };
                    }
                    _navigation.DrawMenuTree();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("模板 AI Skill", GUILayout.Height(28))) EmberTemplateSkillsWindow.Open();
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    SirenixEditorGUI.Title(DevTabs[_selectedTab], _selectedTab == 1 ? "当前 Assets → 保存模板 → 显式 Bump 版本" : "", TextAlignment.Left, true);

                    if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    {
                        EditorGUILayout.HelpBox(
                            EditorApplication.isCompiling
                                ? "Unity 正在编译，写操作暂时禁用。"
                                : "Unity 正在更新资源，写操作暂时禁用。",
                            MessageType.Info);
                    }

                    GUILayout.Space(6);
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorGUILayout.HelpBox("运行期间可以查看信息；退出播放模式后可保存、同步和校验。", MessageType.Info);
                    if (_selectedTab == 1 && devMode)
                        _templateDevelopmentPanel.Draw(position.width - 180);
                    else if (_selectedTab == 2)
                        _projectValidationPanel.Draw();
                    else
                    {
                        _scroll = EditorGUILayout.BeginScrollView(_scroll);
                        _projectSetupPanel.Draw();
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
        }

        private void EnsurePanels()
        {
            if (_context == null)
                _context = new EmberSetupWindowContext(Repaint);
            _projectSetupPanel ??= new EmberProjectSetupPanel(_context);
            _templateDevelopmentPanel ??= new EmberTemplateDevelopmentPanel(_context);
            _projectValidationPanel ??= new EmberProjectValidationPanel(_context);
        }

        #endregion
    }

    /// <summary>统一窗口的公共忙碌状态与重绘入口。</summary>
    internal sealed class EmberSetupWindowContext
    {
        #region 内部参数

        private readonly Action _repaint;

        internal bool IsBusy { get; set; }
        internal int Revision { get; private set; }
        internal bool OperationsBlocked => IsBusy
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberSetupWindowContext(Action repaint)
        {
            _repaint = repaint;
        }

        internal void Repaint()
        {
            _repaint?.Invoke();
        }

        internal void Invalidate()
        {
            Revision++;
            Repaint();
        }

        #endregion
    }
}
