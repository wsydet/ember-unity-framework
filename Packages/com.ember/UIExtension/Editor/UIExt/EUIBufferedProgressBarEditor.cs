using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    [CustomEditor(typeof(EUIBufferedProgressBar)), CanEditMultipleObjects]
    internal sealed class EUIBufferedProgressBarEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUILayout.LabelField("借入引用（各层只能选一种主目标）", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            foreach (var name in new[] { "_mainSlider", "_mainImage", "_slowSlider", "_slowImage", "_whiteImage", "_whiteTrack", "_progressText" }) Draw(name);
            bool rebind = EditorGUI.EndChangeCheck();
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            Draw("_slowBufferEnabled");
            if (serializedObject.FindProperty("_slowBufferEnabled").boolValue) { Draw("_slowDelay"); Draw("_slowDuration"); }
            bool slow = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            Draw("_whiteMode");
            if (serializedObject.FindProperty("_whiteMode").enumValueIndex != 0) { Draw("_whiteDelay"); Draw("_whiteDuration"); }
            bool white = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            Draw("_useUnscaledTime");
            bool clock = EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space(); Draw("_textMode");
            if (serializedObject.FindProperty("_textMode").enumValueIndex != 0) Draw("_textPrecision");
            if (serializedObject.FindProperty("_textMode").enumValueIndex == 3) Draw("_customTextFormat");
            bool textChanged = EditorGUI.EndChangeCheck();
            if (serializedObject.ApplyModifiedProperties() && Application.isPlaying)
                foreach (var item in targets)
                {
                    var bar = (EUIBufferedProgressBar)item;
                    if (rebind)
                    {
                        var so = new SerializedObject(bar);
                        bar.ConfigureTargets(so.FindProperty("_mainSlider").objectReferenceValue as UnityEngine.UI.Slider,
                            so.FindProperty("_mainImage").objectReferenceValue as UnityEngine.UI.Image,
                            so.FindProperty("_slowSlider").objectReferenceValue as UnityEngine.UI.Slider,
                            so.FindProperty("_slowImage").objectReferenceValue as UnityEngine.UI.Image,
                            so.FindProperty("_whiteImage").objectReferenceValue as UnityEngine.UI.Image,
                            so.FindProperty("_whiteTrack").objectReferenceValue as RectTransform,
                            so.FindProperty("_progressText").objectReferenceValue as TMPro.TMP_Text);
                    }
                    else if (clock) bar.SyncImmediate();
                    else
                    {
                        // SerializedProperty bypasses setters. Re-enter only the affected
                        // timing setter to apply its reset without touching the other layer.
                        if (slow) { float value = bar.SlowDelay; bar.SlowDelay = value == 0 ? 1 : 0; bar.SlowDelay = value; }
                        if (white) { float value = bar.WhiteDelay; bar.WhiteDelay = value == 0 ? 1 : 0; bar.WhiteDelay = value; }
                        if (textChanged) bar.RefreshText();
                    }
                }
            EditorGUILayout.HelpBox("排序 Background → Slow → White → Main → Text；完整轨道对齐、同向，无相对旋转或斜切。白条必须是完整 White Track 的直接子 Image。\nSlider 内部若为 Filled Image，其 H/V method 与 origin 必须匹配 Slider.direction，且 preserveAspect=false；不支持 radial。禁止交互、Animator 或业务同时写同一目标。TMPEx Key 留空。", MessageType.Info);
            foreach (var item in targets)
            {
                var bar = (EUIBufferedProgressBar)item;
                if (bar.Diagnostic != null) EditorGUILayout.HelpBox(bar.Diagnostic, MessageType.Warning);
                var view = new SerializedObject(bar);
                var mainImage = view.FindProperty("_mainImage").objectReferenceValue as UnityEngine.UI.Image;
                var slowImage = view.FindProperty("_slowImage").objectReferenceValue as UnityEngine.UI.Image;
                var whiteImage = view.FindProperty("_whiteImage").objectReferenceValue as UnityEngine.UI.Image;
                if (OutOfOrder(slowImage, whiteImage) || OutOfOrder(whiteImage, mainImage) || OutOfOrder(slowImage, mainImage))
                    EditorGUILayout.HelpBox("同级图层顺序有冲突；应为 Slow → White → Main。", MessageType.Warning);
                if (Application.isPlaying) EditorGUILayout.LabelField("真实值 / 动画", $"{bar.CurrentValue} / {bar.MaxValue}   n={bar.NormalizedValue}   {bar.IsAnimating}");
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                if (GUILayout.Button("立即同步")) foreach (var item in targets) ((EUIBufferedProgressBar)item).SyncImmediate();
        }

        private void Draw(string name) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
        private bool OutOfOrder(UnityEngine.UI.Image behind, UnityEngine.UI.Image front)
            => behind && front && behind.transform.parent == front.transform.parent && behind.transform.GetSiblingIndex() >= front.transform.GetSiblingIndex();
    }
}
