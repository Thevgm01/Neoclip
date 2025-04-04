Shader "Unlit/STest"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        // We actually DON'T want this here because it'll cause all materials to set a default value which overrides Shader.SetGlobalInteger
        //[Enum(UnityEngine.Rendering.CullMode)] _NeoclipCullMode ("Cull Mode", Integer) = 2
    }
    SubShader
    {
        Cull [_NeoclipCullMode]
        /*
        Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
        Blend One OneMinusSrcAlpha // Premultiplied transparency
        Blend One One // Additive
        Blend OneMinusDstColor One // Soft additive
        Blend DstColor Zero // Multiplicative
        Blend DstColor SrcColor // 2x multiplicative
        */
        Blend [_NeoclipBlendSourceFactor] [_NeoclipBlendDestinationFactor]
        BlendOp [_NeoclipBlendOp]
        ZTest [_NeoclipZTest]
        ZWrite [_NeoclipZWrite]
        AlphaToMask [_NeoclipAlphaToMask]
        
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldSpacePosition : POSITION_WS;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            int _NeoclipIsClipping;

            static const int CENTER = 0;
            static const int UPLEFT = 1;
            static const int UPRIGHT = 2;
            static const int DOWNLEFT = 3;
            static const int DOWNRIGHT = 4;
            
            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
            float RobertsCross(float4 samples[5])
            {
                const float4 difference_1 = samples[UPRIGHT] - samples[DOWNLEFT];
                const float4 difference_2 = samples[UPLEFT] - samples[DOWNRIGHT];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldSpacePosition = mul(v.vertex, unity_ObjectToWorld);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f v) : SV_Target
            {
                if (_NeoclipIsClipping)
                {
                    // Screen-space coordinates which we will use to sample.
                    float2 texel_size = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);

                    // Generate 4 diagonally placed samples.
                    const float half_width_f = floor(2 * 0.5);
                    const float half_width_c = ceil(2 * 0.5);
                    
                    float2 uvs[5];
                    uvs[CENTER] = v.uv;
                    uvs[UPLEFT] = v.uv + texel_size * float2(-half_width_f, half_width_c);
                    uvs[UPRIGHT] = v.uv + texel_size * float2(half_width_c, half_width_c);
                    uvs[DOWNLEFT] = v.uv + texel_size * float2(-half_width_f, -half_width_f);
                    uvs[DOWNRIGHT] = v.uv + texel_size * float2(half_width_c, -half_width_f);
                    
                    float4 colors[5];
                    
                    for (int i = 0; i < 5; i++) {
                        colors[i] = tex2D(_MainTex, uvs[i]);
                    }
                    
                    // Alpha clipping hack?
                    if (colors[CENTER].a < 0.5)
                    {
                        return 1.0;
                    }
                    
                    // Apply edge detection kernel on the samples to compute edges.
                    float color_cross = RobertsCross(colors);
                    float4 col = color_cross;
                    
                    float cameraDistance = distance(_WorldSpaceCameraPos, v.worldSpacePosition);
                    //return col * 10; // Looks cool with Blend DstColor OneMinusDstColor
                    //return lerp(col, 1.0, min(sqrt(cameraDistance) / 10, 1.0)); // Good with Multiplicative (Blend DstColor Zero)
                    return lerp(col, 1.0, 1.0 - exp(cameraDistance / -100) * 0.8); // Even better
                    //return lerp(col, 0.0, 1.0 - exp(cameraDistance / -50)); // Good with Additive (Blend One One)
                }
                else
                {
                    // sample the texture
                    fixed4 col = tex2D(_MainTex, v.uv) * _BaseColor;
                    // apply fog
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    
                    return col;
                }
            }
            ENDCG
        }
    }
}
