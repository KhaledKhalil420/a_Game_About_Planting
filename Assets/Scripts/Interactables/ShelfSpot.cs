using DG.Tweening;
using UnityEngine;

public class ShelfSpot : SaveableObject, IInteractableOnce
{
    private Item storedItem;
    [SerializeField, Savable] private string heldItem = "";
    [SerializeField, Savable] private int quantity = 0;
    

    private void Start()
    {
        if (!string.IsNullOrEmpty(heldItem))
        {
            Item savedItem = Resources.Load<Item>("Prefabs/" + heldItem);

            if (savedItem != null && savedItem.data?.model != null)
            {
                storedItem = Instantiate(savedItem.data.model, transform).GetComponent<Item>();
                
                if (storedItem != null)
                {
                    //Set prefab data
                    storedItem.transform.localPosition = Vector3.zero;
                    storedItem.transform.localEulerAngles = new Vector3(0, 0, -45);

                    storedItem.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    storedItem.GetComponent<Collider>().enabled = false;


                    int targetLayer = LayerMask.NameToLayer("Default");
                    storedItem.gameObject.layer = targetLayer;

                    foreach (Transform child in storedItem.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = targetLayer;
                    }

                    storedItem.HeldQuantity = quantity;                    
                }
            }
        }
    }

    public void Interact(GameObject sender)
    {
        PlayerInventory inventory = sender.GetComponent<PlayerInventory>();

        if (storedItem == null)
        {
            //Get item data 
            Item invHeldItem = inventory.GetHeldItem();
            int invHeldQuantity = inventory.GetSelectedSlot().HeldQuantity;

            if (invHeldItem == null)
                return;

            //Set Savable data
            heldItem = invHeldItem.prefabName;
            quantity = invHeldQuantity;

            //Spawn item
            storedItem = Instantiate(invHeldItem.data.model, transform).GetComponent<Item>();
            
            if (storedItem == null)
                return;
            
            //Set prefab data
            storedItem.HeldQuantity = invHeldQuantity;

            storedItem.transform.localPosition = Vector3.zero;
            storedItem.transform.localEulerAngles = new Vector3(0, 0, -45);
            
            storedItem.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            storedItem.GetComponent<Collider>().enabled = false;


            int targetLayer = LayerMask.NameToLayer("Default");
            storedItem.gameObject.layer = targetLayer;
            
            foreach (Transform child in storedItem.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = targetLayer;
            }

            inventory.GetSelectedSlot().HeldQuantity = 0;
            inventory.GetSelectedSlot().UpdateSlot();
            return;
        }

        else
        {
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
}