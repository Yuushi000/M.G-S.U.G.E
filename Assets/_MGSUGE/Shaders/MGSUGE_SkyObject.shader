// Astres lointains (planete, lune) : eclairage par le soleil + liseret d'atmosphere, sans brouillard (ils sont "a l'infini").
Shader "MGSUGE/SkyObject"
{
    Properties
    {
        _BaseColor ("Couleur", Color) = (0.7, 0.5, 0.45, 1)
        _RimColor ("Halo d'atmosphere", Color) = (0.6, 0.35, 0.9, 1)
        _RimPower ("Finesse du halo", Float) = 3
        _Ambient ("Ambiance", Float) = 0.12
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _RimColor; half _RimPower; half _Ambient;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };
            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float lit = saturate(dot(n, _MainLightPosition.xyz));
                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
                half3 c = _BaseColor.rgb * (lit + _Ambient) + _RimColor.rgb * rim;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
