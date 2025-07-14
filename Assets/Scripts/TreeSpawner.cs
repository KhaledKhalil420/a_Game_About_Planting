using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject tree;
    [SerializeField] private LayerMask placeableLayers;
    [SerializeField] private float spawnEvery = 100f;
    [SerializeField] private Transform upperQuarter, lowerQuarter;

    [SerializeField] private List<GameObject> trees;
    [SerializeField] private int maximumTrees = 10;

    private float timer;

    private void Update()
    {
        trees.RemoveAll(i => i == null);

        if (trees.Count >= maximumTrees)
            return;
        
        timer -= Tick.DeltaTickSpeed;

        if (timer <= 0)
        {
            Vector3 position = GetRandomXZ(upperQuarter, lowerQuarter);

            if (!Physics.Raycast(position, -transform.up, out RaycastHit hit, Mathf.Infinity, placeableLayers))
            {
                position = GetRandomXZ(upperQuarter, lowerQuarter);
                Physics.Raycast(position, -transform.up, out hit, Mathf.Infinity, placeableLayers);
            }

            if (hit.transform != null)
            {
                trees.Add(Instantiate(tree, hit.point, tree.transform.rotation));
            }

            timer = spawnEvery;
        }
    }

    private Vector3 GetRandomXZ(Transform a, Transform b)
    {
        return new Vector3(
            Random.Range(a.position.x, b.position.x),
            555f,
            Random.Range(a.position.z, b.position.z)
        );
    }
}
