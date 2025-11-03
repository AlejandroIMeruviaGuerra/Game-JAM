Shader "Custom/EpicTicTacToeBoard"
{
    Properties
    {
        _PrimaryColor ("Color Primario", Color) = (0.1, 0.1, 0.3, 1)
        _SecondaryColor ("Color Secundario", Color) = (0.3, 0.1, 0.5, 1)
        _GridColor ("Color Grid", Color) = (0, 0.8, 1, 0.5)
        _ParticleColor ("Color Partículas", Color) = (1, 0.5, 0, 1)
        _GridThickness ("Grosor Grid", Range(0.001, 0.05)) = 0.015
        _AnimationSpeed ("Velocidad Animación", Range(0, 2)) = 1.0
        _GlowFrequency ("Frecuencia Brillo", Range(0, 10)) = 3.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
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

            fixed4 _PrimaryColor;
            fixed4 _SecondaryColor;
            fixed4 _GridColor;
            fixed4 _ParticleColor;
            float _GridThickness;
            float _AnimationSpeed;
            float _GlowFrequency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Función de ruido simple
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _AnimationSpeed;
                
                // 1. FONDO GRADIENTE ANIMADO
                float gradient = sin(uv.x * 3.0 + time) * 0.3 + 0.7;
                fixed4 backgroundColor = lerp(_PrimaryColor, _SecondaryColor, gradient);
                
                // 2. TABLERO 3x3 CON EFECTO NEÓN
                float grid = 0.0;
                float glowIntensity = (sin(time * _GlowFrequency) + 1.0) * 0.3;
                
                // Líneas con efecto de brillo
                float verticalLine1 = step(0.333 - _GridThickness, uv.x) * step(uv.x, 0.333 + _GridThickness);
                float verticalLine2 = step(0.666 - _GridThickness, uv.x) * step(uv.x, 0.666 + _GridThickness);
                float horizontalLine1 = step(0.333 - _GridThickness, uv.y) * step(uv.y, 0.333 + _GridThickness);
                float horizontalLine2 = step(0.666 - _GridThickness, uv.y) * step(uv.y, 0.666 + _GridThickness);
                
                grid = verticalLine1 + verticalLine2 + horizontalLine1 + horizontalLine2;
                
                // 3. PARTÍCULAS FLOTANTES EN LAS CELDAS
                float particles = 0.0;
                float2 cellCenters[9] = {
                    float2(0.166, 0.166), float2(0.5, 0.166), float2(0.833, 0.166),
                    float2(0.166, 0.5), float2(0.5, 0.5), float2(0.833, 0.5),
                    float2(0.166, 0.833), float2(0.5, 0.833), float2(0.833, 0.833)
                };
                
                for (int j = 0; j < 9; j++)
                {
                    float2 center = cellCenters[j];
                    float dist = distance(uv, center);
                    
                    // Crear partículas que aparecen y desaparecen
                    float particleTime = time + j * 0.7;
                    float particleLife = sin(particleTime * 2.0) * 0.5 + 0.5;
                    
                    if (dist < 0.08 && particleLife > 0.3)
                    {
                        float particle = (1.0 - dist / 0.08) * particleLife;
                        particles += particle * 0.4;
                    }
                }
                
                // 4. EFECTO DE ONDAS CONCÉNTRICAS DESDE EL CENTRO
                float2 centerUV = uv - 0.5;
                float radius = length(centerUV);
                float waves = sin(radius * 20.0 - time * 3.0) * 0.1 + 0.9;
                
                // 5. MEZCLAR TODOS LOS EFECTOS
                fixed4 finalColor = backgroundColor;
                
                // Aplicar ondas
                finalColor.rgb *= waves;
                
                // Añadir grid con brillo
                finalColor = lerp(finalColor, _GridColor * (1.0 + glowIntensity), grid);
                
                // Añadir partículas
                finalColor.rgb += _ParticleColor.rgb * particles;
                
                // Añadir vignette (oscurecer bordes)
                float vignette = 1.0 - length(uv - 0.5) * 0.5;
                finalColor.rgb *= vignette;
                
                return finalColor;
            }
            ENDCG
        }
    }
}