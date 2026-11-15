#ifndef STYLIZED_GRASS_DETAIL_COMMON_INCLUDED
#define STYLIZED_GRASS_DETAIL_COMMON_INCLUDED

#include "GrassCore.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float4 _Palette[8];

CBUFFER_START(UnityPerMaterial)
    float4 _WindDirection;
    float4 _SizeRange;
    float  _Size;
    float  _MeshHeight;
    float  _RootDarkening;
    float  _LightWrap;
    float  _FlattenResponse;
    float  _FlattenMin;
    float  _WindStrength;
    float  _WindSpeed;
    float  _WindScale;
    float  _PaletteCount;
CBUFFER_END

struct DetailVertex
{
    float3 positionWS;
    float3 normalWS;
    float2 uv;
    float  height01;
    float3 tint;
};

DetailVertex EvaluateDetail(float3 positionOS, float3 normalOS, float2 uv, float4 vcol, uint instanceID)
{
    TuftData t = UnpackTuft(instanceID);
    float fade;
    float visible = TuftVisible(t, fade);

    DetailVertex o;
    o.positionWS = t.root;
    o.normalWS = float3(0, 1, 0);
    o.uv = uv;
    o.height01 = 0;
    o.tint = vcol.rgb;

    [branch]
    if (visible < 0.5) return o;

    float distScale = lerp(1.0, 0.6, fade);

    // --- Interactors (scaled by this layer's response) ---
    float lay = 0.0; float2 layDir = float2(0, 0); float2 push = float2(0, 0);
    [branch]
    if (_FlattenResponse > 0.001)
    {
        EvaluateInteractors(t.root, lay, layDir, push);
        lay = saturate(lay * _FlattenResponse);
        push *= _FlattenResponse;
    }

    float scale = lerp(_SizeRange.x, _SizeRange.y, t.rndScale) * _Size * distScale;
    float heightScale = scale * lerp(1.0, _FlattenMin, lay);

    float3 local = positionOS * float3(scale, heightScale, scale);
    float s, c;
    sincos(t.yaw, s, c);
    float3 rotated = float3(local.x * c + local.z * s, local.y, -local.x * s + local.z * c);
    rotated = ApplyLay(rotated, lay, layDir);

    float3 posWS = t.root + rotated;

    // --- Wind + push, weighted by height on the mesh (skipped when the layer doesn't sway) ---
    float h = saturate(positionOS.y / max(_MeshHeight, 0.001));
    [branch]
    if (_WindStrength > 0.0001 || _FlattenResponse > 0.001)
    {
        float bend = h * h;
        float2 windDir = normalize(_WindDirection.xy + float2(1e-5, 0.0));
        float2 wp = t.root.xz * _WindScale + _Time.y * _WindSpeed;
        float gust = sin(wp.x + wp.y * 0.6) * 0.6 + sin(wp.x * 1.9 - wp.y * 1.3 + t.rnd * 6.2831) * 0.4;
        float2 windDisp = windDir * gust * _WindStrength * scale * _MeshHeight * (1.0 - lay);
        float2 disp = windDisp * bend + push * (1.0 - lay) * h;
        posWS.xz += disp;
        posWS.y  -= min(length(disp), rotated.y) * 0.5;
    }

    // --- Palette pick (per instance) ---
    uint count = (uint)max(_PaletteCount, 1.0);
    uint pick = min((uint)(t.rnd * count), count - 1u);
    float3 pal = _Palette[pick].rgb;

    o.positionWS = posWS;
    o.normalWS = normalize(float3(normalOS.x * c + normalOS.z * s, normalOS.y, -normalOS.x * s + normalOS.z * c));
    o.height01 = h;
    o.tint = lerp(vcol.rgb, vcol.rgb * pal, vcol.a); // vertex alpha = "takes palette color"
    return o;
}

#endif
