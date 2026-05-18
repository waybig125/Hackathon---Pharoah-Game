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
        
        [Header("Procedural Health Bar HUD")]
        private GameObject healthBarObj;
        private UnityEngine.UI.Image healthBarFill;
        private Transform mainCameraTransform;

        private void CreateHealthBar()
        {
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;

            healthBarObj = new GameObject("MummyHealthBarCanvas");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = new Vector3(0f, 2.4f, 0f); // Slightly higher to be fully visible above head
            healthBarObj.transform.localRotation = Quaternion.identity;
            healthBarObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            healthBarObj.layer = 0; // Default layer

            Canvas canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 5; // Render on top of meshes

            UnityEngine.UI.CanvasScaler scaler = healthBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            GameObject bgObj = new GameObject("HealthBarBG");
            bgObj.transform.SetParent(healthBarObj.transform, false);
            bgObj.layer = 0;
            var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.15f, 0.0f, 0.0f, 0.8f); // Darker red BG for higher contrast
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(300f, 40f);

            GameObject fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(healthBarObj.transform, false);
            fillAreaObj.layer = 0;
            var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.sizeDelta = new Vector2(300f, 40f);

            GameObject fillObj = new GameObject("HealthBarFill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            fillObj.layer = 0;
            healthBarFill = fillObj.AddComponent<UnityEngine.UI.Image>();
            healthBarFill.color = Color.green;
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(300f, 40f);
            fillRect.localPosition = new Vector3(-150f, 0f, 0f);
        }

        private void UpdateHealthBar()
        {
            if (isDead)
            {
                if (healthBarObj != null) Destroy(healthBarObj);
                return;
            }

            if (healthBarObj == null || healthBarFill == null) return;

            if (mainCameraTransform == null && Camera.main != null) mainCameraTransform = Camera.main.transform;
            if (mainCameraTransform != null)
            {
                // Force billboard to face camera exactly
                healthBarObj.transform.LookAt(healthBarObj.transform.position + mainCameraTransform.forward);
            }

            float fillPct = Mathf.Clamp01(currentHealth / maxHealth);
            var rect = healthBarFill.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f * fillPct, 40f);
            healthBarFill.color = Color.Lerp(Color.red, Color.green, fillPct);
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
                if (r == null || r is CanvasRenderer) continue;
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
                if (r == null || r is CanvasRenderer) continue;
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
