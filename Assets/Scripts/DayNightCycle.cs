using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle instance;

    public enum TimeState { Day, Night }
    public TimeState CurrentState;

    [Range(0f, 1f)] public float timeOfDay = 0f;
    public float dayDuration = 60f;
    public float timeSpeedMultiplier = 1f;
    public Material skyMaterial;
    public Light sunLight;
    public LightSettings lightSettings = new LightSettings(2f, 0.25f);
    public List<TimeColorSetting> timeColors;
    public float audioFadeDuration = 5f;

    [SerializeField] private float sunRotationSpeed = 10f;
    [SerializeField, Range(0f, 1f)] private float dayStart = 0.25f;
    [SerializeField, Range(0f, 1f)] private float nightStart = 0.75f;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    public event Action OnDayStarted;
    public event Action OnNightStarted;

    float timeSpeed;
    Color currentSceneColor;
    int currentColorIndex = -1;
    AudioSource srcA, srcB;
    bool usingA = true;
    float currentSunYRotation = 0f;
    bool isCurrentlyDay;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (skyMaterial == null)
        {
            RenderSettings.skybox = new Material(RenderSettings.skybox);
            skyMaterial = RenderSettings.skybox;
        }

        timeSpeed = 1f / dayDuration;
        currentSceneColor = RenderSettings.ambientLight;

        srcA = gameObject.AddComponent<AudioSource>();
        srcB = gameObject.AddComponent<AudioSource>();
        srcA.loop = srcB.loop = true;
        srcA.outputAudioMixerGroup = sfxMixerGroup;
        srcB.outputAudioMixerGroup = sfxMixerGroup;
        srcA.playOnAwake = srcB.playOnAwake = false;

        UpdateSceneColor(true);

        srcA.Play();
        srcB.Play();

        isCurrentlyDay = IsDay();
        CurrentState = isCurrentlyDay ? TimeState.Day : TimeState.Night;
    }

    void Update()
    {
        timeOfDay += timeSpeed * timeSpeedMultiplier * TimeManager.Tickspeed * Time.deltaTime;
        if (timeOfDay > 1f) timeOfDay -= 1f;

        float blend = Mathf.Clamp01(Mathf.Cos((timeOfDay + 0.25f) * Mathf.PI * 2f) * 0.5f + 0.5f);
        skyMaterial.SetFloat("_CubemapTransition", blend);

        UpdateSunDirection();
        UpdateSunLight(blend);
        UpdateSceneColor();
        UpdateAudioFade();
        CheckDayNightTransition();
    }

    void UpdateSunDirection()
    {
        currentSunYRotation = (currentSunYRotation + sunRotationSpeed * Time.deltaTime) % 360f;
        if (sunLight)
        {
            var e = sunLight.transform.rotation.eulerAngles;
            e.y = currentSunYRotation;
            sunLight.transform.rotation = Quaternion.Euler(e);
        }
    }

    void UpdateSunLight(float blend)
    {
        if (!sunLight) return;
        sunLight.intensity = Mathf.Lerp(lightSettings.nightIntensity, lightSettings.dayIntensity, 1f - blend);
        sunLight.shadowStrength = Mathf.Lerp(0.1f, 1f, 1f - blend);
    }

    void UpdateSceneColor(bool forceInit = false)
    {
        int newIndex = currentColorIndex;
        for (int i = 0; i < timeColors.Count; i++)
        {
            var t = timeColors[i];
            if (timeOfDay >= t.startTime && timeOfDay <= t.endTime)
            {
                newIndex = i;
                break;
            }
        }

        var setting = timeColors[newIndex];

        if (newIndex != currentColorIndex || forceInit)
        {
            if (setting.ambientClip != null)
            {
                usingA = !usingA;
                var fadeUp = usingA ? srcA : srcB;
                fadeUp.clip = setting.ambientClip;
                fadeUp.Play();
            }
            currentColorIndex = newIndex;
        }

        currentSceneColor = Color.Lerp(currentSceneColor, setting.ambientColor, Time.deltaTime * 2f);
        RenderSettings.ambientLight = currentSceneColor;
        RenderSettings.fogColor = currentSceneColor;
    }

    void UpdateAudioFade()
    {
        float t = Time.deltaTime / audioFadeDuration;
        if (usingA)
        {
            srcA.volume = Mathf.Clamp01(srcA.volume + t);
            srcB.volume = Mathf.Clamp01(srcB.volume - t);
        }
        else
        {
            srcA.volume = Mathf.Clamp01(srcA.volume - t);
            srcB.volume = Mathf.Clamp01(srcB.volume + t);
        }
    }

    void CheckDayNightTransition()
    {
        bool isDay = IsDay();

        if (isDay != isCurrentlyDay)
        {
            isCurrentlyDay = isDay;
            CurrentState = isDay ? TimeState.Day : TimeState.Night;

            if (isDay) OnDayStarted?.Invoke();
            else OnNightStarted?.Invoke();
        }
    }

    bool IsDay()
    {
        return timeOfDay >= dayStart && timeOfDay < nightStart;
    }

    void OnApplicationQuit()
    {
        if (skyMaterial != null)
            skyMaterial.SetFloat("_CubemapTransition", 0f);
    }

    [Serializable]
    public class LightSettings
    {
        public float dayIntensity = 2f;
        public float nightIntensity = 0.25f;
        public LightSettings(float day, float night)
        {
            dayIntensity = day;
            nightIntensity = night;
        }
    }

    [Serializable]
    public class TimeColorSetting
    {
        [Range(0f, 1f)] public float startTime;
        [Range(0f, 1f)] public float endTime;
        [ColorUsage(true, true)] public Color ambientColor = Color.white;
        public AudioClip ambientClip;
    }
}
