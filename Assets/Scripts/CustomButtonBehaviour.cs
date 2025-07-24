using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(Button), typeof(AudioSource), typeof(EventTrigger))]
public class CustomButtonBehaviour : MonoBehaviour
{
    private Vector3 originalSize;
    private RectTransform rectTransform;
    public AudioClip hoverSound, clickSound;
    public AudioSource source;

    public AudioSource musicSource;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.localScale;

        if(source == null)
            source = GetComponent<AudioSource>();

        GetComponent<Button>().onClick.AddListener(OnClick);

        var trigger = GetComponent<EventTrigger>();
        trigger.triggers = new List<EventTrigger.Entry>();

        AddTrigger(trigger, EventTriggerType.PointerEnter, OnHoverEnter);
        AddTrigger(trigger, EventTriggerType.PointerExit, OnHoverExit);
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
    {
        Entry entry = new()
        {
            eventID = type
        };

        entry.callback.AddListener((_) => action());

        trigger.triggers.Add(entry);
    }

    void OnHoverEnter()
    {
        if (hoverSound) source.PlayOneShot(hoverSound);
        rectTransform.DOScale(originalSize * 1.1f, 0.15f);
        
    }

    void OnHoverExit()
    {
        rectTransform.DOScale(originalSize, 0.15f);
    }

    void OnClick()
    {
        if(clickSound) source.PlayOneShot(clickSound);
        if (musicSource != null) DOVirtual.Float(musicSource.volume, 0, 1, value => musicSource.volume = value);
        rectTransform.DOScale(originalSize * 0.85f, 0.1f).OnComplete(() => {rectTransform.DOScale(originalSize, 0.15f);});
    }
}
