// Partículas V6: color de vértice, partículas suaves contra la geometría, fundido cercano
// a cámara e influencia opcional de la luz de escena (para polvo/humo que no "brille").
Shader "Bosque/V6/Particle"
{
    Properties
    {
        [MainTexture] _BaseMap("Textura", 2D) = "white" {}
        [HDR] _TintColor("Tinte", Color) = (1,1,1,1)
        _SoftDistance("Suavizado con geometría (m)", Float) = 0.4
        _CameraFadeNear("Fundido cerca de cámara (m)", Float) = 0.3
        _CameraFadeFar("Fin fundido cámara (m)", Float) = 1.2
        _LightInfluence("Influencia de la luz", Range(0,1)) = 0
        _AlphaCutoff("Recorte alfa", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Mezcla origen", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Mezcla destino", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "Particle"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "BosqueV6Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TintColor;
                float _SoftDistance, _CameraFadeNear, _CameraFadeFar;
                half _LightInfluence, _AlphaCutoff;
                float _SrcBlend, _DstBlend;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                half3 light : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.color = input.color;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                Light sun = GetMainLight();
                half3 lit = sun.color * 0.8 + SampleSHPixel(half3(0,0,0), half3(0,1,0));
                o.light = lerp(half3(1,1,1), lit, _LightInfluence);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _TintColor * i.color;
                clip(c.a - _AlphaCutoff - 0.001);
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float eye = i.screenPos.w;
                c.a *= saturate((sceneEye - eye) / max(_SoftDistance, 0.001));
                c.a *= saturate((eye - _CameraFadeNear) / max(_CameraFadeFar - _CameraFadeNear, 0.001));
                c.rgb *= i.light;
                c.rgb = MixFogColor(c.rgb, unity_FogColor.rgb, i.fogFactor);
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
