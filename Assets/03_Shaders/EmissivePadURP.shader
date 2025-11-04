Shader "Custom/EmissivePadURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.05, 0.05, 1)
        [HDR]_EmissionColor ("Emission Color (HDR)", Color) = (1,1,1,1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float  fogCoord    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = _BaseColor;
                // Emisión HDR = color * intensidad
                half3 emit = _EmissionColor.rgb * _EmissionIntensity;

                half3 color = baseCol.rgb + emit;
                color = MixFog(color, IN.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
