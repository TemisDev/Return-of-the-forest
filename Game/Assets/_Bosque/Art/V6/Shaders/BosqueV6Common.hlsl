#ifndef BOSQUE_V6_COMMON_INCLUDED
#define BOSQUE_V6_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Definidos por URP al renderizar sombras; se declaran aquí para los pases propios.
float3 _LightDirection;
float3 _LightPosition;

// Viento de vegetación: oscilación principal por posición del objeto + aleteo de hojas.
// heightMask 0 en la base, 1 en la copa. Sin dependencias de scripts.
float3 BosqueWindOffset(float3 positionWS, float3 objectOriginWS, float heightMask,
                        float strength, float speed, float flutter)
{
    float t = _Time.y * speed;
    float phase = dot(objectOriginWS.xz, float2(0.37, 0.71));
    float2 dir = normalize(float2(1.0, 0.35));
    float sway = sin(t + phase) * 0.65 + sin(t * 2.31 + phase * 1.7) * 0.25 + sin(t * 0.43 + phase) * 0.35;
    float3 offset = float3(dir.x, 0.0, dir.y) * sway * strength * heightMask * heightMask;
    float leaf = sin(_Time.y * 7.3 + dot(positionWS, float3(4.1, 2.7, 3.3))) * flutter * heightMask;
    offset += float3(leaf, leaf * 0.6, -leaf * 0.8);
    offset.y -= abs(sway) * strength * heightMask * 0.15;
    return offset;
}

void BosqueInitInputData(float3 positionWS, float4 positionCS, half3 normalWS, half fogFactor, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.positionCS = positionCS;
    inputData.normalWS = NormalizeNormalPerPixel(normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), fogFactor);
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = SampleSHPixel(half3(0, 0, 0), inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);
}

float4 BosqueShadowPositionCS(float3 positionWS, float3 normalWS)
{
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    return ApplyShadowClamping(positionCS);
}

#endif
