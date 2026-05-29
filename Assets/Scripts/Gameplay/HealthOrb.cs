using UnityEngine;
using TheAlchemistsCrypt.Player;

namespace TheAlchemistsCrypt.Gameplay
{
    public class HealthOrb : MonoBehaviour
    {
        [Header("Orb Settings")]
        [SerializeField] private float healAmount = 10f;
        [SerializeField] private float fadeStartSecond = 5f;
        [SerializeField] private float fadeEndSecond = 10f;

        private float lifetime = 0f;
        private Vector3 startPos;
        private Material orbMaterial;
        private Light orbLight;
        private Transform player;
        private bool hasShownTip = false;

        private void Start()
        {
            startPos = transform.position;

            // 1. Procedurally generate a gorgeous glowing 3D sphere
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(visual.GetComponent<Collider>()); // Avoid physics collision
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.45f;

            // Apply premium Egyptian-Crimson Alchemical glow material
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                // Create a clean instance of URP Lit material
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                
                orbMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
                Color crimson = new Color(0.9f, 0.1f, 0.2f, 1f);
                
                if (orbMaterial.HasProperty("_Color")) orbMaterial.SetColor("_Color", crimson);
                if (orbMaterial.HasProperty("_BaseColor")) orbMaterial.SetColor("_BaseColor", crimson);
                if (orbMaterial.HasProperty("_EmissionColor"))
                {
                    orbMaterial.SetColor("_EmissionColor", crimson * 3f);
                    orbMaterial.EnableKeyword("_EMISSION");
                }
                rend.material = orbMaterial;
            }

            // 2. Add dynamic point light to illuminate surroundings with a crimson pulse
            orbLight = gameObject.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.color = new Color(1.0f, 0.15f, 0.25f);
            orbLight.range = 4f;
            orbLight.intensity = 6f;

            // 3. Add dynamic SphereCollider for collections
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.2f;

            // Add a continuous ambient particle system emitting soft crimson sparkles
            var psGo = new GameObject("AmbientSparks");
            psGo.transform.SetParent(transform, false);
            psGo.transform.localPosition = Vector3.zero;
            
            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psMain = ps.main;
            psMain.duration = 1f;
            psMain.loop = true;
            psMain.startLifetime = 1.2f;
            psMain.startSpeed = 0.5f;
            psMain.startSize = 0.12f;
            psMain.startColor = new Color(0.9f, 0.15f, 0.3f, 0.8f);
            psMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            var psEmission = ps.emission;
            psEmission.rateOverTime = 20f;
            
            var psShape = ps.shape;
            psShape.shapeType = ParticleSystemShapeType.Sphere;
            psShape.radius = 0.5f;
            
            var psRend = psGo.GetComponent<ParticleSystemRenderer>();
            if (psRend != null)
            {
                psRend.sharedMaterial = CreateParticleMaterial(new Color(0.9f, 0.15f, 0.3f));
            }

            FindPlayer();
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.transform;
            }
        }

        private bool isMagnetized = false;
        private float magnetSpeed = 0f;

        private void Update()
        {
            lifetime += Time.deltaTime;

            // Spin the orb elegantly
            transform.Rotate(Vector3.up, 140f * Time.deltaTime);

            // Hover up and down beautifully (only if not magnetized)
            if (!isMagnetized)
            {
                transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 2.8f) * 0.18f;
            }

            // Find player if null
            if (player == null) FindPlayer();

            // Track proximity and tell the player about the orb
            if (player != null && !hasShownTip && !isMagnetized)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= 5f)
                {
                    hasShownTip = true;
                    // Inform the player without overlaps
                    if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
                    {
                        TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowOrbTooltip("Restorative Essence Orb! Step closer to absorb +10 Health.");
                    }
                }
            }

            // Trigger homing magnetism if player is within 6m
            if (player != null && !isMagnetized)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= 6f)
                {
                    isMagnetized = true;
                    if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
                    {
                        TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.HideOrbTooltip();
                    }
                }
            }

            // Magnetism logic
            if (isMagnetized)
            {
                Transform targetCam = (Camera.main != null) ? Camera.main.transform : player;
                if (targetCam != null)
                {
                    magnetSpeed += 25f * Time.deltaTime; // Accelerate towards camera
                    transform.position = Vector3.MoveTowards(transform.position, targetCam.position, magnetSpeed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, targetCam.position) < 0.4f)
                    {
                        CollectOrb();
                    }
                }
            }

            // Fade out logic from 5 to 10 seconds (only if not magnetized)
            if (lifetime >= fadeStartSecond && !isMagnetized)
            {
                float progress = (lifetime - fadeStartSecond) / (fadeEndSecond - fadeStartSecond);
                float alpha = Mathf.Clamp01(1f - progress);

                if (orbMaterial != null)
                {
                    Color c = new Color(0.9f, 0.1f, 0.2f, alpha);
                    if (orbMaterial.HasProperty("_Color")) orbMaterial.SetColor("_Color", c);
                    if (orbMaterial.HasProperty("_BaseColor")) orbMaterial.SetColor("_BaseColor", c);
                    if (orbMaterial.HasProperty("_EmissionColor"))
                    {
                        orbMaterial.SetColor("_EmissionColor", c * (3f * alpha));
                    }
                }

                if (orbLight != null)
                {
                    orbLight.intensity = 6f * alpha;
                    orbLight.range = 4f * alpha;
                }

                if (lifetime >= fadeEndSecond)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void CollectOrb()
        {
            // Heal the player!
            var health = GameObject.FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                health.Heal(healAmount);
            }

            // Hide the tooltip on collect
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
            {
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.HideOrbTooltip();
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.SplashHealthParticles();
            }

            // Play collection SFX
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 0.8f);

            Debug.Log("Essence Orb Collected! Restored 10 health.");
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<InfimaGames.LowPolyShooterPack.Character>() != null || other.GetComponentInParent<InfimaGames.LowPolyShooterPack.Character>() != null)
            {
                isMagnetized = true;
            }
        }

        private Material CreateParticleMaterial(Color baseColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            
            // Set particles transparent in URP to prevent solid box rendering
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // Create a gorgeous soft anti-aliased circular brush texture
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f;
                    float dy = y - 7.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - (dist / 7.5f));
                    alpha = Mathf.Pow(alpha, 2.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }
    }
}
