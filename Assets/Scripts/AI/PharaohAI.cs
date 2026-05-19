using UnityEngine;
using UnityEngine.AI;

namespace TheAlchemistsCrypt.AI
{
    public class PharaohAI : ZombieAI
    {
        [Header("Pharaoh Boss Settings")]
        public float spellCooldown = 5.0f;
        private float lastSpellTime = 0f;
        
        protected override void Start()
        {
            // Boost boss stats
            maxHealth = 300f; // Boss health
            currentHealth = maxHealth;
            vulnerableElement = "none"; // Boss is immune to basic stuns
            
            base.Start();
            
            maxHealth *= 3f;
            currentHealth = maxHealth;
        }

        protected override void Update()
        {
            base.Update(); // Let base logic handle movement

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
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage * 0.33f); // Takes 1/3rd damage
        }
    }
}
