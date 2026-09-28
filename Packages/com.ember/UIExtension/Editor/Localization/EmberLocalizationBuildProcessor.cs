using Ember.Table.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Ember.UIExtension.Editor
{
    /// <summary>构建前从源表更新文案，避免外部编辑 CSV 后打包陈旧文案。</summary>
    internal sealed class EmberLocalizationBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 100;
        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var source in EmberLocalizationEditorService.Sources())
                EmberLocalizationEditorService.Save(source,
                    EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(source.Source)));
        }
    }
}
