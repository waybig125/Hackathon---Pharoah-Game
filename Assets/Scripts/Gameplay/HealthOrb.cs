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

        private void Update()
        {
            lifetime += Time.deltaTime;

            // Spin the orb elegantly
            transform.Rotate(Vector3.up, 140f * Time.deltaTime);

            // Hover up and down beautifully
            transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 2.8f) * 0.18f;

            // Find player if null
            if (player == null) FindPlayer();

            // Track proximity and tell the player about the orb
            if (player != null && !hasShownTip)
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

            // Fade out logic from 5 to 10 seconds
            if (lifetime >= fadeStartSecond)
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<InfimaGames.LowPolyShooterPack.Character>() != null || other.GetComponentInParent<InfimaGames.LowPolyShooterPack.Character>() != null)
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
                }

                // Play a brief high-quality sound/particle effect if desired or just log and destroy
                Debug.Log("Essence Orb Collected! Restored 10 health.");
                Destroy(gameObject);
            }
        }
    }
}
