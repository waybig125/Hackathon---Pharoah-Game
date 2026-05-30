using UnityEngine;

namespace TheAlchemistsCrypt.Weapons
{
    public class Projectile : MonoBehaviour
    {
        public enum ElementType { Sulfur, Mercury, Salt }
        
        [Header("Projectile Settings")]
        public ElementType element;
        [SerializeField] private float speed = 250f; // Snappy tracer speed
        [SerializeField] private float lifetime = 1.5f;

        private Rigidbody rb;
        private TrailRenderer trail;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            // Setup the 'Tracer' visual
            trail = GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                // High-quality tracer configuration
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.08f), new Keyframe(1f, 0.01f));
                trail.time = 0.08f; // Extremely short trail for high speed
                trail.minVertexDistance = 0.05f;
                trail.emitting = true;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                
                // Set color based on element
                Gradient gradient = new Gradient();
                Color col = GetElementColor();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(col * 2.5f, 0.0f), new GradientColorKey(col * 0.8f, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trail.colorGradient = gradient;

                // Ensure the material is additive/unlit for a 'glow' look
                if (trail.sharedMaterial != null)
                {
                    trail.sharedMaterial.EnableKeyword("_EMISSION");
                    trail.sharedMaterial.SetColor("_EmissionColor", col * 3.0f);
                }
            }

            // Hide the physical bullet mesh (tracers don't need a visible lead object)
            if (meshRenderer != null) meshRenderer.enabled = false;

            var colCom = GetComponent<Collider>();
            if (colCom != null) colCom.isTrigger = true;
        }

        private Color GetElementColor()
        {
            switch (element)
            {
                case ElementType.Sulfur: return new Color(1f, 0.5f, 0.05f); // Fiery orange
                case ElementType.Mercury: return new Color(0.1f, 0.9f, 1f); // Electric cyan
                case ElementType.Salt: return new Color(0.9f, 0.9f, 1.0f); // Bright white-blue
                default: return Color.white;
            }
        }

        private void OnEnable()
        {
            // Clear trail history to prevent 'jumping' lines when pooled
            if (trail != null) trail.Clear();
            Invoke(nameof(Deactivate), lifetime);
        }

        private void OnDisable()
        {
            CancelInvoke();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void FixedUpdate()
        {
            if (rb != null)
                rb.linearVelocity = transform.forward * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Apply damage and elemental effects to enemies
            Debug.Log($"Hit {other.name} with {element}");
            var zombie = other.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
            if (zombie == null) zombie = other.GetComponentInParent<TheAlchemistsCrypt.AI.ZombieAI>();
            if (zombie != null)
            {
                // Hybrid headshot detection: check skeletal bone name or relative vertical height (relativeY >= 1.4f)
                float relativeY = transform.position.y - zombie.transform.position.y;
                bool isHeadshot = (other != null && (other.name.ToLower().Contains("head") || other.name.ToLower().Contains("skull") || other.name.ToLower().Contains("brain"))) 
                                  || (relativeY >= 1.4f);
                if (isHeadshot)
                {
                    Debug.Log($"[HEADSHOT] Hit mummy head bone {other.name} (relativeY: {relativeY:F2})!");
                }

                var pharaoh = zombie as TheAlchemistsCrypt.AI.PharaohAI;

                switch (element)
                {
                    case ElementType.Sulfur:
                        float sulfurDamage = isHeadshot ? 10f : 5f;
                        zombie.TakeDamage(sulfurDamage);
                        if (pharaoh != null) pharaoh.ApplyBossReaction("Sulfur");
                        else zombie.ApplyAlchemicalElement(TheAlchemistsCrypt.AI.ZombieAI.AlchemicalResidue.Sulfur);
                        ApplySulfurAOE(transform.position, zombie);
                        break;
                    case ElementType.Mercury:
                        float mercuryDamage = isHeadshot ? 5f : 2f;
                        zombie.TakeDamage(mercuryDamage);
                        if (pharaoh != null) pharaoh.ApplyBossReaction("Mercury");
                        else
                        {
                            zombie.ApplyMercurySlow(4f);
                            zombie.ApplyAlchemicalElement(TheAlchemistsCrypt.AI.ZombieAI.AlchemicalResidue.Mercury);
                        }
                        break;
                    case ElementType.Salt:
                        float saltDamage = isHeadshot ? 5f : 2f;
                        zombie.TakeDamage(saltDamage);
                        if (pharaoh != null) pharaoh.ApplyBossReaction("Salt");
                        else
                        {
                            zombie.ApplySaltStun(3f);
                            zombie.ApplyAlchemicalElement(TheAlchemistsCrypt.AI.ZombieAI.AlchemicalResidue.Salt);
                        }
                        break;
                }
            }
            Deactivate();
        }

        private void ApplySulfurAOE(Vector3 position, TheAlchemistsCrypt.AI.ZombieAI directHitZombie = null)
        {
            Collider[] colliders = Physics.OverlapSphere(position, 4.0f);
            foreach (Collider c in colliders)
            {
                var z = c.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
                if (z == null) z = c.GetComponentInParent<TheAlchemistsCrypt.AI.ZombieAI>();
                if (z != null && z != directHitZombie)
                {
                    // Calculate falloff damage based on distance
                    float dist = Vector3.Distance(position, z.transform.position);
                    float damage = Mathf.Lerp(5f, 1f, dist / 4.0f);
                    z.TakeDamage(damage);
                    
                    // Knockback if Rigidbody exists
                    var rb = z.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(300f, position, 4.0f);
                    }
                }
            }
            
            // Create a small procedural explosion light
            GameObject exp = new GameObject("SulfurExplosionLight");
            exp.transform.position = position;
            Light l = exp.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.4f, 0f);
            l.intensity = 15f;
            l.range = 6f;
            Destroy(exp, 0.4f);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
