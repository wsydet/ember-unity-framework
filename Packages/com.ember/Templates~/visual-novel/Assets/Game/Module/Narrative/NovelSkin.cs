using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.UI;
using Game.Table;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative
{
    /// <summary>
    /// UI 皮肤运行期应用器。一套皮肤就是若干「页面 + 锚点 + 图片节点 → 另一张图」的覆盖记录，
    /// 按小说（剧情）赋值，本期只覆盖图片，位置与大小仍由各小说自己在布局窗口里调。
    /// <para><b>为什么是运行期覆盖而不是多套 Prefab：</b>正式 UI 由 EUI 开发中心生成，
    /// 复制一套 Prefab 就要重新生成 Binding，既昂贵又容易漂移。这里只在页面绑定完成后按名换图，
    /// 不修改任何 Prefab、Binding 或布局。</para>
    /// <para><b>定位方式：</b>锚点取 EUI 绑定控件名（<c>ControlMap</c> 的键），节点是锚点下的相对路径；
    /// 两者都可以留空，分别表示"页面根"与"锚点自身"。</para>
    /// </summary>
    public static class NovelSkin
    {
        #region 内部参数
        private static NarrativeTableCatalog _catalog;
        private static string _skinId;
        // 已解析对应的剧情标识：小说切换后必须重新解析，否则会把上一部小说的皮肤继续套下去。
        private static string _resolvedStoryId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>当前生效的皮肤标识；没有命中时为 null。</summary>
        public static string CurrentSkinId => _skinId;

        /// <summary>是否已接入皮肤配表。</summary>
        public static bool IsInstalled => _catalog != null && _catalog.SkinsReady;

        /// <summary>安装配表并清掉已解析的皮肤；缺表时按未接入处理。</summary>
        [NoGC]
        public static void Install(NarrativeTableCatalog catalog)
        {
            _catalog = catalog;
            _skinId = null;
            _resolvedStoryId = null;
        }

        /// <summary>卸载：之后所有页面都沿用 Prefab 外观。</summary>
        [NoGC]
        public static void Uninstall()
        {
            _catalog = null;
            _skinId = null;
            _resolvedStoryId = null;
        }

        /// <summary>
        /// 按当前小说重新解析皮肤并缓存。小说标识取 <see cref="NarrativeLibrarySO.Current"/>，
        /// 因此主界面与阅读页看到的是同一套皮肤。
        /// </summary>
        [HasGC]
        public static bool RefreshCurrent()
        {
            string storyId = CurrentStoryId();
            _resolvedStoryId = storyId;
            _skinId = null;
            if (!IsInstalled || string.IsNullOrWhiteSpace(storyId)) return false;
            return _catalog.TryGetSkinId(storyId, out _skinId);
        }

        /// <summary>当前小说标识；没有当前小说时返回 null。</summary>
        [HasGC]
        public static string CurrentStoryId()
        {
            var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
            return library && library.Current ? library.Current.StoryId : null;
        }

        /// <summary>
        /// 把皮肤套到某个页面上，返回真正命中的图片节点数量。
        /// 在页面逻辑的 <c>OnInit</c>（ControlMap 已就绪、页面尚未可见）里调用最稳妥。
        /// </summary>
        /// <param name="skinId">显式指定皮肤；留空表示按当前小说解析（正式流程用这个）。</param>
        [HasGC]
        public static int Apply(EUILogic logic, string page, string skinId = null)
        {
            if (logic == null || !IsInstalled || string.IsNullOrWhiteSpace(page)) return 0;
            string target = skinId;
            if (string.IsNullOrEmpty(target))
            {
                // 小说换了就重新解析；同一部小说内不重复查表。
                if (_resolvedStoryId != CurrentStoryId()) RefreshCurrent();
                target = _skinId;
            }
            if (string.IsNullOrEmpty(target)) return 0;
            Transform root = Root(logic);
            if (root == null) return 0;
            IReadOnlyList<NovelSkinSpriteRow> rows = _catalog.SkinSprites;
            int applied = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                NovelSkinSpriteRow row = rows[i];
                if (row == null || row.SkinId != target || row.Page != page) continue;
                if (ApplyRow(logic, root, row)) applied++;
            }
            return applied;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>页面根：优先用 EUI 页面根，退化为控件共同祖先，最后什么都不做。</summary>
        private static Transform Root(EUILogic logic)
        {
            if (logic.Page != null && logic.Page.GameObject) return logic.Page.GameObject.transform;
            if (logic.ControlMap == null) return null;
            foreach (var pair in logic.ControlMap)
                if (pair.Value) return pair.Value.transform.root == pair.Value.transform ? pair.Value.transform : TopMost(pair.Value.transform);
            return null;
        }

        /// <summary>沿父级上溯到最靠近场景根的 Transform，供没有 Page 引用的 Item 使用。</summary>
        private static Transform TopMost(Transform start)
        {
            Transform current = start;
            while (current.parent != null) current = current.parent;
            return current;
        }

        private static bool ApplyRow(EUILogic logic, Transform root, NovelSkinSpriteRow row)
        {
            Transform target = Anchor(logic, root, row.Control);
            if (target == null) return false;
            if (!string.IsNullOrEmpty(row.Node))
            {
                target = target.Find(row.Node);
                if (target == null) return false;
            }
            var image = target.GetComponent<Image>();
            if (image == null) return false;
            var sprite = Resources.Load<Sprite>(row.SpritePath);
            if (sprite == null || image.sprite == sprite) return false;
            image.sprite = sprite;
            return true;
        }

        /// <summary>锚点：先查绑定控件名，再退回按名字从页面根查找；都为空时用页面根。</summary>
        private static Transform Anchor(EUILogic logic, Transform root, string control)
        {
            if (string.IsNullOrEmpty(control)) return root;
            if (logic.ControlMap != null && logic.ControlMap.TryGetValue(control, out Component bound) && bound)
                return bound.transform;
            return root.Find(control);
        }
        #endregion
    }
}
