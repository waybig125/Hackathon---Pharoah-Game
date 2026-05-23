using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        [Header("Fog Settings")]
        public bool enableFog = true;
        // Dusty, bright sand reflection
        public Color fogColor = new Color(0.95f, 0.85f, 0.75f, 1f);
        public float fogStartDistance = 150f;
        public float fogEndDistance = 1200f;
        public FogMode fogMode = FogMode.Linear;

        [Header("Lighting Settings")]
        // Brighter, crisper sky
        public Color ambientSkyColor = new Color(0.45f, 0.65f, 0.85f); 
        // Dusty, bright sand reflection
        public Color ambientEquatorColor = new Color(0.90f, 0.85f, 0.75f); 
        // Distinct deep blue/purple for the stark shadows seen in the image
        public Color ambientGroundColor = new Color(0.35f, 0.40f, 0.55f); 
        // Whiter, crisper sunlight
        public Color sunColor = new Color(1.0f, 0.95f, 0.90f, 1.0f); 
        public float sunIntensity = 2.0f; 

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
                    var urpCamData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    if (urpCamData != null)
                    {
                        urpCamData.backgroundType = UnityEngine.Rendering.Universal.CameraBackgroundType.Skybox;
                    }
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
                // Changed to a lower sunset angle so sunlight falls on the houses
                sun.transform.rotation = Quaternion.Euler(20f, -60f, 0f); 
                RenderSettings.sun = sun;
            }
        }
    }
}
