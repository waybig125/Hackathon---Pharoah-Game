using UnityEngine;

namespace TheAlchemistsCrypt.Weapons
{
    public class Projectile : MonoBehaviour
    {
        public enum ElementType { Sulfur, Mercury, Salt }
        
        [Header("Projectile Settings")]
        public ElementType element;
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 5f;
        // [SerializeField] private float damage = 10f; // Unused warning fix

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
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
                // Robust headshot detection: check if hit collider name contains "head", "skull", or "brain"
                bool isHeadshot = other != null && (other.name.ToLower().Contains("head") || other.name.ToLower().Contains("skull") || other.name.ToLower().Contains("brain"));
                if (isHeadshot)
                {
                    Debug.Log($"[HEADSHOT] Hit mummy head bone {other.name}!");
                }

                switch (element)
                {
                    case ElementType.Sulfur:
                        float sulfurDamage = isHeadshot ? 10f : 5f;
                        zombie.TakeDamage(sulfurDamage);
                        ApplySulfurAOE(transform.position, zombie);
                        break;
                    case ElementType.Mercury:
                        float mercuryDamage = isHeadshot ? 5f : 2f;
                        zombie.TakeDamage(mercuryDamage);
                        zombie.ApplyMercurySlow(4f);
                        break;
                    case ElementType.Salt:
                        float saltDamage = isHeadshot ? 5f : 2f;
                        zombie.TakeDamage(saltDamage);
                        zombie.ApplySaltStun(3f);
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
