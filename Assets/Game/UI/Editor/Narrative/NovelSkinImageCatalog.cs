using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ember.Basic;
using Ember.UIExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>
    /// 皮肤可覆盖图片清单导出。
    /// <para><b>为什么不是"差异捕获"：</b>皮肤覆盖表按「页面 + 绑定控件 + 相对节点」寻址，
    /// 而在布局窗口里改外观只覆盖阅读页与阅读菜单两套 Prefab，主界面 <c>EUIMainPanel</c> 根本不在那个窗口里，
    /// 靠差异快照既覆盖不全、也说不清每一行该怎么写。这里改为把每个页面上**所有可被皮肤寻址的图片**列出来，
    /// 作者照着清单往 novel_skin_sprites 里加行即可。</para>
    /// <para><b>硬约束：</b>覆盖目标必须是项目 Resources 下的图片。Unity 内置的 <c>UISprite</c>
    /// （<c>Resources/unity_builtin_extra</c>）无法用路径寻址，清单里会标成「内置，不可覆盖」。</para>
    /// </summary>
    public static class NovelSkinImageCatalog
    {
        #region 内部参数
        /// <summary>参与皮肤覆盖的页面 Prefab；page 名就是 Prefab 文件名，与运行期传的页面名一致。</summary>
        private static readonly string[] PREFABS =
        {
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReadingMenuPage.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelHistoryPage.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelFontPage.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelChoiceItem.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelPortraitItem.prefab",
            "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelBackgroundItem.prefab",
            "Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/EUINovelSavePage.prefab",
            "Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/EUINovelSaveSlotItem.prefab",
            "Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPanel.prefab"
        };
        private const string RESOURCES_ROOT = "Assets/GameResource/Resources/";
        private const string OUTPUT_PATH = ".utmp/novel-skin/image-candidates.md";
        private const string TAG = LogTags.Game + "." + nameof(NovelSkinImageCatalog);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>把全部可覆盖图片写入 .utmp，并把待填的 CSV 行放进剪贴板。</summary>
        [MenuItem("Ember/视觉小说/导出皮肤可覆盖图片清单")]
        public static void Export()
        {
            var report = new StringBuilder();
            var csv = new StringBuilder("id,skinId,page,control,node,spritePath\r\n");
            int addressable = 0, builtin = 0, empty = 0;
            report.AppendLine("# 皮肤可覆盖图片清单");
            report.AppendLine();
            report.AppendLine("覆盖行的写法：`id` 约定 `skinId.page.control.node`，`control` 留空表示页面根，`node` 留空表示锚点自身。");
            report.AppendLine("`spritePath` 必须是项目 Resources 下的图片；内置图（Unity 默认 UISprite）不能用路径表示，只能作为被替换方。");
            report.AppendLine();
            foreach (string path in PREFABS)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;
                string page = Path.GetFileNameWithoutExtension(path);
                var binding = prefab.GetComponentInChildren<EUIBinding>(true);
                var bound = BoundTransforms(binding);
                report.AppendLine("## " + page);
                report.AppendLine();
                report.AppendLine("| 控件 | 节点 | 当前图片 | 路径 |");
                report.AppendLine("|---|---|---|---|");
                foreach (var image in prefab.GetComponentsInChildren<Image>(true))
                {
                    string control = ResolveControl(image.transform, bound, out Transform anchor);
                    string node = ReferenceEquals(anchor, image.transform) ? string.Empty
                        : AnimationUtility.CalculateTransformPath(image.transform, anchor);
                    string spritePath = ResourcesPath(image.sprite);
                    // 三种状态必须分开：项目图片可整行替换；空图能直接指定新图（最常见的皮肤用法）；
                    // 内置图既反解不出路径，也不能当作覆盖值，只能换成项目图片。
                    string state = !image.sprite ? "空图，可直接指定新图"
                        : spritePath != null ? spritePath : "内置，不可寻址";
                    if (!image.sprite) empty++;
                    else if (spritePath != null) addressable++;
                    else builtin++;
                    report.AppendLine("| " + (string.IsNullOrEmpty(control) ? "（页面根）" : control) + " | " +
                        (string.IsNullOrEmpty(node) ? "（锚点自身）" : node) + " | " +
                        (image.sprite ? image.sprite.name : "（无）") + " | " + state + " |");
                    if (spritePath != null)
                        csv.AppendLine(string.Join(",", "SKINID." + page + "." + control + "." + node,
                            "SKINID", page, control, node, spritePath));
                }
                report.AppendLine();
            }
            report.AppendLine("---");
            report.AppendLine("当前为项目图片、可整行替换 " + addressable + " 项；空图可直接指定新图 " + empty +
                " 项；内置图不可寻址 " + builtin + " 项。");
            EnsureFolder(Path.GetDirectoryName(OUTPUT_PATH));
            File.WriteAllText(OUTPUT_PATH, report.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
            EditorGUIUtility.systemCopyBuffer = csv.ToString();
            EmberDebug.Log(TAG, "皮肤图片清单已写入 " + OUTPUT_PATH + "（项目图片 " + addressable + "，空图 " + empty +
                "，内置 " + builtin + "）；待填 CSV 行已复制到剪贴板，把 SKINID 换成皮肤标识即可。");
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(OUTPUT_PATH);
            if (asset) EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 描述一个图片节点对应的皮肤覆盖行。page 取 Prefab 根名，control 取最近的 EUI 绑定控件名
        /// （没有就用页面根），node 是锚点下的相对路径。spritePath 是该图当前的项目 Resources 路径；
        /// 当前图是 Unity 内置资源或该节点没有图时为 null（内置图无法用路径表示，只能当被替换方）。
        /// </summary>
        /// <remarks>与 Export 共用同一套寻址实现，两处不会分叉。</remarks>
        public static bool TryDescribe(Transform target, out string page, out string control, out string node, out string spritePath)
        {
            page = control = node = spritePath = null;
            if (target == null) return false;
            Transform root = target.root;
            page = root == null ? null : root.name;
            if (string.IsNullOrEmpty(page)) return false;
            var binding = root.GetComponentInChildren<EUIBinding>(true);
            control = ResolveControl(target, BoundTransforms(binding), out Transform anchor);
            node = ReferenceEquals(anchor, target) ? string.Empty : AnimationUtility.CalculateTransformPath(target, anchor);
            var image = target.GetComponent<Image>();
            spritePath = image ? ResourcesPath(image.sprite) : null;
            return true;
        }

        /// <summary>按约定拼出 novel_skin_sprites 的一行 CSV：id 为 skinId.page.control.node。</summary>
        public static string BuildRow(string skinId, string page, string control, string node, string spritePath)
            => string.Join(",", skinId + "." + page + "." + control + "." + node, skinId, page, control, node, spritePath);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static Dictionary<Transform, string> BoundTransforms(EUIBinding binding)
        {
            var map = new Dictionary<Transform, string>();
            if (binding == null || binding.Bindings == null) return map;
            foreach (var entry in binding.Bindings)
                if (entry.GameObject && !map.ContainsKey(entry.GameObject.transform)) map[entry.GameObject.transform] = entry.Name;
            return map;
        }

        /// <summary>从图片节点向上找最近的绑定控件；找不到就用页面根，anchor 返回锚点自身。</summary>
        private static string ResolveControl(Transform target, Dictionary<Transform, string> bound, out Transform anchor)
        {
            for (Transform current = target; current != null; current = current.parent)
                if (bound.TryGetValue(current, out string name)) { anchor = current; return name; }
            anchor = target.root;
            return string.Empty;
        }

        /// <summary>把 Sprite 反解成 Resources 下的相对路径；不是项目 Resources 资源时返回 null。</summary>
        private static string ResourcesPath(Sprite sprite)
        {
            if (!sprite) return null;
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(RESOURCES_ROOT, StringComparison.Ordinal)) return null;
            string relative = assetPath.Substring(RESOURCES_ROOT.Length);
            int dot = relative.LastIndexOf('.');
            if (dot > 0) relative = relative.Substring(0, dot);
            // 反解后必须能取回同一张图，否则运行期按路径加载会拿到别的对象。
            return Resources.Load<Sprite>(relative) == sprite ? relative : null;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || Directory.Exists(folder)) return;
            EnsureFolder(Path.GetDirectoryName(folder));
            Directory.CreateDirectory(folder);
        }
        #endregion
    }
}
