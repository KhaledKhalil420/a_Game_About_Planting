using UnityEngine;

public class PlaceableItem : Item
{
    public GameObject placeableObject;
    public Material ghostMaterial;

    private GameObject preview;
    private Camera _camera;
    private Quaternion previewRotation = Quaternion.identity;

    private PlayerInventory inventory;
    public LayerMask PlaceableLayers;

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
                preview = Instantiate(placeableObject);
                preview.GetComponent<Placeable>().enabled = false;
                preview.GetComponent<Collider>().enabled = false;
                SetGhostMaterial(preview);
            }

            preview.transform.position = SnapToGrid(hit.point);
            preview.transform.rotation = previewRotation;
        }
        else if (preview)
        {
            Destroy(preview);
            preview = null;
        }
    }

    public override void OnUse()
    {
        if (preview == null) return;

        Instantiate(placeableObject, preview.transform.position, previewRotation);

        inventory.GetSelectedSlot().HeldQuantity--;
        inventory.GetSelectedSlot().UpdateSlot();

        AudioManager.instance?.PlaySound("Place", 0.8f, 1.15f);
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
        previewRotation = Quaternion.identity;
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
