using UnityEngine;
using UnityEngine.Events;

public class SellablePriceIncrease : Sellable
{
    public string sellableName;
    public float costIncrease;
    public int maxPrice = 200000;


    public UnityEvent onBuyEvent;

    private void Start()
    {
        cost = (int)ShopEconomy.instance.GetPrice(sellableName, cost);

        group = GetComponentInChildren<CanvasGroup>();

        text.text = cost.ToString();
        desrcriptionText.text = description;
    }

    public override void OnBought()
    {
        onBuyEvent?.Invoke();

        if(cost < maxPrice)
        ShopEconomy.instance.IncreaseItemCost(sellableName, costIncrease);

        if(item == null)    
        Destroy(gameObject);
    }
}
