Shader "Custom/URP/VolumetricBeam_Pro"
{
    Properties
    {
        [HDR]_BeamColor("Beam Color (HDR)", Color) = (1,0.9,0.4,1)
        _Intensity("Intensity", Range(0,20)) = 6
        _CoreRadius("Core Radius", Range(0,1)) = 0.25
        _CoreFeather("Core Feather", Range(0,1)) = 0.45
        _OuterSoftness("Outer Softness", Range(0,1)) = 0.7
        _HeightFalloff("Height Falloff", Range(0,4)) = 1.3
        [HDR]_AbsorbColor("Absorb Color", Color) = (1,0.85,0.35,1)
        _AbsorbStrength("Absorb Strength", Range(0,2)) = 0.7
        _NoiseAmount("Noise Amount", Range(0,1)) = 0.25
        _NoiseScale("Noise Scale", Range(0.1,8)) = 1.8
        _NoiseSpeed("Noise Speed", Range(0,5)) = 0.6
        _DepthFade("Depth Fade (m)", Range(0,2)) = 0.6
        _Alpha("Global Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 150

        Pass
        {
            Name "VolumetricBeam"
            Tags{ "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings  { float4 positionHCS:SV_POSITION; float3 posWS:TEXCOORD0; float3 posOS:TEXCOORD1; float fog:TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamColor; float _Intensity;
                float _CoreRadius; float _CoreFeather; float _OuterSoftness;
                float _HeightFalloff; float4 _AbsorbColor; float _AbsorbStrength;
                float _NoiseAmount; float _NoiseScale; float _NoiseSpeed;
                float _DepthFade; float _Alpha;
            CBUFFER_END

            float hash21(float2 p) { return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p); float2 f=frac(p);
                float a=hash21(i);
                float b=hash21(i+float2(1,0));
                float c=hash21(i+float2(0,1));
                float d=hash21(i+float2(1,1));
                float2 u=f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos=GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS=pos.positionCS;
                OUT.posWS=pos.positionWS;
                OUT.posOS=IN.positionOS.xyz;
                OUT.fog=ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN):SV_Target
            {
                float3 sx=float3(unity_ObjectToWorld._m00,unity_ObjectToWorld._m01,unity_ObjectToWorld._m02);
                float3 sy=float3(unity_ObjectToWorld._m10,unity_ObjectToWorld._m11,unity_ObjectToWorld._m12);
                float3 sz=float3(unity_ObjectToWorld._m20,unity_ObjectToWorld._m21,unity_ObjectToWorld._m22);
                float3 scale=float3(length(sx),length(sy),length(sz));

                float3 posOS=IN.posOS;
                float h=saturate(posOS.y / max(scale.y,1e-4));
                float2 radial=posOS.xz / max(scale.xz,float2(1e-4,1e-4));
                float rNorm=saturate(length(radial)/max(h,1e-4));

                float core=1.0 - smoothstep(_CoreRadius,_CoreRadius+_CoreFeather,rNorm);
                float edge=1.0 - smoothstep(1.0-_OuterSoftness,1.0,rNorm);
                float profile=saturate(core + edge*0.5);
                float heightAtten=pow(1.0 - h,_HeightFalloff);

                float2 uvSS=IN.positionHCS.xy / IN.positionHCS.w; uvSS=uvSS*0.5+0.5;
                float sceneDepth=SampleSceneDepth(uvSS);
                float df=saturate(sceneDepth - IN.positionHCS.z / max(_DepthFade,1e-4));

                float t=_Time.y*_NoiseSpeed;
                float n=noise(IN.posWS.xz*_NoiseScale+t);
                n=lerp(1.0,n,_NoiseAmount);

                float3 absorb=lerp(1.0.xxx,_AbsorbColor.rgb,saturate(h*_AbsorbStrength));

                float alpha=_Alpha * profile * heightAtten * df * n;
                float3 col=_BeamColor.rgb * absorb * _Intensity;
                col = MixFog(col, IN.fog);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
