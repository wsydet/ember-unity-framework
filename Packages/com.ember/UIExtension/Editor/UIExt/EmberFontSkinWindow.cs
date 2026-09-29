using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>字体皮肤工作台：导航、单皮肤编辑、字体枚举及旧 UI 接入。</summary>
    public sealed class EmberFontSkinWindow : OdinMenuEditorWindow
    {
        #region 内部参数
        public const string ProjectPath = "Assets/GameResource/Resources/Config/FontSkins/EmberFontSkins.asset";
        private EmberFontSkinCatalog _catalog;
        private string _message;
        private MessageType _messageType;
        private bool _rebuildPending;
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        protected override void OnEnable()
        {
            base.OnEnable();
            _catalog = AssetDatabase.LoadAssetAtPath<EmberFontSkinCatalog>(ProjectPath);
            Undo.undoRedoPerformed += OnUndo;
            MenuWidth = 220;
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            base.OnDisable();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<EmberFontSkinCatalog>(ProjectPath);
            var tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;
            tree.Add("一键换肤", new OverviewPage(this), EditorGUIUtility.IconContent("d_PreMatCube").image);
            if (_catalog)
            {
                foreach (var skin in _catalog.Skins)
                    tree.Add("皮肤/" + skin.Name.Replace("/", "／") + "  [" + skin.Id + "]", new SkinPage(this, skin));
                tree.Add("字体枚举", new SlotsPage(this));
                tree.Add("旧 UI 接入", new MigrationPage(this));
            }
            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("字体皮肤", EditorStyles.largeLabel);
                using (new EditorGUI.DisabledScope(!_catalog))
                {
                    if (GUILayout.Button("新增皮肤", GUILayout.Width(85))) QueueChange(AddSkin);
                    if (GUILayout.Button("保存并生成枚举", GUILayout.Width(130)))
                        Run(() => { SaveAndGenerate(_catalog); QueueRebuild(); });
                }
            }
            if (_catalog)
                EditorGUILayout.LabelField("当前皮肤：" + (_catalog.FindSkin(EmberFontSkins.ActiveSkinId)?.Name ?? "未设置")
                    + (EditorUtility.IsDirty(_catalog) ? "    · 有未保存修改" : "    · 已保存"), EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
            EditorGUILayout.Space(10);
        }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void OnUndo()
        {
            RefreshPreview();
            QueueRebuild();
        }

        private void Change(Action action)
        {
            Undo.RecordObject(_catalog, "编辑字体皮肤");
            action();
            EditorUtility.SetDirty(_catalog);
            Repaint();
        }

        // 集合和导航树的结构变更延迟到当前绘制完成，避免 Layout/Repaint 数量不一致。
        private void QueueChange(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                if (!this || !_catalog) return;
                Run(() => { Change(action); QueueRebuild(); });
            };
        }

        private void QueueRebuild()
        {
            if (_rebuildPending) return;
            _rebuildPending = true;
            EditorApplication.delayCall += () =>
            {
                if (!this) return;
                _rebuildPending = false;
                ForceMenuTreeRebuild();
                Repaint();
            };
        }

        private void AddSkin()
        {
            int id = _catalog.NextSkinId++;
            var skin = new EmberFontSkinCatalog.FontSkin { Id = id, Name = "皮肤 " + id, EnumName = "Skin" + id };
            foreach (var slot in _catalog.Slots) skin.Fonts.Add(new EmberFontSkinCatalog.FontEntry { SlotId = slot.Id });
            _catalog.Skins.Add(skin);
        }

        private void SwitchSkin(int id)
        {
            Run(() =>
            {
                if (!_catalog.Validate(out var error)) throw new InvalidOperationException(error);
                if (_catalog.FindSkin(id) == null) throw new InvalidOperationException("请选择有效皮肤。");
                if (Application.isPlaying) EmberFontSkins.SetSkin(id);
                else
                {
                    Change(() => _catalog.DefaultSkinId = id);
                    AssetDatabase.SaveAssetIfDirty(_catalog);
                    RefreshPreview();
                }
                _message = "已切换到 " + _catalog.FindSkin(id).Name;
            });
        }

        private void Run(Action action)
        {
            try { _message = null; _messageType = MessageType.Info; action(); if (_message == null) _message = "操作完成。"; }
            catch (Exception ex) { _message = ex.Message; _messageType = MessageType.Error; }
        }

        private static void RefreshPreview()
        {
            EmberFontSkins.Reload();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private sealed class OverviewPage
        {
            private readonly EmberFontSkinWindow _window;
            private int _skinId;
            public OverviewPage(EmberFontSkinWindow window)
            {
                _window = window;
                _skinId = window._catalog ? window._catalog.DefaultSkinId : 0;
            }
            private bool HasCatalog => _window._catalog;
            private IEnumerable<ValueDropdownItem<int>> Choices()
                => _window._catalog.Skins.Select(s => new ValueDropdownItem<int>(s.Name, s.Id));

            [Title("一键换肤", "选择全局皮肤，每个 TMPEx 按各自的配置换字体；固定字体保持不变。")]
            [ShowInInspector, ShowIf(nameof(HasCatalog)), LabelText("目标皮肤"), ValueDropdown(nameof(Choices))]
            public int Skin { get => _skinId; set => _skinId = value; }

            [ShowIf(nameof(HasCatalog)), Button("应用此皮肤", ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.65f)]
            private void Apply() => _window.SwitchSkin(_skinId);

            [OnInspectorGUI]
            private void DrawHelp()
            {
                EditorGUILayout.Space(14);
                EditorGUILayout.HelpBox(Application.isPlaying
                    ? "运行中：只切换本次会话，不改变项目默认。"
                    : "编辑中：设为项目默认。隐藏文本和未打开的 Prefab 会在启用时使用该皮肤。", MessageType.None);
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("使用步骤", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. 左侧选择一套皮肤，配置它包含的字体。\n2. 在 TMPEx 中分别选择每套皮肤下的字体。\n3. 回到这里，一键切换。", EditorStyles.wordWrappedLabel);
            }

            [HideIf(nameof(HasCatalog)), Button("从框架预设创建项目配置", ButtonSizes.Large)]
            private void Create() => _window.Run(() =>
            {
                _window._catalog = CreateProjectCatalog();
                _window.QueueRebuild();
            });
        }

        private sealed class SkinPage
        {
            private readonly EmberFontSkinWindow _window;
            private readonly EmberFontSkinCatalog.FontSkin _skin;
            public SkinPage(EmberFontSkinWindow window, EmberFontSkinCatalog.FontSkin skin)
            {
                _window = window; _skin = skin;
                Fonts = skin.Fonts.Select(f => new FontRow(window, skin, f)).ToList();
            }

            [BoxGroup("皮肤信息"), ShowInInspector, LabelText("显示名称"), PropertyOrder(0)]
            public string Name
            {
                get => _skin.Name;
                set
                {
                    _window.Change(() => _skin.Name = value);
                    var item = _window.MenuTree.EnumerateTree().FirstOrDefault(i => ReferenceEquals(i.Value, this));
                    if (item != null) item.Name = value.Replace("/", "／") + "  [" + _skin.Id + "]";
                }
            }
            [BoxGroup("皮肤信息"), ShowInInspector, LabelText("代码枚举名"), PropertyOrder(1)]
            public string EnumName { get => _skin.EnumName; set => _window.Change(() => _skin.EnumName = value); }

            [Title("此皮肤的字体", "每个 TMPEx 可以在这些字体中独立选择。")]
            [ShowInInspector, TableList(AlwaysExpanded = true, IsReadOnly = true), PropertyOrder(2)]
            public List<FontRow> Fonts { get; }

            [Button("添加一个字体选项", ButtonSizes.Medium), PropertyOrder(3)]
            private void AddFont() => _window.QueueChange(() =>
            {
                int id = _window._catalog.NextSlotId++;
                _window._catalog.Slots.Add(new EmberFontSkinCatalog.FontSlot { Id = id, Name = "字体 " + id, EnumName = "Font" + id });
                _skin.Fonts.Add(new EmberFontSkinCatalog.FontEntry { SlotId = id });
            });

            [Button("立即使用此皮肤", ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.65f), PropertyOrder(4)]
            private void Apply() => _window.SwitchSkin(_skin.Id);

            [FoldoutGroup("高级操作"), ShowInInspector, ReadOnly, LabelText("稳定 ID"), PropertyOrder(10)]
            public int Id => _skin.Id;
            [FoldoutGroup("高级操作"), Button("删除此皮肤"), GUIColor(1f, 0.6f, 0.6f), PropertyOrder(11)]
            private void Delete() => _window.QueueChange(() =>
            {
                if (_skin.Id == _window._catalog.DefaultSkinId)
                    throw new InvalidOperationException("请先切换到其他默认皮肤，再删除此皮肤。");
                _window._catalog.Skins.Remove(_skin);
            });
        }

        [HideReferenceObjectPicker]
        private sealed class FontRow
        {
            private readonly EmberFontSkinWindow _window;
            private readonly EmberFontSkinCatalog.FontSkin _skin;
            private readonly EmberFontSkinCatalog.FontEntry _entry;
            private EmberFontSkinCatalog.FontSlot Slot => _window._catalog.Slots.Find(s => s.Id == _entry.SlotId);
            public FontRow(EmberFontSkinWindow window, EmberFontSkinCatalog.FontSkin skin, EmberFontSkinCatalog.FontEntry entry)
            { _window = window; _skin = skin; _entry = entry; }

            [ShowInInspector, LabelText("字体选项"), TableColumnWidth(110)]
            public string Name { get => Slot?.Name ?? "缺失选项"; set { if (Slot != null) _window.Change(() => Slot.Name = value); } }
            [ShowInInspector, LabelText("TMP 字体资源"), AssetsOnly]
            public TMP_FontAsset Font { get => _entry.Font; set => _window.Change(() => _entry.Font = value); }
            [Button("移除"), TableColumnWidth(50)]
            private void Remove() => _window.QueueChange(() => _skin.Fonts.Remove(_entry));
        }

        private sealed class SlotsPage
        {
            private readonly EmberFontSkinWindow _window;
            public SlotsPage(EmberFontSkinWindow window)
            { _window = window; Slots = window._catalog.Slots.Select(s => new SlotRow(window, s)).ToList(); }

            [Title("字体枚举", "名称用于 TMPEx 下拉选项；代码名用于生成枚举。ID 不随重命名改变。")]
            [ShowInInspector, TableList(AlwaysExpanded = true, IsReadOnly = true)]
            public List<SlotRow> Slots { get; }

            [Button("添加到所有皮肤", ButtonSizes.Large)]
            private void Add() => _window.QueueChange(() =>
            {
                int id = _window._catalog.NextSlotId++;
                _window._catalog.Slots.Add(new EmberFontSkinCatalog.FontSlot { Id = id, Name = "字体 " + id, EnumName = "Font" + id });
                foreach (var skin in _window._catalog.Skins)
                    skin.Fonts.Add(new EmberFontSkinCatalog.FontEntry { SlotId = id, Font = skin.Fonts.FirstOrDefault()?.Font });
            });
        }

        [HideReferenceObjectPicker]
        private sealed class SlotRow
        {
            private readonly EmberFontSkinWindow _window;
            private readonly EmberFontSkinCatalog.FontSlot _slot;
            public SlotRow(EmberFontSkinWindow window, EmberFontSkinCatalog.FontSlot slot) { _window = window; _slot = slot; }
            [ShowInInspector, ReadOnly, TableColumnWidth(35)] public int ID => _slot.Id;
            [ShowInInspector, LabelText("显示名称")] public string Name { get => _slot.Name; set => _window.Change(() => _slot.Name = value); }
            [ShowInInspector, LabelText("代码枚举名")] public string Code { get => _slot.EnumName; set => _window.Change(() => _slot.EnumName = value); }
            [Button("删除"), TableColumnWidth(50)]
            private void Delete() => _window.QueueChange(() =>
            {
                _window._catalog.Slots.Remove(_slot);
                foreach (var skin in _window._catalog.Skins) skin.Fonts.RemoveAll(f => f.SlotId == _slot.Id);
            });
        }

        private sealed class MigrationPage
        {
            private readonly EmberFontSkinWindow _window;
            public MigrationPage(EmberFontSkinWindow window) { _window = window; }
            [Title("旧 UI 接入", "仅在接入已有 UI 时使用，日常换肤不需要执行。")]
            [LabelText("Prefab 目录"), FolderPath(RequireExistingPath = true)]
            public string Folder = "Assets/GameResource";
            private bool IsPlaying => Application.isPlaying;
            [InfoBox("转换会保存 Prefab，并重新生成 EUI 绑定。保留已有 TMPEx 的固定字体设置和多语言 Key；原文件会备份。")]
            [Button("转换为 TMPEx 并接入皮肤", ButtonSizes.Large), DisableIf(nameof(IsPlaying))]
            private void Convert() => _window.Run(() =>
                _window._message = EmberFontSkinMigration.ConvertPrefabs(Folder) + " 个文本已转换。\n备份：" + EmberFontSkinMigration.LastBackupPath);
        }

        private static void AppendEnum(StringBuilder source, string name, System.Collections.Generic.IEnumerable<(int id, string name)> entries)
        {
            source.Append("    public enum ").Append(name).AppendLine("\n    {");
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.name) || entry.name == name || !Regex.IsMatch(entry.name, "^[A-Za-z_][A-Za-z0-9_]*$") || !names.Add(entry.name))
                    throw new InvalidOperationException("枚举名必须使用不重复的英文字母、数字或下划线，且不能以数字开头。");
                // @ 允许名称恰好是 C# 关键字。
                source.Append("        @").Append(entry.name).Append(" = ").Append(entry.id).AppendLine(",");
            }
            source.AppendLine("    }");
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [MenuItem("Ember/Tool/字体皮肤", false, 111)]
        public static void ShowWindow()
        {
            var window = GetWindow<EmberFontSkinWindow>("字体皮肤");
            window.minSize = new Vector2(780, 560);
            window.Show();
        }

        public static EmberFontSkinCatalog CreateProjectCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EmberFontSkinCatalog>(ProjectPath);
            if (existing) return existing;
            var preset = Resources.Load<EmberFontSkinCatalog>(EmberFontSkins.DefaultResourcePath);
            if (!preset) throw new InvalidOperationException("框架预设字体皮肤库缺失。");
            Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath));
            AssetDatabase.Refresh();
            var copy = Instantiate(preset);
            AssetDatabase.CreateAsset(copy, ProjectPath);
            AssetDatabase.SaveAssets();
            EmberFontSkins.Reload();
            return copy;
        }

        public static void SaveAndGenerate(EmberFontSkinCatalog catalog)
        {
            if (!catalog.Validate(out var error)) throw new InvalidOperationException(error);
            var source = new StringBuilder("// Generated by Ember Font Skin Tool. Do not edit.\nnamespace Game.Fonts\n{\n");
            AppendEnum(source, "FontSkinId", catalog.Skins.Select(s => (s.Id, s.EnumName)));
            AppendEnum(source, "FontSlotId", catalog.Slots.Select(s => (s.Id, s.EnumName)));
            source.AppendLine("}");
            const string path = "Assets/Game/Generated/FontSkins/EmberFontSkinIds.cs";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string content = source.ToString();
            if (!File.Exists(path) || File.ReadAllText(path) != content) File.WriteAllText(path, content, new UTF8Encoding(false));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.Refresh();
            RefreshPreview();
        }
        #endregion
    }
}
