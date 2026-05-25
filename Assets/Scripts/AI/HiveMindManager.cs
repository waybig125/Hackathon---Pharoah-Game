using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TheAlchemistsCrypt.AI
{
    [Serializable]
    public class SessionMetadata
    {
        public int tick_id;
        public bool last_tactic_success;
        public float difficulty_scaling;
    }

    [Serializable]
    public class PlayerState
    {
        public List<float> pos;
        public List<float> vel;
        public string active_element;
        public int health;
        public bool is_firing;
    }

    [Serializable]
    public class MummyState
    {
        public int id;
        public List<float> pos;
        public int hp;
        public string state;
    }

    [Serializable]
    public class GameStatePayload
    {
        public string gameState;
        public SessionMetadata session_metadata;
        public PlayerState player;
        public List<MummyState> mummies;
        public bool pharaoh_active;
        public string nearby_environment;
    }

    [Serializable]
    public class MummyInstruction
    {
        public int id;
        public string action;
        public List<float> target;
        public float delay;
        public float speed_mult;
    }

    [Serializable]
    public class AgenticNegotiation
    {
        public string pharaoh_proposal;
        public string arbiter_veto;
        public string empathy_note;
        public string final_consensus;
    }

    [Serializable]
    public class HiveTacticsResponse
    {
        public string hive_tactic;
        public AgenticNegotiation agentic_negotiation;
        public string reasoning_trace;
        public string arbiter_check;
        public List<MummyInstruction> instructions;
        public string narration;
    }

    public class HiveMindManager : MonoBehaviour
    {
        [Header("Backend Connection")]
        // Updated API base to Render deployment
        [SerializeField] private string primaryEndpoint  = "https://alchemists-crypt-backend.onrender.com/api/v1/hive-mind";
        [SerializeField] private string baselineEndpoint = "https://alchemists-crypt-backend.onrender.com/api/v1/hive-mind/baseline";
        [SerializeField] private float pollInterval = 2.0f;

        private int currentTick = 0;
        private bool lastTacticSuccess = true;
        private float difficultyScaling = 1.0f;

        // ── PERFORMANCE: Cached references ──────────────────────────────────
        // Avoids FindGameObjectWithTag / FindObjectsByType / OverlapSphere allocations
        // every 2-second poll tick.
        private GameObject cachedPlayerObj;                         // Cache player GO across ticks
        private ZombieAI[] cachedZombieArray = new ZombieAI[0];    // Cache zombie list, refresh every N ticks
        private int zombieCacheRefreshInterval = 5;                 // Refresh every 5 ticks (~10 seconds)
        private readonly List<MummyState> mStates = new List<MummyState>(); // Reused list, avoids new List<> per tick
        private readonly Collider[] overlapBuffer = new Collider[64];       // Pre-alloc buffer for NonAlloc physics

        private void Start()
        {
            StartCoroutine(HiveMindLoop());
        }

        private IEnumerator HiveMindLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(pollInterval);
                yield return StartCoroutine(SendGameStateAndRoute());
            }
        }

        private IEnumerator SendGameStateAndRoute()
        {
            // ── 1. Find / cache player ────────────────────────────────────────
            // PERFORMANCE: Only call expensive Find* APIs when the cached reference is null
            // (e.g. first tick, or after scene reload). Normally returns the cached GO.
            if (cachedPlayerObj == null)
            {
                cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
                if (cachedPlayerObj == null)
                {
                    var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                    if (character != null) cachedPlayerObj = character.gameObject;
                }
            }

            GameObject playerObj = cachedPlayerObj;
            if (playerObj == null) yield break;

            // 1. Gather player state
            var pState = new PlayerState();
            pState.pos = new List<float> { playerObj.transform.position.x, playerObj.transform.position.y, playerObj.transform.position.z };
            
            var rb = playerObj.GetComponent<Rigidbody>();
            Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
            pState.vel = new List<float> { vel.x, vel.y, vel.z };

            string activeElement = "sulphur";
            var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(FindObjectsInactive.Include);
            if (focus != null)
            {
                activeElement = focus.CurrentMode.ToString().ToLower();
                if (activeElement == "sulfur") activeElement = "sulphur";
            }
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
                if (character != null)
                {
                    var weapon = character.GetEquippedWeapon();
                    if (weapon != null)
                    {
                        string wName = weapon.name.ToLower();
                        if (wName.Contains("sulfur") || wName.Contains("sulphur")) activeElement = "sulphur";
                        else if (wName.Contains("mercury")) activeElement = "mercury";
                        else if (wName.Contains("salt")) activeElement = "salt";
                    }
                }
            }
            pState.active_element = activeElement;

            int healthVal = 100;
            var health = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Player.PlayerHealth>();
            if (health != null)
            {
                healthVal = Mathf.RoundToInt(health.currentHealth);
            }
            pState.health = healthVal;
            pState.is_firing = TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsFiring;

            // ── 2. Gather mummies state ────────────────────────────────────────
            // PERFORMANCE: Refresh zombie array only every N ticks; reuse list with Clear()
            // instead of allocating a new List<MummyState> every poll.
            if (currentTick % zombieCacheRefreshInterval == 1 || cachedZombieArray.Length == 0)
            {
                cachedZombieArray = GameObject.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
            }

            mStates.Clear(); // Reuse existing list — no GC allocation
            foreach (var z in cachedZombieArray)
            {
                if (z == null || z.IsDead) continue;
                var mState = new MummyState
                {
                    id    = z.mummyId,
                    pos   = new List<float> { z.transform.position.x, z.transform.position.y, z.transform.position.z },
                    hp    = Mathf.RoundToInt(z.currentHealth),
                    state = "walk"
                };
                mStates.Add(mState);
            }

            // If no mummies left, don't ping
            if (mStates.Count == 0) yield break;

            // ── Pharaoh check ────────────────────────────────────────────────
            var pharaoh = GameObject.Find("Pharaoh_Prefab(Clone)");
            if (pharaoh == null) pharaoh = GameObject.Find("Pharaoh_Prefab");
            bool pharaoh_active_flag = (pharaoh != null);

            // ── Environment scan (NonAlloc) ───────────────────────────────────
            // PERFORMANCE: Physics.OverlapSphereNonAlloc writes into a pre-allocated buffer
            // instead of allocating a new Collider[] array on every poll tick.
            int treeCount  = 0;
            int houseCount = 0;
            int hitCount = Physics.OverlapSphereNonAlloc(playerObj.transform.position, 25f, overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                if (overlapBuffer[i] == null) continue;
                string colName = overlapBuffer[i].gameObject.name.ToLower();
                if (colName.Contains("tree") || colName.Contains("palm")) treeCount++;
                if (colName.Contains("house")) houseCount++;
            }
            string nearbyEnv = $"{treeCount} trees, {houseCount} houses";

            // ── 3. Construct GameState payload ─────────────────────────────────
            var payload = new GameStatePayload
            {
                gameState = "running",
                session_metadata = new SessionMetadata
                {
                    tick_id             = ++currentTick,
                    last_tactic_success = lastTacticSuccess,
                    difficulty_scaling  = difficultyScaling
                },
                player             = pState,
                mummies            = mStates,
                pharaoh_active     = pharaoh_active_flag,
                nearby_environment = nearbyEnv
            };

            string jsonPayload = JsonUtility.ToJson(payload);

            // 4. Send network request
            yield return StartCoroutine(PostRequest(primaryEndpoint, jsonPayload, cachedZombieArray, true));

        }

        private IEnumerator PostRequest(string endpoint, string json, ZombieAI[] zombies, bool fallbackAllowed)
        {
            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Timeout after 4 seconds to avoid blocking game loops
                request.timeout = 4;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    try
                    {
                        var response = JsonUtility.FromJson<HiveTacticsResponse>(jsonResponse);
                        if (response != null && response.instructions != null)
                        {
                            ApplyTactics(response.instructions, zombies);
                            lastTacticSuccess = true;
                            Debug.Log($"[HiveMind] Successfully received and applied tactics: {response.hive_tactic}");
                            if (!string.IsNullOrEmpty(response.narration))
                            {
                                Debug.Log($"[HiveMind Narration] {response.narration}");
                                if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
                                {
                                    TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowNarration(response.narration);
                                }
                            }

                            // Audio Tactics - Priority System
                            string tactic = response.hive_tactic != null ? response.hive_tactic.ToLower() : "";
                            string selectedVoice = null;

                            if (tactic.Contains("ambush")) selectedVoice = "Voice/vo_tactical_ambush";
                            else if (tactic.Contains("flank")) selectedVoice = "Voice/vo_tactical_flank";
                            else if (tactic.Contains("mercy")) selectedVoice = "Voice/vo_tactical_mercy";
                            else if (tactic.Contains("vision") || tactic.Contains("sight") || tactic.Contains("scan")) selectedVoice = "Voice/vo_tactical_vision";

                            // If no tactic voice, check for health/element voices
                            if (string.IsNullOrEmpty(selectedVoice))
                            {
                                int hp = 100;
                                string activeElement = "sulphur";
                                var playerObj = GameObject.FindGameObjectWithTag("Player");
                                if (playerObj == null) {
                                    var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
                                    if (character != null) playerObj = character.gameObject;
                                }
                                if (playerObj != null)
                                {
                                    var pHealth = playerObj.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
                                    if (pHealth != null) hp = Mathf.RoundToInt(pHealth.currentHealth);
                                    var w = playerObj.GetComponentInChildren<TheAlchemistsCrypt.Weapons.AlchemicalFocus>(true);
                                    if (w != null)
                                    {
                                        activeElement = w.CurrentMode.ToString().ToLower();
                                        if (activeElement == "sulfur") activeElement = "sulphur";
                                    }
                                }

                                if (hp < 25)
                                {
                                    string[] lowHpVoices = { "Voice/vo_tactical_lowhealth_01", "Voice/vo_tactical_lowhealth_02" };
                                    selectedVoice = lowHpVoices[UnityEngine.Random.Range(0, 2)];
                                }
                                else if (UnityEngine.Random.value < 0.3f) // Only play element voices 30% of the time to avoid spam
                                {
                                    if (activeElement == "sulfur" || activeElement == "sulphur")
                                    {
                                        string[] sulfurVoices = { "Voice/vo_sulfur_01", "Voice/vo_sulfur_02" };
                                        selectedVoice = sulfurVoices[UnityEngine.Random.Range(0, 2)];
                                    }
                                    else if (activeElement == "mercury")
                                    {
                                        string[] mercuryVoices = { "Voice/vo_mercury_01", "Voice/vo_mercury_02" };
                                        selectedVoice = mercuryVoices[UnityEngine.Random.Range(0, 2)];
                                    }
                                    else if (activeElement == "salt")
                                    {
                                        string[] saltVoices = { "Voice/vo_salt_01", "Voice/vo_salt_02" };
                                        selectedVoice = saltVoices[UnityEngine.Random.Range(0, 2)];
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(selectedVoice))
                            {
                                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(selectedVoice);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[HiveMind] Error parsing response JSON: {ex.Message}");
                        lastTacticSuccess = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"[HiveMind] Request failed to {endpoint}: {request.error}");
                    lastTacticSuccess = false;

                    if (fallbackAllowed)
                    {
                        Debug.Log("[HiveMind] Attempting fallback to baseline endpoint...");
                        yield return StartCoroutine(PostRequest(baselineEndpoint, json, zombies, false));
                    }
                    else
                    {
                        // If both failed, reset tactical overrides so mummies default to chasing player
                        ResetTacticalOverrides(zombies);
                    }
                }
            }
        }

        private void ApplyTactics(List<MummyInstruction> instructions, ZombieAI[] zombies)
        {
            foreach (var inst in instructions)
            {
                foreach (var z in zombies)
                {
                    if (z != null && z.mummyId == inst.id && !z.IsDead)
                    {
                        // If API returned "idle" action → clear tactical target so wander takes over.
                        // This is the "Standard Patrol" fallback response from the server.
                        string action = (inst.action ?? "").ToLower();
                        if (action == "idle" || action == "patrol" || action == "standard patrol")
                        {
                            z.hasTacticalTarget = false;
                            continue;
                        }

                        // Apply a real tactical target only if:
                        //  - target coords are provided (3 floats)
                        //  - target is NOT the mummy's own position (> 3m away)
                        if (inst.target != null && inst.target.Count >= 3)
                        {
                            Vector3 targetPos = new Vector3(inst.target[0], inst.target[1], inst.target[2]);
                            // Enforce world boundary: do not accept instructions that push mummies into the sea area
                            if (targetPos.z < -95f) targetPos.z = -95f;
                            if (Vector3.Distance(targetPos, z.transform.position) > 3f)
                            {
                                z.tacticalTarget = targetPos;
                                z.tacticalSpeedMult = Mathf.Max(inst.speed_mult, 0.5f);
                                z.hasTacticalTarget = true;
                                Debug.Log($"[HiveMind] Mummy {z.mummyId} ordered to {action} -> {targetPos}");
                            }
                            else
                            {
                                // Target is own position — API gave no real instruction
                                z.hasTacticalTarget = false;
                            }
                        }
                        else
                        {
                            // No target coordinates at all — let wander take over
                            z.hasTacticalTarget = false;
                            z.tacticalSpeedMult = Mathf.Max(inst.speed_mult, 0.5f);
                        }
                    }
                }
            }
        }

        private void ResetTacticalOverrides(ZombieAI[] zombies)
        {
            foreach (var z in zombies)
            {
                if (z != null)
                {
                    z.hasTacticalTarget = false;
                }
            }
        }
    }
}
