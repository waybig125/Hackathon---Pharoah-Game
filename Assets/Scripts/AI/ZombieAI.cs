using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

namespace TheAlchemistsCrypt.AI
{
    public class ZombieAI : MonoBehaviour
    {
        private NavMeshAgent agent;
        protected Transform player;
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
        public System.Action<GameObject> onReleaseToPool;

        [Header("Elemental Status Settings")]
        public string vulnerableElement = "sulfur";
        private bool isSlowed = false;
        private float mercurySlowTimer = 0f;
        private bool isStunned = false;
        private float saltStunTimer = 0f;
        
        public enum AlchemicalResidue { None, Sulfur, Mercury, Salt }
        public AlchemicalResidue activeResidue = AlchemicalResidue.None;
        private float residueTimer = 0f;
        
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

        // ── PERFORMANCE: Pre-cached component arrays ──────────────────────────────
        // Avoids GetComponentsInChildren<Renderer> allocating a new array on every
        // elemental status-effect hit. Cached once in Start().
        private Renderer[] cachedRenderers;
        private MaterialPropertyBlock cachedMPB;
        // Dirty flag: only rebuild health-bar fill geometry when HP actually changes
        private float lastHealthForBar = -1f;

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

            // Billboard rotation runs every frame (cheap — just a matrix op)
            if (mainCameraTransform == null && Camera.main != null) mainCameraTransform = Camera.main.transform;
            if (mainCameraTransform != null)
            {
                healthBarObj.transform.LookAt(healthBarObj.transform.position + mainCameraTransform.forward);
            }

            // PERFORMANCE: Only rebuild fill visuals when HP has actually changed.
            // This avoids per-frame localPosition + localScale + Color.Lerp overhead
            // for every mummy every frame (20 mummies × 60 fps = 1200 wasted calls/s).
            if (Mathf.Approximately(currentHealth, lastHealthForBar)) return;
            lastHealthForBar = currentHealth;

            float fillPct    = Mathf.Clamp01(currentHealth / maxHealth);
            float maxScaleX  = 0.88f;
            float targetScaleX = maxScaleX * fillPct;
            float posX       = -0.5f * maxScaleX * (1f - fillPct);

            healthBarFillSr.gameObject.transform.localPosition = new Vector3(posX, 0f, 0f);
            healthBarFillSr.gameObject.transform.localScale    = new Vector3(targetScaleX, 0.10f, 1f);
            healthBarFillSr.color = Color.Lerp(Color.red, Color.green, fillPct);
        }

        private void BackupOriginalColors()
        {
            // Deprecated: using high-performance MaterialPropertyBlock overrides instead
        }

        protected void SetStatusColor(Color col)
        {
            // PERFORMANCE: Use pre-cached renderer array instead of allocating a new
            // one via GetComponentsInChildren on every elemental hit.
            if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
            if (cachedMPB == null)       cachedMPB = new MaterialPropertyBlock();
            foreach (Renderer r in cachedRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(cachedMPB);
                cachedMPB.SetColor("_Color",         col);
                cachedMPB.SetColor("_BaseColor",     col);
                cachedMPB.SetColor("_EmissionColor", col * 0.8f);
                r.SetPropertyBlock(cachedMPB);
            }
        }

        private void RestoreColors()
        {
            // PERFORMANCE: Use pre-cached array — avoids allocation on every status expiry.
            if (cachedRenderers == null) return;
            foreach (Renderer r in cachedRenderers)
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

        public void ApplyAlchemicalElement(AlchemicalResidue element)
        {
            if (isDead) return;
            if (vulnerableElement == "none") return; // Boss handles reactions separately

            if (activeResidue == AlchemicalResidue.None)
            {
                activeResidue = element;
                residueTimer = 4.0f;
                switch (element)
                {
                    case AlchemicalResidue.Sulfur: SetStatusColor(new Color(1.0f, 0.55f, 0.05f)); break;
                    case AlchemicalResidue.Mercury: SetStatusColor(new Color(0.15f, 0.6f, 1.0f)); break;
                    case AlchemicalResidue.Salt: SetStatusColor(new Color(0.85f, 0.3f, 1.0f)); break;
                }
            }
            else if (activeResidue != element)
            {
                TriggerAlchemicalReaction(activeResidue, element);
                activeResidue = AlchemicalResidue.None;
                residueTimer = 0f;
            }
            else
            {
                residueTimer = 4.0f;
            }
        }

        private void TriggerAlchemicalReaction(AlchemicalResidue r1, AlchemicalResidue r2)
        {
            if ((r1 == AlchemicalResidue.Sulfur && r2 == AlchemicalResidue.Salt) ||
                (r1 == AlchemicalResidue.Salt && r2 == AlchemicalResidue.Sulfur))
            {
                TriggerAcidicExplosion();
            }
            else if ((r1 == AlchemicalResidue.Sulfur && r2 == AlchemicalResidue.Mercury) ||
                     (r1 == AlchemicalResidue.Mercury && r2 == AlchemicalResidue.Sulfur))
            {
                TriggerThermiteBlaze();
            }
            else if ((r1 == AlchemicalResidue.Mercury && r2 == AlchemicalResidue.Salt) ||
                     (r1 == AlchemicalResidue.Salt && r2 == AlchemicalResidue.Mercury))
            {
                TriggerCrystalShatter();
            }
        }

        private void TriggerAcidicExplosion()
        {
            TakeDamage(25f);
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mummy_death", false, 0.8f, 0.1f);
            SetStatusColor(new Color(0.2f, 0.9f, 0.2f));

            Collider[] colliders = Physics.OverlapSphere(transform.position, 5.0f);
            foreach (Collider c in colliders)
            {
                var z = c.GetComponent<ZombieAI>();
                if (z == null) z = c.GetComponentInParent<ZombieAI>();
                if (z != null && z != this && !z.IsDead)
                {
                    z.TakeDamage(12f);
                    z.ApplySaltStun(1.5f);
                }
            }

            if (TheAlchemistsCrypt.Gameplay.VFXManager.Instance != null) {
                TheAlchemistsCrypt.Gameplay.VFXManager.Instance.PlayAcidExplosion(transform.position + Vector3.up);
            }
        }

        private void TriggerThermiteBlaze()
        {
            TakeDamage(15f);
            SetStatusColor(new Color(1f, 0.1f, 0f));
            StartCoroutine(ThermiteBurnRoutine());
        }

        private IEnumerator ThermiteBurnRoutine()
        {
            float duration = 5f;
            float elapsed = 0f;
            float originalSpeed = baseSpeed;
            baseSpeed = 7f;
            if (agent != null) agent.speed = baseSpeed;

            while (elapsed < duration && !isDead)
            {
                TakeDamage(4f);
                elapsed += 0.5f;

                Collider[] colliders = Physics.OverlapSphere(transform.position, 2.0f);
                foreach (Collider c in colliders)
                {
                    var z = c.GetComponent<ZombieAI>();
                    if (z == null) z = c.GetComponentInParent<ZombieAI>();
                    if (z != null && z != this && !z.IsDead && z.activeResidue == AlchemicalResidue.None)
                    {
                        z.TakeDamage(2f);
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }

            if (!isDead)
            {
                baseSpeed = originalSpeed;
                if (agent != null) agent.speed = baseSpeed;
                RestoreColors();
            }
        }

        private void TriggerCrystalShatter()
        {
            TakeDamage(10f);
            SetStatusColor(new Color(0f, 0.8f, 1f));
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, 4.0f);
            foreach (Collider c in colliders)
            {
                var z = c.GetComponent<ZombieAI>();
                if (z == null) z = c.GetComponentInParent<ZombieAI>();
                if (z != null && z != this && !z.IsDead)
                {
                    z.ApplyMercurySlow(3.0f);
                }
            }

            if (TheAlchemistsCrypt.Gameplay.VFXManager.Instance != null) {
                TheAlchemistsCrypt.Gameplay.VFXManager.Instance.PlayShatterExplosion(transform.position + Vector3.up);
            }
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
                // PERFORMANCE: Stop GPU bone skinning and CPU blend-tree updates when off-screen.
                // With 20 mummies all animating every frame regardless of visibility,
                // SkinnedMeshRenderer was updating bone transforms even for mummies behind the player.
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                hasSpeedParameter = HasParameter("Speed");
                if (hasSpeedParameter)
                {
                    speedParamHash = Animator.StringToHash("Speed");
                }
            }

            // Slower speed for realism and Mummy thematic movement (buffed 1.2x)
            agent.speed = baseSpeed;
            agent.stoppingDistance = attackDistance;
            
            // PERFORMANCE: LowQuality avoidance is sufficient for 20 mummies on mobile.
            // HighQuality ORCA runs O(n^2) comparisons per agent — very expensive at scale.
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.radius        = 0.8f;
            agent.acceleration  = 12f;
            agent.angularSpeed  = 240f;
            
            currentHealth = maxHealth;
            FindPlayer();

            // Randomly assign elemental vulnerability: "sulfur", "mercury", or "salt"
            string[] vulnerabilities = { "sulfur", "mercury", "salt" };
            vulnerableElement = vulnerabilities[Random.Range(0, vulnerabilities.Length)];

            // Same color mummies: backup and do not apply initial vulnerability tint!
            BackupOriginalColors();
            CreateHealthBar();

            // PERFORMANCE: Cache renderer array and MPB once at spawn instead of
            // allocating on every elemental hit.
            if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
            if (cachedMPB == null)       cachedMPB = new MaterialPropertyBlock();

            // ── NavMesh snap: ensure agent starts ON the nav mesh ──
            // If spawned slightly off-mesh (floating, on steep slope), SetDestination silently
            // fails and the mummy stands frozen. Snap to nearest valid surface within 5m.
            StartCoroutine(SnapToNavMeshDelayed());
        }

        private void OnEnable()
        {
            // Reset state for object pooling
            isDead = false;
            deathTimer = 0f;
            currentHealth = maxHealth;
            isStunned = false;
            isSlowed = false;
            hasWanderTarget = false;
            pathfindCooldown = 0f;
            combatMusicTriggered = false;
            stuckTimer = 0f;
            
            if (agent != null)
            {
                agent.enabled = true;
                agent.speed = baseSpeed;
                agent.stoppingDistance = attackDistance;
            }

            var colliders = GetComponents<Collider>();
            foreach (var c in colliders) c.enabled = true;
            var childColliders = GetComponentsInChildren<Collider>();
            foreach (var c in childColliders) c.enabled = true;

            string[] vulnerabilities = { "sulfur", "mercury", "salt" };
            vulnerableElement = vulnerabilities[Random.Range(0, vulnerabilities.Length)];

            RestoreColors();
            if (healthBarObj == null && sharedHealthBarSprite != null)
            {
                CreateHealthBar();
            }
            
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SnapToNavMeshDelayed());
            }
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
                if (deathTimer > 4f) {
                    if (onReleaseToPool != null) {
                        onReleaseToPool(gameObject);
                    } else {
                        Destroy(gameObject);
                    }
                }
                return;
            }

            // PERFORMANCE: Frame throttle for distant mummies.
            // Mummies >40m away only run full AI logic every 3rd frame, staggered by mummyId.
            // The player never notices at that distance — pathfinding is already throttled at 0.2–0.4s.
            // This spreads CPU load across frames rather than spiking every frame.
            if (player != null)
            {
                float sqrDistCheck = (transform.position - player.position).sqrMagnitude;
                if (sqrDistCheck > 1600f && (Time.frameCount % 3) != (mummyId % 3))
                    return;
            }

            if (player == null) {
                SetAnimSpeed(0f);
                timer += Time.deltaTime;
                if (timer >= checkInterval) { FindPlayer(); timer = 0; }
                return;
            }

            // Stuck detection: if we are supposed to move but haven't changed position much
            if ((transform.position - lastPos).sqrMagnitude < 0.0025f && !isStunned && !isDead && agent.hasPath)
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
            if (residueTimer > 0f)
            {
                residueTimer -= Time.deltaTime;
                if (residueTimer <= 0f)
                {
                    activeResidue = AlchemicalResidue.None;
                    if (!isSlowed && !isStunned)
                    {
                        RestoreColors();
                    }
                }
            }

            Vector3 currentTargetPos = player.position;
            float currentSpeed = baseSpeed;

            // PERFORMANCE: Compute player distance ONCE using sqrMagnitude (no sqrt).
            // Previously this was called 3 separate times (lines ~364, ~444, ~500)
            // with identical inputs — 60 wasted sqrt() calls per second per mummy.
            float sqrDistToPlayer  = (transform.position - player.position).sqrMagnitude;
            float distanceToPlayer = Mathf.Sqrt(sqrDistToPlayer); // Single sqrt, reused everywhere

            // ── Priority 1: HiveMind tactical target ──
            // API "Standard Patrol/idle" fallback returns target = mummy's own coords,
            // so we reject any target within 3m of self (raised from 1.5m).
            // Use sqrMagnitude to avoid sqrt on the tactical check too.
            bool hasMeaningfulTactical = hasTacticalTarget &&
                                         (tacticalTarget - transform.position).sqrMagnitude > 9f;

            if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null && TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.hasKey)
            {
                // ── Priority 0: Player has papyrus — chase relentlessly at 2x base speed! ──
                if (player != null) currentTargetPos = player.position;
                currentSpeed = baseSpeed * 2f;
            }
            else if (hasMeaningfulTactical && distanceToPlayer > 4.0f)
            {
                // ── Priority 1: HiveMind tactical instruction ──
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
                        if ((transform.position - wanderTarget).sqrMagnitude < 4f)
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
            // PERFORMANCE: Reuse distanceToPlayer computed at top of Update — no redundant sqrt.
            if (!isStunned && !isDead && player != null)
            {
                if (distanceToPlayer >= shootMinRange && distanceToPlayer <= shootMaxRange && (Time.time - lastShootTime) >= shootCooldown)
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
                if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null && TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.hasKey)
                {
                    agent.acceleration = 40f;
                    agent.angularSpeed = 360f;
                }
                else
                {
                    agent.acceleration = 12f;
                    agent.angularSpeed = 240f;
                }
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

            // Dynamically switch to combat music when mummy closes within 20 units
            // PERFORMANCE: 20^2 = 400, compare sqrDist to avoid sqrt.
            if (!combatMusicTriggered && sqrDistToPlayer < 400f)
            {
                combatMusicTriggered = true;
                TheAlchemistsCrypt.Gameplay.AudioManager.PlayCombatTheme();
            }

            float vel = agent.velocity.magnitude;
            
            // PERFORMANCE: Reuse distanceToPlayer — third usage in this Update(), zero extra cost.
            // (Previously a new Vector3.Distance call was made here, identical to lines ~364 and ~444.)
            // distanceToPlayer is already computed at the top of Update().

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
            else if (distanceToPlayer <= attackDistance) { // Reuse pre-computed distance
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
            // PERFORMANCE: Strip debug log from production builds — string.Format has measurable
            // cost when called on every hit across 20 mummies.
#if UNITY_EDITOR
            Debug.Log($"Zombie took {damage} damage. Health: {currentHealth}/{maxHealth}");
#endif
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            if (healthBarObj != null) Destroy(healthBarObj);
            
            if (TheAlchemistsCrypt.Gameplay.EscapeManager.Instance != null) {
                TheAlchemistsCrypt.Gameplay.EscapeManager.Instance.AddKill();
            }
            
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

            // Trigger decoupled death event
            TheAlchemistsCrypt.Core.EventManager.Trigger(new TheAlchemistsCrypt.Core.EnemyDeathEvent
            {
                EnemyObject = gameObject,
                Position = transform.position
            });

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
                        FlashEyesRed();
                    }
                }
            }
        }

        private void FlashEyesRed()
        {
            if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
            if (cachedMPB == null) cachedMPB = new MaterialPropertyBlock();
            
            System.Collections.Generic.List<Renderer> targets = new System.Collections.Generic.List<Renderer>();
            foreach (var r in cachedRenderers)
            {
                if (r == null) continue;
                string lowerName = r.gameObject.name.ToLower();
                if (lowerName.Contains("eye") || lowerName.Contains("head") || lowerName.Contains("face"))
                {
                    targets.Add(r);
                }
            }
            if (targets.Count == 0)
            {
                targets.AddRange(cachedRenderers);
            }

            foreach (var r in targets)
            {
                if (r == null) continue;
                float emissionVal = 0f;
                DG.Tweening.Core.DOSetter<float> setter = val => {
                    emissionVal = val;
                    if (r == null) return;
                    r.GetPropertyBlock(cachedMPB);
                    cachedMPB.SetColor("_EmissionColor", new Color(1.0f, 0.0f, 0.0f) * val);
                    r.SetPropertyBlock(cachedMPB);
                };
                DG.Tweening.DOTween.To(() => emissionVal, setter, 4.0f, 0.1f)
                    .SetLoops(2, DG.Tweening.LoopType.Yoyo)
                    .OnComplete(() => {
                        if (r != null) r.SetPropertyBlock(null);
                    });
            }
        }
    }
}
