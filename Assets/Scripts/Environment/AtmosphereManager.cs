using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        public Color fogColor = new Color(0.8f, 0.7f, 0.5f, 1.0f);
        public float fogDensity = 0.02f;
        public FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Lighting Settings")]
        public Color ambientLight = new Color(0.9f, 0.8f, 0.7f, 1.0f);
        public Color sunColor = new Color(1.0f, 0.85f, 0.7f, 1.0f);
        public float sunIntensity = 1.2f;

        private void Start()
        {
            ApplyAtmosphere();
        }

        public void ApplyAtmosphere()
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogMode = fogMode;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLight;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sun = l;
                        break;
                    }
                }
            }

            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                RenderSettings.sun = sun;
            }
        }
    }
}
