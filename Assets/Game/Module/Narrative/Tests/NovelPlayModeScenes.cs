using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 视觉小说 Play Mode 用例的场景脚手架。
    ///
    /// 背景：Test Runner 跑 EditMode 时会用 <c>EditorSceneManager.NewScene</c> 建一个
    /// 未保存的引导场景（Untitled），这些用例又用 <c>EditorSceneManager.OpenScene</c>
    /// 把它换成 FrameworkScene。只要过程中留下任何脏场景，
    /// <c>FrameworkSceneBootstrapper.SaveAndCleanScenes</c>（进 Play 前）与
    /// Test Runner 的 <c>SaveModifiedSceneTask</c>（每次运行开始时）都会调用
    /// <c>SaveCurrentModifiedScenesIfUserWantsTo</c>，弹出「Scene(s) Have Been Modified」
    /// 确认框；无人值守运行时运行会被挂住。
    ///
    /// 这里统一三件事：进入 Play 前先丢弃未保存的临时场景、退出 Play 时只在确实处于
    /// Play Mode 时才 yield ExitPlayMode、退出后同样清理，从而不再产生保存确认框，
    /// 也避免 <c>ExitPlayMode</c> 抛「Editor is already in EditMode」。
    /// </summary>
    internal static class NovelPlayModeScenes
    {
        #region 内部参数

        /// <summary>视觉小说模板的启动场景，同时是 Build Settings 的首场景。</summary>
        internal const string FrameworkScenePath = "Assets/Game/Scenes/FrameworkScene.unity";

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 打开 FrameworkScene 并进入 Play Mode。
        /// 只应在 EditMode 用例里调用，且随后必须配对 <see cref="ExitIfPlaying"/>。
        ///
        /// 这里比裸的 <c>yield return new EnterPlayMode()</c> 多做两件事，都是为了在
        /// 全量运行里可靠地拿到运行期：
        /// <list type="number">
        /// <item>进 Play 之前先等资源管线结算。前面的用例只要做过
        /// <c>AssetDatabase.CreateAsset / MoveAsset / DeleteAsset</c>（删掉临时夹具也算），
        /// Unity 会在 Play Mode 过渡的瞬间停下导入（Editor.log 里的 <c>StopAssetImportingV2</c>），
        /// 过渡会被打断。</item>
        /// <item>进 Play 之后等它真正落地，仍没落地就显式驱动一次。
        /// 打断发生时 <c>EnterPlayMode</c> 已经返回，但 <c>EditorApplication.isPlaying</c>
        /// 会停留在 false，用例随即拿到空的启动状态（表现为"主菜单未就绪"或断言失败）。</item>
        /// </list>
        /// </summary>
        internal static IEnumerator EnterFrameworkScenePlayMode()
        {
            DiscardUnsavedScenes();
            for (int i = 0; i < 300 && (EditorApplication.isUpdating || EditorApplication.isCompiling); i++) yield return null;
            yield return null;

            EditorSceneManager.OpenScene(FrameworkScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();

            // 域重载需要时间，先给它足够帧数落地。
            for (int i = 0; i < 180 && !EditorApplication.isPlaying; i++) yield return null;

            // 仍没进 Play，说明过渡被打断了：解除装配锁后显式驱动一次。
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.UnlockReloadAssemblies();
                EditorApplication.isPlaying = true;
                for (int i = 0; i < 180 && !EditorApplication.isPlaying; i++) yield return null;
            }

            Assert.IsTrue(EditorApplication.isPlaying,
                "进入 Play Mode 失败：资源管线打断了过渡，用例无法在运行期验证。");
        }

        /// <summary>
        /// 仅在确实处于（或正在进入）Play Mode 时退出，并清掉运行期间可能留下的未保存场景。
        /// 用它替代裸的 <c>yield return new ExitPlayMode()</c>：Play Mode 已经结束时
        /// <c>ExitPlayMode</c> 会抛异常，把用例带崩成连锁失败。
        ///
        /// <para><b>只能用于测试方法体。</b>它本身是嵌套枚举器，而
        /// <c>[UnitySetUp]</c>/<c>[UnityTearDown]</c> 的框架实现明确禁止嵌套枚举器
        /// yield 退出指令（报 <c>Nested enumerators are not allowed to yield ExitPlayMode</c>）。
        /// 在 SetUp/TearDown 里请写成
        /// <c>DiscardUnsavedScenes(); if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode(); DiscardUnsavedScenes();</c>，
        /// 让指令由方法本身直接 yield。</para>
        /// </summary>
        internal static IEnumerator ExitIfPlaying()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            DiscardUnsavedScenes();
        }

        /// <summary>
        /// 丢弃未保存的临时场景（不弹保存框、不触碰项目场景），
        /// 让后续的 OpenScene / 进 Play Mode 都不会触发保存确认。
        /// </summary>
        internal static void DiscardUnsavedScenes()
        {
            // Play Mode 中不允许改场景集合，交给 ExitPlayMode 之后再清理。
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            for (int i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(scene.path)) continue;

                if (EditorSceneManager.sceneCount > 1)
                {
                    // CloseScene 不询问是否保存；脏内容随临时场景一起丢弃。
                    EditorSceneManager.CloseScene(scene, true);
                }
                else if (scene.isDirty)
                {
                    // 只剩这一个临时场景时不能直接关掉：换成新的空场景，等价于放弃改动。
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        #endregion
    }
}
