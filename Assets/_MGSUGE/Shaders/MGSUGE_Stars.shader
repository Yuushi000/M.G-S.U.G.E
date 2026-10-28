// Etoiles : maillage de points colores (6 500 petites + 600 grosses), plus visibles en altitude (_Fade), sans brouillard.
Shader "MGSUGE/Stars"
{
    Properties
    {
        _Fade ("Visibilite", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float size : PSIZE; };
            Varyings vert (Attributes i)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.color = i.color; o.size = 1.5; return o;
            }
            half4 frag (Varyings i) : SV_Target { return half4(i.color.rgb * _Fade, 1); }
            ENDHLSL
        }
    }
}
