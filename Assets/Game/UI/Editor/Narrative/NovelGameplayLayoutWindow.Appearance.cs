using System;
using System.Collections.Generic;
using System.Linq;
using Ember.UIExtension;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    public sealed partial class NovelGameplayLayoutWindow
    {
        #region 内部参数
        [Serializable]
        private sealed class StyleValue
        {
            public string Target, Component, Property;
            public int Index, Integer;
            public float Number;
            public bool Boolean;
            public Color Color;
            public Vector4 Vector;
            public UnityEngine.Object Asset;
        }
        [SerializeField] private List<StyleValue> _styles = new();
        [SerializeField] private string _detailKey;
        [SerializeField] private bool _showHitArea;
        // 皮肤清单按钮用的目标皮肤标识：留空则复制成 SKINID 占位，便于先取行再决定皮肤名。
        [SerializeField] private string _skinId = "lastlight_alt";
        private string SelectedKey => !string.IsNullOrEmpty(_detailKey) && _targets.ContainsKey(_detailKey) ? _detailKey : Keys[_selected];
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static void AddAppearanceTargets(GameObject root, GameObject menu, Dictionary<string, RectTransform> targets)
        {
            targets["Shading"] = root.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "ReadingShading");
            targets["ChoiceTemplate"] = (RectTransform)Array.Find(root.GetComponent<EUIBinding>().Bindings, b => b.Name == "ChoiceTemplate").GameObject.transform;
            targets["MenuPanel"] = (RectTransform)menu.transform;
            AddDescendants(root.transform, "reader", targets);
            AddDescendants(menu.transform, "menu", targets);
        }

        private static void AddDescendants(Transform root, string path, Dictionary<string, RectTransform> targets)
        {
            if (root is RectTransform rect) targets[path] = rect;
            for (int i = 0; i < root.childCount; i++) AddDescendants(root.GetChild(i), path + "/" + i, targets);
        }

        private void DrawDetailSelector()
        {
            var root = _targets[Keys[_selected]];
            var entries = _targets.Where(p => (p.Key.StartsWith("reader/") || p.Key.StartsWith("menu/") || p.Key.StartsWith("popup/")) &&
                (p.Value == root || p.Value.IsChildOf(root))).ToArray();
            if (entries.Length == 0) return;
            int current = Array.FindIndex(entries, p => p.Value == _targets[SelectedKey]);
            int next = EditorGUILayout.Popup("子元素", Mathf.Max(0, current), entries.Select(p =>
                p.Value == root ? "整体" : AnimationUtility.CalculateTransformPath(p.Value, root)).ToArray());
            if (next != current) { _detailKey = entries[next].Key; Repaint(); }
        }

        private static IEnumerable<(string path, string label)> StyleFields(Component component)
        {
            if (component.GetType().FullName == "Game.UI.NovelHistoryAppearance")
            {
                yield return ("SpeakerColor", "姓名颜色"); yield return ("LatestMarkerColor", "最新记录标记颜色");
                yield return ("TextIndent", "正文缩进（字号倍数）"); yield return ("MarkerPosition", "标记位置（字号倍数）");
                yield return ("SpeakerWidth", "姓名最大宽度（字号倍数）"); yield return ("CenterShortHistory", "短记录垂直居中");
            }
            else if (component is Image)
            {
                yield return ("m_Sprite", "图片 Sprite"); yield return ("m_Color", "颜色 / 透明度");
                yield return ("m_Type", "图片模式"); yield return ("m_PreserveAspect", "保持比例");
                yield return ("m_FillCenter", "绘制九宫格中心"); yield return ("m_PixelsPerUnitMultiplier", "九宫格像素倍率");
            }
            else if (component is TMP_Text)
            {
                yield return ("m_fontAsset", "字体"); yield return ("m_fontSize", "基础字号");
                yield return ("m_fontColor", "文字颜色"); yield return ("m_fontStyle", "字形");
                yield return ("m_HorizontalAlignment", "水平对齐"); yield return ("m_VerticalAlignment", "垂直对齐");
                yield return ("m_characterSpacing", "字距"); yield return ("m_lineSpacing", "行距");
                yield return ("m_margin", "边距 左 / 上 / 右 / 下");
            }
            else if (component is Selectable)
            {
                yield return ("m_Transition", "状态过渡"); yield return ("m_Colors", "状态颜色");
                yield return ("m_SpriteState", "状态图片");
            }
            else if (component is EUIGradient)
            {
                yield return ("m_Enabled", "启用渐变"); yield return ("_fourColors", "四角渐变");
                yield return ("_isLeftToRight", "水平渐变"); yield return ("_topColor", "上 / 左颜色");
                yield return ("_topRightColor", "右上颜色"); yield return ("_bottomColor", "下 / 右颜色");
                yield return ("_bottomRightColor", "右下颜色");
            }
            else if (component is VerticalLayoutGroup)
            {
                yield return ("m_Padding", "内边距"); yield return ("m_Spacing", "间距");
            }
            else if (component is LayoutElement)
            {
                yield return ("m_MinHeight", "最小高度"); yield return ("m_PreferredHeight", "首选高度");
            }
        }

        private void DrawAppearance(RectTransform rect)
        {
            if (Keys[_selected] is "Background" or "Left" or "Center" or "Right")
            { EditorGUILayout.HelpBox("背景和立绘图片由剧情控制；下方素材仅供预览。", MessageType.Info); return; }
            GUILayout.Space(10);
            GUILayout.Label("外观（保存到正式 Prefab）", EditorStyles.boldLabel);
            foreach (var component in rect.GetComponents<Component>())
            {
                if (!component) continue;
                var fields = StyleFields(component).ToArray();
                if (fields.Length == 0) continue;
                using var serialized = new SerializedObject(component);
                serialized.Update();
                var oldFont = component is TMP_Text previousText ? previousText.font : null;
                EditorGUI.BeginChangeCheck();
                foreach (var field in fields)
                {
                    var property = serialized.FindProperty(field.path);
                    if (property == null) continue;
                    if (field.path == "m_Transition")
                        property.intValue = EditorGUILayout.IntPopup(field.label, property.intValue,
                            new[] { "无", "颜色", "图片" }, new[] { 0, 1, 2 });
                    else EditorGUILayout.PropertyField(property, new GUIContent(field.label), true);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (component is TMP_Text)
                    {
                        var fontSize = serialized.FindProperty("m_fontSize");
                        if (fontSize != null)
                        {
                            fontSize.floatValue = Mathf.Max(1, fontSize.floatValue);
                            var baseSize = serialized.FindProperty("m_fontSizeBase");
                            if (baseSize != null) baseSize.floatValue = fontSize.floatValue;
                        }
                    }
                    serialized.ApplyModifiedProperties();
                    // TMP keeps a separate base size and material; update them when editing font settings.
                    if (component is TMP_Text text)
                    {
                        Undo.RecordObject(text, "调整文字样式");
                        if (text.font && text.font != oldFont) text.fontSharedMaterial = text.font.material;
                    }
                    if (component is Graphic graphic) graphic.SetAllDirty();
                    if (rect.TryGetComponent<Graphic>(out var owner)) owner.SetAllDirty();
                    if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    Changed();
                    RebuildPreview();
                }
            }
            if (rect.TryGetComponent<Image>(out var image) && image.color.a == 0)
                EditorGUILayout.HelpBox("此图片透明度为 0，换图后仍不可见。可提高颜色的 Alpha；推进命中区域本身通常保持透明。", MessageType.Info);
            if (Keys[_selected] == "Advance")
                EditorGUILayout.HelpBox("整体是推进点击范围；在子元素中选择箭头换图。请保持整体覆盖底部对白区域。", MessageType.Info);
            if (GUILayout.Button("还原此元素到已保存状态")) RestoreAppearanceElement(rect);
            _showHitArea = EditorGUILayout.Toggle("显示推进点击范围", _showHitArea);

            GUILayout.Space(8);
            GUILayout.Label("皮肤覆盖", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("皮肤只换图片，不改位置与大小；这一行填进 novel_skin_sprites 即可。" +
                "覆盖值必须是项目 Resources 下的图片，Unity 内置图片无法用路径表示。", MessageType.None);
            _skinId = EditorGUILayout.TextField("目标皮肤", _skinId);
            if (GUILayout.Button("把此元素加入皮肤清单（复制 CSV 行）"))
            {
                if (!rect.TryGetComponent<Image>(out _)) _message = "此元素上没有 Image；皮肤本期只覆盖图片。";
                else if (!NovelSkinImageCatalog.TryDescribe(rect, out string page, out string control, out string node, out string spritePath))
                    _message = "无法定位页面，未复制任何内容。";
                else
                {
                    // 内置图没有可寻址路径：仍给出定位信息，路径留占位提醒换成项目图片。
                    string path = string.IsNullOrEmpty(spritePath) ? "UI/Common/Atlas/Novel/换成项目图片" : spritePath;
                    EditorGUIUtility.systemCopyBuffer = NovelSkinImageCatalog.BuildRow(
                        string.IsNullOrWhiteSpace(_skinId) ? "SKINID" : _skinId.Trim(), page, control, node, path);
                    _message = string.IsNullOrEmpty(spritePath)
                        ? "当前图片是 Unity 内置资源（无法用路径表示），已复制带占位路径的行。"
                        : "已复制到剪贴板：" + EditorGUIUtility.systemCopyBuffer;
                }
            }
        }

        private void RestoreAppearanceElement(RectTransform rect)
        {
            GameObject reader = null, menu = null;
            var popups = new Dictionary<string, GameObject>();
            try
            {
                if (SourceHash != _sourceHash) { _message = "资源已在外部改变，请重新载入。"; return; }
                reader = PrefabUtility.LoadPrefabContents(PrefabPath);
                menu = PrefabUtility.LoadPrefabContents(ReadingMenuPrefabPath);
                var originals = new Dictionary<string, RectTransform>();
                FindTargets(reader, originals, menu);
                LoadPopups(popups); AddPopupTargets(popups, originals);
                var original = originals[SelectedKey];
                Undo.RecordObjects(rect.GetComponents<Component>(), "还原 UI 元素");
                new LayoutRecord(SelectedKey, original).Apply(rect);
                var sourceComponents = original.GetComponents<Component>();
                var destinationComponents = rect.GetComponents<Component>();
                for (int i = 0; i < sourceComponents.Length; i++)
                {
                    if (!sourceComponents[i]) continue;
                    using var source = new SerializedObject(sourceComponents[i]);
                    using var destination = new SerializedObject(destinationComponents[i]);
                    var paths = StyleFields(sourceComponents[i]).Select(f => f.path);
                    if (sourceComponents[i] is TMP_Text) paths = paths.Concat(new[] { "m_fontSizeBase", "m_sharedMaterial" });
                    foreach (string path in paths)
                    {
                        var property = source.FindProperty(path);
                        if (property != null) destination.CopyFromSerializedProperty(property);
                    }
                    destination.ApplyModifiedPropertiesWithoutUndo();
                    if (PrefabUtility.IsPartOfPrefabInstance(destinationComponents[i]))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(destinationComponents[i]);
                }
                if (PrefabUtility.IsPartOfPrefabInstance(rect)) PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                Changed(); RebuildPreview();
            }
            finally
            {
                ReleasePopups(popups);
                if (menu) PrefabUtility.UnloadPrefabContents(menu);
                if (reader) PrefabUtility.UnloadPrefabContents(reader);
            }
        }

        private void DrawHitArea(Rect area, float fit)
        {
            if (!_showHitArea || IsMenuKey(Keys[_selected])) return;
            var corners = new Vector3[4];
            _previewTargets["Advance"].GetWorldCorners(corners);
            var bounds = new Rect(area.x + (corners[0].x + Size.x / 2) * fit,
                area.y + (Size.y / 2 - corners[1].y) * fit, (corners[2].x - corners[0].x) * fit, (corners[1].y - corners[0].y) * fit);
            EditorGUI.DrawRect(bounds, new Color(0, .8f, 1, .12f));
            GUI.Label(new Rect(bounds.x, bounds.y, 180, 20), "蓝色区域：对白推进点击范围");
        }

        private void CaptureStyles()
        {
            _styles.Clear();
            foreach (var pair in _targets.Where(p => p.Key.StartsWith("reader/") || p.Key.StartsWith("menu/") || p.Key.StartsWith("popup/")))
            {
                var components = pair.Value.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    if (!component) continue;
                    using var serialized = new SerializedObject(component);
                    foreach (var field in StyleFields(component))
                    {
                        var property = serialized.FindProperty(field.path);
                        if (property == null) continue;
                        CaptureProperty(pair.Key, component, i, property);
                    }
                    if (component is TMP_Text)
                        foreach (string path in new[] { "m_fontSizeBase", "m_sharedMaterial" })
                        {
                            var property = serialized.FindProperty(path);
                            if (property != null) CaptureProperty(pair.Key, component, i, property);
                        }
                }
            }
        }

        private void CaptureProperty(string key, Component component, int index, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                var child = property.Copy(); var end = property.GetEndProperty();
                if (child.Next(true)) do
                {
                    if (SerializedProperty.EqualContents(child, end)) break;
                    CaptureProperty(key, component, index, child);
                } while (child.Next(false));
                return;
            }
            var value = new StyleValue { Target = key, Component = component.GetType().FullName, Index = index, Property = property.propertyPath };
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: value.Integer = property.intValue; break;
                case SerializedPropertyType.Boolean: value.Boolean = property.boolValue; break;
                case SerializedPropertyType.Float: value.Number = property.floatValue; break;
                case SerializedPropertyType.Color: value.Color = property.colorValue; break;
                case SerializedPropertyType.Vector4: value.Vector = property.vector4Value; break;
                case SerializedPropertyType.ObjectReference: value.Asset = property.objectReferenceValue; break;
                default: return;
            }
            _styles.Add(value);
        }

        private void ApplyStyles()
        {
            foreach (var value in _styles)
            {
                if (!_targets.TryGetValue(value.Target, out var rect)) throw new InvalidOperationException("外观草稿目标缺失：" + value.Target);
                var components = rect.GetComponents<Component>();
                if (value.Index >= components.Length || !components[value.Index] || components[value.Index].GetType().FullName != value.Component)
                    throw new InvalidOperationException("外观草稿组件已改变，请重新载入。");
                var component = components[value.Index];
                using var serialized = new SerializedObject(component);
                var property = serialized.FindProperty(value.Property);
                if (property == null) throw new InvalidOperationException("外观属性已改变：" + value.Property);
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: property.intValue = value.Integer; break;
                    case SerializedPropertyType.Boolean: property.boolValue = value.Boolean; break;
                    case SerializedPropertyType.Float: property.floatValue = value.Number; break;
                    case SerializedPropertyType.Color: property.colorValue = value.Color; break;
                    case SerializedPropertyType.Vector4: property.vector4Value = value.Vector; break;
                    case SerializedPropertyType.ObjectReference: property.objectReferenceValue = value.Asset; break;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
        #endregion
    }
}
