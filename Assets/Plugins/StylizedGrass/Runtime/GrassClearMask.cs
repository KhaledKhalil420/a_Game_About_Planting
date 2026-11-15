using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedGrass
{
    /// <summary>
    /// Hides grass and details inside areas (farmland...) at runtime, no re-bake needed.
    /// Add(bounds) when the object appears, Remove(bounds) with the SAME bounds when it goes away.
    /// Covers the combined bake bounds of every GrassRenderer in the scene.
    /// </summary>
    public static class GrassClearMask
    {
        const float MaxTexelsPerMeter = 4f;
        const int MaxSize = 4096;
        const float Padding = 0.15f; // hides a little past the edge so blades don't poke over it

        static readonly int ID_Mask = Shader.PropertyToID("_GrassClearMask");
        static readonly int ID_Rect = Shader.PropertyToID("_GrassClearRect");
        static readonly int ID_Size = Shader.PropertyToID("_GrassClearSize");

        static Texture2D tex;
        static byte[] counts; // how many areas cover each texel, so overlapping ones don't un-hide each other
        static Vector2 origin;
        static float texelsPerMeter;
        static int width, height;
        static int dirtyMin = int.MaxValue, dirtyMax = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            Application.quitting -= Cleanup;
            Shader.SetGlobalVector(ID_Rect, Vector4.zero);
            GrassRenderer.SafeDestroy(tex);
            tex = null;
            counts = null;
            dirtyMin = int.MaxValue;
            dirtyMax = -1;
        }

        public static bool Add(Bounds b)
        {
            if (!Init()) return false;
            Paint(b, 1);
            return true;
        }

        public static void Remove(Bounds b)
        {
            if (tex != null) Paint(b, -1);
        }

        static bool Init()
        {
            if (tex != null) return true;

            var renderers = Object.FindObjectsByType<GrassRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (renderers.Length == 0) return false;

            Bounds area = renderers[0].bakeBounds;
            for (int i = 1; i < renderers.Length; i++)
                area.Encapsulate(renderers[i].bakeBounds);

            texelsPerMeter = Mathf.Min(MaxTexelsPerMeter, MaxSize / Mathf.Max(area.size.x, area.size.z, 1f));
            width = Mathf.Max(1, Mathf.CeilToInt(area.size.x * texelsPerMeter));
            height = Mathf.Max(1, Mathf.CeilToInt(area.size.z * texelsPerMeter));
            origin = new Vector2(area.min.x, area.min.z);

            counts = new byte[width * height];
            tex = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Shader.SetGlobalTexture(ID_Mask, tex);
            Shader.SetGlobalVector(ID_Rect, new Vector4(origin.x, origin.y, texelsPerMeter, 1f));
            Shader.SetGlobalVector(ID_Size, new Vector4(width, height, 0f, 0f));

            dirtyMin = 0; // first upload also clears the new texture
            dirtyMax = height - 1;

            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            Application.quitting += Cleanup;
            return true;
        }

        static void Paint(Bounds b, int delta)
        {
            b.Expand(Padding * 2f);

            int x0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - origin.x) * texelsPerMeter));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt((b.max.x - origin.x) * texelsPerMeter) - 1);
            int z0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - origin.y) * texelsPerMeter));
            int z1 = Mathf.Min(height - 1, Mathf.CeilToInt((b.max.z - origin.y) * texelsPerMeter) - 1);
            if (x0 > x1 || z0 > z1) return;

            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int i = z * width + x;
                    counts[i] = (byte)Mathf.Clamp(counts[i] + delta, 0, 255);
                }
            }

            dirtyMin = Mathf.Min(dirtyMin, z0);
            dirtyMax = Mathf.Max(dirtyMax, z1);
        }

        // Uploads the changed rows once per frame, however many squares were added or removed
        static void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (tex == null || dirtyMax < dirtyMin) return;

            var raw = tex.GetRawTextureData<byte>();
            for (int i = dirtyMin * width; i < (dirtyMax + 1) * width; i++)
                raw[i] = counts[i] > 0 ? (byte)255 : (byte)0;

            tex.Apply(false);
            dirtyMin = int.MaxValue;
            dirtyMax = -1;
        }
    }
}
