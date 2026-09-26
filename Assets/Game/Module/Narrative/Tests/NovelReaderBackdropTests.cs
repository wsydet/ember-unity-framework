using System.Linq;
using Ember.UIExtension;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Narrative.Tests
{
    /// <summary>
    /// 阅读页不透明底板与「揭幕只作用于内容容器」的结构不变量。
    ///
    /// 背景、章节卡和页面过渡原本都可能让整屏出现「什么都没有」的一帧：
    /// 背景 Show 从全透明淡入、章节卡退场把整屏底色渐隐到 0、以及页面级淡入把整页带走。
    /// 这三条都靠阅读页根节点上的 <c>Backdrop</c> 底板修掉，所以这些结构约束必须由用例守住——
    /// 底板被挪进过渡容器、被调成半透明、或被排到背景之上，缺陷就会静默回来。
    /// </summary>
    public sealed class NovelReaderBackdropTests
    {
        #region 内部参数
        private const string READER = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
        /// <summary>底板节点名；NovelSkin 的行也按这个名字寻址（control 留空 + node = Backdrop）。</summary>
        private const string BACKDROP = "Backdrop";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static GameObject LoadReader() => AssetDatabase.LoadAssetAtPath<GameObject>(READER);
        private static Image BackdropOf(GameObject root) =>
            root.transform.Find(BACKDROP)?.GetComponent<Image>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void ReaderPageKeepsAnOpaqueFullScreenBackdropUnderTheBackgroundLayer()
        {
            var prefab = LoadReader();
            Assert.IsNotNull(prefab, "缺少阅读页 Prefab：" + READER);
            var backdrop = prefab.transform.Find(BACKDROP);
            Assert.IsNotNull(backdrop, "阅读页根节点下缺少不透明底板节点 " + BACKDROP +
                "：背景淡入、章节卡渐隐和页面过渡都会因此透出页面之后的画面。");

            Assert.AreEqual(0, backdrop.GetSiblingIndex(), "底板必须是根节点的第一个子节点，才会画在 Background 之下");
            var background = prefab.transform.Find("Background");
            Assert.IsNotNull(background);
            Assert.Less(backdrop.GetSiblingIndex(), background.GetSiblingIndex(),
                "底板必须排在背景层之前（uGUI 按层级顺序绘制，索引小的先画）");

            var image = backdrop.GetComponent<Image>();
            Assert.IsNotNull(image, "底板必须是 Image，才能提供不透明基色");
            Assert.AreEqual(1f, image.color.a, .001f, "底板必须完全不透明，否则等于没有底板");
            Assert.IsFalse(image.raycastTarget, "底板不能拦截点击，否则推进和按钮都会失效");

            var rect = (RectTransform)backdrop;
            Assert.AreEqual(Vector2.zero, rect.anchorMin, "底板必须铺满整页");
            Assert.AreEqual(Vector2.one, rect.anchorMax, "底板必须铺满整页");
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
        }

        [Test]
        public void ReaderPageBackdropStaysOutsideTheTransitionContainer()
        {
            var prefab = LoadReader();
            var backdrop = prefab.transform.Find(BACKDROP);
            Assert.IsNotNull(backdrop, "缺少底板节点 " + BACKDROP);

            // 框架（EUIPage.GetTransitionCanvasGroup）优先把 Animator 节点的 CanvasGroup 当作过渡层；
            // 该层缺 CanvasGroup 时会退化为淡入整页根 CanvasGroup，底板就会跟着一起被淡掉。
            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, "阅读页需要一个 Animator 节点作为内容容器，否则框架会退化为淡入整页");
            Assert.IsNotNull(animator.GetComponent<CanvasGroup>(),
                "Animator 节点必须带 CanvasGroup：它是框架的过渡层，缺了就会改成淡入整页根 CanvasGroup");

            Assert.IsFalse(backdrop.IsChildOf(animator.transform),
                "底板不能放在过渡容器（Animator 子树）里，否则揭幕会连底板一起淡出，页面后的画面仍然可见");
            Assert.IsFalse(backdrop.IsChildOf(prefab.transform.Find("Background")),
                "底板必须是背景层的兄弟节点：放进 Background 会随背景的 Hide/Clear 一起消失");
            Assert.IsTrue(animator.transform.IsChildOf(prefab.transform),
                "过渡容器必须仍在页面根之下，根 CanvasGroup 继续负责暂停与关闭总闸");
        }

        [Test]
        public void ReaderPageBindingStillResolvesWithTheBackdropNode()
        {
            var prefab = LoadReader();
            var binding = prefab.GetComponent<EUIBinding>();
            Assert.IsNotNull(binding);
            Assert.AreEqual("EUINovelReaderPage", binding.ClassName);
            // 底板刻意不进 ControlMap：它是页面基色，不是可绑定的业务控件。
            // 皮肤仍能按 control 留空 + node = Backdrop 寻址它。
            Assert.IsFalse(binding.Bindings.Any(entry => entry.GameObject && entry.GameObject.name == BACKDROP),
                "底板不应登记为 EUI 绑定控件");
            // 新增节点不能把已生成的绑定打散：生成代码取用的控件名必须仍然全部可解析。
            var names = binding.Bindings.Where(entry => entry.GameObject).Select(entry => entry.Name).ToArray();
            foreach (var required in new[] { "Background", "Left", "Center", "Right", "TitleLayout", "TitleBody", "ReadingShading", "Dialogue", "Body", "Speaker" })
                Assert.Contains(required, names, "阅读页绑定缺少控件：" + required);
        }

        [Test]
        public void ChapterCardBackdropIsOpaqueSoItNeverRevealsWhatIsBehindThePage()
        {
            var prefab = LoadReader();
            // 章节卡借 TitleLayout 的矩形与底色画在根节点层级上（见 EUINovelReaderPage.Text.ApplyTextMode），
            // 它自己就是整屏底色。底板必须比它更早绘制，这样卡片退场渐隐时透出的是底板基色。
            var title = prefab.transform.Find("TitleLayout");
            Assert.IsNotNull(title);
            var backdrop = prefab.transform.Find(BACKDROP);
            Assert.Less(backdrop.GetSiblingIndex(), title.GetSiblingIndex(),
                "底板必须排在 TitleLayout 之前，章节卡渐隐时才能露出基色而不是页面后的画面");
            var backdropAlpha = BackdropOf(prefab).color.a;
            Assert.AreEqual(1f, backdropAlpha, .001f,
                "底板必须完全不透明：章节卡退场、或节点把背景指令排在章节卡之后时，它是唯一的兜底底色");
        }
        #endregion
    }
}
