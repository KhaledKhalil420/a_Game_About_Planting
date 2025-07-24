using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private Transform[] sellingPoints;
    [SerializeField] private List<Sellable> possibleItemsToSell = new();
    [SerializeField] private List<Sellable> currentItems = new();
    [SerializeField] private string foldersName;

    private void Awake()
    {
        GameObject[] loaded = Resources.LoadAll<GameObject>("Shop" + foldersName);
        foreach (GameObject obj in loaded)
        {
            if (obj.TryGetComponent(out Sellable i)) possibleItemsToSell.Add(i);
        }

        RefreshTrades();
    }

    public void RefreshTrades()
    {
        foreach (Transform point in sellingPoints)
        {
            if (point.childCount > 0)
                Destroy(point.GetChild(0).gameObject);
        }

        currentItems.Clear();

        List<Sellable> pool = new(possibleItemsToSell);

        for (int i = 0; i < sellingPoints.Length && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            Sellable item = pool[index];
            pool.RemoveAt(index);

            Sellable spawned = Instantiate(item, sellingPoints[i].position, Quaternion.identity, sellingPoints[i]);
            spawned.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            currentItems.Add(spawned);
        }
    }

    public void Sell()
    {
        //Get player inventory
        PlayerInventory inventory = PlayerInventory.instance;

        if (inventory.GetSelectedSlot().HeldItem is Seed seed)
        {
            inventory.GetSelectedSlot().HeldQuantity -= 1;
            inventory.GetSelectedSlot().UpdateSlot();
            PlayersPurse.GiveCoins(seed.seedData.sellingProfit);
        }
    }
}