using UnityEngine;
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

        private void Start()
        {
            // Destroy after lifetime to prevent memory leaks
            Destroy(gameObject, lifetime);

            // Procedurally create a beautiful glowing alchemical project mesh
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.4f;

            // Remove sphere collider from child visual to avoid double collision or self-hits
            var sphereCollider = visual.GetComponent<SphereCollider>();
            if (sphereCollider != null) Destroy(sphereCollider);

            // Give the projectile a brilliant golden-amber glowing lit material
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.95f, 0.6f, 0.1f, 1f); // Warm alchemical amber
                mat.SetColor("_EmissionColor", new Color(0.95f, 0.5f, 0.05f) * 4f);
                mat.EnableKeyword("_EMISSION");
                renderer.sharedMaterial = mat;
            }

            // Add a point light to glow dynamically
            var lightGo = new GameObject("LightGlow");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.zero;
            var pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.95f, 0.55f, 0.1f);
            pointLight.intensity = 8.0f;
            pointLight.range = 4.0f;
            pointLight.shadows = LightShadows.None;

            // Add a beautiful Sand/Amber TrailRenderer
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(transform, false);
            trailGo.transform.localPosition = Vector3.zero;
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0.0f;
            
            // Set trail material and colors
            var trailMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            trailMat.color = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            trailMat.SetColor("_EmissionColor", new Color(0.9f, 0.6f, 0.1f) * 2f);
            trailMat.EnableKeyword("_EMISSION");
            trail.sharedMaterial = trailMat;

            // Add trigger collider for the projectile itself
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.3f;

            // Add Rigidbody so triggers receive collision messages reliably
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private void Update()
        {
            // Move strictly in a straight line
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
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

            // Destroy projectile
            Destroy(gameObject);
        }

        private void SpawnSplashEffect()
        {
            var splashGo = new GameObject("ProjectileSplash");
            splashGo.transform.position = transform.position;
            var system = splashGo.AddComponent<ParticleSystem>();

            // Stop before modifying parameters to avoid Unity 6 duration warnings
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.startColor = new Color(0.95f, 0.65f, 0.15f);
            main.startSize = 0.12f;
            main.startSpeed = 4f;
            main.duration = 0.4f;
            main.loop = false;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 15));

            // Set URP-compatible particle material to prevent pink boxes
            var renderer = splashGo.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateParticleMaterial(new Color(0.95f, 0.65f, 0.15f));
            }

            system.Play();
            Destroy(splashGo, 0.8f);
        }

        private Material CreateParticleMaterial(Color baseColor)
        {
            Shader uShared = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (uShared == null) uShared = Shader.Find("Standard");
            
            Material mat = new Material(uShared);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);
            
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
                    alpha = Mathf.Pow(alpha, 2.5f); // Smooth falloff gradient
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            return mat;
        }
    }
}