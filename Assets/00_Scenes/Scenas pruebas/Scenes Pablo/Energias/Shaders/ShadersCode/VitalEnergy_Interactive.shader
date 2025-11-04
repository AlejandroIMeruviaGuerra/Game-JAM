Shader "Custom/VitalEnergy_Interactive"
{
    Properties
    {
        _BaseColor ("Base Glow Color", Color) = (0.1, 1, 0.4, 1)
        _PulseColor ("Pulse Color", Color) = (0.4, 2, 0.8, 1)
        _LightningColor ("Lightning Color", Color) = (0.3, 2.5, 0.6, 1)
        _Speed ("Pulse Speed", Range(0,5)) = 1.5
        _Intensity ("Glow Intensity", Range(0,5)) = 2
        _Distortion ("Distortion", Range(0,1)) = 0.2
        _LifeFlow ("Life Flow", Range(0,2)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _PulseColor;
                float4 _LightningColor;
                float _Speed;
                float _Intensity;
                float _Distortion;
                float _LifeFlow;
            CBUFFER_END

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = rand(i);
                float b = rand(i + float2(1,0));
                float c = rand(i + float2(0,1));
                float d = rand(i + float2(1,1));
                float2 u = f*f*(3.0-2.0*f);
                return lerp(a,b,u.x)+(c-a)*u.y*(1.0-u.x)+(d-b)*u.x*u.y;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y * _Speed;

                // Movimiento vital (ondas biológicas)
                float2 wave = float2(sin(uv.y * 6.0 + t) * _Distortion, cos(uv.x * 5.0 - t) * 0.1);
                float n = noise(uv * 6.0 + wave) + noise(uv * 12.0 - wave) * 0.5;
                n = pow(saturate(n), 1.8);

                // Gradiente principal verde
                float3 baseGlow = lerp(_BaseColor.rgb, _PulseColor.rgb, n + _LifeFlow);

                // Efecto de relámpagos (ruido lineal)
                float lightning = abs(sin(uv.y * 20.0 + t * 8.0 + noise(uv * 15.0) * 3.0));
                lightning = smoothstep(0.8, 1.0, lightning);
                float3 electric = _LightningColor.rgb * lightning * (1.0 + _LifeFlow);

                float3 color = (baseGlow + electric) * _Intensity;
                float alpha = saturate(n + lightning * 0.5);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
