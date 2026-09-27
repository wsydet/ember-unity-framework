using System;
using System.IO;
using System.Linq;
using System.Text;
using Ember.Basic;
using Ember.Table.Editor;
using UnityEditor;

namespace Game.Narrative.Editor
{
    /// <summary>Retire the five prototype images only after the table center has baked their replacement paths.</summary>
    internal static class NarrativeSampleArtMigration
    {
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Migrate;

        private static void Migrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeEditorAvailability.Visible) return;
            const string oldRoot = "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/M1Sample/";
            string[] pictures = { "Backgrounds/campus", "Backgrounds/evening", "Portraits/alice_neutral", "Portraits/alice_smile", "Portraits/lin_neutral" };
            if (!pictures.Any(p => File.Exists(oldRoot + p + ".png"))) return;
            try
            {
                foreach (string id in new[] { "novel_backgrounds", "novel_portraits" })
                {
                    string source = "Assets/GameResource/TableSources/" + id + ".etable.csv";
                    if (File.ReadAllText(source).Contains("Atlas/M1Sample/"))
                        throw new InvalidOperationException("源表仍引用旧图片：" + source);
                    AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceUpdate);
                    var definition = EmberTablePipeline.FindAllDefinitions().Single(d => d.TableId == id);
                    var result = EmberTablePipeline.BakeCurrent(definition);
                    if (!result.Succeeded) throw new InvalidOperationException(string.Join("\n", result.Diagnostics));
                    if (Encoding.UTF8.GetString(File.ReadAllBytes(definition.RuntimeOutputPath)).Contains("Atlas/M1Sample/"))
                        throw new InvalidOperationException("烘焙后仍引用旧图片：" + id);
                }
                // Exact file list only. Keep recoverable copies outside Assets before retiring imported assets.
                string allowed = Path.GetFullPath(oldRoot);
                foreach (string picture in pictures)
                {
                    string path = oldRoot + picture + ".png";
                    if (!Path.GetFullPath(path).StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("图片路径超出迁移范围");
                    if (!File.Exists(path)) continue;
                    string backup = ".utmp/visual-novel-e1/retired-art/" + picture + ".png";
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(path, backup, true);
                    if (File.Exists(path + ".meta")) File.Copy(path + ".meta", backup + ".meta", true);
                    if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("无法移除旧图片：" + path);
                }
            }
            catch (Exception error)
            {
                EmberDebug.LogError("Narrative.SampleArt", "正式素材迁移未完成，保留未删除的旧图。请在配置表中心检查导出诊断：" + error);
            }
        }
        #endregion
    }
}
