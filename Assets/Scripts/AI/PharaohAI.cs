using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace TheAlchemistsCrypt.AI
{
    public class PharaohAI : ZombieAI
    {
        [Header("Pharaoh Boss Phase Settings")]
        [SerializeField] private float phase1ShieldDamageReduction = 0.5f;
        [SerializeField] private float phase1DamageReflection = 0.2f;
        
        private int currentPhase = 1;
        private bool isPhase1ShieldActive = true;
        private float shieldDisruptedTime = 0f;
        private float shieldDisruptDuration = 6.0f;
        
        private bool isPhase2FrenzyActive = false;
        private float frenzyDisruptedTime = 0f;
        private float frenzyDisruptDuration = 6.0f;
        
        private bool isPhase3StasisActive = false;
        private float stasisDisruptedTime = 0f;
        private float stasisDisruptDuration = 8.0f;
        
        private Light shieldLight;
        private float lastRegenTime = 0f;
        private float originalBaseSpeed;

        protected override void Start()
        {
            // Boss starts with higher stats
            vulnerableElement = "none";
            baseSpeed = 4.2f;
            originalBaseSpeed = baseSpeed;
            maxHealth = 150f;
            currentHealth = maxHealth;

            base.Start();

            var pharaohAgent = GetComponent<NavMeshAgent>();
            if (pharaohAgent != null)
            {
                pharaohAgent.radius = 1.3f;
                pharaohAgent.speed = baseSpeed;
            }

            // Create dynamic shield light indicator
            GameObject lightObj = new GameObject("PharaohShieldLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = Vector3.up * 1.5f;
            shieldLight = lightObj.AddComponent<Light>();
            shieldLight.type = LightType.Point;
            shieldLight.range = 6.0f;
            shieldLight.shadows = LightShadows.None;
            
            UpdatePhaseVisuals();
        }

        protected override void Update()
        {
            base.Update();

            if (IsDead)
            {
                if (shieldLight != null && shieldLight.enabled)
                {
                    shieldLight.enabled = false;
                }
                return;
            }

            // Phase transition checks
            float hpPct = currentHealth / maxHealth;
            if (currentPhase == 1 && hpPct <= 0.5f)
            {
                TransitionToPhase(2);
            }
            else if (currentPhase == 2 && hpPct <= 0.2f)
            {
                TransitionToPhase(3);
            }

            // Phase logic updates
            HandlePhaseLogic();
        }

        private void TransitionToPhase(int newPhase)
        {
            currentPhase = newPhase;
            Debug.Log($"[Pharaoh Boss] Entering Phase {currentPhase}!");
            
            // Switch sounds or play SFX
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_death", false, 0.9f);
            
            if (currentPhase == 2)
            {
                isPhase2FrenzyActive = true;
                baseSpeed = originalBaseSpeed * 1.4f;
                var agent = GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed = baseSpeed;
            }
            else if (currentPhase == 3)
            {
                isPhase3StasisActive = true;
                baseSpeed = originalBaseSpeed * 0.7f;
                var agent = GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed = baseSpeed;
            }

            UpdatePhaseVisuals();
        }

        private void HandlePhaseLogic()
        {
            // Phase 1 Shield disruption recovery
            if (currentPhase == 1)
            {
                if (!isPhase1ShieldActive && Time.time - shieldDisruptedTime > shieldDisruptDuration)
                {
                    isPhase1ShieldActive = true;
                    UpdatePhaseVisuals();
                    Debug.Log("[Pharaoh Boss] Mercury Shield restored!");
                }
            }
            // Phase 2 Frenzy disruption recovery
            else if (currentPhase == 2)
            {
                if (!isPhase2FrenzyActive && Time.time - frenzyDisruptedTime > frenzyDisruptDuration)
                {
                    isPhase2FrenzyActive = true;
                    baseSpeed = originalBaseSpeed * 1.4f;
                    var agent = GetComponent<NavMeshAgent>();
                    if (agent != null) agent.speed = baseSpeed;
                    UpdatePhaseVisuals();
                    Debug.Log("[Pharaoh Boss] Fire Frenzy restored!");
                }

                // Fire Aura Damage: damage player when they are too close
                if (isPhase2FrenzyActive && player != null)
                {
                    float sqrDist = (player.position - transform.position).sqrMagnitude;
                    if (sqrDist < 16.0f) // 4 meters
                    {
                        var playerHealth = player.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(8f * Time.deltaTime);
                        }
                    }
                }
            }
            // Phase 3 Stasis healing & stasis recovery
            else if (currentPhase == 3)
            {
                if (!isPhase3StasisActive && Time.time - stasisDisruptedTime > stasisDisruptDuration)
                {
                    isPhase3StasisActive = true;
                    UpdatePhaseVisuals();
                    Debug.Log("[Pharaoh Boss] Stasis field restored!");
                }

                if (isPhase3StasisActive && Time.time - lastRegenTime > 1.0f)
                {
                    lastRegenTime = Time.time;
                    currentHealth = Mathf.Min(maxHealth, currentHealth + 5f);
                    Debug.Log($"[Pharaoh Boss] Regenerating. Health: {currentHealth}/{maxHealth}");
                }
            }
        }

        private void UpdatePhaseVisuals()
        {
            if (shieldLight == null) return;

            shieldLight.enabled = true;
            if (currentPhase == 1)
            {
                // Cyan Shield Light
                shieldLight.color = isPhase1ShieldActive ? new Color(0f, 0.9f, 1f) : new Color(0f, 0.2f, 0.3f);
                shieldLight.intensity = isPhase1ShieldActive ? 12.0f : 2.0f;
                SetStatusColor(isPhase1ShieldActive ? new Color(0f, 0.5f, 1f) : Color.white);
            }
            else if (currentPhase == 2)
            {
                // Orange Fire Light
                shieldLight.color = isPhase2FrenzyActive ? new Color(1f, 0.4f, 0f) : new Color(0.3f, 0.1f, 0f);
                shieldLight.intensity = isPhase2FrenzyActive ? 15.0f : 2.0f;
                SetStatusColor(isPhase2FrenzyActive ? new Color(1f, 0.2f, 0f) : Color.gray);
            }
            else if (currentPhase == 3)
            {
                // Purple Stasis Light
                shieldLight.color = isPhase3StasisActive ? new Color(0.8f, 0.1f, 1f) : new Color(0.2f, 0f, 0.3f);
                shieldLight.intensity = isPhase3StasisActive ? 18.0f : 3.0f;
                SetStatusColor(isPhase3StasisActive ? new Color(0.6f, 0f, 0.8f) : Color.white);
            }
        }

        public override void TakeDamage(float damage)
        {
            if (IsDead) return;

            // Phase 1 Damage reflection and reduction
            if (currentPhase == 1 && isPhase1ShieldActive)
            {
                damage *= phase1ShieldDamageReduction;
                
                if (player != null)
                {
                    var playerHealth = player.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damage * phase1DamageReflection, true);
                    }
                }
            }
            // Phase 3 Invulnerability when stasis is active
            else if (currentPhase == 3 && isPhase3StasisActive)
            {
                damage = 0f;
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_attack", false, 0.4f, 0.1f);
            }

            if (damage > 0f)
            {
                base.TakeDamage(damage);
            }
        }

        // Handle specific elemental vulnerabilities
        public void ApplyBossReaction(string element)
        {
            if (IsDead) return;

            if (currentPhase == 1 && element == "Salt" && isPhase1ShieldActive)
            {
                // Salt shatters Phase 1 Shield
                isPhase1ShieldActive = false;
                shieldDisruptedTime = Time.time;
                UpdatePhaseVisuals();
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_death", false, 1.0f);
                Debug.Log("[Pharaoh Boss] Mercury Shield SHATTERED by Salt!");
            }
            else if (currentPhase == 2 && element == "Mercury" && isPhase2FrenzyActive)
            {
                // Mercury cools Phase 2 Frenzy, slowing boss
                isPhase2FrenzyActive = false;
                frenzyDisruptedTime = Time.time;
                baseSpeed = originalBaseSpeed * 0.5f;
                var agent = GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed = baseSpeed;
                UpdatePhaseVisuals();
                ApplyMercurySlow(frenzyDisruptDuration);
                Debug.Log("[Pharaoh Boss] Fire Frenzy QUENCHED by Mercury!");
            }
            else if (currentPhase == 3 && element == "Sulfur" && isPhase3StasisActive)
            {
                // Sulfur triggers explosion that shatters Phase 3 Stasis
                isPhase3StasisActive = false;
                stasisDisruptedTime = Time.time;
                UpdatePhaseVisuals();
                TakeDamage(25f); // High shatter damage
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_death", false, 1.0f);
                Debug.Log("[Pharaoh Boss] Crystalline Stasis BROKEN by Sulfur!");
            }
        }
    }
}
