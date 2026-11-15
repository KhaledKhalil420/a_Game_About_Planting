#ifndef STYLIZED_GRASS_COMMON_INCLUDED
#define STYLIZED_GRASS_COMMON_INCLUDED

#include "GrassCore.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _TipColor;
    float4 _VariationColor;
    float4 _WindDirection;
    float4 _ScaleRange;
    float  _ColorVariation;
    float  _RootDarkening;
    float  _BladeHeight;
    float  _TuftSize;
    float  _FlattenMin;
    float  _WindStrength;
    float  _WindSpeed;
    float  _WindScale;
    float  _LightWrap;
CBUFFER_END

struct GrassVertex
{
    float3 positionWS;
    float3 normalWS;
    float2 uv;
    float  variation;
};

GrassVertex EvaluateGrass(float3 positionOS, float2 uv, uint instanceID)
{
    TuftData t = UnpackTuft(instanceID);
    float fade;
    float visible = TuftVisible(t, fade);

    GrassVertex o;
    o.positionWS = t.root;
    o.normalWS = float3(0, 1, 0);
    o.uv = uv;
    o.variation = 0;

    // Culled by quality / distance: collapse to a point and skip ALL the work below
    [branch]
    if (visible < 0.5) return o;

    float scale = lerp(_ScaleRange.x, _ScaleRange.y, t.rndScale);
    float distScale = lerp(1.0, 0.6, fade);

    // --- Interactors ---
    float lay; float2 layDir; float2 push;
    EvaluateInteractors(t.root, lay, layDir, push);

    float heightScale = scale * distScale * lerp(1.0, _FlattenMin, lay);
    float widthScale  = scale * _QualityWidth;

    float3 local = positionOS * float3(_TuftSize * widthScale, _BladeHeight * heightScale, _TuftSize * widthScale);
    float s, c;
    sincos(t.yaw, s, c);
    float3 rotated = float3(local.x * c + local.z * s, local.y, -local.x * s + local.z * c);
    rotated = ApplyLay(rotated, lay, layDir);

    float3 posWS = t.root + rotated;

    // --- Wind + soft push (less of both when flattened) ---
    float bend = uv.y * uv.y;
    float2 windDir = normalize(_WindDirection.xy + float2(1e-5, 0.0));
    float2 wp = t.root.xz * _WindScale + _Time.y * _WindSpeed;
    float gust = sin(wp.x + wp.y * 0.6) * 0.6 + sin(wp.x * 1.9 - wp.y * 1.3 + t.rnd * 6.2831) * 0.4;
    float2 windDisp = windDir * gust * _WindStrength * _BladeHeight * heightScale * (1.0 - lay);
    float2 disp = windDisp * bend + push * (1.0 - lay) * uv.y;
    posWS.xz += disp;
    posWS.y  -= min(length(disp), rotated.y) * 0.5;

    o.positionWS = posWS;
    o.normalWS = normalize(lerp(float3(0, 1, 0), t.groundN, 0.5));
    o.variation = frac(t.rnd * 91.7 + t.yaw);
    return o;
}

#endif
