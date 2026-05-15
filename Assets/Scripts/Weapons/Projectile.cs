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
            // TODO: Apply damage and elemental effects to enemies
            Debug.Log($"Hit {other.name} with {element}");
            Deactivate();
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
