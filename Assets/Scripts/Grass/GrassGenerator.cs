using UnityEngine;

public class GrassGenerator : MonoBehaviour
{
    [Header("Grass")]
    [SerializeField] private GrassPainter grassPainter;
    [SerializeField] private int maxAttemptsGrass = 10;
    [SerializeField] private int grassAmount = 500;
    [SerializeField] private bool spawnGrassAtStart = true;
    [SerializeField] private Vector3 mapMin, mapMax;
    [SerializeField] private LayerMask layers;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnGrass), 0, 5);
    }

    private void SpawnGrass()
    {
        grassPainter.FinalizeMesh();

        GrassComputeScript grassCompute = grassPainter.GetComponent<GrassComputeScript>();
        if (grassCompute != null)
            grassCompute.Refresh();
            grassCompute.OnValidate();
        grassCompute.ToggleGrass(true);
    }

}
