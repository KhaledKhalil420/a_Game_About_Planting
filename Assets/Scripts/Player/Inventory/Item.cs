using DG.Tweening;
using UnityEngine;

public class Item : SaveableObject, IHighlightable
{
    public ItemData data;

    public bool isSingleQuantityItem = false;
    [Savable] public int HeldQuantity;

    [Savable] internal bool isItemPickedUp;
    [Savable] internal int savedSlot = -1; // which inventory slot it was in when saved

    // Shelf items are saved by the ShelfSpot. Items in hand but not in a slot are leftovers, don't save them.
    public override bool ShouldSave =>
        GetComponentInParent<ShelfSpot>() == null && (!isItemPickedUp || savedSlot >= 0);

    // Works even if a subclass has its own Start()
    public override string PrefabName => data != null && data.model != null ? data.model.name : prefabName;

    public void UpdateHoldingItem(bool isHolding)
    {
        isItemPickedUp = isHolding;
        int targetLayer = LayerMask.NameToLayer(isHolding ? "Held" : "Pickable");

        GetComponent<Collider>().enabled = !isHolding;
        GetComponent<Rigidbody>().constraints = isHolding ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        gameObject.layer = targetLayer;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = targetLayer;
        }
    }

    public virtual void OnUse()
    {
    }

    public virtual void OnUseAlt()
    {
    }

    public virtual void OnStoppingUse()
    {
    }

    public virtual void OnStoppingUseAlt()
    {
    }

    public virtual void OnUseMiddle()
    {
    }

    public virtual void OnSelect()
    {
    }

    public virtual void OnThrow()
    {
    }

    public virtual void OnChangingItems()
    {
    }

    public void SetItemParent(Transform parent)
    {
        transform.parent = parent;
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        UpdateHoldingItem(parent != null);
    }

    public void Highlight()
    {

    }

    public void UnHighlight()
    {

    }
    
    public virtual Item Clone()
    {
        return (Item)this.MemberwiseClone();
    }

}