using UnityEngine;
using DG.Tweening;

public class WaterCan : Tool
{
    private Transform _player;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _wateringSpeed = 0.5f;
    [SerializeField] private float _distance = 15;
    [SerializeField] private AudioSource _source;
    [SerializeField] private ParticleSystem _waterParticles;
    private bool isUsing;
    private bool isSelected = false;

    private void Start()
    {
        _player = PlayerInventory.instance.transform;
    }

    private void Update()
    {
        if (!isSelected)
            return;

        if (!isUsing)
            return;

        // Water soil
        Vector3 position = _player.position + _player.forward * _offset.z;

        if (Physics.Raycast(position, -_player.up, out RaycastHit hit, _distance, LayerMask.GetMask("Interactable")))
        {
            if (hit.collider.TryGetComponent(out FarmlandSquare farmland))
            {
                farmland.IncreaseWaterLevel(_wateringSpeed);
            }
        }
    }

    public override void OnUse()
    {
        isUsing = true;

        _waterParticles.Play();

        _source.DOFade(0.7f, 0.2f);
        transform.DOLocalRotate(new Vector3(50, 0, 0), 0.2f);
        transform.DOLocalMove(new Vector3(0, 0.5f, 0), 0.2f);
    }

    public override void OnStoppingUse()
    {
        isUsing = false;

        _waterParticles.Stop();

        _source.DOFade(0f, 0.2f);
        transform.DOLocalRotate(Vector3.zero, 0.2f);
        transform.DOLocalMove(Vector3.zero, 0.2f);
    }

    public override void OnChangingItems()
    {
        isUsing = false;
        isSelected = false;

        transform.localRotation = Quaternion.identity;
        _waterParticles.Stop();
        _source.volume = 0;
    }

    public override void OnThrow()
    {
        DOTween.Kill(this);
        _waterParticles.Stop();
        _source.volume = 0;
    }

    public override void OnSelect()
    {
        isSelected = true;
    }
}
