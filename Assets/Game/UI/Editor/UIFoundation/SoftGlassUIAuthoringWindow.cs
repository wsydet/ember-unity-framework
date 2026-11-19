using System;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    public sealed class SoftGlassUIAuthoringWindow : EditorWindow
    {
        #region 内部参数
        private string _message;
        private static IDisposable _menu;
        #endregion
        #region 生命周期
        [InitializeOnLoadMethod]
        private static void RegisterMenu()
        {
            _menu?.Dispose();
            _menu = EmberConditionalMenu.Register("Ember/UI/柔光玻璃制作", () => EmberProjectSetup.IsTemplateActive("base"),
                () => GetWindow<SoftGlassUIAuthoringWindow>("Soft Glass"), () => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling);
        }
        private void OnGUI()
        {
            if (!EmberProjectSetup.IsTemplateActive("base")) { EditorGUILayout.HelpBox("当前身份不属于base；制作入口已失效。", MessageType.Info); return; }
            EditorGUILayout.LabelField("Soft Glass / 首次制作", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("只处理新SoftGlass资产；每批之后显式编译。已有目标拒绝覆盖，DefaultLibrary不重建。", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (GUILayout.Button("24目标零写入预检")) Run(SoftGlassUILibraryBuilder.Preflight);
                if (GUILayout.Button("S3 / 制作骨架与基础Items")) Run(() => SoftGlassUILibraryBuilder.Build(0));
                if (GUILayout.Button("S3 / 制作Center与示例Pages")) Run(() => SoftGlassUILibraryBuilder.Build(1));
                if (GUILayout.Button("S4 / 制作GM并登记独立库")) Run(() => SoftGlassUILibraryBuilder.Build(2));
                if (GUILayout.Button("仅重新生成22个Binding")) Run(SoftGlassUILibraryBuilder.RegenerateAll);
                if (GUILayout.Button("GM D10 / 有界预检")) Run(GMControlCenterAuthoring.Preflight);
                if (GUILayout.Button("GM D10 / 制作已批准控制中心及玻璃接线")) Run(GMControlCenterAuthoring.Apply);
            }
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.TextArea(_message);
        }
        #endregion
        #region 内部方法
        private void Run(Func<string> action)
        {
            try { _message = action(); }
            catch (Exception e) { _message = e.ToString(); }
        }
        #endregion
    }
}
