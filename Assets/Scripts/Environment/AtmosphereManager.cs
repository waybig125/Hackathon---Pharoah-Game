using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        // Warm Sunset Peach Horizon
        public Color fogColor = new Color(0.92f, 0.74f, 0.52f, 1f);
        public float fogStartDistance = 150f;
        public float fogEndDistance = 1200f;
        public FogMode fogMode = FogMode.Linear;

        [Header("Lighting Settings")]
        public Color ambientSkyColor = new Color(0.35f, 0.40f, 0.60f); // Cool purple-blue shadows
        public Color ambientEquatorColor = new Color(0.85f, 0.68f, 0.52f); // Warm peach transition
        public Color ambientGroundColor = new Color(0.25f, 0.20f, 0.22f); // Cool dark ground
        public Color sunColor = new Color(1.0f, 0.86f, 0.72f, 1.0f); // Warm sunset light
        public float sunIntensity = 1.8f; 

        private void Start()
        {
            ApplyAtmosphere();
        }

        public void ApplyAtmosphere()
        {
            // Set camera clear flags to Skybox to show procedural skybox
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (cam != null && cam.name != "TopDownClarityCamera" && cam.name != "MinimapCamera")
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;

            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;

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
                // Sunset low angle matching the editor setup
                sun.transform.rotation = Quaternion.Euler(25f, 220f, 0f);
                RenderSettings.sun = sun;
            }
        }
    }
}
