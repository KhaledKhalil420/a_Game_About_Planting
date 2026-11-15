using UnityEngine;

public class PlaceableItem : Item
{
    public string soundName = "Place";
    public GameObject placeableObject;
    public Material ghostMaterial;

    private GameObject preview;
    private Camera _camera;
    private Quaternion previewRotation = Quaternion.identity;
    private bool canPlace;

    private PlayerInventory inventory;
    public LayerMask PlaceableLayers;
    public LayerMask BlockingLayers; // layers of things you can't place inside of

    private void Start()
    {
        inventory = PlayerInventory.instance;
        _camera = PlayerLook.mainCamera;
    }

    private void Update()
    {
        if (!isItemPickedUp || placeableObject == null) return;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, PlaceableLayers))
        {
            if (preview == null)
            {
                preview = Instantiate(placeableObject.gameObject);

                if (preview.TryGetComponent(out Placeable placeable))
                {
                    placeable.enabled = false;
                }

                if (preview.TryGetComponent(out SpruceTree tree))
                {
                    tree.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                    tree.enabled = false;
                }

                preview.GetComponent<Collider>().enabled = false;
                SetGhostMaterial(preview);
            }

            preview.transform.position = SnapToGrid(hit.point);
            preview.transform.rotation = previewRotation;

            Bounds b = GetBounds(preview);
            canPlace = !Physics.CheckBox(b.center, b.extents * 0.9f, Quaternion.identity, BlockingLayers, QueryTriggerInteraction.Ignore);
        }
        else if (preview)
        {
            Destroy(preview);
            preview = null;
            canPlace = false;
        }
    }

    public override void OnUse()
    {
        if (preview == null || !canPlace) return;

        Instantiate(placeableObject, preview.transform.position, previewRotation);

        inventory.GetSelectedSlot().HeldQuantity--;
        inventory.GetSelectedSlot().UpdateSlot();

        AudioManager.instance?.PlaySound(soundName, 0.8f, 1.15f);
    }

    public override void OnUseAlt()
    {
        previewRotation *= Quaternion.Euler(0, 90f, 0);
    }

    public override void OnChangingItems()
    {
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }
        canPlace = false;
        previewRotation = Quaternion.identity;
    }

    private Bounds GetBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y),
            Mathf.Round(pos.z)
        );
    }

    private void SetGhostMaterial(GameObject obj)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            Material[] newMats = new Material[r.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = ghostMaterial;
            }
            r.materials = newMats;
        }
    }
}