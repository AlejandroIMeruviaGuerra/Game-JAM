Shader "Custom/NeonMargin_WarmScene"
{
    Properties
    {
        _BorderThickness ("Border Thickness", Range(0.001, 0.25)) = 0.05
        _GlowColor ("Glow Color", Color) = (1, 0.65, 0.2, 1)
        _CoreColor ("Inner Edge Tint", Color) = (0.8, 0.25, 0.05, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 6
        _PulseSpeed ("Pulse Speed", Range(0.1, 10)) = 2.5
        _TravelSpeed ("Travel Speed", Range(0.1, 10)) = 1.8
        _Alpha ("Transparency", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _BorderThickness;
            float4 _GlowColor;
            float4 _CoreColor;
            float _GlowIntensity;
            float _PulseSpeed;
            float _TravelSpeed;
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
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Calcular margen (solo bordes)
                float2 edgeDist = min(uv, 1.0 - uv);
                float dist = min(edgeDist.x, edgeDist.y);
                float borderMask = smoothstep(_BorderThickness, _BorderThickness * 0.6, dist);

                // Luz que recorre el borde
                float t = _Time.y * _TravelSpeed;
                float phase = (sin((uv.x + uv.y + t) * 6.283) * 0.5 + 0.5);

                // Pulso de intensidad
                float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                float dynamicGlow = saturate(phase * 0.6 + pulse * 0.4);

                // Color principal cálido
                float3 warmBase = lerp(_CoreColor.rgb, _GlowColor.rgb, dynamicGlow);
                float3 finalColor = warmBase * _GlowIntensity;

                float alpha = (1 - borderMask) * _Alpha;

                // Centro vacío (transparente)
                if (dist > _BorderThickness) alpha = 0;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
