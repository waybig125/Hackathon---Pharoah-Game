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
        [SerializeField] private string primaryEndpoint = "https://alchemists-crypt-ai-production.up.railway.app/api/v1/hive-mind";
        [SerializeField] private string baselineEndpoint = "https://alchemists-crypt-ai-production.up.railway.app/api/v1/hive-mind/baseline";
        [SerializeField] private float pollInterval = 2.0f;

        private int currentTick = 0;
        private bool lastTacticSuccess = true;
        private float difficultyScaling = 1.0f;

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
            // Find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) playerObj = character.gameObject;
            }

            if (playerObj == null) yield break;

            // 1. Gather player state
            var pState = new PlayerState();
            pState.pos = new List<float> { playerObj.transform.position.x, playerObj.transform.position.y, playerObj.transform.position.z };
            
            var rb = playerObj.GetComponent<Rigidbody>();
            Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
            pState.vel = new List<float> { vel.x, vel.y, vel.z };

            string activeElement = "sulfur";
            var focus = GameObject.FindAnyObjectByType<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
            if (focus != null)
            {
                activeElement = focus.CurrentMode.ToString().ToLower();
            }
            else
            {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null)
                {
                    var weapon = character.GetEquippedWeapon();
                    if (weapon != null)
                    {
                        string wName = weapon.name.ToLower();
                        if (wName.Contains("sulfur")) activeElement = "sulfur";
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

            // 2. Gather mummies state
            var mStates = new List<MummyState>();
            var zombies = GameObject.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude);
            foreach (var z in zombies)
            {
                if (z == null || z.IsDead) continue;

                var mState = new MummyState();
                mState.id = z.mummyId;
                mState.pos = new List<float> { z.transform.position.x, z.transform.position.y, z.transform.position.z };
                mState.hp = Mathf.RoundToInt(z.currentHealth);
                mState.state = "walk"; // default active movement state
                mStates.Add(mState);
            }

            // If no mummies left, don't ping
            if (mStates.Count == 0) yield break;

            // 3. Construct GameState payload
            var payload = new GameStatePayload();
            payload.gameState = "running";
            
            payload.session_metadata = new SessionMetadata();
            payload.session_metadata.tick_id = ++currentTick;
            payload.session_metadata.last_tactic_success = lastTacticSuccess;
            payload.session_metadata.difficulty_scaling = difficultyScaling;
            
            payload.player = pState;
            payload.mummies = mStates;

            // Pharaoh Active flag
            var pharaoh = GameObject.Find("Pharaoh_Prefab(Clone)");
            if (pharaoh == null) pharaoh = GameObject.Find("Pharaoh_Prefab");
            payload.pharaoh_active = (pharaoh != null);

            // Check Environment using OverlapSphere
            int treeCount = 0;
            int houseCount = 0;
            var colliders = Physics.OverlapSphere(playerObj.transform.position, 25f);
            foreach (var col in colliders)
            {
                if (col.gameObject.name.ToLower().Contains("tree") || col.gameObject.name.ToLower().Contains("palm")) treeCount++;
                if (col.gameObject.name.ToLower().Contains("house")) houseCount++;
            }
            payload.nearby_environment = $"{treeCount} trees, {houseCount} houses";

            string jsonPayload = JsonUtility.ToJson(payload);

            // 4. Send network request
            yield return StartCoroutine(PostRequest(primaryEndpoint, jsonPayload, zombies, true));
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

                            // Audio Tactics
                            string tactic = response.hive_tactic != null ? response.hive_tactic.ToLower() : "";
                            if (tactic.Contains("ambush")) TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_ambush");
                            else if (tactic.Contains("flank")) TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_flank");
                            else if (tactic.Contains("mercy")) TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_mercy");
                            else if (tactic.Contains("vision") || tactic.Contains("sight") || tactic.Contains("scan")) TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_vision");

                            // Health/Element specific voices
                            int hp = 100;
                            string activeElement = "sulfur";
                            var playerObj = GameObject.FindGameObjectWithTag("Player");
                            if (playerObj == null) {
                                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                                if (character != null) playerObj = character.gameObject;
                            }
                            if (playerObj != null)
                            {
                                var pHealth = playerObj.GetComponent<TheAlchemistsCrypt.Player.PlayerHealth>();
                                if (pHealth != null) hp = Mathf.RoundToInt(pHealth.currentHealth);
                                var w = playerObj.GetComponentInChildren<TheAlchemistsCrypt.Weapons.AlchemicalFocus>();
                                if (w != null) activeElement = w.CurrentMode.ToString().ToLower();
                            }

                            if (hp < 25)
                            {
                                string[] lowHpVoices = { "Voice/vo_tactical_lowhealth_01", "Voice/vo_tactical_lowhealth_02" };
                                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(lowHpVoices[UnityEngine.Random.Range(0, 2)]);
                            }
                            else if (activeElement == "sulfur")
                            {
                                string[] sulfurVoices = { "Voice/vo_sulfur_01", "Voice/vo_sulfur_02" };
                                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(sulfurVoices[UnityEngine.Random.Range(0, 2)]);
                            }
                            else if (activeElement == "mercury")
                            {
                                string[] mercuryVoices = { "Voice/vo_mercury_01", "Voice/vo_mercury_02" };
                                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(mercuryVoices[UnityEngine.Random.Range(0, 2)]);
                            }
                            else if (activeElement == "salt")
                            {
                                string[] saltVoices = { "Voice/vo_salt_01", "Voice/vo_salt_02" };
                                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(saltVoices[UnityEngine.Random.Range(0, 2)]);
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
                        // Only apply a tactical target if the API returned real coordinates
                        // (target list has 3 elements AND the position is not the mummy's own position)
                        if (inst.target != null && inst.target.Count >= 3)
                        {
                            Vector3 targetPos = new Vector3(inst.target[0], inst.target[1], inst.target[2]);
                            // Skip if API returned the mummy's own position (default/null target)
                            if (Vector3.Distance(targetPos, z.transform.position) > 1.5f)
                            {
                                z.tacticalTarget = targetPos;
                                z.tacticalSpeedMult = Mathf.Max(inst.speed_mult, 0.5f);
                                z.hasTacticalTarget = true;
                            }
                        }
                        else
                        {
                            // No target from API — let wander behavior take over
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
