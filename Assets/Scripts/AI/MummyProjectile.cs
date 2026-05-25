using UnityEngine;
using System.Collections.Generic;
using TheAlchemistsCrypt.Player;

namespace TheAlchemistsCrypt.AI
{
    public class MummyProjectile : MonoBehaviour
    {
        public float speed = 15f;
        public float damage = 10f;
        public float lifetime = 5f;
        
        [HideInInspector]
        public Vector3 direction = Vector3.forward;

        private float lifetimeTimer = 5f;
        private bool isInitialized = false;

        // Static object pools to completely eliminate Instantiate/Destroy allocations during gameplay
        private static List<MummyProjectile> projectilePool = new List<MummyProjectile>();
        private static List<ParticleSystem> splashPool = new List<ParticleSystem>();

        private static GameObject projectileTemplate;
        private static GameObject splashTemplate;

        private static Material sharedParticleMaterial;
        private static Texture2D sharedParticleTexture;

        private void Start()
        {
            if (isInitialized) return;
            InitializeInstance();
        }

        public void InitializeInstance()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Procedurally create a beautiful glowing alchemical project mesh
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.4f;

            // Remove sphere collider from child visual to avoid double collision or self-hits
            var sphereCollider = visual.GetComponent<SphereCollider>();
            if (sphereCollider != null) DestroyImmediate(sphereCollider);

            // Give the projectile a brilliant golden-amber glowing lit material
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");
                Material mat = new Material(litShader);
                mat.color = new Color(0.95f, 0.6f, 0.1f, 1f); // Warm alchemical amber
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.95f, 0.6f, 0.1f, 1f));
                mat.SetColor("_EmissionColor", new Color(0.95f, 0.5f, 0.05f) * 4f);
                mat.EnableKeyword("_EMISSION");
                renderer.sharedMaterial = mat;
            }

            // Add a point light to glow dynamically
            // PERFORMANCE: Real-time point lights are extremely expensive on mobile GPUs.
            // Each active projectile was adding a full pixel light — disabled on Android/iOS.
            // The emissive material already provides visual glow without GPU cost.
#if !UNITY_ANDROID && !UNITY_IOS
            var lightGo = new GameObject("LightGlow");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.zero;
            var pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.95f, 0.55f, 0.1f);
            pointLight.intensity = 8.0f;
            pointLight.range = 4.0f;
            pointLight.shadows = LightShadows.None;
#endif

            // Add a beautiful Sand/Amber TrailRenderer
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(transform, false);
            trailGo.transform.localPosition = Vector3.zero;
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0.0f;
            
            // Set trail material and colors
            Shader trailShader = Shader.Find("Universal Render Pipeline/Lit");
            if (trailShader == null) trailShader = Shader.Find("Standard");
            var trailMat = new Material(trailShader);
            trailMat.color = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            if (trailMat.HasProperty("_BaseColor")) trailMat.SetColor("_BaseColor", new Color(0.9f, 0.7f, 0.2f, 0.6f));
            trailMat.SetColor("_EmissionColor", new Color(0.9f, 0.6f, 0.1f) * 2f);
            trailMat.EnableKeyword("_EMISSION");
            trail.sharedMaterial = trailMat;

            // Add trigger collider for the projectile itself
            var col = gameObject.GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.3f;

            // Add Rigidbody so triggers receive collision messages reliably
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private static void InitializeTemplates()
        {
            if (projectileTemplate != null) return;

            // Create projectile template
            projectileTemplate = new GameObject("MummyProjectileTemplate");
            projectileTemplate.SetActive(false);
            DontDestroyOnLoad(projectileTemplate);
            
            var proj = projectileTemplate.AddComponent<MummyProjectile>();
            proj.InitializeInstance();

            // Create splash template
            splashTemplate = new GameObject("ProjectileSplashTemplate");
            splashTemplate.SetActive(false);
            DontDestroyOnLoad(splashTemplate);
            
            var system = splashTemplate.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.startColor = new Color(0.95f, 0.65f, 0.15f);
            main.startSize = 0.12f;
            main.startSpeed = 4f;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 15));

            var partRenderer = splashTemplate.GetComponent<ParticleSystemRenderer>();
            if (partRenderer != null)
            {
                partRenderer.sharedMaterial = CreateSharedParticleMaterial(new Color(0.95f, 0.65f, 0.15f));
            }
        }

        private static Material CreateSharedParticleMaterial(Color baseColor)
        {
            if (sharedParticleMaterial != null) return sharedParticleMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            sharedParticleMaterial = new Material(shader);
            sharedParticleMaterial.SetColor("_Color", baseColor);
            if (sharedParticleMaterial.HasProperty("_BaseColor")) sharedParticleMaterial.SetColor("_BaseColor", baseColor);
            
            // Create a gorgeous soft anti-aliased circular brush texture once
            sharedParticleTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f;
                    float dy = y - 7.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - (dist / 7.5f));
                    alpha = Mathf.Pow(alpha, 2.5f); // Smooth falloff gradient
                    sharedParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            sharedParticleTexture.Apply();
            sharedParticleMaterial.mainTexture = sharedParticleTexture;
            if (sharedParticleMaterial.HasProperty("_BaseMap")) sharedParticleMaterial.SetTexture("_BaseMap", sharedParticleTexture);
            if (sharedParticleMaterial.HasProperty("_MainTex")) sharedParticleMaterial.SetTexture("_MainTex", sharedParticleTexture);
            return sharedParticleMaterial;
        }

        public static MummyProjectile Spawn(Vector3 position, Vector3 direction, float speed, float damage)
        {
            InitializeTemplates();

            // Find an inactive projectile in the pool
            MummyProjectile proj = null;
            for (int i = projectilePool.Count - 1; i >= 0; i--)
            {
                if (projectilePool[i] == null)
                {
                    projectilePool.RemoveAt(i);
                    continue;
                }
                if (!projectilePool[i].gameObject.activeSelf)
                {
                    proj = projectilePool[i];
                    break;
                }
            }

            // Create a new instance if none found in pool
            if (proj == null)
            {
                GameObject newObj = Instantiate(projectileTemplate);
                proj = newObj.GetComponent<MummyProjectile>();
                proj.enabled = true; // Enable Update loop on instance
                projectilePool.Add(proj);
            }

            // Set variables
            proj.transform.position = position;
            proj.direction = direction;
            proj.speed = speed;
            proj.damage = damage;
            proj.lifetimeTimer = proj.lifetime; // Reset custom lifetime timer

            // Clear the trail renderer history so it doesn't draw from previous active location
            var trail = proj.GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }

            proj.gameObject.SetActive(true);
            return proj;
        }

        private void Update()
        {
            // Move strictly in a straight line
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            // Custom lifetime tracking for pooled object
            lifetimeTimer -= Time.deltaTime;
            if (lifetimeTimer <= 0f)
            {
                Deactivate();
            }
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignore other triggers or projectiles or the shooting mummy itself
            if (other.isTrigger || other.GetComponent<ZombieAI>() != null || other.GetComponent<MummyProjectile>() != null)
                return;

            // Deal damage if player
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            // Spawn beautiful alchemical amber particle splash
            SpawnSplashEffect();

            // Return to pool (instead of Destroy)
            Deactivate();
        }

        private void SpawnSplashEffect()
        {
            InitializeTemplates();

            ParticleSystem splash = null;
            for (int i = splashPool.Count - 1; i >= 0; i--)
            {
                if (splashPool[i] == null)
                {
                    splashPool.RemoveAt(i);
                    continue;
                }
                if (!splashPool[i].gameObject.activeSelf)
                {
                    splash = splashPool[i];
                    break;
                }
            }

            if (splash == null)
            {
                GameObject newObj = Instantiate(splashTemplate);
                splash = newObj.GetComponent<ParticleSystem>();
                
                // Add helper component to return to pool automatically
                newObj.AddComponent<ParticlePoolHelper>();
                splashPool.Add(splash);
            }

            splash.transform.position = transform.position;
            splash.gameObject.SetActive(true);
            splash.Play();
        }
    }

    // Helper component to return particles to pool automatically when finished playing
    public class ParticlePoolHelper : MonoBehaviour
    {
        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (ps != null && !ps.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }
    }
}