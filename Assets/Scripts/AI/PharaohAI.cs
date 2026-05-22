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
            // Boss is immune to elemental status effects
            vulnerableElement = "none";
            
            // Pharaoh is 1.5x the base speed of mummies (1.5 * 3.2 = 4.8)
            baseSpeed = 4.8f;
            
            base.Start(); // Let base initialize agent, animator, etc.
            
            // Match player health: 100f
            maxHealth = 100f;
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
        
        // Override damage - boss takes full damage, same as player HP 
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage); // Full damage — 100 HP fight
        }
    }
}
