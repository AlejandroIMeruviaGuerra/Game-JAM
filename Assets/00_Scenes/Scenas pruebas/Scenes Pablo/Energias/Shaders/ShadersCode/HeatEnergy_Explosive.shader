Shader "Custom/HeatEnergy_Explosive"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.3, 0.05, 1)
        _HotColor ("Hot Core Color", Color) = (2, 1.2, 0.3, 1)
        _SparkColor ("Spark Color", Color) = (2.5, 2, 1, 1)
        _Speed ("Flame Speed", Range(0,5)) = 1.8
        _Intensity ("Glow Intensity", Range(0,5)) = 2.5
        _Distortion ("Distortion", Range(0,1)) = 0.35
        _Explosion ("Explosion Strength", Range(0,2)) = 0
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
                float4 _SparkColor;
                float _Speed;
                float _Intensity;
                float _Distortion;
                float _Explosion;
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

                // Movimiento de la llama (hacia arriba con turbulencia)
                float2 flow = float2(sin(uv.y * 8.0 + t) * _Distortion, -t * 0.5);
                float n = noise(uv * 5.0 + flow) + noise(uv * 10.0 - flow * 0.8) * 0.5;
                n = pow(saturate(n), 2.0);

                // Gradiente fuego base
                float3 flame = lerp(_BaseColor.rgb, _HotColor.rgb, n);
                float alpha = saturate(n * 2.0);

                // Chispas ascendentes (pequeños puntos brillantes)
                float spark = noise(uv * 20.0 + float2(t * 3.0, -t * 8.0));
                spark = step(0.95, spark) * (1.0 + _Explosion * 2.0);
                float3 sparkCol = _SparkColor.rgb * spark;

                // Pulso de explosión
                float exp = smoothstep(0.0, 1.0, sin(t * 10.0 + uv.y * 5.0)) * _Explosion;
                float3 explosion = _HotColor.rgb * exp * 2.0;

                float3 color = (flame + sparkCol + explosion) * _Intensity;
                alpha = saturate(alpha + spark * 0.5 + exp * 0.4);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
