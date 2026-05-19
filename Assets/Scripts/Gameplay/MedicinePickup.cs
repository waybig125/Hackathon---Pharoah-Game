using UnityEngine;

namespace TheAlchemistsCrypt.Gameplay
{
    public class MedicinePickup : MonoBehaviour
    {
        [Header("Healing Settings")]
        public float healAmount = 10f;

        [Header("Movement")]
        public float rotationSpeed = 60f;
        public float hoverAmplitude = 0.2f;
        public float hoverFrequency = 1.5f;

        private Vector3 startPos;
        private Light glowLight;
        private GameObject crystalVisual;

        private void Start()
        {
            startPos = transform.position;

            // Create a gorgeous glowing alchemical sphere/orb
            crystalVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = crystalVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            crystalVisual.transform.SetParent(transform, false);
            crystalVisual.transform.localPosition = Vector3.zero;
            crystalVisual.transform.localScale = Vector3.one * 0.45f;

            MeshRenderer renderer = crystalVisual.GetComponent<MeshRenderer>();

            // Setup custom glowing alchemical material (Emerald/Jade)
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material mat = new Material(litShader);
            mat.SetColor("_BaseColor", new Color(0.1f, 0.9f, 0.3f, 1f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.1f, 0.9f, 0.3f, 1f));
            mat.SetColor("_EmissionColor", new Color(0.05f, 0.6f, 0.15f, 1f) * 3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.9f);
            renderer.material = mat;

            // Add clean procedural collider
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.8f, 1.2f, 0.8f);

            // Add smooth green alchemical light glow
            var ltGo = new GameObject("GlowLight");
            ltGo.transform.SetParent(transform, false);
            ltGo.transform.localPosition = Vector3.zero;
            glowLight = ltGo.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.1f, 0.9f, 0.2f);
            glowLight.intensity = 6.0f;
            glowLight.range = 5.0f;
            glowLight.shadows = LightShadows.None;
        }

        private void Update()
        {
            // Floating & rotating motion
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
            float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(startPos.x, startPos.y + hoverOffset, startPos.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect player
            var player = other.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (player == null) player = other.GetComponentInParent<TheAlchemistsCrypt.Player.PlayerHealth>();

            if (player != null)
            {
                player.Heal(healAmount);

                // Play custom procedural emerald spark particle burst
                var sparkGo = new GameObject("SparkBurst");
                sparkGo.transform.position = transform.position;
                var system = sparkGo.AddComponent<ParticleSystem>();
                
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = system.main;
                main.startColor = new Color(0.2f, 1f, 0.4f);
                main.startSize = 0.15f;
                main.startSpeed = 3f;
                main.duration = 0.5f;
                main.loop = false;
                
                var emission = system.emission;
                emission.rateOverTime = 0f;
                emission.burstCount = 1;
                emission.SetBurst(0, new ParticleSystem.Burst(0f, 25));

                var renderer = sparkGo.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateParticleMaterial(new Color(0.2f, 1f, 0.4f));
                }

                system.Play();
                Destroy(sparkGo, 1.0f);

                // Destroy the pickup
                Destroy(gameObject);
            }
        }

        private Material CreateParticleMaterial(Color baseColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            
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
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }
    }
}
