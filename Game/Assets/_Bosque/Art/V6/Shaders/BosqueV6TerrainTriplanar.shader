// Suelo/roca V6 en espacio mundo: no depende de las UV ni de la escala de los bloques.
// Capa superior (suelo) en caras horizontales, capa lateral (roca) en paredes,
// mezcla por altura, variación de macro-escala y humedad bajo un nivel de agua.
Shader "Bosque/V6/TerrainTriplanar"
{
    Properties
    {
        [Header(Capa superior)]
        _TopBaseMap("Base", 2D) = "white" {}
        [Normal] _TopNormal("Normal", 2D) = "bump" {}
        _TopMask("Máscara (G AO, B altura, A suavidad)", 2D) = "white" {}
        _TopColor("Tinte", Color) = (1,1,1,1)
        _TopTiling("Metros por repetición", Float) = 3

        [Header(Capa lateral)]
        _SideBaseMap("Base", 2D) = "white" {}
        [Normal] _SideNormal("Normal", 2D) = "bump" {}
        _SideMask("Máscara (G AO, B altura, A suavidad)", 2D) = "white" {}
        _SideColor("Tinte", Color) = (1,1,1,1)
        _SideTiling("Metros por repetición", Float) = 4

        [Header(Mezcla)]
        _TopThreshold("Inclinación mínima para suelo", Range(0,1)) = 0.6
        _BlendSoftness("Suavidad de transición", Range(0.01,1)) = 0.15
        _HeightContrast("Contraste por altura", Range(0,2)) = 0.6
        _NormalStrength("Intensidad normal", Range(0,2)) = 1
        _SmoothnessScale("Suavidad global", Range(0,2)) = 1

        [Header(Variacion y humedad)]
        _MacroNoise("Ruido de macro-variación", 2D) = "gray" {}
        _MacroScale("Escala macro (m)", Float) = 18
        _MacroStrength("Intensidad macro", Range(0,1)) = 0.25
        _WetLevel("Altura del agua (Y mundo)", Float) = -100
        _WetRange("Franja húmeda (m)", Float) = 0.35
        _WetDarkening("Oscurecido húmedo", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "BosqueV6Common.hlsl"

        TEXTURE2D(_TopBaseMap); SAMPLER(sampler_TopBaseMap);
        TEXTURE2D(_TopNormal); TEXTURE2D(_TopMask);
        TEXTURE2D(_SideBaseMap); SAMPLER(sampler_SideBaseMap);
        TEXTURE2D(_SideNormal); TEXTURE2D(_SideMask);
        TEXTURE2D(_MacroNoise); SAMPLER(sampler_MacroNoise);

        CBUFFER_START(UnityPerMaterial)
            half4 _TopColor; float _TopTiling;
            half4 _SideColor; float _SideTiling;
            half _TopThreshold, _BlendSoftness, _HeightContrast, _NormalStrength, _SmoothnessScale;
            float _MacroScale; half _MacroStrength;
            float _WetLevel; float _WetRange; half _WetDarkening;
        CBUFFER_END

        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Normal de proyección "whiteout" (Golus) para una proyección dada.
            half3 Whiteout(half3 tn, half2 nAxis, half nComp)
            {
                return half3(tn.xy + nAxis, abs(tn.z) * nComp);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 p = i.positionWS;
                half3 n = normalize(i.normalWS);
                half3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
                half3 s = step(0, n) * 2 - 1;

                // --- Capa lateral, triplanar completa
                float2 uvX = p.zy / _SideTiling; uvX.x *= s.x;
                float2 uvY = p.xz / _SideTiling; uvY.x *= s.y;
                float2 uvZ = p.xy / _SideTiling; uvZ.x *= -s.z;
                half4 sX = SAMPLE_TEXTURE2D(_SideBaseMap, sampler_SideBaseMap, uvX);
                half4 sY = SAMPLE_TEXTURE2D(_SideBaseMap, sampler_SideBaseMap, uvY);
                half4 sZ = SAMPLE_TEXTURE2D(_SideBaseMap, sampler_SideBaseMap, uvZ);
                half4 mX = SAMPLE_TEXTURE2D(_SideMask, sampler_SideBaseMap, uvX);
                half4 mY = SAMPLE_TEXTURE2D(_SideMask, sampler_SideBaseMap, uvY);
                half4 mZ = SAMPLE_TEXTURE2D(_SideMask, sampler_SideBaseMap, uvZ);
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideBaseMap, uvX), _NormalStrength);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideBaseMap, uvY), _NormalStrength);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideBaseMap, uvZ), _NormalStrength);
                nX.x *= s.x; nY.x *= s.y; nZ.x *= -s.z;
                half3 sideAlb = (sX.rgb * w.x + sY.rgb * w.y + sZ.rgb * w.z) * _SideColor.rgb;
                half4 sideMask = mX * w.x + mY * w.y + mZ * w.z;
                half3 sideN = normalize(Whiteout(nX, n.zy, n.x).zyx * w.x + Whiteout(nY, n.xz, n.y).xzy * w.y + Whiteout(nZ, n.xy, n.z) * w.z);

                // --- Capa superior, proyección vertical
                float2 uvT = p.xz / _TopTiling;
                half3 topAlb = SAMPLE_TEXTURE2D(_TopBaseMap, sampler_TopBaseMap, uvT).rgb * _TopColor.rgb;
                half4 topMask = SAMPLE_TEXTURE2D(_TopMask, sampler_TopBaseMap, uvT);
                half3 tT = UnpackNormalScale(SAMPLE_TEXTURE2D(_TopNormal, sampler_TopBaseMap, uvT), _NormalStrength);
                half3 topN = normalize(Whiteout(tT, n.xz, n.y).xzy);

                // --- Mezcla por inclinación + altura de ambas capas
                half slope = (n.y - _TopThreshold) / _BlendSoftness;
                half t = saturate(slope + (topMask.b - sideMask.b) * _HeightContrast);
                t = smoothstep(0, 1, t);

                half3 albedo = lerp(sideAlb, topAlb, t);
                half4 mask = lerp(sideMask, topMask, t);
                half3 normalWS = normalize(lerp(sideN, topN, t));

                // --- Macro-variación para romper la repetición
                half macro = SAMPLE_TEXTURE2D(_MacroNoise, sampler_MacroNoise, p.xz / _MacroScale).r;
                half macro2 = SAMPLE_TEXTURE2D(_MacroNoise, sampler_MacroNoise, p.xz / (_MacroScale * 0.37) + 0.5).g;
                albedo *= lerp(1, (macro * 0.6 + macro2 * 0.4) * 2.0 * 0.85 + 0.15, _MacroStrength);

                // --- Humedad junto al agua
                half wet = saturate((_WetLevel - p.y) / max(_WetRange, 0.01) + 1.0) * step(-99, _WetLevel);
                albedo *= lerp(1, 1 - _WetDarkening, wet);
                half smooth = lerp(mask.a * _SmoothnessScale, 0.82, wet * 0.8);

                InputData inputData;
                BosqueInitInputData(p, i.positionCS, normalWS, i.fogFactor, inputData);
                SurfaceData sd = (SurfaceData)0;
                sd.albedo = albedo;
                sd.alpha = 1;
                sd.smoothness = saturate(smooth);
                sd.occlusion = mask.g;
                sd.normalTS = half3(0, 0, 1);
                half4 color = UniversalFragmentPBR(inputData, sd);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return BosqueShadowPositionCS(TransformObjectToWorld(input.positionOS.xyz), TransformObjectToWorldNormal(input.normalOS));
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }
            half4 frag(float4 positionCS : SV_POSITION) : SV_Target { return positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };
            V vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                V o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }
            half4 frag(V i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
    FallBack Off
}
