using UnityEngine;

public class ShelfSpot : SaveableObject, IInteractableOnce, ISavable
{
    private Item storedItem;
    [SerializeField, Savable] private string heldItem = "";
    [SerializeField, Savable] private int quantity = 0;

    private void Start()
    {
        if (storedItem == null)
            ShowSaved();
    }

    public void OnSave() { }

    public void OnLoad()
    {
        ShowSaved();
    }

    private void ShowSaved()
    {
        if (storedItem != null)
            Destroy(storedItem.gameObject);
        storedItem = null;

        if (string.IsNullOrEmpty(heldItem))
            return;

        Item prefab = Resources.Load<Item>("Prefabs/" + heldItem);
        if (prefab != null && prefab.data?.model != null)
            Show(prefab.data.model, quantity);
        else
            Debug.LogWarning($"ShelfSpot: no item prefab at Resources/Prefabs/{heldItem}");
    }

    private void Show(GameObject model, int amount)
    {
        storedItem = Instantiate(model, transform).GetComponent<Item>();
        if (storedItem == null)
            return;

        storedItem.HeldQuantity = amount;
        storedItem.transform.localPosition = Vector3.zero;
        storedItem.transform.localEulerAngles = new Vector3(0, 0, -45);

        storedItem.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        storedItem.GetComponent<Collider>().enabled = false;

        int targetLayer = LayerMask.NameToLayer("Default");
        foreach (Transform child in storedItem.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = targetLayer;
        }
    }

    public void Interact(GameObject sender)
    {
        PlayerInventory inventory = sender.GetComponent<PlayerInventory>();

        if (storedItem == null)
        {
            Item invItem = inventory.GetHeldItem();
            if (invItem == null || string.IsNullOrEmpty(invItem.PrefabName))
                return;

            SlotHolder slot = inventory.GetSelectedSlot();

            heldItem = invItem.PrefabName;
            quantity = slot.HeldQuantity;
            Show(invItem.data.model, quantity);

            //Remove the item from the inventory for real
            slot.ResetSlot();
            Destroy(invItem.gameObject);
            return;
        }

        inventory.GiveItem(storedItem, out bool isGiven);

        if (isGiven)
        {
            heldItem = "";
            quantity = 0;

            storedItem.transform.localScale = storedItem.data.model.transform.localScale;
            storedItem = null;
        }
    }
}