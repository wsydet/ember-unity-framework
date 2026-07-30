//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//
//namespace Burner.UIExtension
//{
//    [CustomEditor(typeof(UITweener), true)]
//    public class UITweenerEditor : UnityEditor.Editor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(110f);
//            base.OnInspectorGUI();
//            DrawCommonProperties();
//        }
//
//        protected void DrawCommonProperties()
//        {
//            UITweener tw = target as UITweener;
//            if (DrawHeader("Tweener"))
//            {
//                BeginContents();
//                SetLabelWidth(110f);
//
//                GUI.changed = false;
//
//                UITweener.Style style = (UITweener.Style)EditorGUILayout.EnumPopup("Play Style", tw.style);
//                EaseType easeType = (EaseType)EditorGUILayout.EnumPopup("Ease Type", tw.easeType);
//                AnimationCurve curve = EditorGUILayout.CurveField("Animation Curve", tw.animationCurve, GUILayout.Width(170f), GUILayout.Height(62f));
//                GUILayout.BeginHorizontal();
//                float dur = EditorGUILayout.FloatField("Duration", tw.duration, GUILayout.Width(170f));
//                GUILayout.Label("seconds");
//                GUILayout.EndHorizontal();
//                bool playOnEnable = EditorGUILayout.Toggle("Play OnEnable", tw.playOnEnable, GUILayout.Width(170f));
//
//                GUILayout.BeginHorizontal();
//                float del = EditorGUILayout.FloatField("Start Delay", tw.delay, GUILayout.Width(170f));
//                GUILayout.Label("seconds");
//                GUILayout.EndHorizontal();
//
//                int tg = EditorGUILayout.IntField("Tween Group", tw.tweenGroup, GUILayout.Width(170f));
//                bool ts = EditorGUILayout.Toggle("Ignore TimeScale", tw.ignoreTimeScale);
//
//                if (GUI.changed)
//                {
//                    RegisterUndo("Tween Change", tw);
//                    tw.easeType = easeType;
//                    tw.style = style;
//                    tw.ignoreTimeScale = ts;
//                    tw.tweenGroup = tg;
//                    tw.duration = dur;
//                    tw.playOnEnable = playOnEnable;
//                    tw.delay = del;
//                    EditorUtility.SetDirty(tw);
//                }
//                EndContents();
//            }
//
//            SetLabelWidth(80f);
//            DrawEvents("On Finished", tw, tw.onFinished);
//
//            using (new EditorGUILayout.HorizontalScope())
//            {
//                if (GUILayout.Button("Play", GUILayout.Width(50)))
//                {
//                    tw.ResetToBeginning();
//                    tw.PlayForward();
//                }
//
//                if (GUILayout.Button("Stop", GUILayout.Width(50)))
//                {
//                    tw.ResetToBeginning();
//                    tw.Stop();
//                }
//                if (GUILayout.Button("Play group", GUILayout.Width(100)))
//                {
//                    UITweener[] tweener = tw.GetComponents<UITweener>();
//                    foreach (var t in tweener)
//                    {
//                        if (t.tweenGroup == tw.tweenGroup)
//                        {
//                            t.ResetToBeginning();
//                            t.PlayForward();
//                        }
//                    }
//                }
//                if (GUILayout.Button("Stop group", GUILayout.Width(100)))
//                {
//                    UITweener[] tweener = tw.GetComponents<UITweener>();
//                    foreach (var t in tweener)
//                    {
//                        if (t.tweenGroup == tw.tweenGroup)
//                        {
//                            t.ResetToBeginning();
//                            t.Stop();
//                        }
//                    }
//                }
//            }
//        }
//
//        /// <summary>
//        /// ���ñ�ǩ��
//        /// </summary>
//        static public void SetLabelWidth(float width)
//        {
//            EditorGUIUtility.labelWidth = width;
//        }
//
//        /// <summary>
//        /// ����һ�������ǩ
//        /// </summary>
//
//        static public bool DrawHeader(string text) { return DrawHeader(text, text, false); }
//
//        /// <summary>
//        /// ����һ�������ǩ
//        /// </summary>
//
//        static public bool DrawHeader(string text, string key) { return DrawHeader(text, key, false); }
//
//        /// <summary>
//        /// ����һ�������ǩ
//        /// </summary>
//
//        static public bool DrawHeader(string text, bool detailed) { return DrawHeader(text, text, detailed); }
//
//        /// <summary>
//        /// ����һ�������ǩ
//        /// </summary>
//
//        static public bool DrawHeader(string text, string key, bool forceOn)
//        {
//            bool state = EditorPrefs.GetBool(key, true);
//
//
//            if (!forceOn && !state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
//            GUILayout.BeginHorizontal();
//            GUI.changed = false;
//
//            text = "<b><size=11>" + text + "</size></b>";
//            if (state) text = "\u25BC " + text;
//            else text = "\u25BA " + text;
//            if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
//
//            if (GUI.changed) EditorPrefs.SetBool(key, state);
//
//            GUILayout.EndHorizontal();
//            GUI.backgroundColor = Color.white;
//            if (!forceOn && !state) GUILayout.Space(3f);
//            return state;
//        }
//
//
//        /// <summary>
//        /// ��ʼ������������
//        /// </summary>
//        static public void BeginContents()
//        {
//            GUILayout.BeginHorizontal();
//            EditorGUILayout.BeginHorizontal("TextArea", GUILayout.MinHeight(10f));
//            GUILayout.BeginVertical();
//            GUILayout.Space(2f);
//        }
//        /// <summary>
//        /// �����������
//        /// </summary>
//
//        static public void EndContents()
//        {
//            GUILayout.Space(3f);
//            GUILayout.EndVertical();
//            EditorGUILayout.EndHorizontal();
//
//            GUILayout.Space(3f);
//            GUILayout.EndHorizontal();
//            GUILayout.Space(3f);
//        }
//
//
//        /// <summary>
//        /// ����ί���¼�
//        /// </summary>
//
//        static public void DrawEvents(string text, Object undoObject, List<EventDelegate> list)
//        {
//            DrawEvents(text, undoObject, list, null, null);
//        }
//
//        /// <summary>
//        /// ����ί���¼�
//        /// </summary>
//
//        static public void DrawEvents(string text, Object undoObject, List<EventDelegate> list, string noTarget, string notValid)
//        {
//            if (!DrawHeader(text, text, false)) return;
//
//
//            BeginContents();
//            GUILayout.BeginHorizontal();
//            GUILayout.BeginVertical();
//
//            EventDelegateEditor.Field(undoObject, list, notValid, notValid);
//
//            GUILayout.EndVertical();
//            GUILayout.EndHorizontal();
//            EndContents();
//        }
//
//        /// <summary>
//        /// ����ָ����������
//        /// </summary>
//        static public void RegisterUndo(string name, params Object[] objects)
//        {
//            if (objects != null && objects.Length > 0)
//            {
//                UnityEditor.Undo.RecordObjects(objects, name);
//
//                foreach (Object obj in objects)
//                {
//                    if (obj == null) continue;
//                    EditorUtility.SetDirty(obj);
//                }
//            }
//        }
//
//        /// <summary>
//        /// ���Ҳ���� 18 ���� �����ֶζ���
//        /// </summary>
//
//        static public void DrawPadding()
//        {
//            GUILayout.Space(18f);
//        }
//    }
//}
