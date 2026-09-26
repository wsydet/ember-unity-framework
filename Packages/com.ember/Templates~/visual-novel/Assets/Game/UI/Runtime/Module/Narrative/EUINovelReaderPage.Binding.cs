/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelReaderPage
 * page name    : EUINovelReaderPage
 * update time  : 2026/9/26 23:49:30
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelReaderPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Background
        /// </summary>
        private UnityEngine.RectTransform Background;

        /// <summary>
        /// Left
        /// </summary>
        private UnityEngine.RectTransform Left;

        /// <summary>
        /// Center
        /// </summary>
        private UnityEngine.RectTransform Center;

        /// <summary>
        /// Right
        /// </summary>
        private UnityEngine.RectTransform Right;

        /// <summary>
        /// Animator/EUISafeArea/Center/Dialogue/Speaker
        /// </summary>
        private TMPro.TextMeshProUGUI Speaker;

        /// <summary>
        /// Animator/EUISafeArea/Center/Dialogue/Body
        /// </summary>
        private TMPro.TextMeshProUGUI Body;

        /// <summary>
        /// ReadingShading/Advance
        /// </summary>
        private UnityEngine.UI.Button Advance;

        /// <summary>
        /// Animator/EUISafeArea/Center/Choices
        /// </summary>
        private UnityEngine.RectTransform Choices;

        /// <summary>
        /// ChoiceTemplate
        /// </summary>
        private UnityEngine.RectTransform ChoiceTemplate;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Menu
        /// </summary>
        private UnityEngine.UI.Button Menu;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Settings
        /// </summary>
        private UnityEngine.UI.Button Settings;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Status
        /// </summary>
        private TMPro.TextMeshProUGUI Status;

        /// <summary>
        /// Animator/EUISafeArea/Center/Saves
        /// </summary>
        private UnityEngine.UI.Button Saves;

        /// <summary>
        /// Animator/EUISafeArea/Center/QuickSave
        /// </summary>
        private UnityEngine.UI.Button QuickSave;

        /// <summary>
        /// Animator/EUISafeArea/Center/QuickLoad
        /// </summary>
        private UnityEngine.UI.Button QuickLoad;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Auto
        /// </summary>
        private UnityEngine.UI.Button Auto;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Skip
        /// </summary>
        private UnityEngine.UI.Button Skip;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/History
        /// </summary>
        private UnityEngine.UI.Button History;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/HideDialogue
        /// </summary>
        private UnityEngine.UI.Button HideDialogue;

        /// <summary>
        /// Animator/EUISafeArea/Center/Dialogue
        /// </summary>
        private UnityEngine.RectTransform Dialogue;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls
        /// </summary>
        private UnityEngine.RectTransform ReadingControls;

        /// <summary>
        /// Animator/EUISafeArea/Center/ReadingControls/Speed
        /// </summary>
        private UnityEngine.UI.Button Speed;

        /// <summary>
        /// Animator/EUISafeArea/Center/RestoreUI
        /// </summary>
        private UnityEngine.UI.Button RestoreUI;

        /// <summary>
        /// ReadingShading
        /// </summary>
        private UnityEngine.RectTransform ReadingShading;

        /// <summary>
        /// Animator/EUISafeArea/Center/FullScreenLayout
        /// </summary>
        private UnityEngine.RectTransform FullScreenLayout;

        /// <summary>
        /// Animator/EUISafeArea/Center/FullScreenLayout/FullScreenBody
        /// </summary>
        private UnityEngine.RectTransform FullScreenBody;

        /// <summary>
        /// TitleLayout
        /// </summary>
        private UnityEngine.RectTransform TitleLayout;

        /// <summary>
        /// TitleLayout/TitleBody
        /// </summary>
        private UnityEngine.RectTransform TitleBody;

        /// <summary>
        /// ReadingShading/TitleAdvance
        /// </summary>
        private UnityEngine.RectTransform TitleAdvance;



    public override void OnBind()
    {
        base.OnBind();
            Background = ControlMap["Background"] as UnityEngine.RectTransform;
            Left = ControlMap["Left"] as UnityEngine.RectTransform;
            Center = ControlMap["Center"] as UnityEngine.RectTransform;
            Right = ControlMap["Right"] as UnityEngine.RectTransform;
            Speaker = ControlMap["Speaker"] as TMPro.TextMeshProUGUI;
            Body = ControlMap["Body"] as TMPro.TextMeshProUGUI;
            Advance = ControlMap["Advance"] as UnityEngine.UI.Button;
            Choices = ControlMap["Choices"] as UnityEngine.RectTransform;
            ChoiceTemplate = ControlMap["ChoiceTemplate"] as UnityEngine.RectTransform;
            Menu = ControlMap["Menu"] as UnityEngine.UI.Button;
            Settings = ControlMap["Settings"] as UnityEngine.UI.Button;
            Status = ControlMap["Status"] as TMPro.TextMeshProUGUI;
            Saves = ControlMap["Saves"] as UnityEngine.UI.Button;
            QuickSave = ControlMap["QuickSave"] as UnityEngine.UI.Button;
            QuickLoad = ControlMap["QuickLoad"] as UnityEngine.UI.Button;
            Auto = ControlMap["Auto"] as UnityEngine.UI.Button;
            Skip = ControlMap["Skip"] as UnityEngine.UI.Button;
            History = ControlMap["History"] as UnityEngine.UI.Button;
            HideDialogue = ControlMap["HideDialogue"] as UnityEngine.UI.Button;
            Dialogue = ControlMap["Dialogue"] as UnityEngine.RectTransform;
            ReadingControls = ControlMap["ReadingControls"] as UnityEngine.RectTransform;
            Speed = ControlMap["Speed"] as UnityEngine.UI.Button;
            RestoreUI = ControlMap["RestoreUI"] as UnityEngine.UI.Button;
            ReadingShading = ControlMap["ReadingShading"] as UnityEngine.RectTransform;
            FullScreenLayout = ControlMap["FullScreenLayout"] as UnityEngine.RectTransform;
            FullScreenBody = ControlMap["FullScreenBody"] as UnityEngine.RectTransform;
            TitleLayout = ControlMap["TitleLayout"] as UnityEngine.RectTransform;
            TitleBody = ControlMap["TitleBody"] as UnityEngine.RectTransform;
            TitleAdvance = ControlMap["TitleAdvance"] as UnityEngine.RectTransform;

    }
}
}
