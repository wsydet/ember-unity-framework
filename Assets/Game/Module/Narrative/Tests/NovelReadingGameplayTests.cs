using System;
using System.Collections;
using System.Linq;
using Ember.Audio;
using Ember.Core;
using Game.NovelSave;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelGameplayTests
    {
        [UnityTest]
        public IEnumerator DialogueRegionAndSpaceAdvanceWithoutClickThrough()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground;
            Application.runInBackground = true;
            // Keep the settings object alive across the Input System's Play Mode/domain snapshots.
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            var previousEditorBehavior = inputSettings.editorInputBehaviorInPlayMode;
            var previousBackgroundBehavior = inputSettings.backgroundBehavior;
            inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool addedKeyboard = keyboard == null;
            if (addedKeyboard) keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                yield return Wait(() => Page("EUIMainPage"), "主菜单未就绪");
                Button(Page("EUIMainPage"), "Btn_Start").onClick.Invoke();
                yield return Wait(() => Session?.Snapshot.State == NarrativeState.AwaitingAdvance &&
                    Session.Snapshot.PauseReasons.Count == 0 && !NovelLoadInProgress, "对白未就绪");
                var session = Session;
                var advance = Button(Page("EUINovelReaderPage"), "Advance");
                var rect = (RectTransform)advance.transform;
                var events = UnityEngine.EventSystems.EventSystem.current;
                var pointer = new UnityEngine.EventSystems.PointerEventData(events);
                Canvas.ForceUpdateCanvases();
                pointer.position = RectTransformUtility.WorldToScreenPoint(advance.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>().eventCamera,
                    rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * .3f,
                        rect.rect.yMin + rect.rect.height * .5f)));
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                events.RaycastAll(pointer, hits);
                Assert.IsNotEmpty(hits);
                Assert.AreEqual(advance.gameObject,
                    UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject));
                string previous = session.Snapshot.CommandId;
                UnityEngine.EventSystems.ExecuteEvents.Execute(advance.gameObject, pointer,
                    UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                yield return Wait(() => session.Snapshot.CommandId != previous &&
                    session.Snapshot.State == NarrativeState.AwaitingAdvance, "点击渐变区域未推进");
                previous = session.Snapshot.CommandId;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space));
                yield return null; yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return Wait(() => session.Snapshot.CommandId != previous, "空格未推进");
                yield return Wait(() => session.Snapshot.State == NarrativeState.AwaitingAdvance, "下一句未就绪");
                Button(Page("EUINovelReaderPage"), "History").onClick.Invoke();
                yield return Wait(() => Page("EUINovelHistoryPage"), "历史未打开");
                previous = session.Snapshot.CommandId;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space));
                yield return null; yield return null;
                Assert.AreEqual(previous, session.Snapshot.CommandId, "历史页下方的对白不应推进");
                Assert.IsFalse(advance.interactable);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                if (addedKeyboard) UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                inputSettings.editorInputBehaviorInPlayMode = previousEditorBehavior;
                inputSettings.backgroundBehavior = previousBackgroundBehavior;
                Application.runInBackground = background;
            }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        [UnityTest]
        public IEnumerator M4RealHistorySaveNestingAndHiddenDialogueKeepPresentation()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode(); bool background=Application.runInBackground; Application.runInBackground=true;
            try
            {
                yield return Wait(()=>Page("EUIMainPage"),"主菜单未就绪");
                Assert.IsTrue(EmberModuleCollector.Instance.TryGetModule(out NovelSaveModule saves));
                typeof(NovelSaveModule).GetProperty(nameof(saves.Store)).SetValue(saves,
                    new NovelSaveStore(System.IO.Path.Combine(".utmp/visual-novel-m4/tests",Guid.NewGuid().ToString("N"))));
                typeof(NovelSaveModule).GetProperty(nameof(saves.Account)).SetValue(saves,new NovelAccountData { TextSpeed=1,AutoInterval=60 });
                Button(Page("EUIMainPage"),"Btn_Start").onClick.Invoke();
                yield return Wait(()=>Session?.Snapshot.State==NarrativeState.Revealing && Session.Snapshot.PauseReasons.Count==0 && !NovelLoadInProgress && !Page("EUILoadingPage"),"新游戏揭幕未完成");
                var session=Session; var reader=Page("EUINovelReaderPage");
                // Leave the chapter card through its public advance path before testing reading controls.
                var title = session.Snapshot.CommandId;
                session.Advance(Time.frameCount); yield return null;
                session.Advance(Time.frameCount);
                Assert.IsTrue(session.TextTransitionActive, "章节卡应先渐隐");
                yield return Wait(() => session.Snapshot.CommandId != title && session.Snapshot.State == NarrativeState.Revealing,
                    "章节卡渐隐后未进入正文");
                session.Advance(Time.frameCount); session.SetReadMode(NarrativeReadMode.Auto);
                var position=session.Snapshot.CommandId;
                Button(reader,"History").onClick.Invoke(); yield return Wait(()=>Page("EUINovelHistoryPage"),"历史页未打开");
                StringAssert.Contains(session.History.Last().Text,Page("EUINovelHistoryPage").transform.Find("Animator/EUISafeArea/HistoryPanel/Viewport/Content").GetComponent<TMPro.TMP_Text>().text);
                Button(Page("EUINovelHistoryPage"),"Saves").onClick.Invoke(); yield return Wait(()=>Page("EUINovelSavePage"),"嵌套存档未打开");
                Assert.IsTrue(session.Snapshot.PauseReasons.Any(p=>p.StartsWith("History#")));
                Assert.IsTrue(session.Snapshot.PauseReasons.Any(p=>p.StartsWith("SavePage#")));
                Button(Page("EUINovelSavePage"),"Close").onClick.Invoke(); yield return Wait(()=>!Page("EUINovelSavePage"),"存档未关闭");
                Assert.IsTrue(session.Snapshot.PauseReasons.Count>0); Assert.AreEqual(position,session.Snapshot.CommandId);
                Assert.AreEqual(NarrativeReadMode.Auto,session.ReadMode);
                Button(Page("EUINovelHistoryPage"),"Close").onClick.Invoke(); yield return Wait(()=>session.Snapshot.PauseReasons.Count==0,"历史关闭后未恢复");
                Button(reader,"HideDialogue").onClick.Invoke();
                Assert.IsTrue(session.DialogueHidden); Assert.IsFalse(reader.transform.Find("Animator/EUISafeArea/Dialogue").gameObject.activeSelf);
                Assert.IsTrue(reader.transform.Find("Background").gameObject.activeSelf); Assert.IsTrue(reader.transform.Find("Left").gameObject.activeSelf);
                Assert.IsFalse(reader.transform.Find("Animator/EUISafeArea/ReadingControls").gameObject.activeSelf);
                Button(reader,"RestoreUI").onClick.Invoke(); session.Advance(Time.frameCount);
                Assert.IsFalse(session.DialogueHidden); Assert.AreEqual(position,session.Snapshot.CommandId);
                yield return null;
                Button(reader,"Settings").onClick.Invoke(); yield return Wait(()=>Page("EUINovelFontPage"),"字号弹窗未打开");
                Assert.IsTrue(session.Snapshot.PauseReasons.Count > 0);
                Button(Page("EUINovelFontPage"),"Large").onClick.Invoke(); Assert.AreEqual(2,saves.Account.FontSize);
                System.IO.Directory.CreateDirectory(".utmp/visual-novel-m5/reading-ui");
                yield return null; ScreenCapture.CaptureScreenshot(".utmp/visual-novel-m5/reading-ui/font.png"); yield return null;
                Button(Page("EUINovelFontPage"),"Close").onClick.Invoke(); yield return Wait(()=>session.Snapshot.PauseReasons.Count==0,"字号关闭未恢复");
                Button(reader,"Speed").onClick.Invoke(); Assert.AreEqual(2,session.ReadingMultiplier);
                Button(reader,"Speed").onClick.Invoke(); Assert.AreEqual(3,session.ReadingMultiplier);
                yield return null; ScreenCapture.CaptureScreenshot(".utmp/visual-novel-m5/reading-ui/reader.png"); yield return null;
                Button(reader,"Menu").onClick.Invoke(); yield return Wait(()=>Page("EUINovelReadingMenuPage"),"阅读菜单未打开");
                Assert.IsTrue(session.Snapshot.PauseReasons.Count > 0);
                yield return null; ScreenCapture.CaptureScreenshot(".utmp/visual-novel-m5/reading-ui/menu.png"); yield return null;
                Button(Page("EUINovelReadingMenuPage"),"Saves").onClick.Invoke(); yield return Wait(()=>Page("EUINovelSavePage")&&!Page("EUINovelReadingMenuPage"),"菜单转存档失败");
                Button(Page("EUINovelSavePage"),"Close").onClick.Invoke(); yield return Wait(()=>session.Snapshot.PauseReasons.Count==0,"转存档后暂停残留");
                GameLauncher.Instance.Fsm.TransitionTo<MainState>(); yield return Wait(()=>Page("EUIMainPage")&&Session==null,"未退出会话");
                Assert.IsTrue(session.IsDisposed);
            }
            finally { Application.runInBackground=background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        [UnityTest]
        public IEnumerator M4VoiceHandleCompletionPauseVolumeAndBgmSfxRegression()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            // Create captured playback state after the domain reload, inside a fresh iterator.
            yield return VerifyVoiceHandlePlayback();
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        private IEnumerator VerifyVoiceHandlePlayback()
        {
            bool background = Application.runInBackground;
            Application.runInBackground = true;
            AudioClip music=null,effect=null,voice=null; EmberAudioPlayback owned=null,spoken=null;
            try
            {
                yield return Wait(()=>Page("EUIMainPage"),"音频宿主未就绪");
                var manager=EmberAudioManager.Instance; var host=GameLauncher.Instance.AudioHost;
                music=AudioClip.Create("M4 music",44100,1,44100,false); effect=AudioClip.Create("M4 effect",44100,1,44100,false);
                voice=AudioClip.Create("M4 voice",22050,1,44100,false);
                manager.SetBGMVolume(.3f); manager.PlayBGM(music); manager.SetSFXVolume(.4f); manager.PlaySFX(effect);
                owned=manager.PlayOwnedSFX(effect,.5f); spoken=manager.PlayOwnedVoice(voice,.7f);
                var sources=host.GetComponents<AudioSource>(); var bgm=sources.Single(s=>s.clip==music); var line=sources.Single(s=>s.clip==voice);
                Assert.IsTrue(bgm.loop); Assert.AreEqual(.3f,bgm.volume,.001f);
                CollectionAssert.AreEquivalent(new[]{.4f,.2f},sources.Where(s=>s.clip==effect).Select(s=>s.volume).ToArray());
                Assert.AreEqual(.7f,line.volume,.001f); manager.SetSFXVolume(.1f); Assert.AreEqual(.7f,line.volume,.001f);
                int completed=0; spoken.Completed+=()=>completed++; spoken.SetPaused(true);
                yield return new WaitForSecondsRealtime(.7f); spoken.Tick(); Assert.AreEqual(0,completed); Assert.IsFalse(spoken.IsFinished);
                spoken.SetVolume(.2f); Assert.AreEqual(.2f,line.volume,.001f); spoken.SetPaused(false);
                yield return Wait(()=>spoken.IsFinished,"配音未自然结束"); spoken.Tick(); spoken.Tick(); Assert.AreEqual(1,completed);
                Assert.IsTrue(bgm.isPlaying,"配音完成不能停止 BGM");
                spoken.Dispose(); spoken=null; owned.Dispose(); owned=null;
                manager.StopBGM(); Assert.IsNull(bgm.clip);
                using(var audio=new NovelAudio())
                {
                    int notifications=0; audio.VoiceCompleted+=()=>notifications++;
                    audio.Play(NovelCommandKind.Voice,voice); audio.StopVoice(); audio.Tick();
                    Assert.AreEqual(0,notifications,"主动停止不冒充自然完成"); Assert.IsFalse(audio.VoicePlaying);
                }
            }
            finally
            {
                spoken?.Dispose(); owned?.Dispose();
                if(EmberAudioManager.TryGetInstance(out var audio)) audio.StopBGM();
                if(music) UnityEngine.Object.Destroy(music); if(effect) UnityEngine.Object.Destroy(effect); if(voice) UnityEngine.Object.Destroy(voice);
                Application.runInBackground=background;
            }
        }
    }
}
