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

        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

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
            yield return null; // Wait for the dying mummy to deactivate/complete its state change

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
            // Clean up existing sources if they exist (to avoid duplicates in editor)
            var existing = GetComponents<AudioSource>();
            foreach (var s in existing) {
                // We keep them if they are assigned, otherwise we might double up
            }

            if (mainMusicSource == null) mainMusicSource = gameObject.AddComponent<AudioSource>();
            mainMusicSource.loop = true;
            mainMusicSource.volume = 0.35f; // Lowered from 0.6
            mainMusicSource.spatialBlend = 0f; // 2D
            
            if (combatMusicSource == null) combatMusicSource = gameObject.AddComponent<AudioSource>();
            combatMusicSource.loop = true;
            combatMusicSource.volume = 0.45f; // Lowered from 0.7
            combatMusicSource.spatialBlend = 0f;
            
            if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = 0.15f; // Lowered from 0.25
            ambientSource.spatialBlend = 0f;
            
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 0.6f; // Lowered from 0.9
            sfxSource.spatialBlend = 0f; // Global SFX
            
            if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.volume = 0.8f; // Lowered from 1.0
            voiceSource.spatialBlend = 0f; // Global Voice

            if (Application.isPlaying)
            {
                // Start base ambiance and music
                PlayMainTheme();
                AudioClip ambient = LoadClip("ambient/amb_sand_fog_loop");
                if (ambient != null)
                {
                    ambientSource.clip = ambient;
                    ambientSource.Play();
                }

                StartCoroutine(RandomTauntRoutine());
            }
        }

        public static AudioClip LoadClip(string relativePath)
        {
            if (Instance == null) return null;
            if (Instance.clipCache.ContainsKey(relativePath)) return Instance.clipCache[relativePath];
            
            AudioClip clip = Resources.Load<AudioClip>("egypt_game_audio/" + relativePath);
            if (clip != null) Instance.clipCache[relativePath] = clip;
            else Debug.LogWarning($"[AudioManager] Clip not found at Resources/egypt_game_audio/{relativePath}");
            return clip;
        }

        public static void PlayMainTheme()
        {
            if (Instance == null) return;
            if (Instance.combatMusicSource != null && Instance.combatMusicSource.isPlaying) Instance.combatMusicSource.Stop();
            if (Instance.mainMusicSource != null && !Instance.mainMusicSource.isPlaying)
            {
                Instance.mainMusicSource.clip = LoadClip("music/bgm_tomb_main");
                if (Instance.mainMusicSource.clip != null) Instance.mainMusicSource.Play();
            }
        }

        public static void PlayCombatTheme()
        {
            if (Instance == null) return;
            if (Instance.mainMusicSource != null && Instance.mainMusicSource.isPlaying) Instance.mainMusicSource.Stop();
            if (Instance.combatMusicSource != null && !Instance.combatMusicSource.isPlaying)
            {
                Instance.combatMusicSource.clip = LoadClip("music/bgm_tomb_combat");
                if (Instance.combatMusicSource.clip != null) Instance.combatMusicSource.Play();
            }
        }

        private static Dictionary<string, float> sfxThrottles = new Dictionary<string, float>();

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

            Instance.sfxSource.PlayOneShot(clip, volumeScale);
        }

        public static void PlayVoiceLine(string clipPath, bool interrupt = true)
        {
            if (Instance == null) return;
            
            // If we shouldn't interrupt and something is playing, skip
            if (!interrupt && Instance.voiceSource.isPlaying) return;

            AudioClip clip = LoadClip(clipPath);
            if (clip == null) return;

            Instance.voiceSource.Stop();
            Instance.voiceSource.clip = clip;
            Instance.voiceSource.Play();
            Debug.Log($"[AudioManager] Playing voice line: {clipPath}");
        }

        private IEnumerator RandomTauntRoutine()
        {
            string[] taunts = new string[]
            {
                "Voice/vo_taunt_01", "Voice/vo_taunt_02", "Voice/vo_taunt_03", "Voice/vo_taunt_04",
                "Voice/vo_taunt_05", "Voice/vo_taunt_06", "Voice/vo_taunt_07", "Voice/vo_taunt_08"
            };

            while (true)
            {
                yield return new WaitForSeconds(30f);
                if (!voiceSource.isPlaying)
                {
                    string randomTaunt = taunts[Random.Range(0, taunts.Length)];
                    PlayVoiceLine(randomTaunt);
                }
            }
        }
    }
}
