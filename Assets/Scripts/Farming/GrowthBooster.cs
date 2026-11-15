using System.Collections.Generic;
using UnityEngine;

public class GrowthBooster : MonoBehaviour
{
    private static readonly List<GrowthBooster> Boosters = new();

    [SerializeField] private float radius = 5f;
    [SerializeField] private float multiplier = 1.5f;

    private void OnEnable() => Boosters.Add(this);
    private void OnDisable() => Boosters.Remove(this);

    public static float GetMultiplier(Vector3 position)
    {
        float best = 1f;
        foreach (var b in Boosters)
        {
            if (b.multiplier > best && (b.transform.position - position).sqrMagnitude <= b.radius * b.radius)
                best = b.multiplier;
        }
        return best;
    }
}
