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
                switch (element)
                {
                    case ElementType.Sulfur:
                        zombie.TakeDamage(5f);
                        ApplySulfurAOE(transform.position, zombie);
                        break;
                    case ElementType.Mercury:
                        zombie.TakeDamage(2f);
                        zombie.ApplyMercurySlow(4f);
                        break;
                    case ElementType.Salt:
                        zombie.TakeDamage(2f);
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
