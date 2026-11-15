#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace StylizedGrass.EditorTools
{
    struct BakedPoint { public Vector3 position; public float nx, nz; }

    struct BakeSettings
    {
        public string label;        // shown in progress bar + file name
        public float density;       // instances per m2 (inside patches)
        public float maxSlope;
        public bool useMask;
        public float clumping;      // 0 = even, 1 = only in patches
        public float patchSize;
        public float patchCoverage;
        public int seed;
        public TextAsset existing;
    }

    public static class GrassBaker
    {
        // ------------------------------------------------------------------ Public API
        public static bool BakeGrass(GrassRenderer g)
        {
            var s = new BakeSettings
            {
                label = "Grass", density = g.density, maxSlope = g.maxSlope, useMask = true,
                clumping = 0f, patchSize = 10f, patchCoverage = 1f, seed = g.seed, existing = g.bakedData
            };
            int placed = Core(g, s, out TextAsset asset);
            if (placed == -1) return false;
            if (placed == 0)
            {
                EditorUtility.DisplayDialog("Stylized Grass",
                    "No grass was placed. Check that the ground has COLLIDERS, is inside the bake box, and is on a 'Grow On Layers' layer.", "OK");
                return false;
            }
            Undo.RecordObject(g, "Bake Grass");
            g.bakedData = asset;
            EditorUtility.SetDirty(g);
            g.Reload();
            return true;
        }

        public static void BakeDetails(GrassRenderer g)
        {
            if (g.details == null) return;
            for (int i = 0; i < g.details.Count; i++)
            {
                DetailLayer l = g.details[i];
                if (l == null || !l.enabled) continue;
                if (l.shape == DetailShape.CustomMesh && l.customMesh == null)
                {
                    Debug.LogWarning($"[StylizedGrass] Detail layer '{l.name}' uses Custom Mesh but no mesh is assigned - skipped.", g);
                    continue;
                }

                var s = new BakeSettings
                {
                    label = l.name, density = l.density, maxSlope = l.maxSlope, useMask = l.useDensityMask,
                    clumping = l.clumping, patchSize = l.patchSize, patchCoverage = l.patchCoverage,
                    seed = l.seed, existing = l.bakedData
                };
                int placed = Core(g, s, out TextAsset asset);
                if (placed == -1) return; // cancelled
                Undo.RecordObject(g, "Bake Details");
                if (placed == 0)
                {
                    Debug.LogWarning($"[StylizedGrass] Detail layer '{l.name}' placed nothing (raise density / patch coverage, or check layers & slope).", g);
                    l.bakedData = null;
                }
                else l.bakedData = asset;
                EditorUtility.SetDirty(g);
            }
            g.Reload();
        }

        // ------------------------------------------------------------------ Core
        // returns placed count, or -1 if cancelled
        static int Core(GrassRenderer g, BakeSettings s, out TextAsset asset)
        {
            asset = null;
            if (g.growOnLayers.value == 0)
            {
                EditorUtility.DisplayDialog("Stylized Grass", "'Grow On Layers' is empty - nothing to grow on.", "OK");
                return -1;
            }

            Bounds b = g.bakeBounds;
            float spacing = 1f / Mathf.Sqrt(Mathf.Max(s.density, 0.001f));
            int nx = Mathf.CeilToInt(b.size.x / spacing);
            int nz = Mathf.CeilToInt(b.size.z / spacing);
            long samples = (long)nx * nz;

            if (samples > 3000000L &&
                !EditorUtility.DisplayDialog("Stylized Grass",
                    $"'{s.label}' will test {samples:N0} positions and could take a while. Continue?", "Bake", "Cancel"))
                return -1;

            float cs = Mathf.Max(2f, g.chunkSize);
            int cx = Mathf.Max(1, Mathf.CeilToInt(b.size.x / cs));
            int cz = Mathf.Max(1, Mathf.CeilToInt(b.size.z / cs));
            var cells = new List<BakedPoint>[cx * cz];

            Physics.SyncTransforms();
            int rayMask = g.growOnLayers.value | g.blockLayers.value;
            int growMask = g.growOnLayers.value;
            float minUp = Mathf.Cos(s.maxSlope * Mathf.Deg2Rad);
            Vector3 min = b.min;
            float top = b.max.y;
            float rayLen = b.size.y;
            var rng = new System.Random(s.seed);
            Texture2D mask = g.densityMask;
            bool maskOk = s.useMask && mask != null && mask.isReadable;
            if (s.useMask && mask != null && !mask.isReadable)
                Debug.LogWarning("[StylizedGrass] Density mask is not Read/Write enabled - ignoring it.");

            float patchSize = Mathf.Max(0.5f, s.patchSize);
            float noiseOffX = s.seed * 17.31f, noiseOffZ = s.seed * 9.73f;
            float thr = Mathf.Lerp(0.75f, 0.3f, Mathf.Clamp01(s.patchCoverage));

            int total = 0;
            bool cancelled = false;
            long done = 0;

            try
            {
                for (int ix = 0; ix < nx && !cancelled; ix++)
                {
                    for (int iz = 0; iz < nz; iz++)
                    {
                        // Draw all random numbers up-front so results stay deterministic
                        float jx = (float)rng.NextDouble(), jz = (float)rng.NextDouble();
                        float rMask = (float)rng.NextDouble();
                        float rClump = (float)rng.NextDouble();

                        if ((++done & 4095) == 0 &&
                            EditorUtility.DisplayCancelableProgressBar($"Baking {s.label}", $"{total:N0} instances", (float)done / samples))
                        { cancelled = true; break; }

                        float x = min.x + (ix + jx) * spacing;
                        float z = min.z + (iz + jz) * spacing;
                        if (x > b.max.x || z > b.max.z) continue;

                        // patches
                        if (s.clumping > 0f)
                        {
                            float n = Mathf.PerlinNoise(x / patchSize + noiseOffX, z / patchSize + noiseOffZ);
                            float tt = Mathf.Clamp01((n - thr) / 0.12f);
                            float inPatch = tt * tt * (3f - 2f * tt);
                            float keep = Mathf.Lerp(1f, inPatch, s.clumping);
                            if (rClump > keep) continue;
                        }

                        if (maskOk)
                        {
                            float m = mask.GetPixelBilinear((x - min.x) / b.size.x, (z - min.z) / b.size.z).r;
                            if (g.invertMask) m = 1f - m;
                            if (rMask > m) continue;
                        }

                        if (!Physics.Raycast(new Vector3(x, top, z), Vector3.down, out RaycastHit hit, rayLen, rayMask, QueryTriggerInteraction.Ignore))
                            continue;
                        if ((growMask & (1 << hit.collider.gameObject.layer)) == 0) continue; // a blocker was hit first
                        if (hit.normal.y < minUp) continue;

                        int cix = Mathf.Clamp((int)((x - min.x) / cs), 0, cx - 1);
                        int ciz = Mathf.Clamp((int)((z - min.z) / cs), 0, cz - 1);
                        int ci = cix + ciz * cx;
                        if (cells[ci] == null) cells[ci] = new List<BakedPoint>();
                        cells[ci].Add(new BakedPoint { position = hit.point, nx = hit.normal.x, nz = hit.normal.z });
                        total++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            if (cancelled) return -1;
            if (total == 0) return 0;

            // ---- Write file
            string path = s.existing != null ? AssetDatabase.GetAssetPath(s.existing) : null;
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".bytes"))
            {
                string scene = g.gameObject.scene.name;
                if (string.IsNullOrEmpty(scene)) scene = "Grass";
                const string dir = "Assets/StylizedGrass/Baked";
                Directory.CreateDirectory(dir);
                string safe = s.label.Replace(' ', '_');
                foreach (char ch in Path.GetInvalidFileNameChars()) safe = safe.Replace(ch.ToString(), "");
                path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{scene}_{g.name}_{safe}.bytes");
            }

            // Shuffle each chunk (deterministic) so that "the first N instances" is a uniform random subset.
            // The runtime quality + distance LOD rely on this.
            int chunkCount = 0;
            for (int ci = 0; ci < cells.Length; ci++)
            {
                var list = cells[ci];
                if (list == null || list.Count == 0) continue;
                chunkCount++;
                var srng = new System.Random(s.seed * 7919 + ci);
                for (int k = list.Count - 1; k > 0; k--)
                {
                    int j = srng.Next(k + 1);
                    var tmp = list[k]; list[k] = list[j]; list[j] = tmp;
                }
            }

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(GrassBakeFormat.Magic);
                w.Write(GrassBakeFormat.Version);
                w.Write((uint)chunkCount);
                w.Write((uint)total);

                // chunk table: center, extents, count, average ground normal (x, z)
                foreach (var c in cells)
                {
                    if (c == null || c.Count == 0) continue;
                    ChunkBounds(c, out Vector3 lo, out Vector3 hi);
                    Vector3 center = (lo + hi) * 0.5f, ext = (hi - lo) * 0.5f;
                    float anx = 0f, anz = 0f;
                    foreach (var i in c) { anx += i.nx; anz += i.nz; }
                    anx /= c.Count; anz /= c.Count;

                    w.Write(center.x); w.Write(center.y); w.Write(center.z);
                    w.Write(ext.x); w.Write(ext.y); w.Write(ext.z);
                    w.Write((uint)c.Count);
                    w.Write(anx); w.Write(anz);
                }

                // instances: 4 bytes each
                foreach (var c in cells)
                {
                    if (c == null || c.Count == 0) continue;
                    ChunkBounds(c, out Vector3 lo, out Vector3 hi);
                    Vector3 size = hi - lo;
                    foreach (var i in c) w.Write(GrassPack.Pack(i.position, lo, size));
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Debug.Log($"[StylizedGrass] Baked {s.label}: {total:N0} instances in {chunkCount} chunks, {total * 4f / 1048576f:F2} MB -> {path}");
            return total;
        }

        static void ChunkBounds(List<BakedPoint> c, out Vector3 lo, out Vector3 hi)
        {
            lo = hi = c[0].position;
            foreach (var i in c) { lo = Vector3.Min(lo, i.position); hi = Vector3.Max(hi, i.position); }
        }
    }

    [CustomEditor(typeof(GrassRenderer))]
    public class GrassRendererEditor : UnityEditor.Editor
    {
        readonly BoxBoundsHandle handle = new BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty it = serializedObject.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                if (it.name == "renderLayer")
                    it.intValue = EditorGUILayout.LayerField(new GUIContent("Render Layer", it.tooltip), it.intValue);
                else
                    EditorGUILayout.PropertyField(it, true);
            }
            serializedObject.ApplyModifiedProperties();

            var g = (GrassRenderer)target;
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bake Grass", GUILayout.Height(28))) GrassBaker.BakeGrass(g);
                using (new EditorGUI.DisabledScope(g.bakedData == null))
                {
                    if (GUILayout.Button("Clear", GUILayout.Height(28), GUILayout.Width(60)))
                    {
                        Undo.RecordObject(g, "Clear Grass");
                        g.bakedData = null;
                        g.Reload();
                    }
                }
            }
            if (GUILayout.Button("Bake Detail Layers (flowers, mushrooms, rocks...)", GUILayout.Height(28)))
                GrassBaker.BakeDetails(g);
            if (GUILayout.Button("Bake Everything", GUILayout.Height(28)))
            {
                if (GrassBaker.BakeGrass(g)) GrassBaker.BakeDetails(g);
            }

            {
                Bounds eb = g.bakeBounds;
                float maxTufts = eb.size.x * eb.size.z * g.density;
                EditorGUILayout.HelpBox($"Grass upper estimate for current area + density: {maxTufts:N0} tufts = about {maxTufts * 4f / 1048576f:F1} MB (less if some ground is excluded).", MessageType.None);
            }

            if (g.bakedData == null)
                EditorGUILayout.HelpBox("No baked grass yet. Make sure your ground has colliders on a 'Grow On Layers' layer, fit the green box around it, and press Bake Grass.", MessageType.Info);
            else if (g.TuftCount > 0)
                EditorGUILayout.HelpBox($"Grass: {g.TuftCount:N0} tufts in {g.ChunkCount} chunks (~{g.TuftCount * 4f / 1048576f:F1} MB file / GPU memory).", MessageType.None);

            if (g.details != null)
                foreach (var d in g.details)
                    if (d != null && d.LoadedCount > 0)
                        EditorGUILayout.HelpBox($"{d.name}: {d.LoadedCount:N0} instances (~{d.LoadedCount * 4f / 1024f:F0} KB).", MessageType.None);
        }

        void OnSceneGUI()
        {
            var g = (GrassRenderer)target;
            handle.SetColor(new Color(0.2f, 1f, 0.3f, 1f));
            handle.center = g.bakeBounds.center;
            handle.size = g.bakeBounds.size;
            EditorGUI.BeginChangeCheck();
            handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(g, "Change Grass Bounds");
                g.bakeBounds = new Bounds(handle.center, handle.size);
            }
        }
    }

    static class GrassMenu
    {
        [MenuItem("Tools/Stylized Grass/Reset Saved Graphics Settings")]
        static void ResetSaved()
        {
            GrassGraphicsSettings.ClearSaved();
            GrassGraphicsSettings.ResetToDefaults();
            Debug.Log("[StylizedGrass] Saved graphics settings cleared.");
        }

        [MenuItem("GameObject/Stylized Grass/Grass Field", false, 10)]
        static void Create(MenuCommand cmd)
        {
            var go = new GameObject("Grass Field");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            var g = go.AddComponent<GrassRenderer>();
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            g.bakeBounds = new Bounds(go.transform.position, new Vector3(100, 60, 100));
            Undo.RegisterCreatedObjectUndo(go, "Create Grass Field");
            Selection.activeGameObject = go;
        }
    }
}
#endif
