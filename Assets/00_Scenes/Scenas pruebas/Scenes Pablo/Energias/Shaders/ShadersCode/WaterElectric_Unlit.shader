Shader "Custom/WaterElectric_Interactive"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.6, 1, 1)
        _ElectricColor ("Electric Color", Color) = (0.4, 0.9, 2, 1)
        _Intensity ("Glow Intensity", Range(0,5)) = 2
        _Speed ("Water Speed", Range(0,5)) = 1.2
        _Distortion ("Distortion Strength", Range(0,1)) = 0.3
        _Pulse ("Interaction Pulse", Range(0,2)) = 0
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
                float4 _ElectricColor;
                float _Intensity;
                float _Speed;
                float _Distortion;
                float _Pulse;
            CBUFFER_END

            // Funciones auxiliares para ruido
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
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
                float2 uv = IN.uv * 3.0;
                float t = _Time.y * _Speed;

                // Agua con distorsión fluida
                float n = noise(uv + float2(t, -t)) * 0.6 + noise(uv * 2.0 - float2(t, t)) * 0.3;
                float water = smoothstep(0.3, 0.7, n);

                // Rayos eléctricos fractales en los bordes
                float electric = abs(sin((uv.x * 5.0 + n * 4.0 + t * 4.0))) * _Intensity;
                electric *= pow(1.0 - saturate(length(uv - 0.5) * 1.8), 1.3);
                electric += noise(uv * 6.0 + float2(t * 3.0, t * -2.0)) * 0.6;

                // Pulso al interactuar
                electric *= (1.0 + _Pulse);

                // Mezcla visual
                float3 color = _BaseColor.rgb * water + _ElectricColor.rgb * electric;
                float alpha = saturate(water + electric * 0.4);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
