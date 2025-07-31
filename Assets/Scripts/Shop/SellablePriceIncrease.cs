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

        if (cost < maxPrice)
        {
            float currentPrice = ShopEconomy.instance.GetPrice(sellableName, cost);
            float wantedPrice = Mathf.Min(currentPrice * costIncrease, maxPrice);
            float multiplier = wantedPrice / currentPrice;

            ShopEconomy.instance.IncreaseItemCost(sellableName, multiplier);
        }

        if (!PlayersPurse.TrySpendCoins(cost))
            return;

        if (item == null)
        {
            PlayersPurse.SpendCoins(cost);
            Destroy(gameObject);
        }
    }
}
