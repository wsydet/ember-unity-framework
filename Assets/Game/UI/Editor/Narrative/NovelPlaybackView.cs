using System;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>Formal reader stage with editor-owned text/input; never opens a page through the game manager.</summary>
    public sealed class NovelPlaybackView : INovelView, INovelOpacityView, INovelActorView, INovelScreenView, INovelEffectView, INovelTextView, INovelCameraView, INovelWipeView, IDisposable
    {
        #region 内部参数
        private readonly EUIPage _page;
        private readonly INovelView _visual;
        private readonly INovelOpacityView _opacity;
        private readonly INovelActorView _actor;
        private readonly INovelScreenView _screen;
        private readonly TMP_Text _body, _speaker, _status;
        private readonly RectTransform _root;
        private bool _disposed;
        public int TextLength => _body.textInfo.characterCount;
        public GameObject Root => _root.gameObject;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelPlaybackView(GameObject root, Camera camera, Vector2 pixels)
        {
            _root = (RectTransform)root.transform;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var scaler = root.GetComponent<CanvasScaler>();
            var size = pixels;
            if (scaler && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                var ratio = pixels / scaler.referenceResolution;
                float scale = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(ratio.x, ratio.y)
                    : scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(ratio.x, ratio.y)
                    : Mathf.Pow(2, Mathf.Lerp(Mathf.Log(ratio.x, 2), Mathf.Log(ratio.y, 2), scaler.matchWidthOrHeight));
                size = pixels / Mathf.Max(.001f, scale);
            }
            if (scaler) scaler.enabled = false;
            foreach (var safe in root.GetComponentsInChildren<EUISafeArea>(true))
            {
                safe.enabled = false; var rect = (RectTransform)safe.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(.5f, .5f);
            _root.sizeDelta = size; _root.position = Vector3.zero; _root.localScale = Vector3.one;
            _page = new EUIPage(root);
            try
            {
                EUIBindingBridge.Attach(_page, root.GetComponent<EUIBinding>());
                if (_page.Logic is not INovelView view || _page.Logic is not INovelOpacityView opacity ||
                    _page.Logic is not INovelActorView actor || _page.Logic is not INovelScreenView screen)
                    throw new InvalidOperationException("正式阅读页缺少演出接口，请检查 EUI Binding。");
                _visual = view; _opacity = opacity; _actor = actor; _screen = screen;
                // Same initialization path as formal reader render tests. No OnOpen, module or save subscriptions.
                _page.Logic.OnInit();
                var map = _page.Logic.ControlMap;
                _body = (TMP_Text)map["Body"]; _speaker = (TMP_Text)map["Speaker"]; _status = (TMP_Text)map["Status"];
                foreach (string key in new[] { "Saves", "QuickSave", "QuickLoad", "Skip", "RestoreUI", "ChoiceTemplate" })
                    map[key].gameObject.SetActive(false);
                // PreviewRenderUtility renders a texture; clicks are translated by the editor toolbar, never these buttons.
                foreach (var raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
                foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true)) { group.alpha = 1; group.blocksRaycasts = false; }
                ClearVisuals();
            }
            catch { _page.Logic?.OnDispose(); throw; }
        }
        public Vector2 CanvasSize => _root.sizeDelta;
        public void SetCamera(Vector2 offset, float zoom) => ((INovelCameraView)_visual).SetCamera(offset, zoom);
        public void SetWipe(float progress, NovelWipeDirection direction) => ((INovelWipeView)_visual).SetWipe(progress, direction);
        public int PrepareText(NovelCommand command, int start) => ((INovelTextView)_visual).PrepareText(command, start);
        public void SetStoryDialogueVisible(bool visible) => ((INovelTextView)_visual).SetStoryDialogueVisible(visible);
        public void ShowText(string speaker, int visibleCharacters) => ((INovelTextView)_visual).ShowText(speaker, visibleCharacters);
        public void Render(NarrativeSnapshot snapshot, NovelCommand command, string speaker, int visibleCharacters, string status)
        {
            // Keep the last authored frame during waits, draining and the synthetic ending.
            if (command?.Kind == NovelCommandKind.Say &&
                (snapshot.State == NarrativeState.Revealing || snapshot.State == NarrativeState.AwaitingAdvance))
            {
                ((INovelTextView)_visual).ShowText(speaker, visibleCharacters);
                _body.ForceMeshUpdate(true);
            }
            var auto = _page.Logic.ControlMap["Auto"].GetComponentInChildren<TMP_Text>(true);
            if (auto) auto.text = snapshot.ReadMode == NarrativeReadMode.Auto ? "自动 ON" : "自动 OFF";
            _status.text = string.Empty;
        }
        public INovelEffectInstance CreateEffect(GameObject prefab, NovelEffectState state, NovelPortraitSlot slot) =>
            ((INovelEffectView)_visual).CreateEffect(prefab, state, slot);
        public void Flush()
        { Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(_root); _body.ForceMeshUpdate(); }
        public void Visual(NovelCommand command, Sprite sprite, float progress) => _visual.Visual(command, sprite, progress);
        public void SetOpacity(NovelTargetKind kind, NovelPortraitSlot slot, float opacity) => _opacity.SetOpacity(kind, slot, opacity);
        public Vector2 NamedPosition(NovelPortraitSlot slot) => _actor.NamedPosition(slot);
        public void ApplyActor(NovelVisualState state, Vector2 offset, float rotation) => _actor.ApplyActor(state, offset, rotation);
        public void ResetActor(NovelPortraitSlot slot) => _actor.ResetActor(slot);
        public void SetShake(NovelTargetKind kind, NovelPortraitSlot slot, Vector2 offset) => _screen.SetShake(kind, slot, offset);
        public void SetCover(Color color, bool wholeReader) => _screen.SetCover(color, wholeReader);
        public void BeginCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, Sprite sprite) => _screen.BeginCrossFade(kind, slot, sprite);
        public void SetCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, float progress) => _screen.SetCrossFade(kind, slot, progress);
        public void EndCrossFade(NovelTargetKind kind, NovelPortraitSlot slot) => _screen.EndCrossFade(kind, slot);
        public void ClearVisuals()
        {
            _visual.ClearVisuals();
            if (_body) { _body.text = string.Empty; _body.maxVisibleCharacters = int.MaxValue; }
            if (_speaker) _speaker.text = string.Empty;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _page.Logic.OnDispose();
        }
        #endregion
    }
}
