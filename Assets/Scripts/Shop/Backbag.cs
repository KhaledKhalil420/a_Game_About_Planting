using UnityEngine;

public class Backbag : MonoBehaviour
{
    public void Trigger()
    {
        PlayerInventory.instance.AddInventorySlot();
     }
}
