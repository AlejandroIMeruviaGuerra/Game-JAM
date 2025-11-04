Shader "Custom/EnergyPulse"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.2, 0.4, 1)
        _PulseColor("Pulse Color", Color) = (0.3, 0.9, 1.0, 1)
        _Speed("Pulse Speed", Range(0,10)) = 2
        _Intensity("Glow Intensity", Range(0,3)) = 1
        _NoiseScale("Noise Scale", Range(0,10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _TimeY;
            float4 _BaseColor;
            float4 _PulseColor;
            float _Speed;
            float _Intensity;
            float _NoiseScale;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            // función de ruido simple
            float hash(float2 p) { return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float n = noise(i.uv * _NoiseScale + _TimeY * _Speed);
                float pulse = (sin(_TimeY * _Speed * 1.3) + 1) * 0.5;
                float blend = saturate(n * 0.6 + pulse * 0.7);
                float3 color = lerp(_BaseColor.rgb, _PulseColor.rgb, blend) * _Intensity;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
