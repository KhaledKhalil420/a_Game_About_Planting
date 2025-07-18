using UnityEngine;

public class Fertilizer : Tool
{
    public override void OnUse()
    {
        if (Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, 15, LayerMask.GetMask("Interactable")))
        {
            if (hit.transform.TryGetComponent(out FarmlandSquare farmlandSquare))
            {
                if (farmlandSquare.isReadyToHarvest)
                    return;

                //Grow square
                farmlandSquare.FullyGrowSoil();

                //Take item
                PlayerInventory.instance.GetSelectedSlot().HeldQuantity -= 1;
            }
        }
    }
}
