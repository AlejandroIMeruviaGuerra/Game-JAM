Shader "Custom/URP/GroundHalo"
{
    Properties
    {
        [HDR]_HaloColor("Halo Color", Color) = (1,0.92,0.45,1)
        _Intensity("Intensity", Range(0,50)) = 15
        _InnerRadius("Inner Radius", Range(0,1)) = 0.15
        _OuterRadius("Outer Radius", Range(0.1,2)) = 1.1
        _Feather("Feather", Range(0,1)) = 0.7
        _NoiseScale("Noise Scale", Range(0.1,5)) = 3
        _NoiseSpeed("Noise Speed", Range(0,5)) = 0.4
        _DepthFade("Depth Fade", Range(0,2)) = 0.6
        _Alpha("Alpha", Range(0,1)) = 1
        _AnisoBoost("Directional Bias", Range(0,3)) = 1.5
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 posWS:TEXCOORD0; float2 uv:TEXCOORD1; float fog:TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                float4 _HaloColor;
                float _Intensity, _InnerRadius, _OuterRadius, _Feather;
                float _NoiseScale, _NoiseSpeed, _DepthFade, _Alpha, _AnisoBoost;
            CBUFFER_END

            float hash21(float2 p){ return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p); float2 f=frac(p);
                float a=hash21(i);
                float b=hash21(i+float2(1,0));
                float c=hash21(i+float2(0,1));
                float d=hash21(i+float2(1,1));
                float2 u=f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            Varyings vert(Attributes IN){
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.posWS = pos.positionWS;
                OUT.uv = IN.uv * 2 - 1;
                OUT.fog = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN):SV_Target
            {
                float r = length(IN.uv);
                float radial = smoothstep(_InnerRadius, _OuterRadius, r);
                float core = 1.0 - pow(radial, _Feather * 3.0);
                float2 p = IN.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                float n = noise(p);
                float halo = core * lerp(1.0, n * 1.3, 0.35);

                // Simular dirección preferencial de la luz
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.posWS);
                float dirBias = pow(saturate(dot(viewDir, float3(0,1,0))), _AnisoBoost);

                float2 uvSS = IN.positionHCS.xy / IN.positionHCS.w; uvSS = uvSS * 0.5 + 0.5;
                float sceneDepth = SampleSceneDepth(uvSS);
                float df = saturate((sceneDepth - IN.positionHCS.z) / max(_DepthFade, 1e-4));

                float3 col = _HaloColor.rgb * _Intensity * halo * dirBias;
                col = MixFog(col, IN.fog);
                return half4(col, halo * _Alpha * df);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
