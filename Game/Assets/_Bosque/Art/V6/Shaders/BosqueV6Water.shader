// Agua de arroyo V6: corriente, refracción, absorción por profundidad, cáusticas,
// reflejo del cielo, brillo solar y espuma en orillas/rocas. Requiere Depth y Opaque Texture
// activos en el asset URP (ya lo están en PC_RPAsset).
Shader "Bosque/V6/Water"
{
    Properties
    {
        _ShallowColor("Color poco profundo", Color) = (0.55, 0.78, 0.72, 1)
        _DeepColor("Color profundo", Color) = (0.05, 0.22, 0.24, 1)
        _DepthDistance("Distancia de absorción (m)", Float) = 0.9
        _Murkiness("Turbidez", Range(0,1)) = 0.1
        [Normal] _NormalA("Normal A (ondas)", 2D) = "bump" {}
        [Normal] _NormalB("Normal B (detalle)", 2D) = "bump" {}
        _NormalTiling("Metros por repetición", Float) = 2.5
        _NormalStrength("Intensidad de ondas", Range(0,2)) = 0.8
        _FlowDirection("Dirección de corriente (XZ)", Vector) = (0, 1, 0, 0)
        _FlowSpeed("Velocidad de corriente", Float) = 0.45
        _Refraction("Refracción", Range(0,0.2)) = 0.05
        _Smoothness("Suavidad", Range(0,1)) = 0.93
        _ReflectionStrength("Reflejo", Range(0,1)) = 0.8
        _NoiseTex("Ruido (R espuma, G variación, B cáusticas)", 2D) = "gray" {}
        _CausticStrength("Cáusticas", Range(0,3)) = 1.0
        _FoamColor("Espuma", Color) = (0.95, 0.97, 0.95, 0.9)
        _FoamDistance("Ancho de espuma en contacto (m)", Float) = 0.35
        _FlowFoam("Espuma de corriente", Range(0,1)) = 0.25
        _EdgeFade("Suavizado del borde (m)", Float) = 0.06
        _PuddleShape("Forma de charco irregular (UV radial)", Range(0,1)) = 0
        _FlowMap("Corriente por vértice (UV2)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "BosqueV6Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalA); SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor;
                float _DepthDistance; half _Murkiness;
                float _NormalTiling; half _NormalStrength;
                float4 _FlowDirection; float _FlowSpeed;
                half _Refraction, _Smoothness, _ReflectionStrength, _CausticStrength;
                half4 _FoamColor; float _FoamDistance; half _FlowFoam; float _EdgeFade; half _PuddleShape; half _FlowMap;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float2 flowV : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.uv = input.uv;
                o.flowV = input.uv2;
                return o;
            }

            float EyeDepthAt(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            // Corriente en dos fases desfasadas (técnica de flow map): la textura avanza con la corriente y se reinicia
            // a la mitad de cada fase, así la dirección puede variar por vértice sin costuras ni estiramientos.
            float FlowPhase(float offset) { return frac(_Time.y * 0.3 + offset); }
            float FlowWeight() { return 1 - abs(1 - 2 * FlowPhase(0)); }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 p = i.positionWS;
                float2 flow = normalize(lerp(_FlowDirection.xy, i.flowV, _FlowMap) + 1e-5);
                float2 side = float2(-flow.y, flow.x);
                float t = _Time.y * _FlowSpeed;
                float2 uv = p.xz / _NormalTiling;
                float travel = _FlowSpeed / 0.3;            // avance (en UV) durante una fase
                float ph0 = FlowPhase(0), ph1 = FlowPhase(0.5), w0 = FlowWeight();
                float2 d0 = flow * ph0 * travel, d1 = flow * ph1 * travel;

                // Ondas: dos capas desplazadas con la corriente a distinto ritmo, cada una en dos fases.
                half3 nA = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uv + d0), _NormalStrength) * w0
                         + UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uv + d1 + 0.37), _NormalStrength) * (1 - w0);
                half3 nB = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalA, uv * 2.3 + d0 * 1.7), _NormalStrength * 0.6) * w0
                         + UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalA, uv * 2.3 + d1 * 1.7 + 0.61), _NormalStrength * 0.6) * (1 - w0);
                nA = normalize(nA); nB = normalize(nB);
                half3 nT = normalize(half3(nA.xy + nB.xy, nA.z * nB.z));
                half3 normalWS = normalize(half3(nT.x, nT.z, nT.y));

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float surfaceEye = i.screenPos.w;
                float thickness = max(EyeDepthAt(screenUV) - surfaceEye, 0);

                float2 refrUV = screenUV + nT.xy * _Refraction * saturate(thickness * 2.0);
                float refrThickness = EyeDepthAt(refrUV) - surfaceEye;
                if (refrThickness < 0) { refrUV = screenUV; refrThickness = thickness; }
                refrThickness = max(refrThickness, 0);

                half3 viewDir = GetWorldSpaceNormalizeViewDir(p);
                float4 shadowCoord = TransformWorldToShadowCoord(p);
                Light sun = GetMainLight(shadowCoord);
                half shadow = lerp(0.35, 1.0, sun.shadowAttenuation);

                // Lo que se ve a través del agua, absorbido según la profundidad.
                half3 under = SampleSceneColor(refrUV);
                float3 bedPos = p + (-viewDir) * refrThickness;
                float2 drift = float2(0.6, 0.8) * t;
                half caust = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, bedPos.xz * 0.45 + drift * 0.35).b;
                caust *= SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, bedPos.xz * 0.31 - drift * 0.21 + 0.3).b * 2.0;
                half absorb = 1 - exp(-refrThickness / max(_DepthDistance, 0.01));
                under += caust * _CausticStrength * sun.color * shadow * (1 - absorb) * 0.6;
                half3 transmitted = lerp(under * _ShallowColor.rgb, _DeepColor.rgb * (SampleSHPixel(half3(0,0,0), half3(0,1,0)) + sun.color * 0.25),
                                         saturate(absorb + _Murkiness * saturate(refrThickness * 2.2)));   // la turbidez se nota con la profundidad

                // Reflejo del cielo/sonda + brillo especular del sol.
                half3 reflDir = reflect(-viewDir, normalWS);
                half perceptualRoughness = 1 - _Smoothness;
                half3 refl = GlossyEnvironmentReflection(reflDir, p, perceptualRoughness, 1, GetNormalizedScreenSpaceUV(i.positionCS));
                // Respaldo: si no hay cubemap de reflejos horneado, el cielo ambiental en esa dirección.
                refl = max(refl, SampleSHPixel(half3(0,0,0), reflDir) * 1.15);
                half ndv = saturate(dot(normalWS, viewDir));
                half fresnel = 0.02 + 0.98 * pow(1 - ndv, 5);
                half3 color = lerp(transmitted, refl, saturate(fresnel * _ReflectionStrength * 1.5));
                half3 h = normalize(sun.direction + viewDir);
                half spec = pow(saturate(dot(normalWS, h)), exp2(10 * _Smoothness + 1)) * (_Smoothness * 6);
                color += spec * sun.color * sun.shadowAttenuation;

                // Espuma: contacto con orillas/rocas + vetas que viajan con la corriente.
                half foamNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.6 + d0 * 1.2).r * w0
                               + SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.6 + d1 * 1.2 + 0.5).r * (1 - w0);
                half edge = 1 - saturate(thickness / max(_FoamDistance, 0.01));
                half foam = smoothstep(0.45, 0.75, edge * 0.9 + foamNoise * edge);
                half streak = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(dot(p.xz, side) * 1.7, dot(p.xz, flow) * 0.45 - t * 0.9)).g;
                streak *= SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xz * 0.6 + drift * 0.5).r * 1.6;
                foam = max(foam, smoothstep(0.78, 0.98, streak) * _FlowFoam);
                half3 foamLit = _FoamColor.rgb * (sun.color * shadow * saturate(dot(half3(0,1,0), sun.direction)) + SampleSHPixel(half3(0,0,0), half3(0,1,0)));
                color = lerp(color, foamLit, foam * _FoamColor.a);

                // Charcos: contorno irregular (UV radial del disco + ruido de mundo) y borde de barro húmedo.
                half rim = length(i.uv - 0.5) * 2.0;
                half wobble = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xz * 0.35).r - 0.5;
                // V16: contorno difuminado y borde de tierra mojada (el mismo suelo, oscurecido), no un aro de barro.
                half shape = saturate((0.84 + wobble * 0.7 - rim) / 0.26);
                half inner = saturate((0.55 + wobble * 0.7 - rim) / 0.32);
                half3 wetGround = SampleSceneColor(screenUV) * 0.52;
                half3 mud = lerp(wetGround, _FoamColor.rgb * (sun.color * shadow * 0.55 + SampleSHPixel(half3(0,0,0), half3(0,1,0))), _FoamColor.a);
                color = lerp(color, lerp(mud, color, inner), _PuddleShape);

                color = MixFog(color, InitializeInputDataFog(float4(p, 1), i.fogFactor));
                half alpha = saturate(thickness / max(_EdgeFade, 0.001));
                alpha *= lerp(1, shape, _PuddleShape);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
