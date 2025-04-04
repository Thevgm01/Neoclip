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
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldSpacePosition = mul(v.vertex, unity_ObjectToWorld);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                if (_NeoclipIsClipping)
                {
                    float cameraDistance = distance(_WorldSpaceCameraPos, i.worldSpacePosition);
                    //return col * 10; // Looks cool with Blend DstColor OneMinusDstColor
                    //return lerp(col, 1.0, min(sqrt(cameraDistance) / 10, 1.0)); // Good with Multiplicative (Blend DstColor Zero)
                    return lerp(col, 1.0, 1.0 - exp(cameraDistance / -50)); // Even better
                    //return lerp(col, 0.0, 1.0 - exp(cameraDistance / -50)); // Good with Additive (Blend One One)
                }
                else
                {
                    return col;
                }
            }
            ENDCG
        }
    }
}
