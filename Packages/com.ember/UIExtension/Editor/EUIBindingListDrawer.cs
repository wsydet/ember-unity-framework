// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using Sirenix.OdinInspector.Editor;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding.BindingEntry[] 的自定义 Odin 抽屉。
    /// 只负责展示、编辑值、验证、搜索和分页。
    /// 结构变更（添加/删除条目）通过 EUIBinding 上的 Odin [Button] 完成，
    /// 避免在 Odin 属性树绘制期间修改数组结构导致树损坏。
    /// </summary>
    public sealed class EUIBindingListDrawer : OdinAttributeDrawer<EUIBindingListAttribute, EUIBinding.BindingEntry[]>
    {
        #region 内部参数

        private const int PAGE_SIZE = 10;

        /// <summary>变量名与控件类型前缀不匹配时的警告底色</summary>
        private static readonly Color NameMismatchColor = new Color(1f, 0.78f, 0.3f);

        private Dictionary<string, string> _baseFieldNames;
        private string _basePrefabName;
        private int _currentPage;

        #endregion

        // --------------------------------------------------------

        #region 绘制入口

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var binding = Property.Tree.WeakTargets[0] as EUIBinding;
            if (!binding)
            {
                CallNextDrawer(label);
                return;
            }

            ResolveBaseInfo(binding);

            var allDefined = new Dictionary<GameObject, GameObject>();
            EUIBindingEditorUtility.GatherBindingDefinitions(binding, allDefined);

            var entries = Property.ValueEntry.WeakValues[0] as EUIBinding.BindingEntry[]
                ?? System.Array.Empty<EUIBinding.BindingEntry>();

            // 搜索状态
            bool hasSearch = !string.IsNullOrEmpty(EUIBinding.BindingSearchText)
                || EUIBinding.BindingSearchObject;
            var searchLower = EUIBinding.BindingSearchText?.ToLower();

            int totalEntries = entries.Length;
            int totalVisible = 0;
            if (hasSearch)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var en = entries[i].Name;
                    var ego = entries[i].GameObject;
                    bool mn = !string.IsNullOrEmpty(searchLower) && (en ?? "").ToLower().Contains(searchLower);
                    bool mg = EUIBinding.BindingSearchObject && ego == EUIBinding.BindingSearchObject;
                    if (mn || mg) totalVisible++;
                }
            }
            else
            {
                totalVisible = totalEntries;
            }

            // 分页
            int totalPages = hasSearch ? 1 : Mathf.Max(1, Mathf.CeilToInt(totalVisible / (float)PAGE_SIZE));
            if (_currentPage >= totalPages) _currentPage = totalPages - 1;
            int pageStart = hasSearch ? 0 : _currentPage * PAGE_SIZE;
            int pageEnd = hasSearch ? totalVisible : Mathf.Min((_currentPage + 1) * PAGE_SIZE, totalVisible);

            // 标题行
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"绑定数 ({totalVisible}){(hasSearch ? " 搜索结果" : "")}",
                    EditorStyles.boldLabel);
                if (!hasSearch && totalPages > 1)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
                        _currentPage = Mathf.Max(0, _currentPage - 1);
                    EditorGUILayout.LabelField(
                        $"{_currentPage + 1}/{totalPages}",
                        EditorStyles.centeredGreyMiniLabel, GUILayout.Width(35));
                    if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                        _currentPage = Mathf.Min(totalPages - 1, _currentPage + 1);
                }
            }

            var selfDefinedGO = new HashSet<GameObject>();
            var selfDefinedNames = new HashSet<string>();
            bool valueModified = false;

            // 构建可见索引
            var visibleIndices = new List<int>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (hasSearch)
                {
                    var en = entries[i].Name;
                    var ego = entries[i].GameObject;
                    bool mn = !string.IsNullOrEmpty(searchLower) && (en ?? "").ToLower().Contains(searchLower);
                    bool mg = EUIBinding.BindingSearchObject && ego == EUIBinding.BindingSearchObject;
                    if (!mn && !mg) continue;
                }
                int visiblePos = visibleIndices.Count;
                if (!hasSearch && (visiblePos < pageStart || visiblePos >= pageEnd)) { visibleIndices.Add(i); continue; }
                visibleIndices.Add(i);
            }

            for (int vi = 0; vi < visibleIndices.Count; vi++)
            {
                int i = visibleIndices[vi];
                if (!hasSearch)
                {
                    if (vi < pageStart) continue;
                    if (vi >= pageEnd) break;
                }

                var entry = entries[i];
                bool isInherited = _baseFieldNames != null
                    && !string.IsNullOrEmpty(entry.Name)
                    && _baseFieldNames.ContainsKey(entry.Name);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    bool modified;
                    entries[i] = DrawEntryInline(binding, entry, isInherited, allDefined, selfDefinedGO, selfDefinedNames, out modified);
                    if (modified)
                    {
                        valueModified = true;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (valueModified)
            {
                // 通知 Odin 值已变更（数组大小未变，只更新元素内容，不会损坏属性树）
                Property.ValueEntry.WeakValues[0] = entries;
                EditorUtility.SetDirty(binding);
            }

            if (totalVisible == 0)
            {
                EditorGUILayout.HelpBox(
                    hasSearch ? "找不到符合条件的绑定条目。" : "暂无绑定条目。请使用下方的按钮添加或自动收集。",
                    MessageType.Info);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 单条目绘制

        private EUIBinding.BindingEntry DrawEntryInline(
            EUIBinding binding,
            EUIBinding.BindingEntry entry,
            bool isInherited,
            Dictionary<GameObject, GameObject> allDefined,
            HashSet<GameObject> selfDefinedGO,
            HashSet<string> selfDefinedNames,
            out bool modified)
        {
            modified = false;
            bool isFramework = entry.IsFramework;
            bool locked = isInherited || isFramework;

            // ── 第一行：变量名 + 节点 ──
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(locked);

                if (isFramework)
                    EditorGUILayout.LabelField("🔒框架", EditorStyles.miniLabel, GUILayout.Width(44));

                EditorGUILayout.LabelField("变量名", GUILayout.Width(42));

                // 命名校验：节点名/变量名与控件类型前缀不匹配时高亮提醒（可点 ✎ 一键重命名）
                bool nameMismatch = !locked && entry.GameObject
                    && IsBindingNameMismatched(entry.GameObject, entry.Type, entry.Name);
                var nameTooltip = nameMismatch
                    ? "节点名与控件类型前缀不匹配，点击 ✎ 一键重命名（如 m_Btn_ 前缀）"
                    : null;
                var prevBg = GUI.backgroundColor;
                if (nameMismatch) GUI.backgroundColor = NameMismatchColor;
                var newName = EditorGUILayout.TextField(
                    new GUIContent("", nameTooltip), entry.Name, GUILayout.MinWidth(60));
                GUI.backgroundColor = prevBg;
                if (newName != entry.Name) { entry.Name = newName; modified = true; }

                EditorGUILayout.LabelField("节点", GUILayout.Width(28));
                var oldGO = entry.GameObject;
                var newGO = EditorGUILayout.ObjectField(
                    entry.GameObject, typeof(GameObject), true, GUILayout.MinWidth(80)) as GameObject;
                if (newGO != oldGO)
                {
                    entry.GameObject = newGO;
                    modified = true;

                    if (newGO)
                    {
                        if (!IsValidChild(binding, newGO))
                        {
                            entry.GameObject = oldGO;
                            EditorUtility.DisplayDialog("无效绑定",
                                $"\"{newGO.name}\" 不是当前节点的子对象，不能绑定。", "确定");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                                entry.Name = GenerateName(newGO, selfDefinedNames);
                            entry = AutoDetectEntryType(entry);
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();

                if (isFramework)
                {
                    EditorGUILayout.LabelField("受保护", EditorStyles.miniLabel, GUILayout.Width(44));
                }
                else if (!isInherited)
                {
                    if (GUILayout.Button("↻", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        entry = AutoDetectEntryType(entry);
                        modified = true;
                    }
                }
                else
                {
                    GUILayout.Space(26);
                }
            }

            // ── 第二行：控件类型 ──
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(locked);
                EditorGUILayout.LabelField("控件类型", GUILayout.Width(55));
                var typeNames = EUIBindingEditorUtility.GetComponentTypeNames();
                int typeIdx = (int)entry.Type;
                if (typeIdx > (int)EUIBinding.WidgetTypes.End)
                    typeIdx = (int)EUIBinding.WidgetTypes.End + 1;
                int newTypeIdx = EditorGUILayout.Popup(typeIdx, typeNames, GUILayout.MinWidth(80));
                if (newTypeIdx != typeIdx)
                {
                    entry.Type = (EUIBinding.WidgetTypes)newTypeIdx;
                    modified = true;
                }
                EditorGUI.EndDisabledGroup();

                // 重命名按钮：根据控件类型自动修正节点名和变量名（框架/继承条目锁定，不显示）
                if (!locked && entry.GameObject)
                {
                    if (GUILayout.Button("✎", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        RenameNodeToMatchType(entry.GameObject, entry.Type);
                        // 变量名保留类型前缀（如 Cg_），只去掉 m_
                        var goName = entry.GameObject.name;
                        entry.Name = goName.StartsWith("m_") ? goName.Substring(2) : goName;
                        modified = true;
                    }
                }
            }

            // ── 验证 ──
            if (entry.GameObject)
            {
                if (allDefined.TryGetValue(entry.GameObject, out var definedBy)
                    && definedBy != binding.gameObject)
                {
                    EditorGUILayout.HelpBox(
                        $"此节点已被 \"{definedBy.name}\" 上的 Binding 绑定。", MessageType.Error);
                }
                else if (selfDefinedGO.Contains(entry.GameObject))
                {
                    EditorGUILayout.HelpBox("当前列表中已存在相同节点的绑定。", MessageType.Error);
                }
                else
                {
                    selfDefinedGO.Add(entry.GameObject);
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        if (selfDefinedNames.Contains(entry.Name))
                            EditorGUILayout.HelpBox(
                                $"变量名 \"{entry.Name}\" 重复。", MessageType.Error);
                        else
                            selfDefinedNames.Add(entry.Name);
                    }
                }
            }

            // ── 继承来源 ──
            if (isInherited && _baseFieldNames != null
                && _baseFieldNames.TryGetValue(entry.Name, out var sourceGuid))
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("继承自",
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            AssetDatabase.GUIDToAssetPath(sourceGuid)),
                        typeof(GameObject), false);
                }
            }

            return entry;
        }

        private static EUIBinding.BindingEntry AutoDetectEntryType(EUIBinding.BindingEntry entry)
        {
            if (!entry.GameObject) return entry;
            var detected = EUIBindingEditorUtility.DetectWidgetType(entry.GameObject);
            entry.Type = detected.Type;
            entry.ClassName = detected.ClassName;
            return entry;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 基类信息

        private void ResolveBaseInfo(EUIBinding binding)
        {
            _baseFieldNames = null;
            _basePrefabName = null;

            if (!binding) return;

            string guid;
            using (var so = new SerializedObject(binding))
                guid = so.FindProperty("baseBindingUUID").stringValue;

            if (string.IsNullOrEmpty(guid)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) return;

            var baseBinding = prefab.GetComponent<EUIBinding>();
            if (!baseBinding || baseBinding.Bindings == null) return;

            _basePrefabName = prefab.name;
            _baseFieldNames = new Dictionary<string, string>();
            foreach (var entry in baseBinding.Bindings)
            {
                if (!string.IsNullOrEmpty(entry.Name)
                    && !_baseFieldNames.ContainsKey(entry.Name))
                {
                    _baseFieldNames[entry.Name] = guid;
                }
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 辅助

        private static bool IsValidChild(EUIBinding binding, GameObject go)
        {
            if (!go || !binding) return false;
            if (go == binding.gameObject) return false;

            var t = go.transform;
            while (t)
            {
                if (t == binding.transform) return true;
                t = t.parent;
            }
            return false;
        }

        private static string GenerateName(GameObject go, HashSet<string> defined)
        {
            var baseName = go.name.Replace(" ", "_");
            if (baseName.StartsWith("m_"))
                baseName = baseName.Substring(2);

            if (!defined.Contains(baseName))
            {
                defined.Add(baseName);
                return baseName;
            }

            int idx = 1;
            while (defined.Contains($"{baseName}_{idx}"))
                idx++;
            var finalName = $"{baseName}_{idx}";
            defined.Add(finalName);
            return finalName;
        }

        /// <summary>
        /// 校验绑定条目的命名是否符合控件类型的命名规范。
        /// 规则（与 <see cref="RenameNodeToMatchType"/> 一致）：
        /// 节点名应为 <c>m_{prefix}Core</c>（如 m_Btn_Close），变量名应为 <c>{prefix}Core</c>（如 Btn_Close）。
        /// 不匹配时绑定列表会高亮「变量名」输入框，提示点击 ✎ 一键重命名。
        /// </summary>
        private static bool IsBindingNameMismatched(GameObject go, EUIBinding.WidgetTypes type, string entryName)
        {
            if (!go) return false;

            var prefix = GetWidgetPrefix(go, type);
            if (string.IsNullOrEmpty(prefix)) return false; // 无固定前缀的类型（如 Component）不校验

            // 节点名校验：m_{prefix}Core 或 {prefix}Core 均视为合法
            var goName = go.name ?? "";
            var core = StripBindingPrefix(goName);
            bool nodeOk = goName == $"m_{prefix}{core}" || goName == $"{prefix}{core}";

            // 变量名校验：应为 {prefix}Core（去掉 m_ 前缀）
            bool nameOk = string.IsNullOrEmpty(entryName) || entryName.StartsWith(prefix, System.StringComparison.Ordinal);

            return !nodeOk || !nameOk;
        }

        /// <summary>
        /// 根据控件类型将节点重命名为匹配的前缀格式。
        /// 例如 m_Close + Button → m_Btn_Close。
        /// 如果节点名已有正确的类型前缀，则不重复添加。
        /// </summary>
        private static void RenameNodeToMatchType(GameObject go, EUIBinding.WidgetTypes type)
        {
            if (!go) return;

            var oldName = go.name;
            var coreName = StripBindingPrefix(oldName);

            // 如果核心名已经以目标前缀开头，说明已经正确，跳过
            var targetPrefix = GetWidgetPrefix(go, type);
            if (oldName == $"m_{targetPrefix}{coreName}" || oldName == $"{targetPrefix}{coreName}")
                return;

            var newName = $"m_{targetPrefix}{coreName}";
            Undo.RecordObject(go, "重命名节点");
            go.name = newName;
            EditorUtility.SetDirty(go);
        }

        /// <summary>去掉节点的 m_ / mXxx 前缀和已知类型前缀，得到核心名</summary>
        private static string StripBindingPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // 去掉 m_ 前缀
            if (name.StartsWith("m_"))
                name = name.Substring(2);
            // 去掉 mXxx 前缀（m 后跟大写字母）
            else if (name.StartsWith("m") && name.Length > 1 && char.IsUpper(name[1]))
                name = name.Substring(1);

            // 去掉已知类型前缀（如 Btn_, Txt_, Img_ 等）
            string[] knownPrefixes = { "EUIBtn_", "EUI_", "Btn_", "Txt_", "Tgl_", "EUITgl_", "Img_", "EUIImg_",
                "Inp_", "Pgb_", "Scr_", "Tgp_", "Ctn_", "Raw_", "Cvs_", "Tab_", "Cg_" };
            foreach (var p in knownPrefixes)
            {
                if (name.StartsWith(p))
                    return name.Substring(p.Length);
            }
            return name;
        }

        /// <summary>获取控件类型对应的命名前缀</summary>
        private static string GetWidgetPrefix(GameObject go, EUIBinding.WidgetTypes type)
        {
            // EUI 增强组件有独立前缀
            if (go.GetComponent<EUIButtonEx>()) return "EUIBtn_";
            if (go.GetComponent<EUIToggleEx>()) return "EUITgl_";
            if (go.GetComponent<EUIImageEx>()) return "EUIImg_";

            switch (type)
            {
                case EUIBinding.WidgetTypes.Button:      return "Btn_";
                case EUIBinding.WidgetTypes.Text:        return "Txt_";
                case EUIBinding.WidgetTypes.Toggle:      return "Tgl_";
                case EUIBinding.WidgetTypes.Image:       return "Img_";
                case EUIBinding.WidgetTypes.InputField:  return "Inp_";
                case EUIBinding.WidgetTypes.ProgressBar: return "Pgb_";
                case EUIBinding.WidgetTypes.ScrollRect:  return "Scr_";
                case EUIBinding.WidgetTypes.ToggleGroup: return "Tgp_";
                case EUIBinding.WidgetTypes.UIContainer: return "Ctn_";
                case EUIBinding.WidgetTypes.RawImage:    return "Raw_";
                case EUIBinding.WidgetTypes.Canvas:      return "Cvs_";
                case EUIBinding.WidgetTypes.TabLoader:   return "Tab_";
                case EUIBinding.WidgetTypes.CanvasGroup: return "Cg_";
                default:                                 return "";
            }
        }

        #endregion
    }
}
