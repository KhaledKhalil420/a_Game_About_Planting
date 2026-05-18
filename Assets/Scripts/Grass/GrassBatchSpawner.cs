using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a batch of grass across a rectangular area defined by two corner points.
/// Uses GrassPainter under the hood — attach this to any GameObject in your scene.
/// </summary>
public class GrassBatchSpawner : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Bounds")]
    [Tooltip("One corner of the spawn area (XZ matters, Y is used as raycast origin height).")]
    public Vector3 startCorner = new Vector3(-25f, 0f, -25f);

    [Tooltip("Opposite corner of the spawn area.")]
    public Vector3 endCorner = new Vector3(25f, 0f, 25f);

    [Header("Density")]
    [Tooltip("How many grass samples to try to place across the area.")]
    public int grassCount = 400;

    [Tooltip("Extra random scatter added to each sample position so it doesn't look grid-like.")]
    public float jitter = 0.5f;

    [Tooltip("Minimum world-space Y height a surface must be at to receive grass.")]
    public float minSpawnHeight = -999f;

    [Tooltip("Maximum world-space Y height a surface must be at to receive grass.")]
    public float maxSpawnHeight = 999f;

    [Header("Layers")]
    [Tooltip("Raycasts will only hit these layers (i.e. the layers that CAN receive grass).")]
    public LayerMask spawnOnLayers = ~0;

    [Tooltip("Even if a layer is in spawnOnLayers, skip it if it also appears here.")]
    public LayerMask excludeLayers = 0;

    [Header("Raycast")]
    [Tooltip("Height above the area from which downward raycasts are fired.")]
    public float raycastOriginHeight = 100f;

    [Tooltip("Maximum distance the raycast will travel downward.")]
    public float raycastDistance = 200f;

    [Header("Timing")]
    [Tooltip("Spread spawning across multiple frames to avoid a one-frame spike.")]
    public bool spawnAsync = true;

    [Tooltip("How many grass points to place per frame when spawnAsync is true.")]
    public int batchSizePerFrame = 50;

    [Tooltip("Spawn automatically on Start. Uncheck if you want to call Spawn() yourself.")]
    public bool spawnOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmoBounds = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.25f);

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private GrassPainter painter;
    private bool isSpawning = false;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Start()
    {
        painter = GrassPainter.instance;

        if (painter == null)
        {
            Debug.LogError("[GrassBatchSpawner] No GrassPainter instance found in scene! " +
                           "Make sure a GrassPainter component exists somewhere.");
            return;
        }

        if (spawnOnStart)
            Spawn();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Clears existing grass and spawns a fresh batch across the defined area.
    /// Call this at runtime if you want to refresh or re-seed the grass.
    /// </summary>
    public void Spawn()
    {
        if (painter == null)
        {
            Debug.LogWarning("[GrassBatchSpawner] Spawn() called but GrassPainter is missing.");
            return;
        }

        if (isSpawning)
        {
            StopAllCoroutines();
            isSpawning = false;
        }

        painter.ClearMesh();

        List<Vector3> samplePoints = BuildSamplePoints();

        if (spawnAsync)
            StartCoroutine(SpawnAsync(samplePoints));
        else
            SpawnImmediate(samplePoints);
    }

    // -------------------------------------------------------------------------
    // Sampling
    // -------------------------------------------------------------------------

    private List<Vector3> BuildSamplePoints()
    {
        float xMin = Mathf.Min(startCorner.x, endCorner.x);
        float xMax = Mathf.Max(startCorner.x, endCorner.x);
        float zMin = Mathf.Min(startCorner.z, endCorner.z);
        float zMax = Mathf.Max(startCorner.z, endCorner.z);

        float width  = xMax - xMin;
        float depth  = zMax - zMin;
        float area   = width * depth;

        if (area <= 0f)
        {
            Debug.LogWarning("[GrassBatchSpawner] Spawn area is zero — check your start/end corners.");
            return new List<Vector3>();
        }

        // Distribute sample points in a grid, then jitter each one
        int cols = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(grassCount * (width / depth))));
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)grassCount / cols));

        float cellW = width  / cols;
        float cellD = depth  / rows;

        List<Vector3> points = new List<Vector3>(cols * rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float x = xMin + (col + 0.5f) * cellW + Random.Range(-jitter, jitter);
                float z = zMin + (row + 0.5f) * cellD + Random.Range(-jitter, jitter);

                x = Mathf.Clamp(x, xMin, xMax);
                z = Mathf.Clamp(z, zMin, zMax);

                points.Add(new Vector3(x, raycastOriginHeight, z));

                if (points.Count >= grassCount) goto Done;
            }
        }
        Done:

        return points;
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    private void SpawnImmediate(List<Vector3> samplePoints)
    {
        foreach (Vector3 point in samplePoints)
            TryPaintAt(point);

        painter.FinalizeMesh();
        RefreshGrassCompute();

        Debug.Log($"[GrassBatchSpawner] Spawned grass from {samplePoints.Count} sample points.");
    }

    private IEnumerator SpawnAsync(List<Vector3> samplePoints)
    {
        isSpawning = true;
        int processed = 0;

        foreach (Vector3 point in samplePoints)
        {
            TryPaintAt(point);
            processed++;

            if (processed % batchSizePerFrame == 0)
                yield return null; // wait a frame between batches
        }

        painter.FinalizeMesh();
        RefreshGrassCompute();

        Debug.Log($"[GrassBatchSpawner] Async spawn complete — {samplePoints.Count} sample points processed.");
        isSpawning = false;
    }

    private void TryPaintAt(Vector3 origin)
    {
        Ray ray = new Ray(origin, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, spawnOnLayers))
            return;

        // Excluded layer check (bitwise AND against the hit object's layer bit)
        if (excludeLayers != 0 && (excludeLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            return;

        float y = hit.point.y;
        if (y < minSpawnHeight || y > maxSpawnHeight)
            return;

        painter.PaintGrassAtPosition(hit.point);
    }

    private void RefreshGrassCompute()
    {
        GrassComputeScript grassCompute = painter.GetComponent<GrassComputeScript>();
        if (grassCompute != null)
            grassCompute.ReLoadGrass(this, System.EventArgs.Empty);
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmoBounds) return;

        Vector3 center = (startCorner + endCorner) * 0.5f;
        Vector3 size   = new Vector3(
            Mathf.Abs(endCorner.x - startCorner.x),
            1f,
            Mathf.Abs(endCorner.z - startCorner.z)
        );

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size);

        // Mark corners
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(startCorner, 0.3f);
        Gizmos.DrawSphere(endCorner,   0.3f);
    }
}