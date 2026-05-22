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
        
        private AudioSource audioSource;

        [Header("Tactical AI Settings")]
        public int mummyId = 0;
        public float baseSpeed = 3.2f;
        [HideInInspector] public Vector3 tacticalTarget;
        [HideInInspector] public float tacticalSpeedMult = 1f;
        [HideInInspector] public bool hasTacticalTarget = false;

        // ── Wander / Patrol fallback (when no tactical target & player is far) ──
        private float wanderTimer = 0f;
        private float wanderInterval = 4f;  // Pick a new wander point every 4 seconds
        private Vector3 wanderTarget = Vector3.zero;
        private bool hasWanderTarget = false;

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
        private static Sprite sharedHealthBarSprite;

        private TheAlchemistsCrypt.Player.PlayerHealth cachedPlayerHealth;
        private bool hasSpeedParameter = false;
        private int speedParamHash;
        private float pathfindCooldown = 0f;

        private void CreateHealthBar()
        {
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;

            // Use statically cached shared sprite to avoid texture allocation per mummy
            if (sharedHealthBarSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                sharedHealthBarSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            healthBarSprite = sharedHealthBarSprite;

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

        private bool combatMusicTriggered = false;

        protected virtual void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.maxDistance = 25f;
            audioSource.volume = 0.4f; // Lowered from default 1.0
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            
            // Decouple agent navigation steering from orientation so custom look-rotation works flawlessly
            agent.updateRotation = false;
            
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            
            if (animator != null)
            {
                animator.applyRootMotion = false; // Fix 'dragged' look by disabling root motion
                hasSpeedParameter = HasParameter("Speed");
                if (hasSpeedParameter)
                {
                    speedParamHash = Animator.StringToHash("Speed");
                }
            }

            // Slower speed for realism and Mummy thematic movement (buffed 1.2x)
            agent.speed = baseSpeed;
            agent.stoppingDistance = attackDistance;
            
            // Higher quality avoidance to prevent overlap and clipping
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 0.5f;
            agent.acceleration = 12f;
            agent.angularSpeed = 240f;
            
            currentHealth = maxHealth;
            FindPlayer();

            // Randomly assign elemental vulnerability: "sulfur", "mercury", or "salt"
            string[] vulnerabilities = { "sulfur", "mercury", "salt" };
            vulnerableElement = vulnerabilities[Random.Range(0, vulnerabilities.Length)];

            // Same color mummies: backup and do not apply initial vulnerability tint!
            BackupOriginalColors();
            CreateHealthBar();

            // ── NavMesh snap: ensure agent starts ON the nav mesh ──
            // If spawned slightly off-mesh (floating, on steep slope), SetDestination silently
            // fails and the mummy stands frozen. Snap to nearest valid surface within 5m.
            StartCoroutine(SnapToNavMeshDelayed());
        }

        private System.Collections.IEnumerator SnapToNavMeshDelayed()
        {
            // Wait one frame for NavMesh to fully initialize after scene load
            yield return null;
            yield return null;

            if (agent == null) yield break;

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"[ZombieAI] Snapped {name} to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogWarning($"[ZombieAI] {name} could not find NavMesh within 5m — mummy will be inactive.");
                }
            }
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

            if (player != null)
            {
                cachedPlayerHealth = player.GetComponentInChildren<TheAlchemistsCrypt.Player.PlayerHealth>();
                if (cachedPlayerHealth == null)
                    cachedPlayerHealth = player.GetComponentInParent<TheAlchemistsCrypt.Player.PlayerHealth>();
            }
            if (cachedPlayerHealth == null)
            {
                cachedPlayerHealth = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
            }
        }

        private void ShootPlayer()
        {
            if (player == null) return;
            lastShootTime = Time.time;

            // Spawn point is slightly forward and upward (at mummy chest/head level)
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
            Vector3 targetPos = player.position + Vector3.up * 1.0f;
            Vector3 dir = (targetPos - spawnPos).normalized;

            // Use the pooled Spawn method
            MummyProjectile.Spawn(spawnPos, dir, 16f, 10f);

            // Trigger attack anim
            PlayAnimation("Attack");
        }

        private float stuckTimer = 0f;
        private Vector3 lastPos;

        protected virtual void Update()
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

            // Stuck detection: if we are supposed to move but haven't changed position much
            if (Vector3.Distance(transform.position, lastPos) < 0.05f && !isStunned && !isDead && agent.hasPath)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 2.5f) // Stuck for 2.5 seconds
                {
                    // Force a warp slightly towards the current destination to break free
                    NavMeshHit hit;
                    Vector3 warpDir = (agent.destination - transform.position).normalized;
                    if (NavMesh.SamplePosition(transform.position + warpDir * 1.5f, out hit, 4f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        Debug.Log($"[ZombieAI] {name} was stuck, warping to {hit.position}");
                    }
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPos = transform.position;

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
            float currentSpeed = baseSpeed;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // ── Priority 1: HiveMind tactical target ──
            // API "Standard Patrol/idle" fallback returns target = mummy's own coords,
            // so we reject any target within 3m of self (raised from 1.5m).
            bool hasMeaningfulTactical = hasTacticalTarget &&
                                         Vector3.Distance(tacticalTarget, transform.position) > 3f;

            if (hasMeaningfulTactical && distanceToPlayer > 4.0f)
            {
                // HiveMind gave a real flanking/ambush target — go there
                // Enforce world Z boundary so mummies never get instructions that send them into the sea
                Vector3 bounded = tacticalTarget;
                if (bounded.z < -95f) bounded.z = -95f;
                currentTargetPos = bounded;
                currentSpeed = baseSpeed * Mathf.Max(tacticalSpeedMult, 0.5f);
            }
            else if (distanceToPlayer <= 15f)
            {
                // ── Priority 2: Player is close — chase them ──
                NavMeshHit playerHit;
                currentTargetPos = NavMesh.SamplePosition(player.position, out playerHit, 15f, NavMesh.AllAreas)
                    ? playerHit.position
                    : player.position;
                currentSpeed = baseSpeed;
            }

            // Apply Papyrus Speed Buff: If player has the scroll, mummies become 1.5x faster!
            if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null && TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.hasKey)
            {
                currentSpeed *= 1.5f;
            }

            else
            {
                // ── Priority 3: Player is far AND no HiveMind target — wander patrol ──
                // This runs when:
                //   a) HiveMind returned "idle" with own-position target (rejected above), OR
                //   b) HiveMind returned no instructions at all.
                // Mummies now always move; they never stand frozen.
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f || !hasWanderTarget)
                {
                    Vector3 randDir = UnityEngine.Random.insideUnitSphere * 25f;
                    randDir.y = 0f;
                    randDir += transform.position;
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(randDir, out navHit, 25f, NavMesh.AllAreas))
                    {
                        wanderTarget = navHit.position;
                        hasWanderTarget = true;
                    }
                    wanderTimer = wanderInterval + UnityEngine.Random.Range(-1f, 1f);
                }
                    if (hasWanderTarget)
                    {
                        // Enforce coastline boundary for wander targets as well
                        Vector3 bounded = wanderTarget;
                        if (bounded.z < -95f) bounded.z = -95f;
                        currentTargetPos = bounded;
                        currentSpeed = 1.8f;
                        if (Vector3.Distance(transform.position, wanderTarget) < 2f)
                            hasWanderTarget = false;
                    }
                else
                {
                    // NavMesh.SamplePosition failed — drift slowly toward player as fallback
                    currentTargetPos = player.position;
                    currentSpeed = 1.2f;
                }
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

            if (agent.isActiveAndEnabled && agent.isOnNavMesh) {
                agent.speed = currentSpeed;
                if (isStunned)
                {
                    agent.velocity = Vector3.zero;
                    if (agent.hasPath) agent.ResetPath();
                }
                else
                {
                    // Throttle SetDestination calls using a random interval to spread CPU load on mobile
                    pathfindCooldown -= Time.deltaTime;
                    if (pathfindCooldown <= 0f)
                    {
                        agent.SetDestination(currentTargetPos);
                        pathfindCooldown = Random.Range(0.2f, 0.4f);
                    }
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

            // Dynamically switch to combat music when mummy closes within 20 units
            if (!combatMusicTriggered && distance < 20f)
            {
                combatMusicTriggered = true;
                TheAlchemistsCrypt.Gameplay.AudioManager.PlayCombatTheme();
            }

            float vel = agent.velocity.magnitude;
            
            // Set Speed parameter safely only if it exists for automatic transitions
            if (animator != null && animator.runtimeAnimatorController != null && hasSpeedParameter) {
                try {
                    animator.SetFloat(speedParamHash, vel);
                } catch (System.Exception) {
                    // Suppress harmless animator parameter warnings during transitions
                }
            }

            if (shootAnimTimer > 0f) {
                PlayAnimation("Attack");
                if (animator != null) animator.speed = 1.0f;
            }
            else if (vel > 0.4f) { // Increased threshold to avoid jittery walk in place
                PlayAnimation("Walk");
                if (animator != null) animator.speed = 1.2f; 
            }
            else if (distance <= attackDistance) {
                PlayAnimation("Attack");
                if (animator != null) animator.speed = 1.0f;
                
                if (cachedPlayerHealth == null) FindPlayer();
                if (cachedPlayerHealth != null) cachedPlayerHealth.TakeDamage(12f * Time.deltaTime);
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
            if (animator != null && animator.runtimeAnimatorController != null && hasSpeedParameter) {
                try {
                    animator.SetFloat(speedParamHash, s);
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

            // Prevent mummies from going past the coastline (Z = -95f)
            if (transform.position.z < -95f)
            {
                Vector3 pos = transform.position;
                pos.z = -95f;
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.Warp(pos);
                }
                else
                {
                    transform.position = pos;
                }
            }
        }

        public virtual void TakeDamage(float damage)
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

            if (audioSource != null)
            {
                audioSource.Stop();
                // Use throttled global SFX for death too
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_death", false, 0.6f, 0.2f);
            }

            // Switch back to main theme if no other mummies are nearby
            var allMummies = GameObject.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
            bool anyNearby = false;
            if (player != null)
            {
                foreach (var m in allMummies)
                {
                    if (m != null && !m.IsDead && m != this &&
                        Vector3.Distance(m.transform.position, player.position) < 25f)
                    {
                        anyNearby = true;
                        break;
                    }
                }
            }
            if (!anyNearby)
            {
                TheAlchemistsCrypt.Gameplay.AudioManager.PlayMainTheme();
            }

            // Attempt to trigger Die/Death animation
            PlayAnimation("Die");
            if (animator != null) {
                animator.speed = 1.0f;
            }
        }

        private void PlayAnimation(string stateName)
        {
            if (animator != null && animator.runtimeAnimatorController != null && currentAnimState != stateName) {
                currentAnimState = stateName;
                // Double fallback: some rigs use trigger, some use CrossFade. Explicitly specify layer 0 to avoid -1 layer warnings.
                animator.CrossFadeInFixedTime(stateName, 0.2f, 0);

                if (audioSource != null)
                {
                    if (stateName == "Walk")
                    {
                        AudioClip walkClip = TheAlchemistsCrypt.Gameplay.AudioManager.LoadClip("sfx/sfx_mummy_walk");
                        if (walkClip != null)
                        {
                            audioSource.clip = walkClip;
                            audioSource.loop = true;
                            if (!audioSource.isPlaying) audioSource.Play();
                        }
                    }
                    else
                    {
                        audioSource.Stop();
                    }

                    if (stateName == "Attack")
                    {
                        // Use throttled global SFX to prevent "wall of noise" when many mummies attack at once
                        TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_attack", false, 0.5f, 0.4f);
                    }
                }
            }
        }
    }
}
