using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        // Vibrant Green + Gray Mix
        public Color fogColor = new Color(0.35f, 0.65f, 0.40f, 1f);
        public float fogDensity = 0.015f; // Increased for thick Egyptian fog
        public FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Lighting Settings")]
        public Color ambientLight = new Color(0.85f, 0.82f, 0.75f);
        public Color sunColor = new Color(1.0f, 0.95f, 0.8f, 1.0f);
        public float sunIntensity = 3.0f;

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

            // Apply solid color to camera to blend sky with fog perfectly
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = fogColor;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLight;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional && l.name != "TopDownClarityLight")
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
                // Move sun a bit above (higher angle)
                sun.transform.rotation = Quaternion.Euler(75f, -30f, 0f);
                RenderSettings.sun = sun;
            }
        }
    }
}
