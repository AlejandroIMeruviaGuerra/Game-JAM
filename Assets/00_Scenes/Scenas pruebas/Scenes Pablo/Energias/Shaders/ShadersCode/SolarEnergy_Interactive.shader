Shader "Custom/SolarEnergy_Interactive"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.9, 0.2, 1)
        _CoreColor ("Core Color", Color) = (3, 2.2, 0.8, 1)
        _SparkColor ("Spark Color", Color) = (4, 3, 1, 1)
        _Speed ("Pulse Speed", Range(0,5)) = 1.5
        _Intensity ("Glow Intensity", Range(0,5)) = 2.5
        _Pulse ("Solar Pulse", Range(0,2)) = 0
        _Rays ("Solar Rays Strength", Range(0,1)) = 0.4
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
                float4 _CoreColor;
                float4 _SparkColor;
                float _Speed;
                float _Intensity;
                float _Pulse;
                float _Rays;
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
                float2 uv = IN.uv - 0.5;
                float t = _Time.y * _Speed;

                // Patrón radial tipo sol
                float angle = atan2(uv.y, uv.x);
                float radius = length(uv) * 2.0;
                float ray = abs(sin(angle * 12.0 + t * 3.0)) * _Rays;

                // Ruido suave para textura solar
                float n = noise(uv * 8.0 + t) + noise(uv * 16.0 - t * 0.5) * 0.5;
                n = pow(saturate(n), 1.8);

                // Brillo pulsante
                float pulse = sin(t * 4.0) * 0.5 + 0.5 + _Pulse;
                float3 color = lerp(_BaseColor.rgb, _CoreColor.rgb, n + ray);
                color += _SparkColor.rgb * ray * pulse;

                float alpha = saturate(1.0 - radius * 0.6 + ray * 0.5);
                return float4(color * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
