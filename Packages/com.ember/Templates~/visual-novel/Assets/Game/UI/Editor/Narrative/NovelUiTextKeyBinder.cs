using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ember.Basic;
using Ember.UIExtension;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    /// <summary>
    /// UI 静态文案的多语言 Key 清单与核对工具（口径见
    /// <c>Assets/Game/Documentation/TemplateSkills/ember-vn-import-story/references/localization.md</c>）。
    ///
    /// <para><b>本工具只核对，不做组件替换。</b>清单里的 45 个控件已经挂好 Key，
    /// 用 <c>Ember/视觉小说/检查 UI 多语言 Key（不改动）</c> 可随时验证；
    /// 以后新增文案要挂 Key，请按口径文档在 TMP 的 Inspector 右上角三点菜单里选
    /// 「替换为 TMPEx（多语言文本）」，再在下面的清单里补一行。</para>
    ///
    /// <para><b>为什么不做自动替换：</b>试过两条自动路径，都会破坏预制体——
    /// EUI 自带的 <c>EUIComponentReplaceUtility.Replace</c>（Instantiate 隐藏备份 +
    /// <c>Undo.DestroyObjectImmediate</c>），以及自写的 <c>DestroyImmediate</c> + <c>AddComponent</c>，
    /// 在 <c>LoadPrefabContents</c> 的批量流程里都会让编辑器崩溃或写坏资源：实测保存后组件
    /// <c>m_Script</c> 仍是 TextMeshPro 的 GUID、<c>m_EditorClassIdentifier</c> 变成残缺字符串、
    /// <c>_key</c> 没有序列化。替换必须在 Inspector 的上下文里做。</para>
    /// </summary>
    internal static class NovelUiTextKeyBinder
    {
        #region 内部参数

        private const string TAG = "Novel.UiText";

        private const string Main = "Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPanel.prefab";
        private const string Setting = "Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPanel.prefab";
        private const string Reader = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
        private const string ChoiceItem = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelChoiceItem.prefab";
        private const string ReadingMenu = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReadingMenuPage.prefab";
        private const string History = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelHistoryPage.prefab";
        private const string Font = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelFontPage.prefab";
        private const string SavePage = "Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/EUINovelSavePage.prefab";
        private const string SaveSlot = "Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/EUINovelSaveSlotItem.prefab";

        /// <summary>
        /// 静态文案清单（预制体 + 相对根节点路径 + Key）。
        /// <para>阅读页的 <c>ChoiceTemplate</c> 与存档页的 <c>SlotTemplate</c> 是
        /// <c>EUINovelChoiceItem</c> / <c>EUINovelSaveSlotItem</c> 的嵌套预制体实例，
        /// 所以那两个控件的 Key 挂在被嵌套的 Item 预制体上，宿主实例自动继承。</para>
        /// </summary>
        private static readonly Assignment[] Assignments =
        {
            // 主菜单
            new Assignment(Main, "Animator/EUISafeArea/Center/TitleText", "ui.main.Title"),
            new Assignment(Main, "Animator/EUISafeArea/Center/MenuButtons/m_Btn_Start/Text", "ui.main.Start"),
            new Assignment(Main, "Animator/EUISafeArea/Center/MenuButtons/NovelContinue/Label", "ui.main.Continue"),
            new Assignment(Main, "Animator/EUISafeArea/Center/MenuButtons/NovelLoad/Label", "ui.main.Load"),
            new Assignment(Main, "Animator/EUISafeArea/Center/MenuButtons/m_Btn_Settings/Text", "ui.main.Settings"),
            new Assignment(Main, "Animator/EUISafeArea/Center/MenuButtons/NovelQuit/Text", "ui.main.Quit"),

            // 设置面板（m_Txt_NowScene 故意不在清单里：它由 EUISettingPage.OnOpen 在运行期写成
            // 「当前场景：主界面」这类拼接文本，挂 Key 会在切语言刷新时被覆盖回去）
            new Assignment(Setting, "Animator/EUISafeArea/Center/PanelBg/TitleText", "ui.setting.Title"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/PanelBg/m_Btn_Close/Text", "ui.setting.Close.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/Title", "ui.setting.ReadingAndAudio.Title"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelTextSpeedLabel", "ui.setting.TextSpeed.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelAutoIntervalLabel", "ui.setting.AutoInterval.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelBgmVolumeLabel", "ui.setting.BgmVolume.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelSfxVolumeLabel", "ui.setting.SfxVolume.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelVoiceVolumeLabel", "ui.setting.VoiceVolume.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelShakePreferenceLabel", "ui.setting.ShakePreference.Label"),
            new Assignment(Setting, "Animator/EUISafeArea/Center/NovelPreferences/NovelFlashPreferenceLabel", "ui.setting.FlashPreference.Label"),

            // 阅读页
            new Assignment(Reader, "Animator/EUISafeArea/Center/Saves/Label", "ui.reader.Saves.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/QuickSave/Label", "ui.reader.QuickSave.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/QuickLoad/Label", "ui.reader.QuickLoad.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/ReadingControls/Settings/Label", "ui.reader.Settings.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/ReadingControls/History/Label", "ui.reader.History.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/ReadingControls/HideDialogue/Label", "ui.reader.HideDialogue.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/ReadingControls/Skip/Label", "ui.reader.Skip.Label"),
            new Assignment(Reader, "Animator/EUISafeArea/Center/ReadingControls/Menu/Label", "ui.reader.Menu.Label"),

            // 选项 Item（阅读页 ChoiceTemplate 的来源）
            new Assignment(ChoiceItem, "Select/Label", "ui.reader.Choice.Label"),

            // 阅读菜单
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/Title", "ui.menu.Title"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/Saves/Label", "ui.menu.Saves.Label"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/QuickSave/Label", "ui.menu.QuickSave.Label"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/QuickLoad/Label", "ui.menu.QuickLoad.Label"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/Settings/Label", "ui.menu.Settings.Label"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/ReadSkip/Label", "ui.menu.ReadSkip.Label"),
            new Assignment(ReadingMenu, "Animator/EUISafeArea/Center/Panel/ReturnMenu/Label", "ui.menu.ReturnMenu.Label"),

            // 历史
            new Assignment(History, "Animator/EUISafeArea/Center/HistoryPanel/Title", "ui.history.Title"),
            new Assignment(History, "Animator/EUISafeArea/Center/HistoryPanel/Close/Label", "ui.history.Close.Label"),
            new Assignment(History, "Animator/EUISafeArea/Center/HistoryPanel/Saves/Label", "ui.history.Saves.Label"),

            // 字号弹窗
            new Assignment(Font, "Animator/EUISafeArea/Center/Panel/Title", "ui.font.Title"),
            new Assignment(Font, "Animator/EUISafeArea/Center/Panel/Small/Label", "ui.font.Small.Label"),
            new Assignment(Font, "Animator/EUISafeArea/Center/Panel/Medium/Label", "ui.font.Medium.Label"),
            new Assignment(Font, "Animator/EUISafeArea/Center/Panel/Large/Label", "ui.font.Large.Label"),
            new Assignment(Font, "Animator/EUISafeArea/Center/Panel/Preview", "ui.font.Preview"),

            // 存档页
            new Assignment(SavePage, "Animator/EUISafeArea/Center/Panel/Title", "ui.save.Title"),
            new Assignment(SavePage, "Animator/EUISafeArea/Center/Panel/Close/Label", "ui.save.Close.Label"),

            // 存档槽位 Item（存档页 SlotTemplate 的来源）
            new Assignment(SaveSlot, "Write/Label", "ui.save.Slot.Write.Label"),
            new Assignment(SaveSlot, "Read/Label", "ui.save.Slot.Read.Label")
        };

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>一条静态文案的绑定意图：预制体 + 相对根节点路径 + 多语言 Key。</summary>
        private sealed class Assignment
        {
            internal readonly string Prefab, Path, Key;
            internal Assignment(string prefab, string path, string key) { Prefab = prefab; Path = path; Key = key; }
        }

        private static List<string> OrderedPrefabs()
        {
            var order = new List<string>();
            foreach (var assignment in Assignments)
                if (!order.Contains(assignment.Prefab)) order.Add(assignment.Prefab);
            return order;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 核对清单里的每个控件：是否已是 TMPEx、Key 是否与清单一致、字体与材质是否还在。只读。
        /// </summary>
        internal static string Report()
        {
            var all = new StringBuilder();
            int ok = 0, bad = 0;
            foreach (string prefab in OrderedPrefabs())
            {
                if (!File.Exists(prefab)) { all.AppendLine(Path.GetFileName(prefab) + "：预制体不存在"); continue; }

                GameObject root = PrefabUtility.LoadPrefabContents(prefab);
                try
                {
                    int fileOk = 0, fileBad = 0;
                    foreach (var assignment in Assignments)
                    {
                        if (assignment.Prefab != prefab) continue;

                        Transform target = root.transform.Find(assignment.Path);
                        var tmp = target == null ? null : target.GetComponent<TextMeshProUGUI>();
                        var asEx = tmp as TMPEx;
                        bool good = asEx != null && string.Equals(asEx.Key, assignment.Key, StringComparison.Ordinal) &&
                                    tmp.font != null && tmp.fontSharedMaterial != null;
                        if (good) { fileOk++; continue; }

                        fileBad++;
                        all.AppendLine(tmp == null
                            ? $"    x {assignment.Path}：找不到 TMP 文本"
                            : $"    x {assignment.Path}：{tmp.GetType().Name} Key={(asEx == null ? "<非 TMPEx>" : asEx.Key)} 期望 {assignment.Key}");
                    }
                    ok += fileOk; bad += fileBad;
                    all.AppendLine($"{Path.GetFileName(prefab)}：{fileOk}/{fileOk + fileBad}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            all.AppendLine(bad == 0 ? $"全部通过：{ok} 个控件都已挂 Key。" : $"合计 {ok} 通过、{bad} 待处理。");
            EmberDebug.Log(TAG, all.ToString());
            return all.ToString();
        }

        /// <summary>只报告核对结果，不修改任何预制体。</summary>
        [MenuItem("Ember/视觉小说/检查 UI 多语言 Key（不改动）", priority = 122)]
        internal static void ReportFromMenu() => Report();

        #endregion
    }
}
