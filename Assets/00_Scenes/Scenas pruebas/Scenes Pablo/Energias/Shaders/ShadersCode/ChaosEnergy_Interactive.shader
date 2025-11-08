Shader "Custom/ChaosEnergy_Interactive"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (1.5, 0.6, 2.5, 1)
        _AuraColor ("Aura Color", Color) = (0.3, 0, 0.8, 1)
        _ChaosColor ("Chaos Edge", Color) = (3, 0.5, 3, 1)
        _Speed ("Chaos Speed", Range(0,5)) = 1.8
        _Intensity ("Glow Intensity", Range(0,5)) = 2.5
        _Distortion ("Distortion", Range(0,1)) = 0.4
        _Pulse ("Pulse Power", Range(0,2)) = 0
        _Twist ("Twist Motion", Range(0,3)) = 1.2
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
                float4 _CoreColor;
                float4 _AuraColor;
                float4 _ChaosColor;
                float _Speed;
                float _Intensity;
                float _Distortion;
                float _Pulse;
                float _Twist;
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

                // Movimiento caótico giratorio
                float angle = atan2(uv.y, uv.x) + sin(t * 0.5) * _Twist;
                float radius = length(uv) * 2.0;

                // Distorsión caótica
                float2 flow = float2(cos(angle * 2.0 + t), sin(angle * 3.0 - t)) * _Distortion;
                float n = noise(uv * 6.0 + flow) + noise(uv * 12.0 - flow * 0.7) * 0.6;
                n = pow(saturate(n), 1.6);

                // Núcleo brillante
                float core = smoothstep(0.2, 0.0, radius);
                float3 color = lerp(_AuraColor.rgb, _CoreColor.rgb, n + core * 1.2);

                // Halo exterior
                float halo = smoothstep(0.8, 0.2, radius + sin(t * 2.0) * 0.1);
                color += _ChaosColor.rgb * halo * (0.6 + _Pulse);

                // Parpadeo de caos (brillo variable)
                float flicker = sin(t * 6.0 + noise(uv * 10.0) * 4.0) * 0.5 + 0.5;
                color *= (1.0 + flicker * 0.5 + _Pulse * 0.3);

                float alpha = saturate(1.0 - radius * 0.7 + halo * 0.4);
                return float4(color * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
