using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        // Mystic Egyptian Orange Fog
        public Color fogColor = new Color(0.45f, 0.25f, 0.05f, 1f);
        public float fogDensity = 0.012f; 
        public FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Lighting Settings")]
        public Color ambientLight = new Color(0.25f, 0.22f, 0.28f); 
        public Color sunColor = new Color(0.8f, 0.6f, 0.4f, 1.0f); 
        public float sunIntensity = 1.0f; 

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

            // Force cameras to Solid Color yellowish clear to fix Pink issue
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLight;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                // Fix Unity 6 API
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
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
