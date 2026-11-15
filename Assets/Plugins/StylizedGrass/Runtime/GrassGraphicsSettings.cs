using UnityEngine;

namespace StylizedGrass
{
    public enum GrassQualityPreset { Off, Low, Medium, High, Ultra }

    /// <summary>
    /// Global, runtime graphics settings for ALL grass renderers. Use it from any script / settings menu:
    ///
    ///     GrassGraphicsSettings.GrassEnabled    = false;
    ///     GrassGraphicsSettings.GrassQuality    = 0.5f;   // 0.05 .. 1   (fraction of the baked instances that get drawn)
    ///     GrassGraphicsSettings.GrassDistance   = 0.75f;  // 0.1 .. 1.5  (x the distance you authored)
    ///     GrassGraphicsSettings.DetailsEnabled / DetailsQuality / DetailsDistance  (same, for flowers/rocks/...)
    ///
    /// Values are saved to PlayerPrefs automatically and restored on startup.
    /// QUALITY = QUANTITY: fewer instances means fewer vertices, fewer draws and less overdraw, the most effective
    /// lever. Grass also gets slightly wider blades at low quality so the ground stays covered, and swaps to a
    /// simpler mesh sooner.
    /// </summary>
    public static class GrassGraphicsSettings
    {
        public const float MinQuality = 0.05f;
        public const float MinDistance = 0.1f;
        public const float MaxDistance = 1.5f;

        const string K = "StylizedGrass.";

        static bool s_grassEnabled = true, s_detailsEnabled = true;
        static float s_grassQuality = 1f, s_detailsQuality = 1f;
        static float s_grassDistance = 1f, s_detailsDistance = 1f;
        static bool s_needsFlush;

        /// <summary>Raised whenever any value changes.</summary>
        public static event System.Action Changed;

        public static bool GrassEnabled
        {
            get => s_grassEnabled;
            set { if (s_grassEnabled == value) return; s_grassEnabled = value; Touch(); }
        }

        public static bool DetailsEnabled
        {
            get => s_detailsEnabled;
            set { if (s_detailsEnabled == value) return; s_detailsEnabled = value; Touch(); }
        }

        public static float GrassQuality
        {
            get => s_grassQuality;
            set { value = Mathf.Clamp(value, MinQuality, 1f); if (Mathf.Approximately(s_grassQuality, value)) return; s_grassQuality = value; Touch(); }
        }

        public static float DetailsQuality
        {
            get => s_detailsQuality;
            set { value = Mathf.Clamp(value, MinQuality, 1f); if (Mathf.Approximately(s_detailsQuality, value)) return; s_detailsQuality = value; Touch(); }
        }

        public static float GrassDistance
        {
            get => s_grassDistance;
            set { value = Mathf.Clamp(value, MinDistance, MaxDistance); if (Mathf.Approximately(s_grassDistance, value)) return; s_grassDistance = value; Touch(); }
        }

        public static float DetailsDistance
        {
            get => s_detailsDistance;
            set { value = Mathf.Clamp(value, MinDistance, MaxDistance); if (Mathf.Approximately(s_detailsDistance, value)) return; s_detailsDistance = value; Touch(); }
        }

        public static void ApplyPreset(GrassQualityPreset preset)
        {
            bool g = true; float q = 1f, d = 1f;
            switch (preset)
            {
                case GrassQualityPreset.Off: g = false; break;
                case GrassQualityPreset.Low: q = 0.3f; d = 0.5f; break;
                case GrassQualityPreset.Medium: q = 0.6f; d = 0.75f; break;
                case GrassQualityPreset.High: q = 1f; d = 1f; break;
                case GrassQualityPreset.Ultra: q = 1f; d = 1.4f; break;
            }
            s_grassEnabled = g; s_detailsEnabled = g;
            s_grassQuality = q; s_detailsQuality = q;
            s_grassDistance = d; s_detailsDistance = d;
            Touch();
        }

        public static void ResetToDefaults() { ApplyPreset(GrassQualityPreset.High); }

        /// <summary>Deletes the saved values (next start uses the defaults).</summary>
        public static void ClearSaved()
        {
            foreach (var key in new[] { "GrassEnabled", "DetailsEnabled", "GrassQuality", "DetailsQuality", "GrassDistance", "DetailsDistance" })
                PlayerPrefs.DeleteKey(K + key);
        }

        // ------------------------------------------------------------------ Persistence
        static void Touch()
        {
            PlayerPrefs.SetInt(K + "GrassEnabled", s_grassEnabled ? 1 : 0);
            PlayerPrefs.SetInt(K + "DetailsEnabled", s_detailsEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(K + "GrassQuality", s_grassQuality);
            PlayerPrefs.SetFloat(K + "DetailsQuality", s_detailsQuality);
            PlayerPrefs.SetFloat(K + "GrassDistance", s_grassDistance);
            PlayerPrefs.SetFloat(K + "DetailsDistance", s_detailsDistance);
            s_needsFlush = true;
            Changed?.Invoke();
        }

        /// <summary>Writes pending changes to disk (called automatically on quit / focus loss).</summary>
        public static void Flush()
        {
            if (!s_needsFlush) return;
            PlayerPrefs.Save();
            s_needsFlush = false;
        }

        static void Load()
        {
            s_grassEnabled = PlayerPrefs.GetInt(K + "GrassEnabled", 1) != 0;
            s_detailsEnabled = PlayerPrefs.GetInt(K + "DetailsEnabled", 1) != 0;
            s_grassQuality = Mathf.Clamp(PlayerPrefs.GetFloat(K + "GrassQuality", 1f), MinQuality, 1f);
            s_detailsQuality = Mathf.Clamp(PlayerPrefs.GetFloat(K + "DetailsQuality", 1f), MinQuality, 1f);
            s_grassDistance = Mathf.Clamp(PlayerPrefs.GetFloat(K + "GrassDistance", 1f), MinDistance, MaxDistance);
            s_detailsDistance = Mathf.Clamp(PlayerPrefs.GetFloat(K + "DetailsDistance", 1f), MinDistance, MaxDistance);
        }

        static void OnFocus(bool focused) { if (!focused) Flush(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Load();
            s_needsFlush = false;
            Application.quitting -= Flush;
            Application.quitting += Flush;
            Application.focusChanged -= OnFocus;
            Application.focusChanged += OnFocus;
        }
    }
}
