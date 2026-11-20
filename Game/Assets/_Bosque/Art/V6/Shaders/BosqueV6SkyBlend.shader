// Cielo panorámico HDR que mezcla dos HDRI (degradado/restaurado). Lo maneja BosqueV6Ambience.
Shader "Bosque/V6/SkyBlend"
{
    Properties
    {
        [NoScaleOffset] _TexA("HDRI A (degradado)", 2D) = "grey" {}
        [NoScaleOffset] _TexB("HDRI B (restaurado)", 2D) = "grey" {}
        _Blend("Mezcla", Range(0,1)) = 0
        _ExposureA("Exposición A", Range(0,4)) = 1
        _ExposureB("Exposición B", Range(0,4)) = 1
        _Rotation("Rotación (grados)", Range(0,360)) = 0
        _TintA("Tinte A", Color) = (1,1,1,1)
        _TintB("Tinte B", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_TexA); SAMPLER(sampler_TexA);
            TEXTURE2D(_TexB); SAMPLER(sampler_TexB);
            CBUFFER_START(UnityPerMaterial)
                half _Blend, _ExposureA, _ExposureB; float _Rotation; half4 _TintA, _TintB;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A i)
            {
                V o;
                float r = radians(_Rotation); float s = sin(r), c = cos(r);
                float3 p = i.positionOS.xyz;
                o.dir = float3(c * p.x - s * p.z, p.y, s * p.x + c * p.z);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float2 uv = float2(atan2(d.x, d.z) / (2 * PI) + 0.5, asin(clamp(d.y, -1, 1)) / PI + 0.5);
                half3 a = SAMPLE_TEXTURE2D_LOD(_TexA, sampler_TexA, uv, 0).rgb * _ExposureA * _TintA.rgb;
                half3 b = SAMPLE_TEXTURE2D_LOD(_TexB, sampler_TexB, uv, 0).rgb * _ExposureB * _TintB.rgb;
                return half4(lerp(a, b, _Blend), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
