using UnityEngine;

namespace StylizedGrass
{
    public static class GrassBakeFormat
    {
        public const uint Magic = 0x53415247; // "GRAS"
        public const uint Version = 3;
        public const int HeaderWords = 4;     // magic, version, chunkCount, totalInstances
        public const int ChunkWords = 9;      // center xyz, extents xyz, count, avg normal x, avg normal z
        public const int InstanceStride = 4;  // 4 bytes per instance
    }

    /// <summary>
    /// 4 bytes per instance: x (10 bit) | z (10 bit) | y (12 bit), quantized inside the chunk's tight bounds
    /// (~1.5 cm precision on a 16 m chunk). Rotation / scale / color are derived from a hash in the shader,
    /// ground normal is stored once per chunk. Instances inside a chunk are SHUFFLED at bake time, so drawing
    /// the first N of them is a perfectly uniform thinning (used by the quality + distance LOD).
    /// </summary>
    public static class GrassPack
    {
        static uint Q(float v, float mn, float size, int max)
        {
            if (size <= 1e-6f) return 0;
            return (uint)Mathf.Clamp(Mathf.RoundToInt((v - mn) / size * max), 0, max);
        }

        public static uint Pack(Vector3 p, Vector3 mn, Vector3 size)
        {
            return Q(p.x, mn.x, size.x, 1023)
                 | (Q(p.z, mn.z, size.z, 1023) << 10)
                 | (Q(p.y, mn.y, size.y, 4095) << 20);
        }
    }
}
