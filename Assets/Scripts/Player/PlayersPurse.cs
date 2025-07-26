using TMPro;
using UnityEngine;

public class PlayersPurse : MonoBehaviour
{
    public int coins = 0;
    public static PlayersPurse instance;
    public TMP_Text coinsQuantityText;

    void Awake()
    {
        instance = this;
        UpdateUi();
    }

    private void Start()
    {
        coins = PlayerPrefs.GetInt("coins", 0);
    }

    public static void GiveCoins(int quantity)
    {
        instance.coins += quantity;
        instance.UpdateUi();
        instance.SaveCoins();
    }

    public static bool TrySpendCoins(int quantity)
    {
        if (quantity <= instance.coins)
        {
            return true;
        }

        return false;
    }

    public static void SpendCoins(int quantity)
    {
        if (quantity <= instance.coins)
        {
            instance.SaveCoins();
            instance.coins -= quantity;
            instance.UpdateUi();
        }
    }


    public void UpdateUi()
    {
        coinsQuantityText.text = coins.ToString();
    }

    public void SaveCoins()
    {
        PlayerPrefs.SetInt("Coins", coins);
    }
}
