using UnityEngine;

namespace StylizedGrass
{
    /// <summary>
    /// Drop this on any GameObject in your settings menu, then wire UI events to its methods in the Inspector:
    ///   Toggle  -> On Value Changed -> SetGrassEnabled (Dynamic bool)
    ///   Slider  -> On Value Changed -> SetGrassQuality (Dynamic float, slider 0..1)
    ///   Slider  -> On Value Changed -> SetGrassDistance (Dynamic float, slider 0.1..1.5)
    ///   (same for Details). Works with UGUI, TextMeshPro, UI Toolkit (via script) - anything.
    /// To initialize your UI, read the current values from the properties below.
    /// </summary>
    [AddComponentMenu("Stylized Grass/Grass Settings Controller")]
    public class GrassSettingsController : MonoBehaviour
    {
        // ---- setters (wire these to UI events)
        public void SetGrassEnabled(bool value) { GrassGraphicsSettings.GrassEnabled = value; }
        public void SetDetailsEnabled(bool value) { GrassGraphicsSettings.DetailsEnabled = value; }
        public void SetGrassQuality(float value) { GrassGraphicsSettings.GrassQuality = value; }
        public void SetDetailsQuality(float value) { GrassGraphicsSettings.DetailsQuality = value; }
        public void SetGrassDistance(float value) { GrassGraphicsSettings.GrassDistance = value; }
        public void SetDetailsDistance(float value) { GrassGraphicsSettings.DetailsDistance = value; }

        public void ToggleGrass() { GrassGraphicsSettings.GrassEnabled = !GrassGraphicsSettings.GrassEnabled; }
        public void ToggleDetails() { GrassGraphicsSettings.DetailsEnabled = !GrassGraphicsSettings.DetailsEnabled; }

        /// <summary>0 = Off, 1 = Low, 2 = Medium, 3 = High, 4 = Ultra (e.g. from a dropdown index).</summary>
        public void ApplyPreset(int preset) { GrassGraphicsSettings.ApplyPreset((GrassQualityPreset)Mathf.Clamp(preset, 0, 4)); }
        public void ResetToDefaults() { GrassGraphicsSettings.ResetToDefaults(); }

        // ---- current values (to initialize your UI)
        public bool GrassEnabled => GrassGraphicsSettings.GrassEnabled;
        public bool DetailsEnabled => GrassGraphicsSettings.DetailsEnabled;
        public float GrassQuality => GrassGraphicsSettings.GrassQuality;
        public float DetailsQuality => GrassGraphicsSettings.DetailsQuality;
        public float GrassDistance => GrassGraphicsSettings.GrassDistance;
        public float DetailsDistance => GrassGraphicsSettings.DetailsDistance;

        void OnDisable() { GrassGraphicsSettings.Flush(); }
    }
}
