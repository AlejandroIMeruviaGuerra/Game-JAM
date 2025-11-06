Shader "Unlit/MustardFlow"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _DripTex ("Drip Mask (gotas)", 2D) = "white" {}
        _Color ("Sauce Color", Color) = (1, 0.85, 0.2, 1)
        _Speed ("Drip Speed", Range(0, 3)) = 0.9
        _Intensity ("Color Intensity", Range(0, 2)) = 1
        _Alpha ("Transparency", Range(0,1)) = 0.85
        _Coverage ("Vertical Coverage", Range(0,1)) = 0.55
        _SmoothEdge ("Smooth Edge", Range(0.01,0.5)) = 0.08
        _WaveAmplitude ("Wave Amplitude", Range(0,0.2)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0,30)) = 10
        _Thickness ("Drip Thickness", Range(0.5,2)) = 0.9
        _ShineStrength ("Shine Strength", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _DripTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Speed;
            float _Intensity;
            float _Alpha;
            float _Coverage;
            float _SmoothEdge;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _Thickness;
            float _ShineStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Movimiento del goteo: más rápido y fluido
                float2 dripUV = i.uv;
                dripUV.y += _Time.y * _Speed;
                dripUV.x += sin(_Time.y * 4 + i.uv.y * _WaveFrequency) * _WaveAmplitude;

                // Muestreamos gotas y las afinamos (mostaza = más líquida)
                fixed4 drip = tex2D(_DripTex, dripUV);
                float dripMask = saturate(pow(drip.r, _Thickness));

                // Límite vertical superior
                float verticalMask = smoothstep(1.0 - _Coverage - _SmoothEdge, 1.0 - _Coverage + _SmoothEdge, i.uv.y);

                // Color mostaza brillante
                float3 mustard = _Color.rgb * _Intensity;

                // Simulamos un leve brillo especular dinámico (para darle vida)
                float shine = sin(i.uv.y * 10 + _Time.y * 2) * 0.5 + 0.5;
                mustard += shine * _ShineStrength;

                float alpha = dripMask * verticalMask * _Alpha;
                return float4(mustard, alpha);
            }
            ENDCG
        }
    }
}
