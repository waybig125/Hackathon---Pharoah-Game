using UnityEngine;
using UnityEngine.AI;

namespace TheAlchemistsCrypt.AI
{
    public class ZombieAI : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Transform player;
        private float attackDistance = 2.5f;
        private float checkInterval = 0.5f;
        private float timer;

        private Animator animator;
        private string currentAnimState = "";

        [Header("Tactical AI Settings")]
        public int mummyId = 0;
        [HideInInspector] public Vector3 tacticalTarget;
        [HideInInspector] public float tacticalSpeedMult = 1f;
        [HideInInspector] public bool hasTacticalTarget = false;

        [Header("Health Settings")]
        public float maxHealth = 10f;
        public float currentHealth = 10f;
        private bool isDead = false;
        public bool IsDead => isDead;
        private float deathTimer = 0f;

        [Header("Elemental Status Settings")]
        public string vulnerableElement = "sulfur";
        private bool isSlowed = false;
        private float mercurySlowTimer = 0f;
        private bool isStunned = false;
        private float saltStunTimer = 0f;
        
        private Color originalColor = Color.white;
        private bool hasBackedUpColor = false;

        private void BackupOriginalColors()
        {
            if (hasBackedUpColor) return;
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null && r.material != null)
            {
                originalColor = r.material.HasProperty("_BaseColor") ? r.material.GetColor("_BaseColor") : (r.material.HasProperty("_Color") ? r.material.GetColor("_Color") : Color.white);
                hasBackedUpColor = true;
            }
        }

        private void SetStatusColor(Color col)
        {
            BackupOriginalColors();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.SetColor("_EmissionColor", col * 0.5f);
                        m.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        private void RestoreColors()
        {
            if (!hasBackedUpColor) return;
            SetStatusColor(originalColor);
        }

        public void ApplyMercurySlow(float duration)
        {
            isSlowed = true;
            mercurySlowTimer = duration;
            SetStatusColor(new Color(0.2f, 0.6f, 1.0f)); // Blue/cyan tint
        }

        public void ApplySaltStun(float duration)
        {
            isStunned = true;
            saltStunTimer = duration;
            SetStatusColor(new Color(0.9f, 0.8f, 1.0f)); // Sparkly crystalline purple tint
        }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            
            if (animator != null) animator.applyRootMotion = false; // Fix 'dragged' look by disabling root motion

            // Slower speed for realism and Mummy thematic movement
            agent.speed = 2.2f;
            agent.stoppingDistance = attackDistance;
            
            // Higher quality avoidance to prevent overlap and clipping
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 0.6f;
            
            currentHealth = maxHealth;
            FindPlayer();

            // Randomly assign elemental vulnerability: "sulfur", "mercury", or "salt"
            string[] vulnerabilities = { "sulfur", "mercury", "salt" };
            vulnerableElement = vulnerabilities[Random.Range(0, vulnerabilities.Length)];

            // Tint mummy models visually based on vulnerability
            Color vulnerabilityColor = Color.white;
            if (vulnerableElement == "sulfur")
            {
                vulnerabilityColor = new Color(1.0f, 0.35f, 0.05f); // Fiery orange
            }
            else if (vulnerableElement == "mercury")
            {
                vulnerabilityColor = new Color(0.0f, 0.85f, 1.0f); // Cool cyan
            }
            else if (vulnerableElement == "salt")
            {
                vulnerabilityColor = new Color(0.85f, 0.85f, 1.0f); // Crystalline purple/white
            }

            // Set up originalColor so status effects can restore back to this base color tint
            originalColor = vulnerabilityColor;
            hasBackedUpColor = true;
            SetStatusColor(vulnerabilityColor);
        }

        private void FindPlayer()
        {
            // Tag search
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else {
                // Component search - More robust
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.transform;
                else {
                    var cam = Camera.main;
                    if (cam != null) player = cam.transform;
                }
            }
        }

        private void Update()
        {
            if (isDead) {
                deathTimer += Time.deltaTime;
                if (deathTimer > 1f) transform.position += Vector3.down * 0.6f * Time.deltaTime;
                if (deathTimer > 4f) Destroy(gameObject);
                return;
            }

            if (player == null) {
                SetAnimSpeed(0f);
                timer += Time.deltaTime;
                if (timer >= checkInterval) { FindPlayer(); timer = 0; }
                return;
            }

            // Update status timers
            if (isStunned)
            {
                saltStunTimer -= Time.deltaTime;
                if (saltStunTimer <= 0f)
                {
                    isStunned = false;
                    RestoreColors();
                }
            }
            if (isSlowed)
            {
                mercurySlowTimer -= Time.deltaTime;
                if (mercurySlowTimer <= 0f)
                {
                    isSlowed = false;
                    RestoreColors();
                }
            }

            Vector3 currentTargetPos = player.position;
            float currentSpeed = 2.2f;

            if (hasTacticalTarget) {
                currentTargetPos = tacticalTarget;
                currentSpeed = 2.2f * tacticalSpeedMult;
            }

            // Apply alchemical status speed modifications
            if (isStunned)
            {
                currentSpeed = 0f;
            }
            else if (isSlowed)
            {
                currentSpeed *= 0.3f; // 70% slow
            }

            if (agent.isActiveAndEnabled) {
                agent.speed = currentSpeed;
                if (isStunned)
                {
                    agent.velocity = Vector3.zero;
                    if (agent.hasPath) agent.ResetPath();
                }
                else
                {
                    agent.SetDestination(currentTargetPos);
                }
            }

            Vector3 targetDir = currentTargetPos - transform.position;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude > 0.01f) {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }

            float distance = Vector3.Distance(transform.position, player.position);
            float vel = agent.velocity.magnitude;
            
            // Set Speed parameter safely only if it exists for automatic transitions
            if (animator != null && animator.runtimeAnimatorController != null) {
                try {
                    if (HasParameter("Speed")) {
                        animator.SetFloat("Speed", vel);
                    }
                } catch (System.Exception) {
                    // Suppress harmless animator parameter warnings during transitions
                }
            }

            if (vel > 0.1f) {
                PlayAnimation("Walk");
                if (animator != null) animator.speed = 1.2f; 
            }
            else if (distance <= attackDistance) {
                PlayAnimation("Attack");
                if (animator != null) animator.speed = 1.0f;
                
                var health = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
                if (health != null) health.TakeDamage(12f * Time.deltaTime);
            }
            else {
                PlayAnimation("Idle");
                if (animator != null) animator.speed = 1.0f;
            }
        }

        private bool HasParameter(string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            try {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == paramName) return true;
                }
            } catch (System.Exception) {}
            return false;
        }

        private void SetAnimSpeed(float s)
        {
            if (animator != null && animator.runtimeAnimatorController != null) {
                try {
                    if (HasParameter("Speed")) animator.SetFloat("Speed", s);
                } catch (System.Exception) {}
            }
        }

        private void LateUpdate()
        {
            if (isDead) return;
            // Enforce upright rotation: local X must be 0 and local Z must be 0
            // This prevents them from falling or tilting during physics/agent movement
            Vector3 rot = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(0f, rot.y, 0f);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"Zombie took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            
            if (agent != null)
            {
                agent.enabled = false;
            }

            // Disable all colliders to allow player and projectiles to pass through
            var colliders = GetComponents<Collider>();
            foreach (var c in colliders) c.enabled = false;
            var childColliders = GetComponentsInChildren<Collider>();
            foreach (var c in childColliders) c.enabled = false;

            // Spawn Restorative Alchemical Essence Orb procedurally
            GameObject orbObj = new GameObject("AlchemicalRestorationOrb");
            orbObj.transform.position = transform.position + Vector3.up * 0.8f;
            orbObj.AddComponent<TheAlchemistsCrypt.Gameplay.HealthOrb>();

            // Attempt to trigger Die/Death animation
            PlayAnimation("Die");
            if (animator != null) {
                animator.speed = 1.0f;
            }
        }

        private void PlayAnimation(string stateName)
        {
            if (animator != null && currentAnimState != stateName) {
                currentAnimState = stateName;
                // Double fallback: some rigs use trigger, some use CrossFade. Explicitly specify layer 0 to avoid -1 layer warnings.
                animator.CrossFadeInFixedTime(stateName, 0.2f, 0);
            }
        }
    }
}
