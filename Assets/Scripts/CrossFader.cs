using DG.Tweening;
using UnityEngine;

public class CrossFader : MonoBehaviour
{
    public CanvasGroup group;
    private static CrossFader instance;

    private void Awake()
    {
        instance = this;
    }

    public static void StartFade()
    {
        PlayerManager.instance.LockPlayer();

        instance.group.alpha = 0;
        instance.group.DOFade(1f, 0.15f).OnComplete(() =>
        {
            instance.group.DOFade(0, 0.15f);
            PlayerManager.instance.UnlockPlayer();
        });
    }
}
