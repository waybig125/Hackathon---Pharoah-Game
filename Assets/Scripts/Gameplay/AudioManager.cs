using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheAlchemistsCrypt.Gameplay
{
    [ExecuteAlways]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource mainMusicSource;
        public AudioSource combatMusicSource;
        public AudioSource ambientSource;
        public AudioSource sfxSource;
        public AudioSource voiceSource;

        [Header("Mix Volumes")]
        [Range(0f, 1f)] public float ambientVolume = 0.05f;
        [Range(0f, 1f)] public float musicVolume = 0.28f;
        [Range(0f, 1f)] public float voiceVolume = 0.95f;

        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private List<int> recentTaunts = new List<int>();
        private int activeMonsterVocalizations = 0;
        private Coroutine musicCrossfade;

        private static HashSet<string> playedElementLines = new HashSet<string>();
        private static float lastElementVoiceTime = 0f;
        private static float lastTacticalVoiceTime = 0f;
        private static float lastLowHealthVoiceTime = 0f;
        private static float lastVoicePlayTime = 0f;
        private static string lastPlayedTacticalVoice = "";

        private const float ElementVoiceCooldown = 20f;
        private const float TacticalVoiceCooldown = 45f;
        private const float LowHealthVoiceCooldown = 45f;
        private const float GlobalVoiceCooldown = 6f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
                InitializeSources();
            }
            else if (Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Instance == null) Instance = this;
            if (voiceSource == null) InitializeSources();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                TheAlchemistsCrypt.Core.EventManager.Subscribe<TheAlchemistsCrypt.Core.EnemyDeathEvent>(OnEnemyDeath);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                TheAlchemistsCrypt.Core.EventManager.Unsubscribe<TheAlchemistsCrypt.Core.EnemyDeathEvent>(OnEnemyDeath);
            }
        }

        private void OnEnemyDeath(TheAlchemistsCrypt.Core.EnemyDeathEvent evt)
        {
            StartCoroutine(CheckNearbyMummiesDeferred());
        }

        private IEnumerator CheckNearbyMummiesDeferred()
        {
            yield return null; 

            var allMummies = GameObject.FindObjectsByType<TheAlchemistsCrypt.AI.ZombieAI>(FindObjectsInactive.Exclude);
            bool anyNearby = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.gameObject;
            }

            if (player != null)
            {
                foreach (var m in allMummies)
                {
                    if (m != null && !m.IsDead &&
                        (m.transform.position - player.transform.position).sqrMagnitude < 625f)
                    {
                        anyNearby = true;
                        break;
                    }
                }
            }
            if (!anyNearby)
            {
                PlayMainTheme();
            }
        }

        private void InitializeSources()
        {
            playedElementLines.Clear();
            lastElementVoiceTime = 0f;
            lastTacticalVoiceTime = 0f;
            lastLowHealthVoiceTime = 0f;
            lastVoicePlayTime = 0f;
            lastPlayedTacticalVoice = "";

            if (mainMusicSource == null) mainMusicSource = gameObject.AddComponent<AudioSource>();
            mainMusicSource.loop = true;
            mainMusicSource.volume = 0f; 
            mainMusicSource.spatialBlend = 0f; 
            
            if (combatMusicSource == null) combatMusicSource = gameObject.AddComponent<AudioSource>();
            combatMusicSource.loop = true;
            combatMusicSource.volume = 0f;
            combatMusicSource.spatialBlend = 0f;
            
            if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = ambientVolume;
            ambientSource.spatialBlend = 0f;
            
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 0.5f;
            sfxSource.spatialBlend = 0f;
            
            if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.volume = voiceVolume;
            voiceSource.spatialBlend = 0f;

            if (Application.isPlaying)
            {
                AudioClip ambient = LoadClip("ambient/amb_sand_fog_loop");
                if (ambient != null) { ambientSource.clip = ambient; ambientSource.Play(); }

                PlayMainTheme();
                StartCoroutine(RandomTauntRoutine());
            }
        }

        public static void PlayMainTheme() { if (Instance != null) Instance.CrossfadeTo(Instance.mainMusicSource, "music/bgm_tomb_main"); }
        public static void PlayCombatTheme() { if (Instance != null) Instance.CrossfadeTo(Instance.combatMusicSource, "music/bgm_tomb_combat"); }

        private void CrossfadeTo(AudioSource targetSource, string clipPath)
        {
            if (musicCrossfade != null) StopCoroutine(musicCrossfade);
            musicCrossfade = StartCoroutine(CrossfadeRoutine(targetSource, clipPath));
        }

        private IEnumerator CrossfadeRoutine(AudioSource target, string path)
        {
            if (target.clip == null) target.clip = LoadClip(path);
            if (!target.isPlaying) target.Play();

            AudioSource other = (target == mainMusicSource) ? combatMusicSource : mainMusicSource;
            float duration = 2.5f;
            float elapsed = 0f;
            float startOtherVol = other.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.volume = Mathf.Lerp(0f, musicVolume, t);
                other.volume = Mathf.Lerp(startOtherVol, 0f, t);
                yield return null;
            }
            other.Stop();
        }

        public static bool RequestMonsterVocalization()
        {
            if (Instance == null) return true;
            if (Instance.activeMonsterVocalizations >= 2) return false;
            Instance.activeMonsterVocalizations++;
            Instance.StartCoroutine(Instance.ReleaseVocalizationSlot());
            return true;
        }

        private IEnumerator ReleaseVocalizationSlot()
        {
            yield return new WaitForSeconds(1.8f);
            activeMonsterVocalizations = Mathf.Max(0, activeMonsterVocalizations - 1);
        }

        public static AudioClip LoadClip(string relativePath)
        {
            if (Instance == null) return null;
            if (Instance.clipCache.ContainsKey(relativePath)) return Instance.clipCache[relativePath];
            
            AudioClip clip = Resources.Load<AudioClip>("egypt_game_audio/" + relativePath);
            if (clip != null) Instance.clipCache[relativePath] = clip;
            return clip;
        }

        public static void PlaySFX(string clipPath, bool loop = false, float volumeScale = 1.0f, float throttleTime = 0f)
        {
            if (Instance == null) return;
            if (throttleTime > 0f)
            {
                if (sfxThrottles.TryGetValue(clipPath, out float lastTime))
                {
                    if (Time.time < lastTime + throttleTime) return;
                }
                sfxThrottles[clipPath] = Time.time;
            }
            AudioClip clip = LoadClip(clipPath);
            if (clip == null) return;
            Instance.sfxSource.PlayOneShot(clip, volumeScale * 0.7f); // Global SFX pad
        }

        private static Dictionary<string, float> sfxThrottles = new Dictionary<string, float>();

        public static void OnWeaponSwitched()
        {
            playedElementLines.Clear();
            lastElementVoiceTime = 0f;
            lastVoicePlayTime = 0f;
        }

        public static void PlayVoiceLine(string clipPath, bool interrupt = true, bool bypassCooldown = false)
        {
            if (Instance == null) return;

            if (Application.isPlaying && !bypassCooldown)
            {
                float currentTime = Time.time;

                // 1. Global cooldown check (prevent rapid back-to-back voice lines)
                if (currentTime - lastVoicePlayTime < GlobalVoiceCooldown)
                {
                    return;
                }

                // 2. Classify the voice line
                bool isLowHealth = clipPath.Contains("vo_tactical_lowhealth");
                bool isTactical = clipPath.Contains("vo_tactical");
                bool isElement = clipPath.Contains("vo_sulfur") || clipPath.Contains("vo_mercury") || clipPath.Contains("vo_salt") || clipPath.Contains("vo_taunt_08");

                if (isLowHealth)
                {
                    // Enforce low health voice cooldown
                    if (currentTime - lastLowHealthVoiceTime < LowHealthVoiceCooldown)
                    {
                        return;
                    }
                }
                else if (isTactical)
                {
                    // Enforce tactical voice cooldown
                    // If it's a new tactic, we allow it (subject to global cooldown), but if it's the same tactic, we enforce cooldown.
                    if (clipPath == lastPlayedTacticalVoice)
                    {
                        if (currentTime - lastTacticalVoiceTime < TacticalVoiceCooldown)
                        {
                            return;
                        }
                    }
                }
                else if (isElement)
                {
                    // Enforce element voice cooldown
                    if (currentTime - lastElementVoiceTime < ElementVoiceCooldown)
                    {
                        return;
                    }

                    // Enforce no-repeat rule for element lines
                    if (playedElementLines.Contains(clipPath))
                    {
                        return;
                    }
                }

                // If not interrupting and currently playing, do not play
                if (!interrupt && Instance.voiceSource.isPlaying) return;

                // Update trackers
                if (isLowHealth)
                {
                    lastLowHealthVoiceTime = currentTime;
                }
                else if (isTactical)
                {
                    lastTacticalVoiceTime = currentTime;
                    lastPlayedTacticalVoice = clipPath;
                }
                else if (isElement)
                {
                    lastElementVoiceTime = currentTime;
                    playedElementLines.Add(clipPath);
                }

                lastVoicePlayTime = currentTime;
            }
            else
            {
                // If not interrupting and currently playing, do not play
                if (!interrupt && Instance.voiceSource.isPlaying) return;

                if (Application.isPlaying)
                {
                    lastVoicePlayTime = Time.time;
                }
            }

            AudioClip clip = LoadClip(clipPath);
            if (clip == null) return;
            Instance.voiceSource.Stop();
            Instance.voiceSource.clip = clip;
            Instance.voiceSource.volume = Instance.voiceVolume;
            Instance.voiceSource.Play();
        }

        private IEnumerator RandomTauntRoutine()
        {
            string[] taunts = { "Voice/vo_taunt_01", "Voice/vo_taunt_02", "Voice/vo_taunt_03", "Voice/vo_taunt_04", 
                                "Voice/vo_taunt_05", "Voice/vo_taunt_06", "Voice/vo_taunt_07", "Voice/vo_taunt_08" };
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(35f, 60f));

                // 1. Check if player has escaped
                if (EscapeManager.Instance != null && EscapeManager.Instance.hasEscaped)
                {
                    Debug.Log("[AudioManager] Player has escaped. Stopping random taunts.");
                    yield break;
                }

                if (!voiceSource.isPlaying)
                {
                    // 2. Check for nearby attacking zombies
                    if (AnyAttackingMummyNearby())
                    {
                        int idx;
                        do { idx = Random.Range(0, taunts.Length); } while (recentTaunts.Contains(idx));
                        recentTaunts.Add(idx);
                        if (recentTaunts.Count > 4) recentTaunts.RemoveAt(0);
                        PlayVoiceLine(taunts[idx]);
                    }
                }
            }
        }

        private bool AnyAttackingMummyNearby()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) {
                var character = GameObject.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>();
                if (character != null) player = character.gameObject;
            }
            if (player == null) return false;

            var allMummies = GameObject.FindObjectsByType<TheAlchemistsCrypt.AI.ZombieAI>(FindObjectsInactive.Exclude);
            foreach (var m in allMummies)
            {
                if (m != null && !m.IsDead)
                {
                    float sqrDist = (m.transform.position - player.transform.position).sqrMagnitude;
                    // Proximity check (20 units) and not stunned
                    if (sqrDist < 400f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
