using System;
using System.IO;
using System.Linq;
using System.Text;
using Ember.Basic;
using Ember.Table.Editor;
using UnityEditor;

namespace Game.Narrative.Editor
{
    internal sealed class NarrativeE2AssetImport : AssetPostprocessor
    {
        #region 内部方法
        private void OnPreprocessTexture()
        {
            if (assetPath != "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Portraits/lin_smile.png" &&
                assetPath != "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_night.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
        }
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += BakeNewKeys;
        private static void BakeNewKeys()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeEditorAvailability.Visible) return;
            try
            {
                foreach (var pair in new[] { ("novel_portraits", "lastlight_wan_smile"), ("novel_backgrounds", "lastlight_rooftop_night") })
                {
                    string output = "Assets/GameResource/Resources/Config/Tables/" + pair.Item1 + ".bytes";
                    if (File.Exists(output) && Encoding.UTF8.GetString(File.ReadAllBytes(output)).Contains(pair.Item2)) continue;
                    AssetDatabase.ImportAsset("Assets/GameResource/TableSources/" + pair.Item1 + ".etable.csv", ImportAssetOptions.ForceUpdate);
                    var definition = EmberTablePipeline.FindAllDefinitions().Single(d => d.TableId == pair.Item1);
                    var result = EmberTablePipeline.BakeCurrent(definition);
                    if (!result.Succeeded) throw new InvalidOperationException("E2 配表导出失败：" + pair.Item1);
                }
            }
            catch (Exception ex) { EmberDebug.LogError("Narrative.E2", "正式图片键未导出，请在配置表中心检查：" + ex); }
        }
        #endregion
    }
}
