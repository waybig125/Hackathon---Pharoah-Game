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
                        Vector3 targetPos = z.transform.position;
                        if (inst.target != null && inst.target.Count >= 3)
                        {
                            targetPos = new Vector3(inst.target[0], inst.target[1], inst.target[2]);
                        }
                        z.tacticalTarget = targetPos;
                        z.tacticalSpeedMult = inst.speed_mult;
                        z.hasTacticalTarget = true;
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
