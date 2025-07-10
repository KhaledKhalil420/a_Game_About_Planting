using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Sellable : MonoBehaviour, IInteractable, IHighlightable
{
    public int cost;
    public Item item;
    public TMP_Text text;
    public CanvasGroup group;

    private void Start()
    {
        text.text = cost.ToString();
        group = GetComponentInChildren<CanvasGroup>();
    }

    public void Interact(GameObject sender)
    {
        if (!PlayersPurse.TrySpendCoins(cost)) return;

        Item runtimeItem = Instantiate(item);
        AudioManager.instance.PlaySound("Pickup", 1.1f, 1.25f);
        PlayerInventory.instance.GiveItem(runtimeItem, out bool wasGiven);

        if (wasGiven)
        {
            PlayersPurse.SpendCoins(cost);
            Destroy(gameObject);
        }
        else Destroy(runtimeItem.gameObject);
    }

    public void Highlight()
    {
        if (group == null) return;
        DOVirtual.Float(group.alpha, 1, 1, x => { if (group != null) group.alpha = x; }).SetAutoKill(true);
        transform.DOLocalMove(new Vector3(0, 0.1f, 0), 0.25f).SetAutoKill(true);
    }

    public void UnHighlight()
    {
        if (group == null) return;
        DOVirtual.Float(group.alpha, 0, 1, x => { if (group != null) group.alpha = x; }).SetAutoKill(true);
        transform.DOLocalMove(new Vector3(0f, 0, 0f), 0.25f).SetAutoKill(true);
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
