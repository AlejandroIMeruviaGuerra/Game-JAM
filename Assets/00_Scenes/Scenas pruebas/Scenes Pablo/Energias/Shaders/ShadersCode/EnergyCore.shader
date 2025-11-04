Shader "Custom/EnergyCore"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.6, 1, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Distortion ("Distortion", Range(0, 1)) = 0.4
        _Pulse ("Pulse", Range(0, 2)) = 0
        _NoiseScale ("Noise Scale", Range(1, 10)) = 3
        _Speed ("Speed", Range(0, 10)) = 1.5
        _SwirlIntensity ("Swirl Intensity", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _Distortion;
                float _Pulse;
                float _NoiseScale;
                float _Speed;
                float _SwirlIntensity;
            CBUFFER_END

            float hash(float3 p)
            {
                return frac(sin(dot(p.xyz, float3(12.9898,78.233,45.164))) * 43758.5453);
            }

            float noise(float3 p)
            {
                float n = hash(p);
                n += 0.5 * hash(p * 2.1);
                n += 0.25 * hash(p * 4.3);
                return saturate(n);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS).xyz;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;
                float3 pos = IN.worldPos * _NoiseScale;

                // 🔄 Swirl (torbellino)
                float angle = atan2(pos.z, pos.x) + t * 0.8;
                float radius = length(pos.xz);
                float swirl = sin(angle * 3 + radius * 4) * _SwirlIntensity;

                float n = noise(pos + swirl);
                n = smoothstep(0.3, 0.8, n);

                float glow = saturate(dot(IN.normalWS, normalize(float3(0,0,1))));
                float pulse = abs(sin(t * 2.2)) * _Pulse;

                float3 color = lerp(_BaseColor.rgb, _GlowColor.rgb, n + glow * 0.8 + pulse * 0.5);
                float alpha = saturate(0.5 + n * 0.5 + pulse * 0.4);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
