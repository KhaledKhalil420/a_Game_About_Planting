using System.Collections.Generic;
using UnityEngine;

public class ShopEconomy : MonoBehaviour
{
    public static ShopEconomy instance;
    public List<ItemCostData> scalableItems;

    private void Awake()
    {
        instance = this;
    }

    public float GetPrice(string item, float basePrice)
    {
        foreach (var data in scalableItems)
        {
            if (data.item == item)
            {
                return basePrice * data.costMultiplier;
            }
        }

        return basePrice;
    }

    public void IncreaseItemCost(string item, float multiplier)
    {
        foreach (var data in scalableItems)
        {
            if (data.item == item)
            {
                data.costMultiplier *= multiplier;
            }
        }
    }
}

[System.Serializable]
public class ItemCostData
{
    public string item;
    public float costMultiplier = 1;
}