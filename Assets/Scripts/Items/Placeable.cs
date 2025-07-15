using DG.Tweening;
using UnityEngine;

public class Placeable : MonoBehaviour, IInteractableAlt
{
    public GameObject associatedItem;

    private void Start()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.35f).SetEase(Ease.OutQuad);
    }

    public void InteractAlt(GameObject sender)
    {
        if (associatedItem == null) return;

        GameObject itemGO = Instantiate(associatedItem);
        Item item = itemGO.GetComponent<Item>();
        item.HeldQuantity = 1;

        if (sender.TryGetComponent<PlayerInventory>(out var inventory))
        {
            inventory.GiveItem(item, out bool wasGiven);

            if (wasGiven)
            {
                AudioManager.instance?.PlaySound("Pickup");
                Destroy(gameObject);
            }

            else
            {
                Destroy(itemGO.gameObject);
            }
        }
    }
}
