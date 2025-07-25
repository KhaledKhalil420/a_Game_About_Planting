using UnityEngine;

public class Hoe : Tool
{
    public GameObject farmlandObject;
    public Material ghostMaterial;
    public Material redMaterial;
    public Item dirtBag;
    public float soilWater;
    public int soilQuality;

    private bool isUsing;
    private bool canPlace;
    private GameObject preview;
    private MeshRenderer previewRenderer;
    private PlayerInventory inventory;

    private void Start()
    {
        _camera = PlayerLook.mainCamera.transform;
        inventory = PlayerInventory.instance;
    }

    private void Update()
    {
        if (!inventory.HasItem(dirtBag)) // this is what I need in the script
            return;

        if (!isUsing)
            return;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, LayerMask.GetMask("Farmland")))
        {
            Vector3 snapPos = SnapToGrid(hit.point);

            if (preview == null)
            {
                preview = Instantiate(farmlandObject);
                preview.GetComponent<FarmlandSquare>().enabled = false;
                previewRenderer = preview.GetComponent<MeshRenderer>();
            }

            preview.transform.position = snapPos;
            
            canPlace = true;
            if (Physics.CheckSphere(snapPos, 0.4f, LayerMask.GetMask("Interactable")))
            {
                Collider[] hits = Physics.OverlapSphere(snapPos, 0.4f, LayerMask.GetMask("Interactable"));

                foreach (var col in hits)
                {
                    if (col.TryGetComponent<FarmlandSquare>(out var fsq) && col.gameObject != preview)
                    {
                        canPlace = false;
                        break;
                    }
                }
            }
        
            previewRenderer.material = canPlace ? ghostMaterial : redMaterial;
        }
        else
        {
            if (preview)
            {
                Destroy(preview);
                preview = null;
                previewRenderer = null;
                canPlace = true;
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
        isUsing = true;
    }

    public override void OnStoppingUse()
    {
        isUsing = false;

        if (!canPlace)
        {
            Destroy(preview);
            preview = null;
            previewRenderer = null;

            canPlace = true;
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
        if (!preview || !canPlace) return;

        AudioManager.instance?.PlaySound("Hoeing", 0.9f, 1.25f);

        FarmlandSquare fsq = Instantiate(farmlandObject, preview.transform.position, preview.transform.rotation)
            .GetComponent<FarmlandSquare>();
        fsq.soilQualityLevel = soilQuality;
        fsq.waterLevel = soilWater;

        inventory.FindSlotWithItem(dirtBag, out SlotHolder holder);
        holder.HeldQuantity--;

        Destroy(preview);
        preview = null;
        previewRenderer = null;
    }

    public override void OnUseAlt()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.transform.TryGetComponent(out FarmlandSquare square))
        {
            if (square != null)
                animator.SetTrigger("Remove");
        }
    }

    public override void OnStoppingUseAlt() { }

    public override void OnUseMiddle()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.transform.TryGetComponent(out FarmlandSquare square))
        {
            if(square != null)
                animator.SetTrigger("Remove");
        }
    }

    public override void OnChangingItems()
    {
        isUsing = false;

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

        if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.transform.TryGetComponent(out FarmlandSquare square))
        {
            if (square.seedIn != null)
            {
                Item s = square.seedIn.reward;
                s.HeldQuantity = 1;

                Instantiate(s, square.transform.position + new Vector3(0, 0.5f, 0), Quaternion.Euler(0, 90, 0));
                
                animator.SetTrigger("Remove");  
            }

            Item dirtBagObj = Instantiate(dirtBag, square.transform.position + new Vector3(0, 0.15f, 0), Quaternion.identity).GetComponent<Item>();
            dirtBagObj.HeldQuantity = 1;
            inventory.GiveItem(dirtBagObj, out bool Given);

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