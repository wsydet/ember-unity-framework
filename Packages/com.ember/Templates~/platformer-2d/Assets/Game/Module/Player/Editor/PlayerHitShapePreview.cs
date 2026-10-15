using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>原生碰撞体编辑与纯值形状之间的唯一转换；不使用运行角色或身体碰撞体。</summary>
    public static class PlayerHitShapeEditor
    {
        #region 外部方法
        public static PlayerHitShape Mirror(PlayerHitShape shape)
        {
            shape.Validate("HitPreview.Mirror");
            var value = new ShapeValue { _kind = (int)shape.Kind, _offset = new Vector2(-shape.Offset.x, shape.Offset.y),
                _angle = -shape.Angle, _size = shape.Size, _radius = shape.Radius,
                _points = shape.Points.Select(p => new Vector2(-p.x, p.y)).ToArray() };
            return JsonUtility.FromJson<PlayerHitShape>(JsonUtility.ToJson(value));
        }
        public static Collider2D Create(PlayerHitShape shape, UnityEngine.SceneManagement.Scene scene)
        {
            PlayerEditorAccess.Require(); shape.Validate("HitPreview");
            var go = EditorUtility.CreateGameObjectWithHideFlags("攻击范围 · 原生 Collider 编辑", HideFlags.DontSave);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = shape.Offset; go.transform.rotation = Quaternion.Euler(0, 0, shape.Angle);
            Collider2D collider;
            if (shape.Kind == PlayerHitShapeKind.Box) { var box = go.AddComponent<BoxCollider2D>(); box.size = shape.Size; collider = box; }
            else if (shape.Kind == PlayerHitShapeKind.Circle) { var circle = go.AddComponent<CircleCollider2D>(); circle.radius = shape.Radius; collider = circle; }
            else { var polygon = go.AddComponent<PolygonCollider2D>(); polygon.SetPath(0, shape.Points.ToArray()); collider = polygon; }
            collider.isTrigger = true; return collider;
        }

        public static PlayerHitShape Capture(Collider2D collider)
        {
            PlayerEditorAccess.Require();
            if (collider == null || collider.transform.localScale != Vector3.one || collider.transform.parent != null)
                throw new InvalidOperationException("范围预览必须是未缩放的独立根对象。");
            int kind = collider is BoxCollider2D ? 0 : collider is CircleCollider2D ? 1 : collider is PolygonCollider2D ? 2 : -1;
            if (kind < 0 || collider is PolygonCollider2D polygon && polygon.pathCount != 1)
                throw new InvalidOperationException("范围只支持 Box、Circle 或单轮廓 Polygon。");
            var value = new ShapeValue { _kind = kind, _offset = collider.transform.TransformPoint(collider.offset),
                _angle = collider.transform.eulerAngles.z, _size = collider is BoxCollider2D box ? box.size : Vector2.one,
                _radius = collider is CircleCollider2D circle ? circle.radius : .5f,
                _points = collider is PolygonCollider2D poly ? poly.GetPath(0) : Array.Empty<Vector2>() };
            var result = JsonUtility.FromJson<PlayerHitShape>(JsonUtility.ToJson(value)); result.Validate("HitPreview"); return result;
        }
        #endregion
        #region 内部参数
        [Serializable] private sealed class ShapeValue
        { public int _kind; public Vector2 _offset, _size; public float _radius, _angle; public Vector2[] _points; }
        #endregion
    }

    /// <summary>独立 PreviewSceneStage。原生 Edit Collider 操作只改临时对象，显式应用才修改指定配置项。</summary>
    public sealed class PlayerHitShapePreview : PreviewSceneStage
    {
        #region 内部参数
        private PlayerFeelProfile _feel;
        private int _segment, _window, _frame, _shape;
        private string _original;
        private Collider2D _collider;
        private UnityEngine.Object _previousSelection;
        private string _message;
        private PlayerScheme _scheme;
        private GameObject _pose;
        private AnimationClip _clip;
        private bool _left;
        private float _time;
        #endregion
        #region 外部方法
        public static void Open(PlayerFeelProfile feel, int segment, int window, int frame, int shape)
            => OpenInternal(feel, null, segment, window, frame, shape);
        public static void Open(PlayerScheme scheme, int segment, int window, int frame, int shape)
            => OpenInternal(scheme.Feel, scheme, segment, window, frame, shape);
        private static void OpenInternal(PlayerFeelProfile feel, PlayerScheme scheme, int segment, int window, int frame, int shape)
        {
            PlayerEditorAccess.Require(); PlayerSchemeOwnership.RequireWritable(feel);
            if (feel == null || !AssetDatabase.Contains(feel))
                throw new InvalidOperationException("原生范围编辑要求已创建资产的独立配置；会保留该资产当前未保存草稿，不自动保存。");
            if (EditorApplication.isPlayingOrWillChangePlaymode || StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("请先返回主场景的非 Play 编辑状态；不会自动关闭当前 Prefab 或其他预览草稿。");
            var source = feel.Attack.Segments[segment].Windows[window].Keyframes[frame].Shapes[shape];
            var stage = CreateInstance<PlayerHitShapePreview>();
            stage._feel = feel; stage._segment = segment; stage._window = window; stage._frame = frame; stage._shape = shape;
            stage._scheme = scheme; stage._time = feel.Attack.Segments[segment].Windows[window].Keyframes[frame].Time;
            stage._original = JsonUtility.ToJson(source); stage._previousSelection = Selection.activeObject;
            StageUtility.GoToStage(stage, true);
        }
        #endregion
        #region 内部方法
        protected override bool OnOpenStage()
        {
            if (!base.OnOpenStage()) return false;
            _collider = PlayerHitShapeEditor.Create(JsonUtility.FromJson<PlayerHitShape>(_original), scene);
            if (_scheme != null)
            {
                string id = _scheme.CombatGraph.Nodes.First(n => n.SegmentId == _feel.Attack.Segments[_segment].Id).AttackNodeId;
                _clip = _scheme.Animation.Slots.First(s => s.NodeId == id).Clip;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/PlayerBlock.prefab");
                var source = prefab == null ? null : prefab.GetComponentInChildren<PlayerAnimationView>(true);
                if (source != null)
                {
                    _pose = EditorUtility.CreateGameObjectWithHideFlags("角色姿态（仅表现）", HideFlags.DontSave);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_pose, scene);
                    _pose.transform.localScale = source.VisualRoot.localScale;
                    var sprite = new GameObject("Sprite"); sprite.transform.SetParent(_pose.transform, false);
                    EditorUtility.CopySerialized(source.Sprite, sprite.AddComponent<SpriteRenderer>());
                    SamplePose();
                }
            }
            Selection.activeGameObject = _collider.gameObject; SceneView.duringSceneGui += Draw;
            return true;
        }
        protected override GUIContent CreateHeaderContent() => new("攻击范围预览 · 显式应用");
        protected override void OnCloseStage()
        { SceneView.duringSceneGui -= Draw; base.OnCloseStage(); Selection.activeObject = _previousSelection; }
        public void ApplyCurrentPreview()
        {
            PlayerEditorAccess.Require(); PlayerSchemeOwnership.RequireWritable(_feel);
            if (_feel == null || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("配置已失效或正在进入 Play。");
            var current = _feel.Attack.Segments[_segment].Windows[_window].Keyframes[_frame].Shapes[_shape];
            if (JsonUtility.ToJson(current) != _original) throw new InvalidOperationException("原形状在预览期间已变化，请退出预览后重新打开，避免覆盖草稿。");
            var value = PlayerHitShapeEditor.Capture(_collider);
            if (_left) value = PlayerHitShapeEditor.Mirror(value);
            Undo.RecordObject(_feel, "应用攻击范围"); JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(value), current);
            EditorUtility.SetDirty(_feel); _original = JsonUtility.ToJson(current); _message = "已应用到当前草稿，尚未保存资产。";
        }
        private void Draw(SceneView view)
        {
            if (StageUtility.GetCurrentStage() != this) return;
            Handles.BeginGUI(); GUILayout.BeginArea(new Rect(12, 12, 450, 180), EditorStyles.helpBox);
            GUILayout.Label("在 Inspector 使用原生 Edit Collider 编辑；应用只改当前形状。", EditorStyles.wordWrappedLabel);
            bool left = GUILayout.Toggle(_left, "左朝向（应用时转换回统一右朝向配置）");
            if (left != _left)
            {
                var mirrored = PlayerHitShapeEditor.Mirror(PlayerHitShapeEditor.Capture(_collider));
                DestroyImmediate(_collider.gameObject); _collider = PlayerHitShapeEditor.Create(mirrored, scene);
                _left = left; Selection.activeGameObject = _collider.gameObject; SamplePose();
            }
            if (_clip != null)
            {
                float time = EditorGUILayout.Slider("动作逻辑秒（形状仍编辑选中关键帧）", _time, 0, _feel.Attack.Segments[_segment].Duration);
                if (time != _time) { _time = time; SamplePose(); }
            }
            using (new EditorGUI.DisabledScope(!PlayerEditorAccess.Available || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("应用到攻击配置草稿"))
                    try { ApplyCurrentPreview(); } catch (Exception error) { _message = error.Message; }
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message, EditorStyles.wordWrappedLabel);
            GUILayout.EndArea(); Handles.EndGUI();
        }
        private void SamplePose()
        {
            if (_pose == null || _clip == null) return;
            var scale = _pose.transform.localScale; scale.x = Mathf.Abs(scale.x) * (_left ? -1 : 1); _pose.transform.localScale = scale;
            _clip.SampleAnimation(_pose, Mathf.Min(_time, _clip.length));
        }
        #endregion
    }
}
