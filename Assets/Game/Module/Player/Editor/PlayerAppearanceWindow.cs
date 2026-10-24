using System;
using Game.Entity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>编辑真实 Prefab 的表现与尺寸；不复制运行配置，不修改实例。</summary>
    public sealed class PlayerAppearanceWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerFeelProfile _feel;
        [SerializeField] private bool _ruler = true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private const string PREFAB = "Assets/GameResource/Player/PlayerBlock.prefab";
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.HelpBox("此入口已整合到玩家开发工具；原草稿保留。", MessageType.Info);
            if (GUILayout.Button("打开玩家配置页"))
            { Open(); GetWindow<PlayerSchemeWindow>().PreserveScaleDraft(_feel, _ruler); Close(); }
        }
        internal static void DrawPanel(PlayerFeelProfile feel, ref bool ruler)
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.LabelField("Prefab 独立于方案；修改后保存 Prefab 并重新试跑。Play 中只读。", EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("打开正式玩家 Prefab 编辑模式")) AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB));
            ruler = EditorGUILayout.Toggle("白盒 Scene 视图世界刻度", ruler);
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var root = stage != null ? stage.prefabContentsRoot : null;
            if (stage != null)
            {
                EditorGUILayout.SelectableLabel("Prefab 写入目标：" + stage.assetPath, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(36));
                EditorGUILayout.LabelField(stage.scene.isDirty ? "Prefab 有未保存变化" : "Prefab 无未保存变化", EditorStyles.miniBoldLabel);
            }
            if (root == null || root.GetComponent<PlayerController>() == null)
                EditorGUILayout.HelpBox("请打开玩家 Prefab。此窗口只编辑当前 Prefab 内容，不修改场景实例或其他玩家。", MessageType.None);
            else DrawPlayer(root);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("当前方案尺度参数", feel, typeof(PlayerFeelProfile), false);
            if (feel != null)
            {
                EditorGUILayout.LabelField("以下挂边参数写入 Movement SO（不属于 Prefab）。尺寸改变后单独复核，不自动缩放运动参数。", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(feel.Movement), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(32));
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    DrawFields(feel.Movement, "_ledge");
                if (GUILayout.Button("定位移动参数（跳跃/下穿/墙面等尺度复核）")) Selection.activeObject = feel.Movement;
            }
            EditorGUILayout.HelpBox("复核路线：平地与低顶 → 墙滑/踢墙 → 单向平台 → 挂边空间/翻越 → 落地/反弹。窄通道必须容纳实际碰撞体；出生中心应高于地面至少半个碰撞体高度。试跑位置仍由白盒入口显式指定。", MessageType.None);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void DrawPlayer(GameObject root)
        {
            var sensors = root.GetComponent<EntitySensors2D>();
            var facing = root.GetComponent<EntityFacing2D>();
            var shape = sensors != null ? sensors.Shape as BoxCollider2D : null;
            Transform visual = null;
            if (facing != null)
            {
                using var serialized = new SerializedObject(facing);
                visual = serialized.FindProperty("_visual").objectReferenceValue as Transform;
            }
            EditorGUILayout.LabelField("当前 Prefab", root.name);
            if (root.transform.localScale != Vector3.one)
                EditorGUILayout.HelpBox("根节点不是单位缩放：世界尺寸会额外变化。先在 Inspector 明确处理缩放，不自动归一化。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.LabelField("物理轮廓与检测（真实组件）", EditorStyles.boldLabel);
                if (shape != null)
                {
                    DrawFields(shape, "m_Size", "m_Offset");
                    EditorGUILayout.LabelField("世界碰撞尺寸", Vector2.Scale(shape.size, new Vector2(Mathf.Abs(shape.transform.lossyScale.x), Mathf.Abs(shape.transform.lossyScale.y))).ToString("F3"));
                }
                else EditorGUILayout.HelpBox("未配置 BoxCollider2D；请在 Inspector 编辑实际 Shape。本入口不替换碰撞类型。", MessageType.Warning);
                DrawFields(sensors, "_groundDistance", "_wallDistance");
                EditorGUILayout.LabelField("独立 Visual（真实朝向引用）", EditorStyles.boldLabel);
                if (visual == null || visual == root.transform || !visual.IsChildOf(root.transform))
                    EditorGUILayout.HelpBox("缺少有效的独立 Visual。请在 EntityFacing2D 指定子节点。", MessageType.Warning);
                else
                {
                    if (GUILayout.Button("定位 Visual / 在 Inspector 替换表现资源")) Selection.activeGameObject = visual.gameObject;
                    var sprite = visual.GetComponentInChildren<PlayerAnimationView>(true)?.Sprite ?? visual.GetComponent<SpriteRenderer>();
                    DrawFields(sprite, "m_Sprite", "m_Color");
                    DrawFields(visual, "m_LocalPosition", "m_LocalScale");
                    EditorGUILayout.HelpBox("替换 Sprite 不自动改变碰撞体。动画片段和映射由完整方案引用的 Animation 配置持有；拟合只调整外层 Visual，不改 Animator 或片段。", MessageType.None);
                    if (shape != null && sprite != null && GUILayout.Button("将单 Sprite 表现拟合当前碰撞轮廓（可撤销）"))
                    {
                        try { FitVisual(root); }
                        catch (Exception e) { EditorUtility.DisplayDialog("无法拟合", e.Message, "确定"); }
                    }
                }
            }
        }
        private static void DrawFields(UnityEngine.Object target, params string[] fields)
        {
            if (target == null) return;
            using var readOnly = new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(target));
            using var serialized = new SerializedObject(target);
            serialized.Update();
            foreach (var field in fields)
            {
                var property = serialized.FindProperty(field);
                if (property != null) DrawField(property, target);
            }
            serialized.ApplyModifiedProperties();
        }
        private static void DrawField(SerializedProperty property, UnityEngine.Object target)
        {
            var label = new GUIContent(PlayerSchemePanel.FieldLabel(property, target), property.tooltip);
            if (property.propertyType != SerializedPropertyType.Generic || !property.hasVisibleChildren)
            { EditorGUILayout.PropertyField(property, label, true); return; }
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;
            var child = property.Copy(); var end = property.GetEndProperty();
            if (child.NextVisible(true))
                do { DrawField(child, target); } while (child.NextVisible(false) && !SerializedProperty.EqualContents(child, end));
            EditorGUI.indentLevel--;
        }
        internal static void DrawWorldRuler(SceneView view, bool ruler)
        {
            if (!PlayerEditorAccess.Available || !ruler || PrefabStageUtility.GetCurrentPrefabStage() != null ||
                !UnityEngine.SceneManagement.SceneManager.GetSceneByName("PlayerFeelLab").isLoaded) return;
            var previous = Handles.color;
            try
            {
                Handles.color = new Color(.3f, .8f, 1, .65f);
                int start = Mathf.FloorToInt(view.pivot.x) - 20;
                for (int x = start; x <= start + 40; x++)
                {
                    Handles.DrawLine(new Vector3(x, 0), new Vector3(x, x % 5 == 0 ? .4f : .15f));
                    if (x % 5 == 0) Handles.Label(new Vector3(x, .45f), x + " u");
                }
                for (int y = 0; y <= 15; y++)
                {
                    Handles.DrawLine(new Vector3(start, y), new Vector3(start + .25f, y));
                    Handles.Label(new Vector3(start + .3f, y), y + " u");
                }
            }
            finally { Handles.color = previous; }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open() => PlayerSchemeWindow.OpenTab(1);
        /// <summary>仅修改独立表现；不改变物理、状态和 SO。复杂层级明确拒绝。</summary>
        public static void FitVisual(GameObject root)
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先停止试跑。");
            var shape = root.GetComponent<EntitySensors2D>()?.Shape as BoxCollider2D;
            var facing = root.GetComponent<EntityFacing2D>();
            if (shape == null || shape.transform != root.transform || facing == null)
                throw new InvalidOperationException("需要根节点 BoxCollider2D 和朝向组件。");
            using var serialized = new SerializedObject(facing);
            var visual = serialized.FindProperty("_visual").objectReferenceValue as Transform;
            var view = visual != null ? visual.GetComponentInChildren<PlayerAnimationView>(true) : null;
            var renderer = view != null ? view.Sprite : visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (visual == null || visual.parent != root.transform || visual.localRotation != Quaternion.identity ||
                renderer == null || renderer.sprite == null || renderer.drawMode != SpriteDrawMode.Simple ||
                visual.GetComponentsInChildren<Renderer>(true).Length != 1 || visual.GetComponentsInChildren<Collider2D>(true).Length != 0)
                throw new InvalidOperationException("仅支持无碰撞体、无旋转、直接子级的单 Simple Sprite Visual。");
            if (view != null && (view.VisualRoot != visual || view.transform.parent != visual || renderer.transform.parent != view.transform ||
                view.transform.localPosition != Vector3.zero || view.transform.localRotation != Quaternion.identity || view.transform.localScale != Vector3.one ||
                renderer.transform.localPosition != Vector3.zero || renderer.transform.localRotation != Quaternion.identity || renderer.transform.localScale != Vector3.one))
                throw new InvalidOperationException("动画层和 Sprite 需要单位初始姿态；请在 Prefab 停止预览后拟合，不改动动画层。");
            var bounds = renderer.sprite.bounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0 || !float.IsFinite(shape.size.x) || !float.IsFinite(shape.size.y) || shape.size.x <= 0 || shape.size.y <= 0)
                throw new InvalidOperationException("Sprite 与碰撞尺寸必须有效。");
            Undo.RecordObject(visual, "拟合玩家表现");
            var scale = new Vector3(shape.size.x / bounds.size.x * Mathf.Sign(visual.localScale.x), shape.size.y / bounds.size.y, visual.localScale.z);
            visual.localScale = scale;
            var center = bounds.center;
            if (renderer.flipX) center.x = -center.x;
            if (renderer.flipY) center.y = -center.y;
            visual.localPosition = new Vector3(shape.offset.x - center.x * scale.x, shape.offset.y - center.y * scale.y, visual.localPosition.z);
            EditorUtility.SetDirty(visual);
        }
        #endregion
    }
}
