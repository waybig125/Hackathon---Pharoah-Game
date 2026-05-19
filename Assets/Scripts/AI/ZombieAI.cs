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

        [Header("Ranged Attack Settings")]
        public float shootCooldown = 3.0f;
        public float shootMinRange = 2.5f;
        public float shootMaxRange = 12.0f;
        private float lastShootTime = 0f;
        private float shootAnimTimer = 0f;

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
        
        [Header("Procedural Health Bar HUD")]
        private GameObject healthBarObj;
        private SpriteRenderer healthBarFillSr;
        private Transform mainCameraTransform;
        private Sprite healthBarSprite;

        private void CreateHealthBar()
        {
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;

            // Generate 1x1 white sprite
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            healthBarSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            // Container
            healthBarObj = new GameObject("MummyHealthBar");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = new Vector3(0f, 2.4f, 0f); // Slightly higher to be fully visible above head
            healthBarObj.transform.localRotation = Quaternion.identity;

            // Background
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(healthBarObj.transform, false);
            var bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = healthBarSprite;
            bgSr.color = new Color(0.15f, 0.0f, 0.0f, 0.8f); // Dark red BG
            bgObj.transform.localScale = new Vector3(0.9f, 0.12f, 1f); // Thin elegant bar

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(healthBarObj.transform, false);
            healthBarFillSr = fillObj.AddComponent<SpriteRenderer>();
            healthBarFillSr.sprite = healthBarSprite;
            healthBarFillSr.color = Color.green;
            healthBarFillSr.sortingOrder = 1; // Draw on top of BG
            fillObj.transform.localScale = new Vector3(0.88f, 0.10f, 1f);
        }

        private void UpdateHealthBar()
        {
            if (isDead)
            {
                if (healthBarObj != null) Destroy(healthBarObj);
                return;
            }

            if (healthBarObj == null || healthBarFillSr == null) return;

            if (mainCameraTransform == null && Camera.main != null) mainCameraTransform = Camera.main.transform;
            if (mainCameraTransform != null)
            {
                // Force billboard to face camera exactly
                healthBarObj.transform.LookAt(healthBarObj.transform.position + mainCameraTransform.forward);
            }

            float fillPct = Mathf.Clamp01(currentHealth / maxHealth);
            float maxScaleX = 0.88f;
            float targetScaleX = maxScaleX * fillPct;
            float posX = -0.5f * maxScaleX * (1f - fillPct);

            healthBarFillSr.gameObject.transform.localPosition = new Vector3(posX, 0f, 0f);
            healthBarFillSr.gameObject.transform.localScale = new Vector3(targetScaleX, 0.10f, 1f);
            healthBarFillSr.color = Color.Lerp(Color.red, Color.green, fillPct);
        }

        private void BackupOriginalColors()
        {
            // Deprecated: using high-performance MaterialPropertyBlock overrides instead
        }

        private void SetStatusColor(Color col)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_Color", col);
                block.SetColor("_BaseColor", col);
                block.SetColor("_EmissionColor", col * 0.8f);
                r.SetPropertyBlock(block);
            }
        }

        private void RestoreColors()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        public void ApplyMercurySlow(float duration)
        {
            isSlowed = true;
            mercurySlowTimer = duration;
            SetStatusColor(new Color(0.15f, 0.6f, 1.0f)); // Glowing metallic cyan-blue
        }

        public void ApplySaltStun(float duration)
        {
            isStunned = true;
            saltStunTimer = duration;
            SetStatusColor(new Color(0.85f, 0.3f, 1.0f)); // Sparkling crystalline royal purple/violet
        }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            
            // Decouple agent navigation steering from orientation so custom look-rotation works flawlessly
            agent.updateRotation = false;
            
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

            // Same color mummies: backup and do not apply initial vulnerability tint!
            BackupOriginalColors();
            CreateHealthBar();
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

        private void ShootPlayer()
        {
            if (player == null) return;
            lastShootTime = Time.time;

            // Spawn point is slightly forward and upward (at mummy chest/head level)
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;

            GameObject projObj = new GameObject("MummyProjectile");
            projObj.transform.position = spawnPos;

            var projectile = projObj.AddComponent<MummyProjectile>();
            // Target the player's chest/body level rather than their feet (which is transform.position)
            Vector3 targetPos = player.position + Vector3.up * 1.0f;
            projectile.direction = (targetPos - spawnPos).normalized;
            projectile.speed = 16f;
            projectile.damage = 10f;

            // Trigger attack anim
            PlayAnimation("Attack");
        }

        private void Update()
        {
            UpdateHealthBar();

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

            // Focus completely on chasing player at close proximity to prevent turning away
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (hasTacticalTarget && distanceToPlayer > 4.0f) {
                currentTargetPos = tacticalTarget;
                currentSpeed = 2.2f * tacticalSpeedMult;
            }

            // Handle ranged shooting anim stop duration
            if (shootAnimTimer > 0f)
            {
                shootAnimTimer -= Time.deltaTime;
                currentSpeed = 0f;
            }

            // Check if we should shoot the player in a straight line
            if (!isStunned && !isDead && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist >= shootMinRange && dist <= shootMaxRange && (Time.time - lastShootTime) >= shootCooldown)
                {
                    ShootPlayer();
                    shootAnimTimer = 0.8f;
                    currentSpeed = 0f;
                }
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

            // Intelligent look-rotation: always orient mummy to face player while moving or surrounding
            Vector3 targetDir = player.position - transform.position;
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

            if (shootAnimTimer > 0f) {
                PlayAnimation("Attack");
                if (animator != null) animator.speed = 1.0f;
            }
            else if (vel > 0.1f) {
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
            if (healthBarObj != null) Destroy(healthBarObj);
            
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
