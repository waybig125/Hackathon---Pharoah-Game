using UnityEngine;

namespace TheAlchemistsCrypt.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Effects")]
        private float damageAlpha = 0f;
        private UnityEngine.UI.Image damageOverlay;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // The HUD is now managed entirely by MobileHUDButtons.cs
            // We just focus on health state and damage effects here.
            FindDamageOverlay();
        }

        private void FindDamageOverlay()
        {
            var overlay = GameObject.Find("MobileHUD_Root/DamageOverlay");
            if (overlay != null) damageOverlay = overlay.GetComponent<UnityEngine.UI.Image>();
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            
            // Trigger red flash on screen
            damageAlpha = 0.6f; 
            if (damageOverlay == null) FindDamageOverlay();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Update()
        {
            // Handle red flash fade out
            if (damageAlpha > 0)
            {
                damageAlpha -= Time.deltaTime * 1.5f;
                if (damageAlpha < 0) damageAlpha = 0;
                if (damageOverlay != null) 
                    damageOverlay.color = new Color(0.8f, 0.1f, 0.1f, damageAlpha);
            }
        }

        private void Die()
        {
            Debug.Log("Player Died! Respawning...");
            currentHealth = maxHealth;
            
            // Simple respawn logic
            transform.position = new Vector3(0, 2, -60);
            
            // Flash red on respawn
            damageAlpha = 0.4f;
        }
    }
}
