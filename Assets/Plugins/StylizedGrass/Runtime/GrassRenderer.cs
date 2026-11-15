using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedGrass
{
    public enum DetailShape { Flower, Daisy, Mushroom, Rock, CustomMesh }

    internal struct ScatterChunk
    {
        public Bounds bounds;
        public Vector4 minN;   // xyz = min corner, w = avg ground normal x
        public Vector4 sizeN;  // xyz = size,       w = avg ground normal z
        public int start;
        public int count;
    }

    /// <summary>One baked file on the GPU (shared by grass and every detail layer).</summary>
    internal class ScatterData
    {
        public ScatterChunk[] chunks;
        public GraphicsBuffer buffer;
        public TextAsset loaded;
        public int total;

        public bool Ensure(TextAsset asset, Object ctx)
        {
            if (asset == null)
            {
                if (loaded != null || buffer != null) Release();
                return false;
            }
            if (loaded != asset)
            {
                Release();
                loaded = asset;
                Load(asset, ctx);
            }
            return buffer != null;
        }

        void Load(TextAsset asset, Object ctx)
        {
            NativeArray<byte> raw = asset.GetData<byte>();
            if (raw.Length < GrassBakeFormat.HeaderWords * 4) return;

            var words = raw.Reinterpret<uint>(1);
            if (words[0] != GrassBakeFormat.Magic || words[1] != GrassBakeFormat.Version)
            {
                Debug.LogError("[StylizedGrass] Baked data has the wrong format/version. Please re-bake.", ctx);
                return;
            }

            int chunkCount = (int)words[2];
            int count = (int)words[3];
            var floats = raw.Reinterpret<float>(1);

            chunks = new ScatterChunk[chunkCount];
            int w = GrassBakeFormat.HeaderWords;
            int start = 0;
            for (int c = 0; c < chunkCount; c++)
            {
                var center = new Vector3(floats[w], floats[w + 1], floats[w + 2]);
                var ext = new Vector3(floats[w + 3], floats[w + 4], floats[w + 5]);
                int n = (int)words[w + 6];
                float nx = floats[w + 7], nz = floats[w + 8];
                w += GrassBakeFormat.ChunkWords;

                Vector3 mn = center - ext, size = ext * 2f;
                chunks[c] = new ScatterChunk
                {
                    bounds = new Bounds(center, size),
                    minN = new Vector4(mn.x, mn.y, mn.z, nx),
                    sizeN = new Vector4(size.x, size.y, size.z, nz),
                    start = start,
                    count = n
                };
                start += n;
            }

            total = count;
            if (count > 0)
            {
                int byteOffset = (GrassBakeFormat.HeaderWords + GrassBakeFormat.ChunkWords * chunkCount) * 4;
                var inst = raw.GetSubArray(byteOffset, count * GrassBakeFormat.InstanceStride).Reinterpret<uint>(1);
                buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, GrassBakeFormat.InstanceStride);
                buffer.SetData(inst);
            }
        }

        public void Release()
        {
            if (buffer != null) { buffer.Release(); buffer = null; }
            chunks = null;
            loaded = null;
            total = 0;
        }
    }

    /// <summary>
    /// One scatter layer: flowers, mushrooms, rocks, your own meshes...
    /// Same GPU-instanced, chunked pipeline as the grass.
    /// </summary>
    [System.Serializable]
    public class DetailLayer
    {
        public string name = "Flowers";
        public bool enabled = true;
        public TextAsset bakedData;

        [Header("Shape")]
        public DetailShape shape = DetailShape.Flower;
        [Tooltip("Used when Shape = Custom Mesh. Should sit on y = 0, pointing up. Vertex colors (alpha = takes palette color) are supported.")]
        public Mesh customMesh;
        [Tooltip("Optional texture multiplied on top (custom meshes with UVs).")]
        public Texture2D texture;
        public bool doubleSided = true;

        [Header("Bake (needs re-bake)")]
        [Tooltip("Instances per square meter INSIDE patches.")]
        public float density = 0.35f;
        [Range(0, 1), Tooltip("0 = spread evenly everywhere, 1 = only inside patches.")]
        public float clumping = 0.8f;
        [Tooltip("Size of the patches in meters.")]
        public float patchSize = 7f;
        [Range(0, 1), Tooltip("How much of the ground the patches cover.")]
        public float patchCoverage = 0.35f;
        [Range(0, 89)] public float maxSlope = 35f;
        [Tooltip("Also multiply by the grass density mask (paint where you want things).")]
        public bool useDensityMask = true;
        public int seed = 11;

        [Header("Look (live)")]
        [Tooltip("Procedural shapes: height in meters. Custom mesh: size multiplier (1 = native size).")]
        public float size = 0.4f;
        public Vector2 sizeRange = new Vector2(0.7f, 1.3f);
        [Tooltip("Each instance picks one of these colors (max 8).")]
        public Color[] palette =
        {
            new Color(1f, 0.85f, 0.2f), new Color(1f, 1f, 1f), new Color(1f, 0.5f, 0.65f), new Color(0.65f, 0.55f, 1f)
        };
        [Range(0, 1)] public float rootDarkening = 0.2f;
        [Range(0, 1)] public float lightWrap = 0.4f;

        [Header("Motion & interaction (live)")]
        [Range(0, 1), Tooltip("How much this layer sways in the grass wind (0 skips the wind math entirely).")]
        public float windAmount = 0.6f;
        [Range(0, 1), Tooltip("How strongly interactors flatten / push this layer (0 = ignore, skips the math entirely).")]
        public float flattenResponse = 1f;
        [Range(0, 1), Tooltip("Fraction of length kept when fully flattened.")]
        public float lengthKeptWhenFlattened = 0.6f;

        [Header("Distance & shadows")]
        [Tooltip("Nothing is drawn beyond this (thins out from 60% of it). The player's 'Details Distance' setting scales it.")]
        public float drawDistance = 30f;
        public bool castShadows = false;
        [Tooltip("Only chunks closer than this cast shadows (shadow casting is expensive).")]
        public float shadowDistance = 12f;

        // ---------------- runtime ----------------
        static readonly Vector4[] s_pal = new Vector4[8];
        [System.NonSerialized] internal ScatterData data;
        [System.NonSerialized] internal Material material;
        [System.NonSerialized] internal Mesh activeMesh;
        [System.NonSerialized] internal float meshHeight = 1f;
        [System.NonSerialized] internal bool ready;
        [System.NonSerialized] internal bool propsDirty = true;
        [System.NonSerialized] Mesh builtMesh;
        [System.NonSerialized] DetailShape builtShape;

        public int LoadedCount => data != null ? data.total : 0;

        public static DetailLayer Flowers()
        {
            return new DetailLayer();
        }

        public static DetailLayer Mushrooms()
        {
            return new DetailLayer
            {
                name = "Mushrooms", shape = DetailShape.Mushroom, doubleSided = false,
                density = 0.08f, clumping = 0.9f, patchSize = 5f, patchCoverage = 0.2f, seed = 23,
                size = 0.22f, windAmount = 0f, flattenResponse = 0.8f, lengthKeptWhenFlattened = 0.5f, drawDistance = 25f,
                palette = new[] { new Color(0.9f, 0.2f, 0.15f), new Color(0.95f, 0.55f, 0.15f), new Color(0.7f, 0.5f, 0.35f) }
            };
        }

        public static DetailLayer Pebbles()
        {
            return new DetailLayer
            {
                name = "Pebbles", shape = DetailShape.Rock, doubleSided = false,
                density = 0.25f, clumping = 0.6f, patchSize = 9f, patchCoverage = 0.4f, seed = 37,
                size = 0.14f, windAmount = 0f, flattenResponse = 0f, drawDistance = 25f,
                palette = new[] { new Color(0.62f, 0.62f, 0.66f), new Color(0.5f, 0.5f, 0.55f), new Color(0.72f, 0.7f, 0.66f) }
            };
        }

        internal bool EnsureMesh()
        {
            if (shape == DetailShape.CustomMesh)
            {
                if (customMesh == null) return false;
                activeMesh = customMesh;
                meshHeight = Mathf.Max(customMesh.bounds.max.y, 0.001f);
                return true;
            }
            if (builtMesh == null || builtShape != shape)
            {
                GrassRenderer.SafeDestroy(builtMesh);
                builtMesh = GrassMeshes.Build(shape);
                builtShape = shape;
                propsDirty = true;
            }
            activeMesh = builtMesh;
            meshHeight = Mathf.Max(builtMesh.bounds.max.y, 0.001f);
            return true;
        }

        internal void ReleaseGpu()
        {
            if (data != null) data.Release();
            ready = false;
        }

        internal bool Prepare(Shader shader, Object ctx)
        {
            ready = false;
            if (!enabled || bakedData == null || shader == null)
            {
                ReleaseGpu(); // frees GPU memory for disabled layers
                return false;
            }
            if (data == null) data = new ScatterData();
            if (!data.Ensure(bakedData, ctx)) return false;

            if (material == null || material.shader != shader)
            {
                GrassRenderer.SafeDestroy(material);
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                propsDirty = true;
            }
            if (!EnsureMesh()) return false;
            ready = true;
            return true;
        }

        // Only touches the material when something changed (OnValidate / MarkDirty)
        internal void Apply(GrassRenderer r)
        {
            if (!propsDirty) return;
            propsDirty = false;

            Material m = material;
            m.SetFloat("_Size", size);
            m.SetVector("_SizeRange", new Vector4(sizeRange.x, sizeRange.y, 0, 0));
            m.SetFloat("_MeshHeight", meshHeight);
            m.SetFloat("_RootDarkening", rootDarkening);
            m.SetFloat("_LightWrap", lightWrap);
            m.SetFloat("_FlattenResponse", flattenResponse);
            m.SetFloat("_FlattenMin", lengthKeptWhenFlattened);
            m.SetFloat("_WindStrength", windAmount * r.windStrength);
            m.SetFloat("_WindSpeed", r.windSpeed);
            m.SetFloat("_WindScale", r.windScale);
            float a = r.windAngle * Mathf.Deg2Rad;
            m.SetVector("_WindDirection", new Vector4(Mathf.Cos(a), Mathf.Sin(a), 0, 0));

            int n = palette != null ? Mathf.Min(palette.Length, 8) : 0;
            if (n == 0) { s_pal[0] = Vector4.one; n = 1; }
            else for (int i = 0; i < n; i++) s_pal[i] = palette[i];
            m.SetVectorArray("_Palette", s_pal);
            m.SetFloat("_PaletteCount", n);

            m.SetTexture("_MainTex", texture != null ? (Texture)texture : Texture2D.whiteTexture);
            m.SetFloat("_Cull", doubleSided ? 0f : 2f);

            SetKeyword(m, "_GRASS_RECEIVE_SHADOWS", r.receiveShadows);
            SetKeyword(m, "_GRASS_EXTRA_LIGHTS", r.extraLights);
            m.SetShaderPassEnabled("DepthOnly", r.writeDepthTexture);
        }

        internal static void SetKeyword(Material m, string kw, bool on)
        {
            if (on) m.EnableKeyword(kw); else m.DisableKeyword(kw);
        }

        internal void DestroyRuntime()
        {
            if (data != null) { data.Release(); data = null; }
            GrassRenderer.SafeDestroy(material); material = null;
            GrassRenderer.SafeDestroy(builtMesh); builtMesh = null;
            activeMesh = null;
            ready = false;
            propsDirty = true;
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("Stylized Grass/Grass Renderer")]
    public class GrassRenderer : MonoBehaviour
    {
        // ------------------------------------------------------------------ Data
        [Header("Baked data (filled by the Bake button)")]
        public TextAsset bakedData;
        public Shader grassShader;
        public Shader detailShader;

        // ------------------------------------------------------------------ Layers
        [Header("Layers")]
        [Tooltip("Grass and details grow ONLY on colliders in these layers (used when baking).")]
        public LayerMask growOnLayers = ~0;
        [Tooltip("Colliders in these layers stop grass and details (paths, rocks, buildings).")]
        public LayerMask blockLayers = 0;
        [Tooltip("The Unity layer everything is drawn on (camera culling masks / renderer features respect it).")]
        public int renderLayer = 0;

        // ------------------------------------------------------------------ Bake settings
        [Header("Bake (grass)")]
        [Tooltip("World-space area to fill. Drag the green box handles in the Scene view.")]
        public Bounds bakeBounds = new Bounds(Vector3.zero, new Vector3(100, 60, 100));
        [Tooltip("Tufts per square meter (4 bytes each in the bake file). Each tuft is 3 blades. Players can lower the drawn amount with the Grass Quality setting, but the file always holds what you bake.")]
        public float density = 8f;
        [Tooltip("Random size multiplier per tuft. Applies live, no re-bake needed.")]
        public Vector2 scaleRange = new Vector2(0.7f, 1.3f);
        [Range(0, 89)] public float maxSlope = 50f;
        [Tooltip("Optional density painting: red channel = density (white = full, black = none). Spans the whole bake bounds on X/Z. Texture needs Read/Write enabled.")]
        public Texture2D densityMask;
        public bool invertMask = false;
        public int seed = 1;
        [Tooltip("Chunk size in meters (culling granularity).")]
        public float chunkSize = 16f;

        // ------------------------------------------------------------------ Look
        [Header("Look")]
        public Color baseColor = new Color(0.05f, 0.6f, 0.22f, 1f);
        public Color tipColor = new Color(0.45f, 1f, 0.4f, 1f);
        public Color variationColor = new Color(0.75f, 0.95f, 0.25f, 1f);
        [Range(0, 1)] public float colorVariation = 0.35f;
        [Range(0, 1)] public float rootDarkening = 0.35f;
        [Tooltip("Blade height in meters. Keep it SHORT for readability.")]
        public float bladeHeight = 0.28f;
        [Tooltip("Width of one tuft (also scales blade width).")]
        public float tuftSize = 0.5f;
        [Range(0, 1), Tooltip("0 = hard lambert, 1 = very soft/flat lighting.")]
        public float lightWrap = 0.4f;

        // ------------------------------------------------------------------ Wind
        [Header("Wind (shared with details)")]
        public float windStrength = 0.25f;
        public float windSpeed = 1.2f;
        public float windScale = 0.35f;
        [Range(0, 360)] public float windAngle = 20f;

        // ------------------------------------------------------------------ Interaction / readability
        [Header("Interaction & readability")]
        [Range(0, 1), Tooltip("Fraction of blade LENGTH kept when fully flattened (blades also fall over, away from the interactor).")]
        public float flattenMinHeight = 0.5f;

        [System.Serializable]
        public struct AutoTag { public string tag; public float radius; public float flatten; public float push; }

        [Tooltip("At runtime, every object with one of these tags automatically gets a GrassInteractor. Tags must exist in Project Settings > Tags.")]
        public AutoTag[] autoInteractorTags =
        {
            new AutoTag { tag = "Player", radius = 1.4f, flatten = 0.85f, push = 0.3f },
            new AutoTag { tag = "Enemy",  radius = 1.6f, flatten = 1.0f, push = 0.15f },
        };

        // ------------------------------------------------------------------ Distance / LOD
        [Header("Distance (authored maximums - the player's Distance setting scales these)")]
        [Tooltip("Grass blades start thinning out here.")]
        public float fadeStart = 25f;
        [Tooltip("No grass beyond this distance (chunks are skipped entirely).")]
        public float fadeEnd = 50f;
        [Tooltip("Chunks farther than this use a much simpler tuft mesh (3 triangles instead of 9). Shrinks with the Quality setting.")]
        public float grassLodDistance = 20f;

        // ------------------------------------------------------------------ Performance
        [Header("Performance")]
        [Tooltip("Grass receives shadows. (Details too.) Cheap.")]
        public bool receiveShadows = true;
        [Tooltip("Grass casts shadows. Expensive - off by default.")]
        public bool castShadows = false;
        [Tooltip("Only chunks closer than this cast shadows.")]
        public float castShadowDistance = 15f;
        [Tooltip("Point / spot lights affect grass and details (per-pixel loop, costly). Main light + ambient are always used.")]
        public bool extraLights = false;
        [Tooltip("Draw grass into the camera depth texture too (needed by soft particles / depth-based effects that must see grass). Doubles vertex work, so off by default.")]
        public bool writeDepthTexture = false;

        // ------------------------------------------------------------------ Detail layers
        [Header("Detail layers (flowers, mushrooms, rocks, your own meshes...)")]
        public List<DetailLayer> details = new List<DetailLayer>
        {
            DetailLayer.Flowers(), DetailLayer.Mushrooms(), DetailLayer.Pebbles()
        };

        // ------------------------------------------------------------------ Runtime state
        const int MaxInteractors = 16;
        static readonly Vector4[] s_pos = new Vector4[MaxInteractors];
        static readonly Vector4[] s_par = new Vector4[MaxInteractors];
        static readonly float[] s_dist = new float[MaxInteractors];
        static readonly GrassInteractor[] s_pick = new GrassInteractor[MaxInteractors];
        static readonly Plane[] s_planes = new Plane[6];

        static readonly int ID_Instances = Shader.PropertyToID("_Instances");
        static readonly int ID_ChunkOffset = Shader.PropertyToID("_ChunkOffset");
        static readonly int ID_ChunkCount = Shader.PropertyToID("_ChunkCount");
        static readonly int ID_ChunkMin = Shader.PropertyToID("_ChunkMin");
        static readonly int ID_ChunkSize = Shader.PropertyToID("_ChunkSize");
        static readonly int ID_FadeStart = Shader.PropertyToID("_FadeStart");
        static readonly int ID_FadeEnd = Shader.PropertyToID("_FadeEnd");
        static readonly int ID_Quality = Shader.PropertyToID("_Quality");
        static readonly int ID_QualityWidth = Shader.PropertyToID("_QualityWidth");
        static readonly int ID_Interactors = Shader.PropertyToID("_GrassInteractors");
        static readonly int ID_InteractorParams = Shader.PropertyToID("_GrassInteractorParams");
        static readonly int ID_InteractorCount = Shader.PropertyToID("_GrassInteractorCount");

        readonly ScatterData grassData = new ScatterData();
        Mesh tuftMesh, tuftMeshLow;
        Material grassMaterial;
        MaterialPropertyBlock mpb;
        float nextAutoScan;
        bool grassPropsDirty = true;

        public int TuftCount => grassData.total;
        public int ChunkCount => grassData.chunks != null ? grassData.chunks.Length : 0;

        // ------------------------------------------------------------------ Lifecycle
        void Reset()
        {
            grassShader = Shader.Find("StylizedGrass/Grass");
            detailShader = Shader.Find("StylizedGrass/Detail");
            bakeBounds = new Bounds(transform.position, new Vector3(100, 60, 100));
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            MarkDirty();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            grassData.Release();
            SafeDestroy(grassMaterial); grassMaterial = null;
            SafeDestroy(tuftMesh); tuftMesh = null;
            SafeDestroy(tuftMeshLow); tuftMeshLow = null;
            if (details != null) foreach (var d in details) d?.DestroyRuntime();
        }

        void OnValidate()
        {
            if (fadeEnd < fadeStart + 0.1f) fadeEnd = fadeStart + 0.1f;
            density = Mathf.Max(0.01f, density);
            chunkSize = Mathf.Max(2f, chunkSize);
            renderLayer = Mathf.Clamp(renderLayer, 0, 31);
            if (grassShader == null) grassShader = Shader.Find("StylizedGrass/Grass");
            if (detailShader == null) detailShader = Shader.Find("StylizedGrass/Detail");
            if (details != null)
                foreach (var d in details)
                {
                    if (d == null) continue;
                    d.density = Mathf.Max(0.001f, d.density);
                    d.patchSize = Mathf.Max(0.5f, d.patchSize);
                    d.drawDistance = Mathf.Max(1f, d.drawDistance);
                }
            MarkDirty();
        }

        /// <summary>Call after changing look settings from a script at runtime (the inspector does it automatically).</summary>
        public void MarkDirty()
        {
            grassPropsDirty = true;
            if (details != null) foreach (var d in details) if (d != null) d.propsDirty = true;
        }

        /// <summary>Re-read all baked files (called automatically after baking).</summary>
        public void Reload()
        {
            grassData.Release();
            if (details != null) foreach (var d in details) if (d != null && d.data != null) d.data.Release();
        }

        internal static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // Auto-add interactors to tagged objects (play mode only)
        void Update()
        {
            if (!Application.isPlaying || autoInteractorTags == null || Time.unscaledTime < nextAutoScan) return;
            nextAutoScan = Time.unscaledTime + 0.5f;
            foreach (var a in autoInteractorTags)
            {
                if (string.IsNullOrEmpty(a.tag)) continue;
                GameObject[] objs;
                try { objs = GameObject.FindGameObjectsWithTag(a.tag); }
                catch (UnityException) { continue; } // tag not defined
                foreach (var go in objs)
                {
                    if (go.GetComponentInChildren<GrassInteractor>() != null) continue;
                    var it = go.AddComponent<GrassInteractor>();
                    it.radius = a.radius; it.flatten = a.flatten; it.push = a.push;
                }
            }
        }

        // ------------------------------------------------------------------ Preparing
        bool EnsureGrassReady()
        {
            if (!grassData.Ensure(bakedData, this)) return false;

            if (grassShader == null) grassShader = Shader.Find("StylizedGrass/Grass");
            if (grassShader == null) return false;

            if (grassMaterial == null || grassMaterial.shader != grassShader)
            {
                SafeDestroy(grassMaterial);
                grassMaterial = new Material(grassShader) { hideFlags = HideFlags.HideAndDontSave };
                grassPropsDirty = true;
            }
            if (tuftMesh == null) tuftMesh = GrassMeshes.BuildTuft(3);
            if (tuftMeshLow == null) tuftMeshLow = GrassMeshes.BuildTuftLow();
            return true;
        }

        // ------------------------------------------------------------------ Per camera draw
        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == null) return;
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            if ((cam.cullingMask & (1 << renderLayer)) == 0) return; // camera can't see our layer at all

            bool grassOn = GrassGraphicsSettings.GrassEnabled;
            bool detailsOn = GrassGraphicsSettings.DetailsEnabled;

            // ---- prepare (turning a setting OFF also frees that GPU memory)
            bool grassOk = false;
            if (grassOn) grassOk = EnsureGrassReady();
            else if (grassData.buffer != null) grassData.Release();

            if (detailShader == null) detailShader = Shader.Find("StylizedGrass/Detail");
            int detailsReady = 0;
            if (details != null)
            {
                foreach (var d in details)
                {
                    if (d == null) continue;
                    if (!detailsOn) { d.ReleaseGpu(); continue; }
                    if (d.Prepare(detailShader, this)) detailsReady++;
                }
            }

            if (!grassOk && detailsReady == 0) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();

            Vector3 camPos = cam.transform.position;
            UploadInteractors(camPos);
            GeometryUtility.CalculateFrustumPlanes(cam, s_planes);

            // ---- grass
            if (grassOk)
            {
                ApplyGrassMaterial();
                float q = GrassGraphicsSettings.GrassQuality;
                float dScale = GrassGraphicsSettings.GrassDistance;
                float end = Mathf.Min(fadeEnd * dScale, cam.farClipPlane);
                float start = Mathf.Min(fadeStart * dScale, end - 0.1f);
                // fewer tufts -> slightly wider tufts so the ground stays covered
                float comp = Mathf.Min(1.7f, Mathf.Lerp(1f, 1f / Mathf.Sqrt(Mathf.Max(q, 0.05f)), 0.7f));
                float lod = Mathf.Min(grassLodDistance * Mathf.Lerp(0.35f, 1f, q), end);
                float vMargin = bladeHeight * scaleRange.y * 1.5f + 0.2f;

                DrawChunks(grassData, tuftMesh, tuftMeshLow, lod, grassMaterial, start, end, q, comp,
                    new Vector3(2f, vMargin * 2f, 2f), castShadows, castShadowDistance, cam, camPos);
            }

            // ---- details
            if (details != null)
            {
                float q = GrassGraphicsSettings.DetailsQuality;
                float dScale = GrassGraphicsSettings.DetailsDistance;
                foreach (var d in details)
                {
                    if (d == null || !d.ready) continue;
                    d.Apply(this);

                    float end = Mathf.Min(d.drawDistance * dScale, cam.farClipPlane);
                    float start = end * 0.6f;
                    Bounds mb = d.activeMesh.bounds;
                    float hMargin = Mathf.Max(mb.extents.x, mb.extents.z) * d.size * d.sizeRange.y;
                    float vMargin = d.size * d.sizeRange.y * d.meshHeight * 1.5f + 0.2f;

                    DrawChunks(d.data, d.activeMesh, null, 0f, d.material, start, end, q, 1f,
                        new Vector3(2f + hMargin * 2f, vMargin * 2f, 2f + hMargin * 2f),
                        d.castShadows, d.shadowDistance, cam, camPos);
                }
            }
        }

        // The hot loop. Per chunk: distance cull, frustum cull, instance-count LOD, mesh LOD, shadow LOD.
        void DrawChunks(ScatterData data, Mesh meshNear, Mesh meshFar, float lodDistance, Material mat,
                        float fadeStart, float fadeEnd, float quality, float widthComp, Vector3 expand,
                        bool wantShadows, float shadowDistance, Camera cam, Vector3 camPos)
        {
            if (data.buffer == null || data.chunks == null) return;

            mpb.SetBuffer(ID_Instances, data.buffer);
            mpb.SetFloat(ID_FadeStart, fadeStart);
            mpb.SetFloat(ID_FadeEnd, fadeEnd);
            mpb.SetFloat(ID_Quality, quality);
            mpb.SetFloat(ID_QualityWidth, widthComp);

            float endSq = fadeEnd * fadeEnd;
            float range = Mathf.Max(fadeEnd - fadeStart, 0.1f);
            float lodSq = lodDistance * lodDistance;
            float shadowSq = shadowDistance * shadowDistance;
            var chunks = data.chunks;

            for (int i = 0; i < chunks.Length; i++)
            {
                ScatterChunk c = chunks[i];
                if (c.count == 0) continue;

                float dSq = c.bounds.SqrDistance(camPos);
                if (dSq >= endSq) continue;                                   // too far: skip

                // Instance-count LOD: the shader hides exactly the same ones, so we simply never submit them
                float fadeMin = Mathf.Clamp01((Mathf.Sqrt(dSq) - fadeStart) / range);
                int n = Mathf.CeilToInt(c.count * quality * (1f - fadeMin));
                if (n <= 0) continue;

                Bounds b = c.bounds;
                b.Expand(expand);

                bool inView = GeometryUtility.TestPlanesAABB(s_planes, b);
                bool shadow = wantShadows && dSq < shadowSq;

                ShadowCastingMode mode;
                if (inView) mode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
                else if (shadow) mode = ShadowCastingMode.ShadowsOnly;        // off-screen but its shadow can reach the screen
                else continue;                                                // off-screen: skip entirely

                Mesh mesh = (meshFar != null && dSq > lodSq) ? meshFar : meshNear;

                mpb.SetInt(ID_ChunkOffset, c.start);
                mpb.SetFloat(ID_ChunkCount, c.count);
                mpb.SetVector(ID_ChunkMin, c.minN);
                mpb.SetVector(ID_ChunkSize, c.sizeN);
                Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, b, n, mpb,
                    mode, true, renderLayer, cam, LightProbeUsage.Off);
            }
        }

        // Only touches the material when something changed (OnValidate / MarkDirty)
        void ApplyGrassMaterial()
        {
            if (!grassPropsDirty) return;
            grassPropsDirty = false;

            Material m = grassMaterial;
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_TipColor", tipColor);
            m.SetColor("_VariationColor", variationColor);
            m.SetFloat("_ColorVariation", colorVariation);
            m.SetFloat("_RootDarkening", rootDarkening);
            m.SetFloat("_BladeHeight", bladeHeight);
            m.SetFloat("_TuftSize", tuftSize);
            m.SetFloat("_FlattenMin", flattenMinHeight);
            m.SetFloat("_WindStrength", windStrength);
            m.SetFloat("_WindSpeed", windSpeed);
            m.SetFloat("_WindScale", windScale);
            m.SetFloat("_LightWrap", lightWrap);
            m.SetVector("_ScaleRange", new Vector4(scaleRange.x, scaleRange.y, 0, 0));
            float a = windAngle * Mathf.Deg2Rad;
            m.SetVector("_WindDirection", new Vector4(Mathf.Cos(a), Mathf.Sin(a), 0, 0));

            DetailLayer.SetKeyword(m, "_GRASS_RECEIVE_SHADOWS", receiveShadows);
            DetailLayer.SetKeyword(m, "_GRASS_EXTRA_LIGHTS", extraLights);
            m.SetShaderPassEnabled("DepthOnly", writeDepthTexture);
        }

        // Picks the 16 interactors closest to the camera and uploads them as globals.
        static void UploadInteractors(Vector3 camPos)
        {
            var all = GrassInteractor.All;
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                GrassInteractor it = all[i];
                if (it == null) continue;
                float d = (it.Position - camPos).sqrMagnitude;
                int pos;
                if (count < MaxInteractors) pos = count++;
                else if (d < s_dist[MaxInteractors - 1]) pos = MaxInteractors - 1;
                else continue;

                while (pos > 0 && s_dist[pos - 1] > d)
                {
                    s_dist[pos] = s_dist[pos - 1];
                    s_pick[pos] = s_pick[pos - 1];
                    pos--;
                }
                s_dist[pos] = d;
                s_pick[pos] = it;
            }

            for (int i = 0; i < MaxInteractors; i++)
            {
                if (i < count)
                {
                    GrassInteractor it = s_pick[i];
                    Vector3 p = it.Position;
                    s_pos[i] = new Vector4(p.x, p.y, p.z, it.radius);
                    s_par[i] = new Vector4(it.push, it.flatten, it.verticalRange, it.core);
                }
                else
                {
                    s_pos[i] = Vector4.zero;
                    s_par[i] = Vector4.zero;
                }
            }
            Shader.SetGlobalVectorArray(ID_Interactors, s_pos);
            Shader.SetGlobalVectorArray(ID_InteractorParams, s_par);
            Shader.SetGlobalFloat(ID_InteractorCount, count);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(bakeBounds.center, bakeBounds.size);
        }
    }
}
