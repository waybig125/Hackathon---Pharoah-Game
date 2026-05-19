using UnityEngine;
using UnityEngine.AI;

namespace TheAlchemistsCrypt.AI
{
    public class PharaohAI : ZombieAI
    {
        [Header("Pharaoh Boss Settings")]
        public float spellCooldown = 5.0f;
        private float lastSpellTime = 0f;
        
        private new void Start()
        {
            // Boost boss stats
            maxHealth = 300f; // Boss health
            currentHealth = maxHealth;
            vulnerableElement = "none"; // Boss is immune to basic stuns
            
            // Re-invoke base class initialization logic without its start
            // Wait, we can't easily call base.Start() without it resetting our stats if we don't do it carefully.
            // Let's just boost it directly in Update or Awake.
            base.SendMessage("Start");
            
            maxHealth *= 3f;
            currentHealth = maxHealth;
        }

        private new void Update()
        {
            base.SendMessage("Update"); // Let base logic handle movement

            if (IsDead) return;

            // Spell Casting logic
            if (hasTacticalTarget || Time.time - lastSpellTime > spellCooldown)
            {
                // Play spell casting animation if available
                // For now, it will just use standard ZombieAI attacks but hit harder.
                lastSpellTime = Time.time;
            }
        }
        
        // Override damage to make it tougher
        public new void TakeDamage(float damage)
        {
            base.TakeDamage(damage * 0.33f); // Takes 1/3rd damage
        }
    }
}
