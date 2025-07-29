using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour, IInteractable, IHighlightable
{
    public UnityEvent unityEvent;

    public void Highlight()
    {
    }
    public void UnHighlight()
    {
    }

    public void Interact(GameObject sender)
    {
        unityEvent?.Invoke();
        OnInteract();
    }


    public virtual void OnInteract()
    {

     }
}
