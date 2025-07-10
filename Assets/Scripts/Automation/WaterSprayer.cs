using UnityEngine;

public class WaterSprayer : Placeable
{
    [SerializeField] private float _sprayingCycle = 30;
    [SerializeField] private float _sprayingRange = 15;
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private AudioSource _source;

    private void Start()
    {
        InvokeRepeating(nameof(Spray), _sprayingCycle / 4, _sprayingCycle);
    }

    private void Spray()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _sprayingRange);

        foreach (var farmland in colliders)
        {
            if (farmland.TryGetComponent(out FarmlandSquare farmlandSquare))
            {
                farmlandSquare._waterLevel += 50;
            }
        }

        _particles.Play();
        _source.Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _sprayingRange);
    }
}
