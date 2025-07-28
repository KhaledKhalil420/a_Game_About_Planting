using DG.Tweening;
using UnityEngine;

public class Bed : MonoBehaviour, IInteractable, IHighlightable
{
    public void Highlight()
    {
    }
    public void UnHighlight()
    {
    }

    public void Interact(GameObject sender)
    {
        CrossFader.StartFade(.5f, 2, .5f, "zzzz...");

        DOVirtual.DelayedCall(1, () =>
        {
            DayNightCycle.instance.timeOfDay = 0;
        });
    }

}
