#ifndef STYLIZED_GRASS_CORE_INCLUDED
#define STYLIZED_GRASS_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Fallback for older URP versions that don't define the light loop macros
#ifndef LIGHT_LOOP_BEGIN
    #define LIGHT_LOOP_BEGIN(count) for (uint lightIndex = 0u; lightIndex < (uint)(count); ++lightIndex) {
    #define LIGHT_LOOP_END }
#endif

#define MAX_GRASS_INTERACTORS 16

// 4 bytes per instance: see GrassPack in C#
StructuredBuffer<uint> _Instances;

// Per chunk (set per draw call by GrassRenderer)
uint   _ChunkOffset;
float  _ChunkCount;  // instances in the chunk (full count, for the thinning rank)
float4 _ChunkMin;    // xyz = min corner, w = avg ground normal x
float4 _ChunkSize;   // xyz = size,       w = avg ground normal z

// Per layer (set per draw call by GrassRenderer)
float  _FadeStart;
float  _FadeEnd;
float  _Quality;      // 0..1 fraction of instances kept (graphics setting)
float  _QualityWidth; // width compensation for lower quality

// Set globally by GrassRenderer
float4 _GrassInteractors[MAX_GRASS_INTERACTORS];       // xyz = position, w = radius
float4 _GrassInteractorParams[MAX_GRASS_INTERACTORS];  // x = push (m), y = flatten (0..1), z = vertical range, w = core (0..1)
float  _GrassInteractorCount;

uint GrassHash(uint x)
{
    x ^= x >> 16; x *= 0x7feb352dU;
    x ^= x >> 15; x *= 0x846ca68bU;
    x ^= x >> 16;
    return x;
}

struct TuftData
{
    float3 root;
    float3 groundN;
    float  yaw;
    float  rnd;       // color variation / palette pick / wind phase
    float  rndScale;  // size
    float  rank;      // 0..1 uniform rank inside the chunk (instances are shuffled at bake)
};

TuftData UnpackTuft(uint instanceID)
{
    uint idx = _ChunkOffset + instanceID;
    uint d = _Instances[idx];

    float3 q = float3((float)(d & 1023u) * (1.0 / 1023.0),
                      (float)(d >> 20) * (1.0 / 4095.0),
                      (float)((d >> 10) & 1023u) * (1.0 / 1023.0));

    TuftData t;
    t.root = _ChunkMin.xyz + q * _ChunkSize.xyz;
    float nx = _ChunkMin.w, nz = _ChunkSize.w;
    t.groundN = float3(nx, sqrt(saturate(1.0 - nx * nx - nz * nz)), nz);

    // ONE hash gives yaw (16 bit), size (8 bit) and a random value (8 bit)
    uint h = GrassHash(idx * 747796405u + 2891336453u);
    t.yaw = (float)(h & 0xFFFFu) * (6.2831853 / 65535.0);
    t.rndScale = (float)((h >> 16) & 0xFFu) * (1.0 / 255.0);
    t.rnd = (float)(h >> 24) * (1.0 / 255.0);
    t.rank = ((float)instanceID + 0.5) / max(_ChunkCount, 1.0);
    return t;
}

// 1 if this instance should be drawn. Combines the quality setting and the distance fade
// into a single rank test, so thinning is uniform and matches what the CPU skips.
float TuftVisible(TuftData t, out float fade)
{
    float dist = distance(_WorldSpaceCameraPos, t.root);
    fade = saturate((dist - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
    return step(t.rank, _Quality * (1.0 - fade));
}

// lay: 0..1 how much things fall over; layDir: direction they fall (away from interactor); push: soft sideways push (meters)
void EvaluateInteractors(float3 root, out float lay, out float2 layDir, out float2 push)
{
    lay = 0.0;
    layDir = float2(0.0, 0.0);
    push = float2(0.0, 0.0);
    int count = (int)_GrassInteractorCount;
    [loop]
    for (int i = 0; i < count; i++)
    {
        float4 a = _GrassInteractors[i];
        float4 p = _GrassInteractorParams[i];
        float3 dd = root - a.xyz;
        float  d2 = length(dd.xz);
        if (d2 >= a.w) continue; // outside the radius: nothing to do

        float t = saturate((1.0 - d2 / max(a.w, 0.0001)) / max(1.0 - p.w, 0.05));
        float f = t * t * (3.0 - 2.0 * t);
        float range = max(p.z, 0.1);
        f *= 1.0 - smoothstep(range * 0.5, range, abs(dd.y));

        float2 away = dd.xz / max(d2, 0.001);
        lay = max(lay, f * p.y);
        layDir += away * (f * p.y);
        push += away * (f * p.x);
    }
    lay = saturate(lay);
}

// Rotates a point's height into a "fallen over" pose (about its base)
float3 ApplyLay(float3 rotated, float lay, float2 layDir)
{
    float2 ld = normalize(layDir + float2(1e-4, 0.0));
    float sa, ca;
    sincos(lay * 1.45, sa, ca);
    float yy = rotated.y;
    rotated.y = yy * ca;
    rotated.xz += ld * (yy * sa);
    return rotated;
}

#endif
