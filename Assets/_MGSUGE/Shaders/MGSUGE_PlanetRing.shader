// Anneau de la planete : bandes concentriques transparentes (uv.x = rayon normalise), double face, sans brouillard.
Shader "MGSUGE/PlanetRing"
{
    Properties
    {
        _ColorA ("Bandes rose-ocre", Color) = (0.85, 0.55, 0.45, 1)
        _ColorB ("Voile violet", Color) = (0.45, 0.25, 0.6, 1)
        _Opacity ("Opacite", Range(0, 1)) = 0.55
        _Bands ("Nombre de bandes", Float) = 23
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA; half4 _ColorB; half _Opacity; half _Bands;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert (Attributes i)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float r = i.uv.x;
                float b = 0.5 + 0.5 * sin(r * _Bands * 6.2831) * sin(r * _Bands * 2.3 + 1.7);
                float edge = smoothstep(0.0, 0.08, r) * smoothstep(1.0, 0.85, r);
                half3 c = lerp(_ColorB.rgb, _ColorA.rgb, b);
                return half4(c, _Opacity * edge * (0.35 + 0.65 * b));
            }
            ENDHLSL
        }
    }
}
