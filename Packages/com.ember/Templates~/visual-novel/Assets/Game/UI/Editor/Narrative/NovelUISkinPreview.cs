using System;
using System.IO;
using Game.Narrative;
using Ember.UIExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>隔离场景预览，只操作实例，窗口和 skill 截图使用同一渲染器。</summary>
    public sealed class NovelUISkinPreview : IDisposable
    {
        #region 内部参数
        private readonly PreviewRenderUtility _preview;
        private readonly GameObject _root;
        private readonly string _page;
        private readonly Vector2 _pixels;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelUISkinPreview(string prefabPath, int width = 1920, int height = 1080)
        {
            if (width < 64 || height < 64 || width > 4096 || height > 4096) throw new ArgumentOutOfRangeException(nameof(width));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab) throw new ArgumentException("页面不存在：" + prefabPath);
            _pixels = new Vector2(width, height); _page = prefab.name;
            _preview = new PreviewRenderUtility();
            try
            {
                var host = new GameObject("NovelSkinPreview", typeof(RectTransform), typeof(Canvas));
                _preview.AddSingleGO(host);
                var canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = _preview.camera;
                var rect = (RectTransform)host.transform; rect.sizeDelta = _pixels;
                rect.position = Vector3.zero; rect.localScale = Vector3.one;
                _root = UnityEngine.Object.Instantiate(prefab, host.transform, false);
                var sourceScaler = _root.GetComponent<CanvasScaler>();
                if (sourceScaler && sourceScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    Vector2 ratio = _pixels / sourceScaler.referenceResolution;
                    float scale = sourceScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(ratio.x, ratio.y)
                        : sourceScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(ratio.x, ratio.y)
                        : Mathf.Pow(2, Mathf.Lerp(Mathf.Log(ratio.x, 2), Mathf.Log(ratio.y, 2), sourceScaler.matchWidthOrHeight));
                    rect.sizeDelta = _pixels / Mathf.Max(.001f, scale);
                }
                foreach (var scaler in _root.GetComponentsInChildren<CanvasScaler>(true)) scaler.enabled = false;
                foreach (var raycaster in _root.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
                foreach (var animator in _root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var safe in _root.GetComponentsInChildren<EUISafeArea>(true))
                {
                    safe.enabled = false; var r = (RectTransform)safe.transform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                }
                foreach (var group in _root.GetComponentsInChildren<CanvasGroup>(true)) { group.alpha = 1; group.blocksRaycasts = false; }
                var rootRect = (RectTransform)_root.transform;
                var rootCanvas = _root.GetComponent<Canvas>();
                if (rootCanvas)
                {
                    rootCanvas.renderMode = RenderMode.WorldSpace; rootCanvas.worldCamera = _preview.camera;
                    rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(.5f, .5f);
                    rootRect.sizeDelta = rect.sizeDelta;
                }
                rootRect.localScale = Vector3.one; rootRect.localPosition = Vector3.zero;
                _root.SetActive(true);
                var camera = _preview.camera; camera.cameraType = CameraType.Game;
                camera.orthographic = true; camera.orthographicSize = rect.sizeDelta.y / 2;
                camera.transform.position = new Vector3(0, 0, -10); camera.transform.rotation = Quaternion.identity;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .12f, .14f);
            }
            catch { _preview.Cleanup(); throw; }
        }

        public Texture Render(Rect area, NovelUISkin skin)
        {
            NovelUISkinRuntime.Apply(_root, _page, skin);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_root.transform);
            _preview.BeginPreview(area, GUIStyle.none);
            Texture texture;
            try { _preview.Render(true); }
            finally { texture = _preview.EndPreview(); }
            return texture;
        }

        public void SavePng(string file, NovelUISkin skin)
        {
            NovelUISkinRuntime.Apply(_root, _page, skin);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_root.transform);
            _preview.BeginStaticPreview(new Rect(0, 0, _pixels.x, _pixels.y));
            Texture2D texture = null;
            try
            {
                try { _preview.Render(true); }
                finally { texture = _preview.EndStaticPreview(); }
                string path = Path.GetFullPath(file); Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { if (texture) UnityEngine.Object.DestroyImmediate(texture); }
        }
        public void Dispose() => _preview.Cleanup();
        #endregion
    }
}
