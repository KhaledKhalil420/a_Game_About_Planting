using UnityEngine;

public class Hoe : Tool
{
    public GameObject farmlandObject;
    public Material ghostMaterial;
    public Material redMaterial;
    public float soilWater;
    public int soilQuality;

    private bool _isUsing;
    private bool _canPlace;
    private GameObject preview;
    private MeshRenderer previewRenderer;

    private void Start()
    {
        _camera = PlayerLook.mainCamera.transform;
    }

    private void Update()
    {
        if (!_isUsing) return;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, LayerMask.GetMask("Ground", "Farmland")))
        {
            Vector3 snapPos = SnapToGrid(hit.point);

            if (preview == null)
            {
                preview = Instantiate(farmlandObject);
                preview.GetComponent<FarmlandSquare>().enabled = false;
                previewRenderer = preview.GetComponent<MeshRenderer>();
            }

            preview.transform.position = snapPos;
            
            _canPlace = true;
            if (Physics.CheckSphere(snapPos, 0.4f, LayerMask.GetMask("Interactable")))
            {
                Collider[] hits = Physics.OverlapSphere(snapPos, 0.4f, LayerMask.GetMask("Interactable"));

                foreach (var col in hits)
                {
                    if (col.TryGetComponent<FarmlandSquare>(out var fsq) && col.gameObject != preview)
                    {
                        _canPlace = false;
                        break;
                    }
                }
            }
        
            previewRenderer.material = _canPlace ? ghostMaterial : redMaterial;
        }
        else
        {
            if (preview)
            {
                Destroy(preview);
                preview = null;
                previewRenderer = null;
                _canPlace = true;
            }
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x * 0.5f) * 2f,
            Mathf.Round(position.y * 0.5f) * 2f,
            Mathf.Round(position.z * 0.5f) * 2f
        );
    }

    public override void OnUse()
    {
        _isUsing = true;
    }

    public override void OnStoppingUse()
    {
        _isUsing = false;

        if (!_canPlace)
        {
            Destroy(preview);
            preview = null;
            previewRenderer = null;

            _canPlace = true;
            return;
        }

        if (preview)
        {
            previewRenderer.enabled = false;
            animator.SetTrigger("Hit");
        }
    }

    public void Plant()
    {
        if (!preview || !_canPlace) return;

        AudioManager.instance?.PlaySound("Hoeing", 0.9f, 1.25f);

        FarmlandSquare fsq = Instantiate(farmlandObject, preview.transform.position, preview.transform.rotation)
            .GetComponent<FarmlandSquare>();
        fsq._soilQualityLevel = soilQuality;
        fsq._waterLevel = soilWater;

        Destroy(preview);
        preview = null;
        previewRenderer = null;
    }

    public override void OnUseAlt() { }
    public override void OnStoppingUseAlt() { }

    public override void OnUseMiddle()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f) &&
            hit.transform.TryGetComponent(out FarmlandSquare square))
        {
            if(square._seedIn == null)
            animator.SetTrigger("Remove");
        }
    }

    public override void OnChangingItems()
    {
        _isUsing = false;

        if (preview)
        {
            Destroy(preview);
            preview = null;
            previewRenderer = null;
        }
    }

    public void Remove()
    {
        AudioManager.instance?.PlaySound("Hoeing", 1.25f, 1.5f);

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f) &&
            hit.transform.TryGetComponent(out FarmlandSquare square))
        {
            Destroy(square.gameObject);
        }
    }

    public override void OnSelect()
    {
        animator.enabled = true;
    }

    public override void OnThrow()
    {
        animator.enabled = false;
        
        if (preview)
        {
            Destroy(preview);
            preview = null;
            previewRenderer = null;
        }
    }
}