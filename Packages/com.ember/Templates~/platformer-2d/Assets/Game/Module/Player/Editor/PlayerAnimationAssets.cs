using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>显式的一次性视觉装配；不重建状态图，不覆盖已有动画资产。</summary>
    public static class PlayerAnimationAssets
    {
        #region 内部参数
        private const string DIRECTORY = "Assets/GameResource/Player/Animation";
        private const string PROFILE = "Assets/GameResource/Resources/Config/Player/PlayerAnimation.asset";
        private const string PREFAB = "Assets/GameResource/Player/PlayerBlock.prefab";
        #endregion
        #region 外部方法
        public static string Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先停止 Play。");
            var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            if (config == null || config.Scheme == null || config.Scheme.Animation != null || File.Exists(PROFILE) || Directory.Exists(DIRECTORY))
                throw new InvalidOperationException("需要未装配动画的完整方案；已有资产必须保留并单独编辑。");
            var root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                var facing = root.GetComponent<Game.Entity.EntityFacing2D>();
                using var facingData = new SerializedObject(facing);
                var visual = facingData.FindProperty("_visual").objectReferenceValue as Transform;
                var original = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                if (visual == null || visual.parent != root.transform || original == null || visual.childCount != 0)
                    throw new InvalidOperationException("Prefab 已存在自定义层级，停止自动装配以保护内容。");
                Directory.CreateDirectory(DIRECTORY);
                AssetDatabase.Refresh();
                var controller = AnimatorController.CreateAnimatorControllerAtPath(DIRECTORY + "/PlayerVisual.controller");
                var machine = controller.layers[0].stateMachine;
                var profile = ScriptableObject.CreateInstance<PlayerAnimationProfile>();
                AssetDatabase.CreateAsset(profile, PROFILE);
                using var serialized = new SerializedObject(profile);
                serialized.FindProperty("_controller").objectReferenceValue = controller;
                var slots = serialized.FindProperty("_slots");
                slots.arraySize = config.Graph.Nodes.Count;
                int index = 0;
                foreach (var node in config.Graph.Nodes)
                {
                    var clip = new AnimationClip { name = node.Id, frameRate = 30 };
                    float duration = .6f;
                    // 方块占位姿态：每个状态独立片段，可直接替换；不含颜色、事件或根曲线。
                    float pulse = node.Id == "idle" ? .015f : node.Id == "rolling" ? .2f : .05f;
                    clip.SetCurve("Sprite", typeof(Transform), "m_LocalScale.x", AnimationCurve.EaseInOut(0, 1, duration / 2, 1 + pulse));
                    var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("Sprite", typeof(Transform), "m_LocalScale.x"));
                    curve.AddKey(duration, 1);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Sprite", typeof(Transform), "m_LocalScale.x"), curve);
                    clip.SetCurve("Sprite", typeof(Transform), "m_LocalScale.y", AnimationCurve.Constant(0, duration, 1));
                    clip.SetCurve("Sprite", typeof(Transform), "m_LocalScale.z", AnimationCurve.Constant(0, duration, 1));
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = node.Id == "idle" || node.Id == "move" || node.Id == "accelerate" || node.Id == "decelerate" ||
                        node.Id == "fall" || node.Id == "wall-slide" || node.Id == "ledge-hang";
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    AssetDatabase.CreateAsset(clip, DIRECTORY + "/" + node.Id + ".anim");
                    var state = machine.AddState(node.Id, new Vector3(index % 4 * 220, index / 4 * 80));
                    state.motion = clip;
                    state.writeDefaultValues = false;
                    var slot = slots.GetArrayElementAtIndex(index++);
                    slot.FindPropertyRelative("_nodeId").stringValue = node.Id;
                    slot.FindPropertyRelative("_state").stringValue = controller.layers[0].name + "." + node.Id;
                    slot.FindPropertyRelative("_key").objectReferenceValue = clip;
                    slot.FindPropertyRelative("_clip").objectReferenceValue = clip;
                    slot.FindPropertyRelative("_speed").floatValue = 1;
                    slot.FindPropertyRelative("_blendSeconds").floatValue = 0;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var animation = new GameObject("Animation");
                animation.transform.SetParent(visual, false);
                var sprite = new GameObject("Sprite");
                sprite.transform.SetParent(animation.transform, false);
                var renderer = sprite.AddComponent<SpriteRenderer>();
                EditorUtility.CopySerialized(original, renderer);
                UnityEngine.Object.DestroyImmediate(original);
                var animator = animation.AddComponent<Animator>();
                animator.applyRootMotion = false;
                // Controller 由运行方案绑定；Prefab 只保存组件接线。
                var view = animation.AddComponent<PlayerAnimationView>();
                using var viewData = new SerializedObject(view);
                viewData.FindProperty("_visualRoot").objectReferenceValue = visual;
                viewData.FindProperty("_sprite").objectReferenceValue = renderer;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
                using var scheme = new SerializedObject(config.Scheme);
                scheme.FindProperty("_animation").objectReferenceValue = profile;
                scheme.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(config.Scheme);
                AssetDatabase.SaveAssetIfDirty(profile);
                AssetDatabase.SaveAssetIfDirty(controller);
                return "21 节点独立片段及映射已装配；图、物理和 Facing 引用保持原值。";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        #endregion
    }
}
