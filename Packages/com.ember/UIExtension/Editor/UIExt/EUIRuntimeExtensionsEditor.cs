using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension.Editor
{
    [CustomEditor(typeof(TMPMarquee)),CanEditMultipleObjects]
    internal sealed class TMPMarqueeEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("固定宽度、单行 NoWrap、Overflow、关闭 AutoSize，祖先需 Mask/RectMask2D。只改网格；Fallback 与 Sprite 子网格使用相同位移。",MessageType.Info);
            foreach(var item in targets){var marquee=(TMPMarquee)item;if(marquee.Diagnostic!=null)EditorGUILayout.HelpBox(marquee.Diagnostic,MessageType.Warning);}
            if(GUILayout.Button("刷新 / 复位"))foreach(var item in targets)((TMPMarquee)item).Refresh();
        }
    }
    [CustomEditor(typeof(EUIScrollController)),CanEditMultipleObjects]
    internal sealed class EUIScrollControllerEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI(){DrawDefaultInspector();EditorGUILayout.HelpBox("沿用 ScrollRect normalized：左0/右1，顶1/底0。Snap 选单轴；内容保持原生同向坐标。offset 是 viewport 本地坐标，ScrollByPixels 正 y 向底浏览。",MessageType.Info);}
    }
    [CustomEditor(typeof(EUIRoundedImageModifier)),CanEditMultipleObjects]
    internal sealed class EUIRoundedImageModifierEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {DrawDefaultInspector();foreach(var item in targets)if(((EUIRoundedImageModifier)item).GetComponent<Image>().type!=Image.Type.Simple)EditorGUILayout.HelpBox("圆角仅支持 Simple；当前 Image 原网格直通。圆角不改变点击遮罩。",MessageType.Warning);}
    }
}
