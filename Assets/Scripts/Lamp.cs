using UnityEngine;

public class Lamp : Interactable
{
    [SerializeField] private GameObject lamp;

    public override void OnInteract()
    {
        lamp.SetActive(!lamp.activeSelf);
    }
}
