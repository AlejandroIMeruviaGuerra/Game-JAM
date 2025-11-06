Shader "Unlit/KetchupDrip_Flow"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _DripTex ("Drip Mask (gotas)", 2D) = "white" {}
        _Color ("Ketchup Color", Color) = (1, 0, 0, 1)
        _Speed ("Drip Speed", Range(0, 2)) = 0.5
        _Intensity ("Color Intensity", Range(0, 2)) = 1
        _Alpha ("Transparency", Range(0,1)) = 0.9
        _Coverage ("Vertical Coverage", Range(0,1)) = 0.6
        _SmoothEdge ("Smooth Edge", Range(0.01,0.5)) = 0.1
        _WaveAmplitude ("Wave Amplitude", Range(0,0.1)) = 0.03
        _WaveFrequency ("Wave Frequency", Range(0,30)) = 10
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
                // invertimos el eje Y para que baje desde arriba
                float2 dripUV = i.uv;
                dripUV.y += _Time.y * _Speed;

                // ondulación lateral tipo gotas chorreando
                dripUV.x += sin(_Time.y * 2 + i.uv.y * _WaveFrequency) * _WaveAmplitude;

                // textura de gotas
                fixed4 drip = tex2D(_DripTex, dripUV);
                float dripMask = drip.r;

                // limitamos a la parte superior
                float verticalMask = smoothstep(1.0 - _Coverage - _SmoothEdge, 1.0 - _Coverage + _SmoothEdge, i.uv.y);

                // color ketchup con intensidad
                float3 ketchup = _Color.rgb * _Intensity;

                float alpha = dripMask * verticalMask * _Alpha;
                return float4(ketchup, alpha);
            }
            ENDCG
        }
    }
}
    