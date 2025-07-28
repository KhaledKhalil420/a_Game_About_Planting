using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CrossFader : MonoBehaviour
{
    public CanvasGroup group;
    private static CrossFader instance;
    public TMP_Text message;

    private void Awake()
    {
        instance = this;
    }

    public static void StartFade()
    {
        instance.message.enabled = false;
        PlayerManager.instance.LockPlayer();

        instance.group.alpha = 0;
        instance.group.DOFade(1f, 0.15f).OnComplete(() =>
        {
            instance.group.DOFade(0, 0.15f);
            PlayerManager.instance.UnlockPlayer();
        });
    }

    public static void StartFade(float duration)
    {
        instance.message.enabled = false;
        PlayerManager.instance.LockPlayer();

        instance.group.alpha = 0;
        instance.group.DOFade(1f, duration).OnComplete(() =>
        {
            instance.group.DOFade(0, duration);
            PlayerManager.instance.UnlockPlayer();
        });
    }

    public static void StartFade(float start, float durationInBetween, float end, string message = "", Action onComplete = null)
    {
        PlayerManager.instance.LockPlayer();

        if (instance?.message != null)
        {
            instance.message.enabled = true;
            instance.message.text = message;
        }

        instance.group.alpha = 0;
        instance.group.DOFade(1f, start).OnComplete(() =>
        {
            DOVirtual.DelayedCall(durationInBetween, () =>
            {
                instance.group.DOFade(0, end).OnComplete(() =>
                {
                    instance.message.text = "";
                    PlayerManager.instance.UnlockPlayer();
                    onComplete?.Invoke();  // Run your custom action here if provided
                });
            });
        });
    }

}
