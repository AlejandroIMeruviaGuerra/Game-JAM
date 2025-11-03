Shader "Custom/MatrixBackground"
{
    Properties
    {
        _Color1 ("Color Principal", Color) = (0, 0.3, 0, 1)
        _Color2 ("Color Secundario", Color) = (0, 0.8, 0.2, 1)
        _Speed ("Velocidad", Range(0, 5)) = 2.0
        _Density ("Densidad", Range(0, 1)) = 0.7
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            fixed4 _Color1;
            fixed4 _Color2;
            float _Speed;
            float _Density;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _Speed;
                
                // Efecto de lluvia Matrix
                float rain = frac(uv.y * 15.0 + time * 2.0);
                float streams = sin(uv.x * 30.0 + time * 0.5);
                
                float brightness = step(0.95, rain) * step(0.4, streams);
                brightness *= step(0.1, frac(sin(uv.x * 100 + uv.y * 200) * 1000));
                
                // Añadir ruido para más variación
                float noise = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                brightness += step(0.98, noise) * 0.3;
                
                fixed4 col = lerp(_Color1, _Color2, brightness);
                col.a = 1.0;
                
                return col;
            }
            ENDCG
        }
    }
}