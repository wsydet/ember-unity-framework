//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//
//namespace Burner.UIExtension
//{
//    [CanEditMultipleObjects]
//    [CustomEditor(typeof(Transform), true)]
//    public class SUGUITransformInspector : UnityEditor.Editor
//    {
//        static public SUGUITransformInspector instance;
//
//        SerializedProperty localPos;
//        SerializedProperty localRot;
//        SerializedProperty localScale;
//
//        void OnEnable()
//        {
//            instance = this;
//            if (!IsTargetValid())
//            {
//                localPos = null;
//                localRot = null;
//                localScale = null;
//                return;
//            }
//
//            localPos = serializedObject.FindProperty("m_LocalPosition");
//            localRot = serializedObject.FindProperty("m_LocalRotation");
//            localScale = serializedObject.FindProperty("m_LocalScale");
//        }
//
//        void OnDestroy()
//        {
//            if (instance == this) instance = null;
//        }
//
//        bool IsTargetValid()
//        {
//            if (target == null || targets == null || targets.Length == 0) return false;
//
//            foreach (Object obj in targets)
//            {
//                if (obj == null) return false;
//            }
//
//            return true;
//        }
//
//        /// <summary>
//        /// 开始绘制Transform
//        /// </summary>
//        public override void OnInspectorGUI()
//        {
//            if (!IsTargetValid() || localPos == null || localRot == null || localScale == null) return;
//
//            EditorGUIUtility.labelWidth = 15;
//
//            serializedObject.Update();
//            DrawPositionButton();
//            DrawRotationButton();
//            DrawScaleButton();
//
//            serializedObject.ApplyModifiedProperties();
//        }
//
//        /// <summary>
//        /// 绘制坐标
//        /// </summary>
//        void DrawPositionButton()
//        {
//            GUILayout.BeginHorizontal();
//            bool reset = GUILayout.Button("P", GUILayout.Width(20f));
//
//            EditorGUILayout.PropertyField(localPos.FindPropertyRelative("x"));
//            EditorGUILayout.PropertyField(localPos.FindPropertyRelative("y"));
//            EditorGUILayout.PropertyField(localPos.FindPropertyRelative("z"));
//
//            if (reset) localPos.vector3Value = Vector3.zero;
//            GUILayout.EndHorizontal();
//        }
//
//        /// <summary>
//        /// 绘制形变
//        /// </summary>
//        void DrawScaleButton()
//        {
//            GUILayout.BeginHorizontal();
//            {
//                bool reset = GUILayout.Button("S", GUILayout.Width(20f));
//
//                EditorGUILayout.PropertyField(localScale.FindPropertyRelative("x"));
//                EditorGUILayout.PropertyField(localScale.FindPropertyRelative("y"));
//                EditorGUILayout.PropertyField(localScale.FindPropertyRelative("z"));
//
//                if (reset) localScale.vector3Value = Vector3.one;
//            }
//            GUILayout.EndHorizontal();
//        }
//
//        #region DrawQuaternions
//        enum ChangedAxes : int
//        {
//            None = 0,
//            X = 1,
//            Y = 2,
//            Z = 4,
//            All = 7,
//        }
//
//        ChangedAxes HasDifferentField(Transform t, Vector3 original)
//        {
//            Vector3 next = t.localEulerAngles;
//
//            ChangedAxes axes = ChangedAxes.None;
//
//            if (IsDifference(next.x, original.x)) axes |= ChangedAxes.X;
//            if (IsDifference(next.y, original.y)) axes |= ChangedAxes.Y;
//            if (IsDifference(next.z, original.z)) axes |= ChangedAxes.Z;
//
//            return axes;
//        }
//
//        ChangedAxes HasDifferentField(SerializedProperty sp)
//        {
//            ChangedAxes axes = ChangedAxes.None;
//
//            if (sp.hasMultipleDifferentValues)
//            {
//                Vector3 original = sp.quaternionValue.eulerAngles;
//
//                foreach (Object obj in serializedObject.targetObjects)
//                {
//                    axes |= HasDifferentField(obj as Transform, original);
//                    if (axes == ChangedAxes.All) break;
//                }
//            }
//            return axes;
//        }
//
//        /// <summary>
//        /// 绘制一个可编辑的浮动区域
//        /// </summary>
//        /// <param name="hidden">是否值用 -- 代替</param>
//        static bool FloatField(string name, ref float value, bool hidden, GUILayoutOption opt)
//        {
//            float newValue = value;
//            GUI.changed = false;
//
//            if (!hidden)
//            {
//                newValue = EditorGUILayout.FloatField(name, newValue, opt);
//            }
//            else
//            {
//                float.TryParse(EditorGUILayout.TextField(name, "--", opt), out newValue);
//            }
//
//            if (IsDifference(newValue, value) && GUI.changed)
//            {
//                value = newValue;
//                return true;
//            }
//            return false;
//        }
//
//        /// <summary>
//        /// 由于 Mathf.Approximately 太敏感.
//        /// </summary>
//
//        static bool IsDifference(float a, float b)
//        {
//            return Mathf.Abs(a - b) > 0.0001f;
//        }
//
//        /// <summary>
//        /// 绘制旋转
//        /// </summary>
//        void DrawRotationButton()
//        {
//            GUILayout.BeginHorizontal();
//            {
//                bool reset = GUILayout.Button("R", GUILayout.Width(20f));
//
//                Vector3 visible = (serializedObject.targetObject as Transform).localEulerAngles;
//
//                visible.x = NormalizeAngle(visible.x);
//                visible.y = NormalizeAngle(visible.y);
//                visible.z = NormalizeAngle(visible.z);
//
//                ChangedAxes changed = HasDifferentField(localRot);
//                ChangedAxes altered = ChangedAxes.None;
//
//                GUILayoutOption opt = GUILayout.MinWidth(30f);
//
//                if (FloatField("X", ref visible.x, (changed & ChangedAxes.X) != 0, opt)) altered |= ChangedAxes.X;
//                if (FloatField("Y", ref visible.y, (changed & ChangedAxes.Y) != 0, opt)) altered |= ChangedAxes.Y;
//                if (FloatField("Z", ref visible.z, (changed & ChangedAxes.Z) != 0, opt)) altered |= ChangedAxes.Z;
//
//                if (reset)
//                {
//                    localRot.quaternionValue = Quaternion.identity;
//                }
//                else if (altered != ChangedAxes.None)
//                {
//                    RecordUndoAction("Change Rotation", serializedObject.targetObjects);
//
//                    foreach (Object obj in serializedObject.targetObjects)
//                    {
//                        Transform t = obj as Transform;
//                        Vector3 v = t.localEulerAngles;
//
//                        if ((altered & ChangedAxes.X) != 0) v.x = visible.x;
//                        if ((altered & ChangedAxes.Y) != 0) v.y = visible.y;
//                        if ((altered & ChangedAxes.Z) != 0) v.z = visible.z;
//
//                        t.localEulerAngles = v;
//                    }
//                }
//            }
//            GUILayout.EndHorizontal();
//        }
//
//        /// <summary>
//        /// 保证角在 180到-180度之间
//        /// </summary>
//
//        [System.Diagnostics.DebuggerHidden]
//        [System.Diagnostics.DebuggerStepThrough]
//        static public float NormalizeAngle(float angle)
//        {
//            while (angle > 180f) angle -= 360f;
//            while (angle < -180f) angle += 360f;
//            return angle;
//        }
//
//
//        /// <summary>
//        /// 创建制定对象的撤消点
//        /// </summary>
//        static public void RecordUndoAction(string name, params Object[] objects)
//        {
//            if (objects != null && objects.Length > 0)
//            {
//                UnityEditor.Undo.RecordObjects(objects, name);
//
//                foreach (Object obj in objects)
//                {
//                    if (obj == null) continue;
//                    UnityEditor.EditorUtility.SetDirty(obj);
//
//                }
//            }
//        }
//        #endregion
//
//    }
//}
