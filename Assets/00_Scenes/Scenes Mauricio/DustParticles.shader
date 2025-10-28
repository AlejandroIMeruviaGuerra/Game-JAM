Shader "Custom/Particles/Dust"
{
    Properties
    {
        [HDR]_Color("Dust Color (HDR)", Color) = (1, 0.9, 0.5, 0.3)
        _Softness("Softness", Range(0,1)) = 0.6
        _FadeByView("Fade by View", Range(0,1)) = 0.4
        _SizeFade("Fade by Distance", Range(0,2)) = 0.8
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend One One
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
                float3 posWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Softness;
                float _FadeByView;
                float _SizeFade;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 toCam = normalize(_WorldSpaceCameraPos - IN.posWS);
                float facing = saturate(dot(toCam, float3(0, 1, 0)));

                float fadeView = lerp(1, facing, _FadeByView);
                float dist = distance(_WorldSpaceCameraPos, IN.posWS);
                float fadeDist = saturate(1 - dist * _SizeFade * 0.05);

                float alpha = _Color.a * fadeView * fadeDist;
                half3 color = _Color.rgb * fadeDist;

                return half4(color, alpha * _Softness);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
