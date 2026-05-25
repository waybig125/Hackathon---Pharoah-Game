using UnityEngine;

namespace TheAlchemistsCrypt.Core
{
    /// <summary>
    /// Applies mobile-specific performance settings at scene start.
    /// Attach this to any persistent GameObject in your first/main scene.
    ///
    /// Key wins on mobile:
    ///   • Shadow distance: 150 → 35 (massive GPU save)
    ///   • vSync off + targetFrameRate 30 (smooth, controlled pacing)
    ///   • 25 Hz physics (from 50 Hz) — enough for this game's AI/physics
    ///   • LOD bias 0.5 — more aggressive LOD switching on small screens
    ///   • Texture streaming — avoids loading all mip levels into VRAM
    /// </summary>
    public class MobilePerformanceOptimizer : MonoBehaviour
    {
        [Header("Frame Rate")]
        [Tooltip("Target FPS. 30 is ideal for mobile battery life and thermals.")]
        [SerializeField] private int targetFrameRate = 30;

        [Header("Shadows")]
        [Tooltip("Shadow render distance in metres. Default Unity is 150. 35 is plenty for this game.")]
        [SerializeField] private float shadowDistance = 35f;

        [Tooltip("0 = no cascades (cheapest). 1–4 = higher quality but heavier GPU load.")]
        [SerializeField] [Range(0, 4)] private int shadowCascades = 0;

        [Header("Lighting")]
        [Tooltip("Max number of real-time pixel lights per object. 1 is enough for directional sun.")]
        [SerializeField] [Range(0, 4)] private int pixelLightCount = 1;

        [Tooltip("Disable real-time reflection probes on mobile — they are very expensive.")]
        [SerializeField] private bool disableRealtimeReflectionProbes = true;

        [Header("LOD & Textures")]
        [Tooltip("Lower = more aggressive LOD transitions. 0.5 is good for mobile.")]
        [SerializeField] [Range(0.1f, 2f)] private float lodBias = 0.5f;

        [Tooltip("Stream mipmap levels based on camera distance. Saves VRAM.")]
        [SerializeField] private bool enableMipmapStreaming = true;

        [Tooltip("VRAM budget for mipmap streaming in MB.")]
        [SerializeField] private float mipmapMemoryBudgetMB = 256f;

        [Header("Physics")]
        [Tooltip("Physics update frequency. 0.04 = 25Hz (vs default 50Hz). Fine for AI/combat.")]
        [SerializeField] private float fixedTimestep = 0.04f;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            ApplyMobileSettings();
#else
            // On desktop/editor: still apply FPS cap but leave quality alone
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount  = 0;
#endif
            // Never let the screen sleep during gameplay
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void ApplyMobileSettings()
        {
            // ── Frame rate ──────────────────────────────────────────────────
            QualitySettings.vSyncCount  = 0;             // vSync blocks targetFrameRate
            Application.targetFrameRate = targetFrameRate;

            // ── Shadows ─────────────────────────────────────────────────────
            QualitySettings.shadowDistance    = shadowDistance;
            QualitySettings.shadowCascades    = shadowCascades;
            QualitySettings.shadows           = ShadowQuality.HardOnly; // Soft shadows are 2× cost
            QualitySettings.shadowResolution  = ShadowResolution.Low;

            // ── Lighting ────────────────────────────────────────────────────
            QualitySettings.pixelLightCount           = pixelLightCount;
            QualitySettings.realtimeReflectionProbes  = !disableRealtimeReflectionProbes;

            // ── LOD & textures ──────────────────────────────────────────────
            QualitySettings.lodBias                     = lodBias;
            QualitySettings.maximumLODLevel             = 0; // Allow all LOD levels
            QualitySettings.streamingMipmapsActive      = enableMipmapStreaming;
            QualitySettings.streamingMipmapsMemoryBudget = mipmapMemoryBudgetMB;
            QualitySettings.globalTextureMipmapLimit    = 0; // Full resolution (streaming handles it)

            // ── Anisotropic filtering ────────────────────────────────────────
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable; // Cheap, minor visual impact on mobile

            // ── Physics ─────────────────────────────────────────────────────
            Time.fixedDeltaTime = fixedTimestep;    // 25 Hz physics
            Physics.sleepThreshold        = 0.005f; // Rigidbodies sleep earlier → fewer physics steps
            Physics.defaultContactOffset  = 0.02f;

            Debug.Log($"[MobilePerformanceOptimizer] Applied mobile settings:" +
                      $" FPS={targetFrameRate}, ShadowDist={shadowDistance}m, Cascades={shadowCascades}," +
                      $" PixelLights={pixelLightCount}, PhysHz={1f/fixedTimestep:F0}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            targetFrameRate      = Mathf.Clamp(targetFrameRate, 15, 60);
            shadowDistance       = Mathf.Clamp(shadowDistance, 5f, 200f);
            fixedTimestep        = Mathf.Clamp(fixedTimestep, 0.016f, 0.1f);
            mipmapMemoryBudgetMB = Mathf.Clamp(mipmapMemoryBudgetMB, 64f, 1024f);
        }
#endif
    }
}
