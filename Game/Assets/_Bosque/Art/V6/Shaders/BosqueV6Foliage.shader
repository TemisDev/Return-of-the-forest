// Vegetación realista V6: recorte alfa, doble cara, translucidez y viento por vértice.
// Compatible con SRP Batcher y GPU instancing. Sin scripts.
Shader "Bosque/V6/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Base (RGB) Alfa (A)", 2D) = "white" {}
        [MainColor] _BaseColor("Tinte", Color) = (1,1,1,1)
        _Cutoff("Recorte alfa", Range(0,1)) = 0.45
        [Normal] _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Intensidad normal", Range(0,2)) = 1
        _MaskMap("Máscara (G oclusión, A suavidad)", 2D) = "white" {}
        _Smoothness("Suavidad", Range(0,1)) = 0.5
        _OcclusionStrength("Oclusión", Range(0,1)) = 1
        _HueVariation("Variación de tono por instancia", Color) = (0.9, 1.0, 0.6, 0.12)
        _Translucency("Translucidez", Range(0,2)) = 0.6
        _TranslucencyColor("Color translúcido", Color) = (0.55, 0.75, 0.2, 1)
        _WindStrength("Viento: fuerza", Range(0,1)) = 0.08
        _WindSpeed("Viento: velocidad", Range(0,5)) = 1.4
        _WindFlutter("Viento: aleteo de hojas", Range(0,0.1)) = 0.01
        _WindHeight("Altura de referencia (m)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off

        HLSLINCLUDE
        #include "BosqueV6Common.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _BumpScale;
            half _Smoothness;
            half _OcclusionStrength;
            half4 _HueVariation;
            half _Translucency;
            half4 _TranslucencyColor;
            float _WindStrength;
            float _WindSpeed;
            float _WindFlutter;
            float _WindHeight;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 FoliageWorldPosition(float3 positionOS)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float3 origin = TransformObjectToWorld(float3(0, 0, 0));
            float h = saturate(positionOS.y / max(_WindHeight, 0.01));
            return positionWS + BosqueWindOffset(positionWS, origin, h, _WindStrength, _WindSpeed, _WindFlutter);
        }
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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half3 tint : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS = FoliageWorldPosition(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = n.normalWS;
                o.tangentWS = half4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                float r = frac(sin(dot(origin.xz, float2(12.9898, 78.233))) * 43758.5453);
                o.tint = lerp(half3(1, 1, 1), _HueVariation.rgb, r * _HueVariation.a * 4.0);
                return o;
            }

            half4 frag(Varyings i, bool isFront : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(albedo.a - _Cutoff);
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                half faceSign = isFront ? 1.0 : -1.0;
                half3 nrm = i.normalWS * faceSign;
                half3 tan = i.tangentWS.xyz;
                half3 bit = cross(nrm, tan) * i.tangentWS.w;
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(tan, bit, nrm));

                InputData inputData;
                BosqueInitInputData(i.positionWS, i.positionCS, normalWS, i.fogFactor, inputData);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo.rgb * i.tint;
                s.alpha = 1;
                s.metallic = 0;
                s.specular = half3(0, 0, 0);
                s.smoothness = mask.a * _Smoothness;
                s.occlusion = lerp(1, mask.g, _OcclusionStrength);
                s.normalTS = normalTS;

                half4 color = UniversalFragmentPBR(inputData, s);

                // Luz que atraviesa las hojas a contraluz.
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half back = pow(saturate(dot(inputData.viewDirectionWS, -mainLight.direction)), 4.0);
                half wrap = saturate(dot(-inputData.normalWS, mainLight.direction) * 0.5 + 0.5);
                color.rgb += s.albedo * _TranslucencyColor.rgb * mainLight.color *
                             mainLight.shadowAttenuation * (back * 0.8 + wrap * 0.25) * _Translucency;

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

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = FoliageWorldPosition(input.positionOS.xyz);
                o.positionCS = BosqueShadowPositionCS(positionWS, TransformObjectToWorldNormal(input.normalOS));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                return 0;
            }
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

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformWorldToHClip(FoliageWorldPosition(input.positionOS.xyz));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                return i.positionCS.z;
            }
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

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformWorldToHClip(FoliageWorldPosition(input.positionOS.xyz));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings i, bool isFront : SV_IsFrontFace) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                return half4(NormalizeNormalPerPixel(i.normalWS * (isFront ? 1.0 : -1.0)), 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
