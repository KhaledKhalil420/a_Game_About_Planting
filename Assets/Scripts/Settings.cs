using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

[Serializable]
public class SaveSettingsData
{
    public float Sensitivity = 1f;
    public float RenderDistance = 100f;
    public int QualityLevel = 2;
    public int ResolutionIndex = 0;
    public float MusicVolume = 0f;
    public float SfxVolume = 0f;
    public float MasterVolume = 0f;
    public float AmbienceVolume = 0f; // 👈 NEW
}

public class Settings : MonoBehaviour
{
    private const string SaveFileName = "settings.json";

    [Header("Player")]
    [SerializeField] private Camera mainCamera;
    private PlayerLook playerLook;

    [Header("UI Elements")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider renderDistanceSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider ambienceSlider; // 👈 NEW
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Audio Mixers")]
    [SerializeField] private AudioMixer masterMixer;

    private List<Resolution> validResolutions = new List<Resolution>();
    private SaveSettingsData settingsData = new SaveSettingsData();
    private string fullPath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        playerLook = mainCamera?.GetComponentInParent<PlayerLook>();
        PopulateResolutions();
        LoadSettings();
        AnimateAllUI();
    }

    private void OnEnable() => SubscribeUI();
    private void OnDisable() => UnsubscribeUI();

    private void SubscribeUI()
    {
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceChanged);
        graphicsDropdown.onValueChanged.AddListener(OnQualityChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        ambienceSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged); // 👈 NEW
    }

    private void UnsubscribeUI()
    {
        sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        renderDistanceSlider.onValueChanged.RemoveListener(OnRenderDistanceChanged);
        graphicsDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        ambienceSlider.onValueChanged.RemoveListener(OnAmbienceVolumeChanged); // 👈 NEW
    }

    private void PopulateResolutions()
    {
        resolutionDropdown.ClearOptions();
        validResolutions.Clear();
        float targetAspect = 16f / 9f;
        foreach (var res in Screen.resolutions)
        {
            if (res.width >= 640 && res.height >= 360 &&
                Mathf.Approximately((float)res.width / res.height, targetAspect) &&
                !validResolutions.Exists(r => r.width == res.width && r.height == res.height))
            {
                validResolutions.Add(res);
            }
        }
        var options = validResolutions.ConvertAll(r => $"{r.width} x {r.height}");
        resolutionDropdown.AddOptions(options);
    }

    private void OnSensitivityChanged(float value)
    {
        settingsData.Sensitivity = value;
        if (playerLook != null) playerLook.Sensitivity = value;
        SaveSettings();
    }

    private void OnRenderDistanceChanged(float value)
    {
        settingsData.RenderDistance = value;
        if (mainCamera != null) mainCamera.farClipPlane = value;
        SaveSettings();
    }

    private void OnQualityChanged(int index)
    {
        settingsData.QualityLevel = index;
        QualitySettings.SetQualityLevel(index, true);
        SaveSettings();
    }

    private void OnResolutionChanged(int index)
    {
        settingsData.ResolutionIndex = index;
        if (index >= 0 && index < validResolutions.Count)
        {
            var res = validResolutions[index];
            Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow, res.refreshRateRatio);
        }
        SaveSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        settingsData.MusicVolume = value;
        masterMixer.SetFloat("VolumeMusic", value);
        SaveSettings();
    }

    private void OnSfxVolumeChanged(float value)
    {
        settingsData.SfxVolume = value;
        masterMixer.SetFloat("VolumeSfx", value);
        SaveSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        settingsData.MasterVolume = value;
        masterMixer.SetFloat("VolumeMaster", value);
        SaveSettings();
    }

    private void OnAmbienceVolumeChanged(float value)
    {
        settingsData.AmbienceVolume = value;
        masterMixer.SetFloat("VolumeAmbience", value);
        SaveSettings();
    }

    private void SaveSettings()
    {
        try { File.WriteAllText(fullPath, JsonUtility.ToJson(settingsData, true)); }
        catch (Exception ex) { Debug.LogError($"Failed to save settings: {ex.Message}"); }
    }

    private void LoadSettings()
    {
        if (File.Exists(fullPath))
        {
            try { settingsData = JsonUtility.FromJson<SaveSettingsData>(File.ReadAllText(fullPath)); }
            catch (Exception ex) { Debug.LogError($"Failed to load settings: {ex.Message}"); settingsData = new SaveSettingsData(); }
        }
        ApplySettingsToUI();
    }

    private void ApplySettingsToUI()
    {
        sensitivitySlider.value = settingsData.Sensitivity;
        renderDistanceSlider.value = settingsData.RenderDistance;
        graphicsDropdown.value = settingsData.QualityLevel;
        resolutionDropdown.value = settingsData.ResolutionIndex;
        musicSlider.value = settingsData.MusicVolume;
        sfxSlider.value = settingsData.SfxVolume;
        masterSlider.value = settingsData.MasterVolume;
        ambienceSlider.value = settingsData.AmbienceVolume; // 👈 NEW

        OnSensitivityChanged(settingsData.Sensitivity);
        OnRenderDistanceChanged(settingsData.RenderDistance);
        OnQualityChanged(settingsData.QualityLevel);
        OnResolutionChanged(settingsData.ResolutionIndex);
        OnMusicVolumeChanged(settingsData.MusicVolume);
        OnSfxVolumeChanged(settingsData.SfxVolume);
        OnMasterVolumeChanged(settingsData.MasterVolume);
        OnAmbienceVolumeChanged(settingsData.AmbienceVolume); // 👈 NEW
    }

    public void ExitGame() => Application.Quit();

    private void AnimateAllUI()
    {
        AnimateUIElement(sensitivitySlider.GetComponent<RectTransform>());
        AnimateUIElement(renderDistanceSlider.GetComponent<RectTransform>());
        AnimateUIElement(musicSlider.GetComponent<RectTransform>());
        AnimateUIElement(sfxSlider.GetComponent<RectTransform>());
        AnimateUIElement(masterSlider.GetComponent<RectTransform>());
        AnimateUIElement(ambienceSlider.GetComponent<RectTransform>()); // 👈 NEW
        AnimateUIElement(graphicsDropdown.GetComponent<RectTransform>());
        AnimateUIElement(resolutionDropdown.GetComponent<RectTransform>());
    }

    private void AnimateUIElement(RectTransform target)
    {
        if (target == null) return;

        float startY = target.anchoredPosition.y;
        target.DOAnchorPosY(startY + 3f, 3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetDelay(UnityEngine.Random.Range(0f, 1f))
            .SetUpdate(true);

        var trigger = target.gameObject.GetComponent<EventTrigger>() ?? target.gameObject.AddComponent<EventTrigger>();
        trigger.triggers ??= new List<EventTrigger.Entry>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, () => Wiggle(target));
        AddTrigger(trigger, EventTriggerType.PointerClick, () => Pulse(target));
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, Action callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((_) => callback());
        trigger.triggers.Add(entry);
    }

    private void Wiggle(RectTransform target) { }

    private void Pulse(RectTransform target)
    {
        AudioManager.instance?.PlaySound("Click", 1, 1.15f);

        target.DOKill(true);
        target.DOPunchScale(Vector3.one * 0.05f, 0.2f, 3, 0.4f)
            .SetUpdate(true);
    }
}
