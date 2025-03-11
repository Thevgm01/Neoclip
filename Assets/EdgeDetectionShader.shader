Shader "Hidden/Edge Detection"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
    }

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
            Name "EDGE DETECTION OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // needed to sample scene depth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // needed to sample scene normals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance

            float _OutlineThickness;
            float4 _OutlineColor;

            // These NEED to be static??
            static const int CENTER = 0;
            static const int UPLEFT = 1;
            static const int UPRIGHT = 2;
            static const int DOWNLEFT = 3;
            static const int DOWNRIGHT = 4;

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag

            // Helper function to sample the depth texture and learnize the result 
            float SampleLinearDepth(float2 uv)
            {
                return Linear01Depth(SampleSceneDepth(uv), _ZBufferParams);
            }
            
            // Helper function to sample scene normals remapped from [-1, 1] range to [0, 1].
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            // Helper function to sample scene luminance.
            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
            float RobertsCross(float3 samples[5])
            {
                const float3 difference_1 = samples[UPRIGHT] - samples[DOWNLEFT];
                const float3 difference_2 = samples[UPLEFT] - samples[DOWNRIGHT];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            // The same kernel logic as above, but for a single-value instead of a vector3.
            float RobertsCross(float samples[5])
            {
                const float difference_1 = samples[UPRIGHT] - samples[DOWNLEFT];
                const float difference_2 = samples[UPLEFT] - samples[DOWNRIGHT];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                float2 texel_size = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);

                // Generate 4 diagonally placed samples.
                const float half_width_f = floor(_OutlineThickness * 0.5);
                const float half_width_c = ceil(_OutlineThickness * 0.5);
                
                float2 uvs[5];
                uvs[CENTER] = uv;
                uvs[UPLEFT] = uv + texel_size * float2(-half_width_f, half_width_c);
                uvs[UPRIGHT] = uv + texel_size * float2(half_width_c, half_width_c);
                uvs[DOWNLEFT] = uv + texel_size * float2(-half_width_f, -half_width_f);
                uvs[DOWNRIGHT] = uv + texel_size * float2(half_width_c, -half_width_f);
                
                float3 normal_samples[5];
                float linear_depth_samples[5];
                float3 color_samples[5];
                float luminance_samples[5];
                
                for (int i = 0; i < 5; i++) {
                    linear_depth_samples[i] = SampleLinearDepth(uvs[i]);
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                    color_samples[i] = SampleSceneColor(uvs[i]);
                    luminance_samples[i] = color_samples[i] * float3(0.3, 0.59, 0.11);;
                }

                
                // Apply edge detection kernel on the samples to compute edges.
                float depth_cross = RobertsCross(linear_depth_samples);
                float normal_cross = RobertsCross(normal_samples);
                float color_cross = RobertsCross(color_samples);
                float luminance_cross = RobertsCross(luminance_samples);
                
                // Threshold the edges (discontinuity must be above certain threshold to be counted as an edge). The sensitivities are hardcoded here.
                const float depth_threshold = 1.0f / 50.0f;
                float edge_depth = depth_cross > depth_threshold ? 1 : 0;
                
                const float normal_threshold = 1 / 4.0f;
                float edge_normal = normal_cross > normal_threshold ? 1 : 0;
                
                const float color_threshold = 1 / 2.0f;
                float edge_luminance = color_cross > color_threshold ? 1 : 0;

                float edge = max(max(depth_cross * 50, normal_cross), color_cross) * linear_depth_samples[CENTER];
                float3 combined_edges = float3(edge, edge, edge);

                //float3 weird_normal_diff = max(normal_samples[UPLEFT] - normal_samples[DOWNRIGHT], normal_samples[UPRIGHT] - normal_samples[DOWNLEFT]);
                //combined_edges *= weird_normal_diff != float3(0, 0, 0) ? weird_normal_diff : float3(1, 1, 1);
                
                return half4(combined_edges * 3, 1.0);


                /*
                // Combine the edges from depth/normals/luminance using the max operator.
                float edge = max(edge_depth, max(edge_normal, edge_luminance));
                
                // Color the edge with a custom color.
                //return half4(normal_samples[CENTER], 0.5f);
                half3 one = half3(1, 1, 1);
                //return half4(one * edge, 1.0f);
                return half4(one * (cos(_Time.y + linear_depth_samples[CENTER] * 5) + 1.0) * 0.5, 1.0f);
                */
            }
            ENDHLSL
        }
    }
}