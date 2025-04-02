using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;
using UnityEngine.Rendering.RenderGraphModule.Util;

// https://ameye.dev/notes/edge-detection-outlines/
// https://roystan.net/articles/outline-shader/
// https://www.youtube.com/watch?v=U8PygjYAF7A

public class EdgeDetectionRendererFeature : ScriptableRendererFeature
{
    class EdgeDetectionPass : ScriptableRenderPass
    {
        private Material blitMaterial;

        private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");

        public EdgeDetectionPass()
        {
            profilingSampler = new ProfilingSampler(nameof(EdgeDetectionPass));
        }

        public void Setup(EdgeDetectionSettings settings, Material edgeDetectionMaterial)
        {
            blitMaterial = edgeDetectionMaterial;
            requiresIntermediateTexture = true;
            
            renderPassEvent = settings.renderPassEvent;

            blitMaterial.SetFloat(OutlineThicknessProperty, settings.outlineThickness);
            blitMaterial.SetColor(OutlineColorProperty, settings.outlineColor);
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
            destinationDesc.name = $"CameraColor-{nameof(EdgeDetectionPass)}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            
            RenderGraphUtils.BlitMaterialParameters para = new (source, destination, blitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: nameof(EdgeDetectionPass));
            
            resourceData.cameraColor = destination;
        }
    }

    [Serializable]
    public class EdgeDetectionSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Range(0, 15)] public int outlineThickness = 3;
        public Color outlineColor = Color.black;
    }

    [SerializeField] private EdgeDetectionSettings settings;
    [SerializeField] Material material;
    
    private EdgeDetectionPass scriptablePass;

    /// <summary>
    /// Called
    /// - When the Scriptable Renderer Feature loads the first time.
    /// - When you enable or disable the Scriptable Renderer Feature.
    /// - When you change a property in the Inspector window of the Renderer Feature.
    /// </summary>
    public override void Create()
    {
        scriptablePass = new EdgeDetectionPass();
        
        scriptablePass.renderPassEvent = settings.renderPassEvent;
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

        scriptablePass.Setup(settings, material);
        scriptablePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        
        renderer.EnqueuePass(scriptablePass);
    }
}
