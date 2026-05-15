using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        // Darker, more dusky Egyptian Night color (Dusty Blue/Gray/Sand)
        public Color fogColor = new Color(0.12f, 0.11f, 0.15f, 1f);
        public float fogDensity = 0.02f; 
        public FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Lighting Settings")]
        public Color ambientLight = new Color(0.15f, 0.15f, 0.2f); // Darker blueish ambient
        public Color sunColor = new Color(0.6f, 0.5f, 0.4f, 1.0f); // Dimmer, warmer "moonlight"
        public float sunIntensity = 0.8f; 

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
