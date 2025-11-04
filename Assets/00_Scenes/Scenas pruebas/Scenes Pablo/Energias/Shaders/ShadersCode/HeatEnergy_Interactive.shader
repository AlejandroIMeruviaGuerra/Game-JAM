Shader "Custom/HeatEnergy_Interactive"
{
    Properties
    {
        _BaseColor ("Base Flame Color", Color) = (1, 0.4, 0.1, 1)
        _HotColor ("Hot Core Color", Color) = (2, 1.2, 0.2, 1)
        _Speed ("Flame Speed", Range(0,5)) = 1.5
        _Intensity ("Glow Intensity", Range(0,5)) = 2
        _Distortion ("Distortion", Range(0,1)) = 0.3
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
                float4 _HotColor;
                float _Speed;
                float _Intensity;
                float _Distortion;
                float _Pulse;
            CBUFFER_END

            // --- ruido simple ---
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

                // Distorsión vertical (efecto llama)
                float2 duv = uv + float2(sin(uv.y * 8.0 + t) * _Distortion, -t * 0.5);
                float n = noise(duv * 5.0) + noise(duv * 10.0) * 0.5;
                n = pow(saturate(n), 2.0);

                // Gradiente de calor (rojo → naranja → amarillo)
                float3 color = lerp(_BaseColor.rgb, _HotColor.rgb, n * (1.0 + _Pulse));
                float alpha = saturate(n * 2.0);

                return float4(color * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
