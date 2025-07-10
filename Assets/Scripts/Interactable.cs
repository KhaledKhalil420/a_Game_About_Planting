using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour, IInteractable, IHighlightable
{
    public UnityEvent unityEvent;

    public void Highlight()
    {
    }

    public void Interact(GameObject sender)
    {
        unityEvent?.Invoke();
    }

    public void UnHighlight()
    {
    }
}
