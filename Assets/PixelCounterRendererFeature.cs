using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;
using UnityEngine.Rendering.RenderGraphModule.Util;

// https://ameye.dev/notes/edge-detection-outlines/
// https://roystan.net/articles/outline-shader/
// https://www.youtube.com/watch?v=U8PygjYAF7A

public class PixelCounterRendererFeature : ScriptableRendererFeature
{
    class PixelCounterPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        
        public PixelCounterPass()
        {
            profilingSampler = new ProfilingSampler(nameof(PixelCounterPass));
        }

        public void Setup(Material edgeDetectionMaterial)
        {
            blitMaterial = edgeDetectionMaterial;
            requiresIntermediateTexture = true;
        }

        //private class PassData {}
        //private static void ExecutePass(PassData data, RasterGraphContext context) {} 

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("Skipping render pass. EdgeDetectionRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{nameof(PixelCounterPass)}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            
            RenderGraphUtils.BlitMaterialParameters para = new (source, destination, blitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: nameof(PixelCounterPass));
            
            resourceData.cameraColor = destination;
        }
    }
    
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private Material material;
    
    private PixelCounterPass scriptablePass;

    /// <summary>
    /// Called
    /// - When the Scriptable Renderer Feature loads the first time.
    /// - When you enable or disable the Scriptable Renderer Feature.
    /// - When you change a property in the Inspector window of the Renderer Feature.
    /// </summary>
    public override void Create()
    {
        scriptablePass = new PixelCounterPass();
        
        scriptablePass.renderPassEvent = renderPassEvent;
    }

    /// <summary>
    /// Called
    /// - Every frame, once for each camera.
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogWarning("EdgeDetectionRendererFeature material is null and will be skipped.");
            return;
        }

        scriptablePass.Setup(material);

        renderer.EnqueuePass(scriptablePass);
    }
}
