using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.Table;
using Ember.UIExtension;
using Game.Narrative;
using Game.Table.Generated;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    /// <summary>
    /// 编辑期的多语言解析器装配。
    ///
    /// <para>运行期由 <c>GameTableModule</c> 装配解析器；编辑期原本只有三个预览窗口
    /// （流程图窗口、节点试播窗口、布局窗口）会各自装配一次，所以在普通编辑预制体时
    /// <c>TextLocalization.Localizer</c> 是空的，<c>TMPEx</c> 的 Inspector 只能显示
    /// 「当前没有注入多语言解析器」。这里补一个随编辑器加载的装配，让 Inspector 的
    /// 多语言区能真正按语言预览，与口径文档"编辑期预览看 Inspector 的多语言区"一致。</para>
    ///
    /// <para>安全边界：<c>TMPEx</c> 只在运行期改写文本，编辑期装配解析器不会改变任何
    /// Prefab 与场景里的显示文本，只影响 Inspector 与试播窗口的预览。</para>
    /// </summary>
    internal static class NovelLocalizationEditorPreview
    {
        #region 内部参数

        private const string TAG = "Novel.Localization";

        private static EmberTableEngine _engine;
        private static NarrativeTableCatalog _catalog;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        /// <summary>
        /// 随资源导入补装解析器。
        ///
        /// <para>实测本程序集里的加载期回调（类级 <c>[InitializeOnLoad]</c> 与
        /// <c>[InitializeOnLoadMethod]</c>）在这个工程里都没有被调到，所以改用
        /// <see cref="AssetPostprocessor"/>——本仓库的 <c>ScriptEncodingPostprocessor</c>、
        /// <c>FrameworkSceneBootstrapper.ScenesAssetPostprocessor</c> 都用它，且都确实在跑。
        /// 每次域重载后的重新导入都会经过这里，代价只是一个空判断。</para>
        /// </summary>
        private sealed class Bootstrap : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedFrom, string[] movedTo)
                => EnsureInstalled();
        }

        private static void EnsureInstalled()
        {
            if (TextLocalization.Localizer == null) Install();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void Release()
        {
            NovelLocalization.Uninstall();
            NovelSkin.Uninstall();
            _catalog = null;
            _engine?.Dispose();
            _engine = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 按已烘焙的导表产物装配编辑期解析器；缺产物时保持原状。
        /// 先建后换：只有新表真的备齐了才卸载旧的，避免一次读失败把已经装好的解析器清空
        /// （那样 Inspector 会退回"没有多语言解析器"）。
        /// </summary>
        internal static void Install()
        {
            try
            {
                var engine = new EmberTableEngine();
                var catalog = GameTables.CreateCatalog();
                var bytes = new Dictionary<string, byte[]>();
                foreach (var entry in catalog.Entries)
                {
                    var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                    if (!asset) { engine.Dispose(); return; } // 还没烘焙过：保持原状。
                    bytes.Add(entry.TableId, asset.bytes);
                }
                if (!engine.Load(catalog, bytes).Succeeded) { engine.Dispose(); return; }

                var next = new NarrativeTableCatalog(engine.Database);

                Release();
                _engine = engine;
                _catalog = next;
                NovelLocalization.Install(_catalog);
                NovelSkin.Install(_catalog);
                NovelLocalization.RefreshUiText();
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, "编辑期多语言未装配（不影响运行期）：" + ex.Message);
            }
        }

        /// <summary>重新读一次导表产物并重刷 TMPEx 显示；改完配表后用它。</summary>
        [MenuItem("Ember/视觉小说/重刷 TMPEx 多语言显示", priority = 123)]
        internal static void Reinstall()
        {
            Install();
            var localizer = TextLocalization.Localizer;
            if (localizer == null)
            {
                EmberDebug.LogWarning(TAG, "仍未装配：请先在配置表中心烘焙导表产物。");
                return;
            }
            var languages = new List<string>(localizer.Languages);
            EmberDebug.LogInit(TAG, $"编辑期多语言已装配，可选语言 {string.Join("、", languages)}。");
        }

        #endregion
    }
}
