using System;
using System.IO;
using Ember.Basic;
using UnityEditor;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 跑测试时 Unity 会顺手改写几个与本模板业务无关的文件，跑完就得手工 checkout：
    /// 运行期数值被写回场景、TMP 动态图集补字形、调试配置登记新标签、贴图导入器补平台设置。
    ///
    /// 这里在每轮测试开始时给这些文件留一份基线，结束时把被改写的还原回去，
    /// 于是跑测试不再污染工作区。基线只保留在 <c>.utmp/</c>（已被 gitignore、不进模板快照）。
    ///
    /// <para><b>注意：</b>基线记录的是"本轮开始时"的状态。所以先手工清一次干净的工作区，
    /// 之后每轮测试自己收尾即可；如果开始前就是脏的，脏状态会被原样保留。</para>
    /// </summary>
    internal static class NarrativeTestArtifactGuard
    {
        #region 内部参数

        private const string TAG = "Game.Narrative.Tests";
        private const string BaselineFolder = ".utmp/novel-artifact-baseline";

        /// <summary>Unity 自己会改、且不属于模板内容的文件。文件名需唯一。</summary>
        private static readonly string[] VolatilePaths =
        {
            "Assets/Game/Scenes/FrameworkScene.unity",
            "Assets/GameResource/Resources/UI/Common/Fonts/NotoSerifSC/NovelSerif SDF.asset",
            "Assets/Resources/EmberDebugConfig.asset",
            "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_night.png.meta",
            "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Portraits/lin_smile.png.meta"
        };

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static string BaselinePath(string assetPath)
            => Path.Combine(BaselineFolder, Path.GetFileName(assetPath));

        /// <summary>该资产在编辑器里是否带着未保存修改（只对场景有意义）。</summary>
        private static bool IsDirtyInEditor(string assetPath)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(assetPath);
            return scene.IsValid() && scene.isLoaded && scene.isDirty;
        }

        /// <summary>字节比较；任一侧读不到就当作不同。</summary>
        private static bool SameContent(string left, string right)
        {
            try
            {
                if (!File.Exists(left) || !File.Exists(right)) return false;
                byte[] a = File.ReadAllBytes(left), b = File.ReadAllBytes(right);
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>测试开始时记录基线；已存在则覆盖，保证基线等于"本轮开始时"。</summary>
        internal static void CaptureBaseline()
        {
            try
            {
                Directory.CreateDirectory(BaselineFolder);
                foreach (string assetPath in VolatilePaths)
                {
                    string baseline = BaselinePath(assetPath);

                    // 场景在编辑器里还带着未保存修改时不留基线：宁可不还原，也不能覆盖别人的工作。
                    if (IsDirtyInEditor(assetPath))
                    {
                        if (File.Exists(baseline)) File.Delete(baseline);
                        continue;
                    }

                    if (!File.Exists(assetPath)) continue;
                    File.Copy(assetPath, baseline, true);
                }
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, "测试产物基线记录失败，本轮不会自动还原：" + ex.Message);
            }
        }

        /// <summary>测试结束后把被 Unity 改写的文件还原回基线，并让 AssetDatabase 重新导入。</summary>
        internal static void RestoreBaseline()
        {
            try
            {
                if (!Directory.Exists(BaselineFolder)) return;

                int restored = 0;
                foreach (string assetPath in VolatilePaths)
                {
                    string baseline = BaselinePath(assetPath);
                    if (!File.Exists(baseline) || SameContent(assetPath, baseline)) continue;

                    File.Copy(baseline, assetPath, true);
                    restored++;

                    // 写回磁盘后必须重新导入，否则编辑器里仍是旧对象、下次又会被写脏。
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }

                if (restored > 0) EmberDebug.Log(TAG, $"已还原 {restored} 个被测试改写的非业务文件。");
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, "测试产物还原失败，请手工检查：" + ex.Message);
            }
        }

        #endregion
    }
}
