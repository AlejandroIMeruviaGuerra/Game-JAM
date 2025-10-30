Shader "Custom/SlotFrame_Realistic"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Tint", Color) = (1, 0.6, 0.25, 1)
        _EmissionColor ("Emission Color", Color) = (1, 0.9, 0.6, 1)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 3.5
        _PulseSpeed ("Pulse Speed", Range(0.1,8)) = 1.8
        _Metallic ("Metallic Amount", Range(0,1)) = 0.35
        _Smoothness ("Smoothness", Range(0,1)) = 0.85
        _EdgeDarkness ("Edge Shadow", Range(0,1)) = 0.3
        _Alpha ("Transparency", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 300

        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _EmissionColor;
            float _EmissionStrength;
            float _PulseSpeed;
            float _Metallic;
            float _Smoothness;
            float _EdgeDarkness;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 baseTex = tex2D(_MainTex, i.uv) * _Color;

                // Gradiente suave
                float grad = smoothstep(0.1, 0.9, i.uv.y);
                baseTex.rgb *= lerp(0.9, 1.1, grad);

                // Simula reflexión metálica con pulso animado
                float t = _Time.y * _PulseSpeed;
                float reflection = (sin(t + i.uv.y * 12.0) * 0.5 + 0.5);
                float3 metal = reflection * _EmissionColor.rgb * _EmissionStrength * 0.2;

                // Sombras en bordes
                float2 edge = min(i.uv, 1.0 - i.uv);
                float shadow = saturate(min(edge.x, edge.y) / 0.1);
                baseTex.rgb *= lerp(1.0 - _EdgeDarkness, 1.0, shadow);

                // Mezcla final con luz metálica
                float3 finalColor = baseTex.rgb + metal * _Metallic;
                finalColor = lerp(finalColor, _EmissionColor.rgb, 0.1 * _Smoothness);

                return float4(finalColor, baseTex.a * _Alpha);
            }
            ENDHLSL
        }
    }
}
