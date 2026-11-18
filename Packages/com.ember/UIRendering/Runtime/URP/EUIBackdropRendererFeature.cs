using Ember.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Ember.UIRendering
{
    public sealed class EUIBackdropRendererFeature : ScriptableRendererFeature
    {
        #region 内部参数
        private CapturePass _capture;
        private PublishPass _publish;
        #endregion

        #region 外部方法
        public override void Create()
        {
            _capture = new CapturePass { renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
            _publish = new PublishPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType != CameraType.Game || !EUIBackdropCameraHost.Active) return;
            if (EUIBackdropCameraHost.Active.HasInput(camera)) renderer.EnqueuePass(_publish);
            if (EUIBackdropCameraHost.Active.HasCapture(camera)) renderer.EnqueuePass(_capture);
        }
        #endregion

        #region 内部方法
        private sealed class PublishPass : ScriptableRenderPass
        {
            private sealed class PassData { }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var camera = frameData.Get<UniversalCameraData>().camera;
                var lease = EUIBackdropCameraHost.Active?.InputFor(camera);
                if (lease == null || lease.Output == null) return;
                using var builder = graph.AddRasterRenderPass<PassData>("EUI Backdrop / Import for foreground", out _);
                var handle = graph.ImportTexture(lease.Output);
                builder.UseTexture(handle, AccessFlags.Read);
                // Native uGUI draws sample a material texture. Publish also registers it with RG's
                // global texture tracking so DrawObjectsPass.UseAllGlobalTextures keeps the dependency.
                builder.SetGlobalTextureAfterPass(handle, Shader.PropertyToID("_EmberBackdropInput"));
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => { });
            }
        }

        private sealed class CapturePass : ScriptableRenderPass
        {
            public CapturePass() { requiresIntermediateTexture = true; }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var lease = EUIBackdropCameraHost.Active?.CaptureFor(cameraData.camera);
                if (lease == null || !lease.ShouldCapture) return;
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer)
                { lease.DiagnosticMessage = "Solid fallback: capture source is backbuffer"; return; }
                lease.EnsureOutput(cameraData.cameraTargetDescriptor);
                if (lease.Output == null || !lease.BlurMaterial) return;
                var descriptor = graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.width = lease.Output.rt.width; descriptor.height = lease.Output.rt.height;
                descriptor.msaaSamples = MSAASamples.None; descriptor.depthBufferBits = DepthBits.None;
                descriptor.name = "EUI Backdrop Scratch"; descriptor.clearBuffer = false;
                var scratchA = graph.CreateTexture(descriptor);
                descriptor.name = "EUI Backdrop Scratch B";
                var scratchB = graph.CreateTexture(descriptor);
                var output = graph.ImportTexture(lease.Output);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(resources.activeColorTexture, scratchA,
                    Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0), "EUI Backdrop / Downsample");
                for (int i = 0; i < lease.Iterations; i++)
                {
                    graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(scratchA, scratchB, lease.BlurMaterial, 0), "EUI Backdrop / Horizontal");
                    graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(scratchB, i == lease.Iterations - 1 ? output : scratchA,
                        lease.BlurMaterial, 1), "EUI Backdrop / Vertical");
                }
                lease.Captured();
            }
        }
        #endregion
    }
}
