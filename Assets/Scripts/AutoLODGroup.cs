using UnityEngine;

[RequireComponent(typeof(LODGroup))]
public class AutoLODGroup : MonoBehaviour
{
    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        LODGroup lodGroup = GetComponent<LODGroup>();

        LOD[] lods = new LOD[1];
        lods[0] = new LOD(0.5f, renderers);

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }
}