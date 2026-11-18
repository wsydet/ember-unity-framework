using System;
using Ember.Basic;
using Ember.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ember.UIRendering
{
    [CreateAssetMenu(menuName = "Ember/UI/URP Backdrop Installation")]
    public sealed class EUIBackdropInstallation : EmberBaseSO
    {
        [Serializable] public sealed class PipelineBinding { public UniversalRenderPipelineAsset Pipeline; public int RendererIndex; }
        #region 编辑器面板参数
        [SerializeField] private PipelineBinding[] _pipelines = Array.Empty<PipelineBinding>();
        [SerializeField] private string[] _reservedLayers = Array.Empty<string>();
        [SerializeField] private int _originalUiRendererIndex = -1;
        #endregion
        #region 外部方法
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterInstallation()
        {
            EUIBackdrop.InstallationRequested -= InstallConfigured;
            EUIBackdrop.InstallationRequested += InstallConfigured;
        }

        private static void InstallConfigured(Camera baseCamera, Camera uiCamera, Transform uiRoot)
        {
            var installation = Resources.Load<EUIBackdropInstallation>("Config/UIFoundation/EUIBackdropInstallation");
            if (installation) installation.Install(baseCamera, uiCamera, uiRoot);
        }

        public EUIBackdropCameraHost Install(Camera baseCamera, Camera uiCamera, Transform uiRoot)
        {
            if (EUIBackdropCameraHost.Active) return EUIBackdropCameraHost.Active;
            var rp = GraphicsSettings.currentRenderPipeline;
            int renderer = -1;
            foreach (var binding in _pipelines) if (binding.Pipeline == rp) renderer = binding.RendererIndex;
            if (renderer < 0) { EmberDebug.LogWarning(LogTags.UIManager, "Backdrop Solid fallback: current URP asset has no installation binding"); return null; }
            var layers = new int[_reservedLayers.Length];
            for (int i = 0; i < layers.Length; i++) layers[i] = LayerMask.NameToLayer(_reservedLayers[i]);
            var go = new GameObject("EUI Backdrop Camera Segments"); go.transform.SetParent(uiRoot.parent, false);
            var host = go.AddComponent<EUIBackdropCameraHost>();
            try { host.Configure(baseCamera, uiCamera, uiRoot, renderer, _originalUiRendererIndex, layers); return host; }
            catch (Exception e) { Destroy(go); EmberDebug.LogWarning(LogTags.UIManager, "Backdrop Solid fallback: " + e.Message); return null; }
        }
        #endregion
    }
}
