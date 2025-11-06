Shader "Unlit/MayonnaiseFlow"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _DripTex ("Drip Mask (gotas)", 2D) = "white" {}
        _Color ("Sauce Color", Color) = (1, 0.95, 0.7, 1)
        _Speed ("Drip Speed", Range(0, 2)) = 0.25
        _Intensity ("Color Intensity", Range(0, 2)) = 1
        _Alpha ("Transparency", Range(0,1)) = 0.85
        _Coverage ("Vertical Coverage", Range(0,1)) = 0.6
        _SmoothEdge ("Smooth Edge", Range(0.01,0.5)) = 0.12
        _WaveAmplitude ("Wave Amplitude", Range(0,0.2)) = 0.08
        _WaveFrequency ("Wave Frequency", Range(0,30)) = 6
        _Thickness ("Drip Thickness", Range(0,2)) = 1.2
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
                float2 dripUV = i.uv;
                dripUV.y += _Time.y * _Speed;

                // Movimiento lateral más suave y grueso
                dripUV.x += sin(_Time.y * 1.5 + i.uv.y * _WaveFrequency) * _WaveAmplitude;

                // Muestreamos la máscara y engrosamos las zonas blancas
                fixed4 drip = tex2D(_DripTex, dripUV);
                float dripMask = saturate(pow(drip.r, 1.0 / _Thickness));

                // Limitamos a la parte superior
                float verticalMask = smoothstep(1.0 - _Coverage - _SmoothEdge, 1.0 - _Coverage + _SmoothEdge, i.uv.y);

                // Color tipo mayonesa espesa
                float3 sauce = _Color.rgb * _Intensity;

                float alpha = dripMask * verticalMask * _Alpha;
                return float4(sauce, alpha);
            }
            ENDCG
        }
    }
}
