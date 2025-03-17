Shader "Custom/Pixel Counter"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            Name "PIXEL COUNTER"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag

            half4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                float2 texel_size = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);

                return half4(SampleSceneColor(IN.texcoord), 1.0);
            }
            ENDHLSL
        }
    }
}