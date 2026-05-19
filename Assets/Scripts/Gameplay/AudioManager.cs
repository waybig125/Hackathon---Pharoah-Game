using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheAlchemistsCrypt.Gameplay
{
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
                DontDestroyOnLoad(gameObject);
                InitializeSources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSources()
        {
            mainMusicSource = gameObject.AddComponent<AudioSource>();
            mainMusicSource.loop = true;
            mainMusicSource.volume = 0.6f;
            
            combatMusicSource = gameObject.AddComponent<AudioSource>();
            combatMusicSource.loop = true;
            combatMusicSource.volume = 0.7f;
            
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = 0.25f;
            
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 0.9f;
            
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.volume = 1.0f;

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
            if (Instance.combatMusicSource.isPlaying) Instance.combatMusicSource.Stop();
            if (!Instance.mainMusicSource.isPlaying)
            {
                Instance.mainMusicSource.clip = LoadClip("music/bgm_tomb_main");
                if (Instance.mainMusicSource.clip != null) Instance.mainMusicSource.Play();
            }
        }

        public static void PlayCombatTheme()
        {
            if (Instance == null) return;
            if (Instance.mainMusicSource.isPlaying) Instance.mainMusicSource.Stop();
            if (!Instance.combatMusicSource.isPlaying)
            {
                Instance.combatMusicSource.clip = LoadClip("music/bgm_tomb_combat");
                if (Instance.combatMusicSource.clip != null) Instance.combatMusicSource.Play();
            }
        }

        public static void PlaySFX(string clipPath, bool loop = false, float volumeScale = 1.0f)
        {
            if (Instance == null) return;
            AudioClip clip = LoadClip(clipPath);
            if (clip == null) return;

            if (loop)
            {
                // Create a temporary AudioSource for looped SFX if needed, 
                // but usually better to manage loops on specific objects.
                // We'll just play one shot on the main SFX source for simple usage.
                Instance.sfxSource.PlayOneShot(clip, volumeScale); 
            }
            else
            {
                Instance.sfxSource.PlayOneShot(clip, volumeScale);
            }
        }

        public static void PlayVoiceLine(string clipPath)
        {
            if (Instance == null) return;
            AudioClip clip = LoadClip(clipPath);
            if (clip == null) return;

            Instance.voiceSource.Stop();
            Instance.voiceSource.clip = clip;
            Instance.voiceSource.Play();
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
