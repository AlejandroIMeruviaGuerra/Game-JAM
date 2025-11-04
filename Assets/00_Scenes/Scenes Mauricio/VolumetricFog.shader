Shader "Custom/Particles/VolumetricFog"
{
    Properties
    {
        [HDR]_FogColor("Fog Color (HDR)", Color) = (1, 0.84, 0.3, 0.1)
        _Density("Density", Range(0,2)) = 0.5
        _Softness("Softness", Range(0,1)) = 0.7
        _NoiseScale("Noise Scale", Range(0.1,10)) = 2
        _NoiseSpeed("Noise Speed", Range(0,5)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _Softness;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            float hash21(float2 p) { return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.posWS.xz * _NoiseScale + _Time.y * _NoiseSpeed;
                float n = noise(uv);
                float alpha = saturate(n * _Density);
                float3 col = _FogColor.rgb * (alpha * 1.5);
                return half4(col, alpha * _FogColor.a * _Softness);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
