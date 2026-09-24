using System;
using System.Reflection;
using Ember.Basic;
using Ember.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>与引导编辑器相同：读取编译后的模块声明，域重载后同步菜单，无逐帧扫描。</summary>
    internal static class NarrativeEditorAvailability
    {
        #region 内部参数
        private const BindingFlags MENU_FLAGS = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly EmberModuleAttribute Module = typeof(NarrativeModule).GetCustomAttribute<EmberModuleAttribute>(false);
        internal static bool Enabled => Module?.Enabled == true;
        internal static bool Visible => Enabled && NarrativeGraphModel.IsTemplateActive();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void ScheduleRefresh()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.delayCall -= RefreshMenus;
            EditorApplication.delayCall += RefreshMenus;
        }

        private static void OnProjectChanged()
        {
            NarrativeGraphModel.InvalidateTemplateCache();
            EditorApplication.delayCall -= RefreshMenus;
            EditorApplication.delayCall += RefreshMenus;
        }

        private static void RefreshMenus()
        {
            if (!Visible)
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<NarrativeGraphWindow>()) window.Close();
            }
            try
            {
                var menu = typeof(Menu);
                var exists = menu.GetMethod("MenuItemExists", MENU_FLAGS, null, new[] { typeof(string) }, null);
                var remove = menu.GetMethod("RemoveMenuItem", MENU_FLAGS, null, new[] { typeof(string) }, null);
                var add = menu.GetMethod("AddMenuItem", MENU_FLAGS, null,
                    new[] { typeof(string), typeof(string), typeof(bool), typeof(int), typeof(Action), typeof(Func<bool>) }, null);
                if (exists == null || remove == null || add == null) throw new MissingMethodException("当前 Unity 版本缺少动态菜单 API。");
                void Register(string path, Action action, Func<bool> validate = null)
                {
                    bool registered = (bool)exists.Invoke(null, new object[] { path });
                    if (registered) remove.Invoke(null, new object[] { path });
                    if (!Visible) return;
                    add.Invoke(null, new object[] { path, string.Empty, false, 1000,
                        (Action)(() => { if (Visible) action(); }), (Func<bool>)(() => Visible && (validate?.Invoke() ?? true)) });
                }
                const string legacy = "Ember/Visual Novel/Gameplay 主UI布局";
                if ((bool)exists.Invoke(null, new object[] { legacy })) remove.Invoke(null, new object[] { legacy });
                Register("Ember/视觉小说/当前小说", NarrativeLibraryWindow.Open);
                Register("Ember/视觉小说/流程编辑与运行观察", NarrativeGraphWindow.Open);
                Register("Ember/视觉小说/Gameplay 主UI布局", Game.UI.Editor.NovelGameplayLayoutWindow.Open, Game.UI.Editor.NovelGameplayLayoutWindow.CanOpen);
                Register("Ember/视觉小说/从所选入口开始新游戏（Play 主菜单）", NarrativeEntryLauncher.Start, NarrativeEntryLauncher.CanStart);
                Register("Assets/Create/Game/Visual Novel/剧情", () => CreateAsset<NarrativeStorySO>("Story"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/章节", () => CreateAsset<NarrativeChapterSO>("Chapter"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/对话段", () => CreateAsset<NarrativeDialogueSO>("Dialogue"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/选择", () => CreateAsset<NarrativeChoiceSO>("Choice"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/条件分流", () => CreateAsset<NarrativeBranchSO>("Branch"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/章节出口", () => CreateAsset<NarrativeChapterExitSO>("Exit"), CanCreate);
                Register("Assets/Create/Game/Visual Novel/结局", () => CreateAsset<NarrativeEndingSO>("Ending"), CanCreate);
            }
            catch (Exception exception)
            {
                EmberDebug.LogWarning("Game.Narrative.Editor", "小说编辑器菜单同步失败：" + (exception.InnerException ?? exception).Message);
            }
        }

        private static bool CanCreate() => Enabled && !EditorApplication.isPlayingOrWillChangePlaymode && NarrativeGraphModel.IsTemplateActive();
        private static void CreateAsset<T>(string name) where T : ScriptableObject
        {
            RequireEnabled();
            if (!CanCreate()) return;
            ProjectWindowUtil.CreateAsset(ScriptableObject.CreateInstance<T>(), name + ".asset");
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void RequireEnabled()
        {
            if (!Enabled) throw new InvalidOperationException("NarrativeModule 未启用。请修改 EmberModule.Enabled 并等待脚本重载。");
        }
        #endregion
    }
}
