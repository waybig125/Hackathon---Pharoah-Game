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
                }
            }

            // Dynamically assign SkyGradientBox material at runtime
            Material skyMat = Resources.Load<Material>("Materials/SkyGradientBox");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }
            else
            {
                Shader gradientSkyShader = Shader.Find("Custom/SkyboxGradient");
                if (gradientSkyShader != null)
                {
                    skyMat = new Material(gradientSkyShader);
                    skyMat.SetColor("_ColorBottom", new Color(0.98f, 0.62f, 0.42f));
                    skyMat.SetColor("_ColorMiddle1", new Color(0.85f, 0.44f, 0.60f));
                    skyMat.SetColor("_ColorMiddle2", new Color(0.24f, 0.44f, 0.74f));
                    skyMat.SetColor("_ColorTop", new Color(0.06f, 0.12f, 0.35f));
                    RenderSettings.skybox = skyMat;
                }
                else
                {
                    Shader proceduralSkyShader = Shader.Find("Skybox/Procedural");
                    if (proceduralSkyShader != null)
                    {
                        
                        skyMat = new Material(proceduralSkyShader);
                        skyMat.SetColor("_SkyTint", new Color(0.06f, 0.12f, 0.35f));
                        skyMat.SetColor("_GroundColor", new Color(0.98f, 0.62f, 0.42f));
                        skyMat.SetFloat("_AtmosphereThickness", 1.3f);
                        skyMat.SetFloat("_Exposure", 1.3f);
                        RenderSettings.skybox = skyMat;
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
                sun.enabled = true;
                sun.gameObject.SetActive(true);
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                // Changed to a lower sunset angle so sunlight falls on the houses
                sun.transform.rotation = Quaternion.Euler(20f, -60f, 0f); 
                RenderSettings.sun = sun;
            }
        }
    }
}
