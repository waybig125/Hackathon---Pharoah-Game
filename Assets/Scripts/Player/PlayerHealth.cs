using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

namespace TheAlchemistsCrypt.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Effects")]
        private UnityEngine.UI.Image damageOverlay;
        private bool isDead = false;

        private void Awake()
        {
            if (maxHealth <= 0f) maxHealth = 100f;
            currentHealth = maxHealth;
        }

        private float lastShakeTime = 0f;

        private bool isLowHealthPlaying = false;
        private AudioSource lowHealthAudioSource;
        private AudioSource heartbeatAudioSource;
        private float lastReflectSoundTime = 0f;

        private void Start()
        {
            FindDamageOverlay();
            lowHealthAudioSource = gameObject.AddComponent<AudioSource>();
            lowHealthAudioSource.loop = true;
            lowHealthAudioSource.volume = 1.0f;
            AudioClip pantClip = TheAlchemistsCrypt.Gameplay.AudioManager.LoadClip("sfx/sfx_player_pant");
            if (pantClip != null) lowHealthAudioSource.clip = pantClip;

            heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
            heartbeatAudioSource.loop = true;
            heartbeatAudioSource.volume = 0.8f;
            AudioClip lowHealthClip = TheAlchemistsCrypt.Gameplay.AudioManager.LoadClip("sfx/sfx_low_health");
            if (lowHealthClip != null) heartbeatAudioSource.clip = lowHealthClip;
        }

        private void FindDamageOverlay()
        {
            var overlay = GameObject.Find("MobileHUD_Root/DamageOverlay");
            if (overlay != null) damageOverlay = overlay.GetComponent<UnityEngine.UI.Image>();
        }

        public void TakeDamage(float amount, bool isReflected = false)
        {
            if (isDead) return;

            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            
            if (!isReflected)
            {
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_player_grunt", false, 0.45f);
            }
            else
            {
                if (Time.time - lastReflectSoundTime > 0.4f)
                {
                    lastReflectSoundTime = Time.time;
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_player_grunt", false, 0.3f);
                }
            }
            
            if (damageOverlay == null) FindDamageOverlay();
            if (damageOverlay != null)
            {
                damageOverlay.DOKill();
                damageOverlay.color = isReflected ? new Color(1f, 0.2f, 0.2f, 0.35f) : new Color(1f, 1f, 1f, 0.65f);
                damageOverlay.DOFade(0f, 0.5f).SetEase(Ease.OutQuad);
            }
            
            if (!isReflected)
            {
                ShakeCamera();
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"Player healed by {amount}. Current health: {currentHealth}/{maxHealth}");
            
            // Play pickup sound on heal (shared with orbs)
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_pickup", false, 0.7f);
        }

        private void Update()
        {
            if (isDead) return;

            // Low health audio logic
            float healthPct = currentHealth / maxHealth;
            if (healthPct < 0.25f && !isLowHealthPlaying) // Lowered threshold to 25% for more tension
            {
                isLowHealthPlaying = true;
                if (lowHealthAudioSource != null && lowHealthAudioSource.clip != null) {
                    lowHealthAudioSource.volume = 0.6f;
                    lowHealthAudioSource.Play();
                }
                if (heartbeatAudioSource != null && heartbeatAudioSource.clip != null) {
                    heartbeatAudioSource.volume = 0.5f;
                    heartbeatAudioSource.Play();
                }
                // Also play tactical voice line
                TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine("Voice/vo_tactical_lowhealth_01", false);
            }
            else if (healthPct >= 0.25f && isLowHealthPlaying)
            {
                isLowHealthPlaying = false;
                if (lowHealthAudioSource != null) lowHealthAudioSource.Stop();
                if (heartbeatAudioSource != null) heartbeatAudioSource.Stop();
            }
        }

        private void Die()
        {
            isDead = true;
            if (lowHealthAudioSource != null) lowHealthAudioSource.Stop();
            if (heartbeatAudioSource != null) heartbeatAudioSource.Stop();
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            Debug.Log("Player Died! Starting death sequence...");

            // 1. Disable character input & movement
            var character = GetComponent<InfimaGames.LowPolyShooterPack.Character>();
            if (character != null)
            {
                character.enabled = false;
            }

            // 2. Cinematic Die Camera Motion (Tilt and Fall)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 initialCamPos = mainCam.transform.localPosition;
                Quaternion initialCamRot = mainCam.transform.localRotation;
                float duration = 1.5f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = Mathf.SmoothStep(0f, 1f, t);
                    
                    // Fall down to 0.3f local height
                    mainCam.transform.localPosition = Vector3.Lerp(initialCamPos, new Vector3(initialCamPos.x, 0.3f, initialCamPos.z), t);
                    
                    // Tilt camera sideways by 75 degrees
                    mainCam.transform.localRotation = Quaternion.Slerp(initialCamRot, Quaternion.Euler(15, 0, 75), t);
                    
                    yield return null;
                }
            }

            // 3. Show full-screen Death Panel overlay through MobileHUDButtons
            if (TheAlchemistsCrypt.UI.MobileHUDButtons.Instance != null)
            {
                TheAlchemistsCrypt.UI.MobileHUDButtons.Instance.ShowDeathScreen();
            }
            else
            {
                // Fallback: enable cursor so player can at least restart manually
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private Font GetRobustFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) f = fonts[0];
            }
            return f;
        }

        private void ShakeCamera()
        {
            if (Time.time - lastShakeTime < 0.4f) return; // Cooldown to prevent overlapping shakes
            lastShakeTime = Time.time;

            // Ensure the Cinemachine Camera has an Impulse Listener attached to listen to our impulses
            var vcam = GameObject.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null && vcam.GetComponent<Unity.Cinemachine.CinemachineImpulseListener>() == null)
            {
                vcam.gameObject.AddComponent<Unity.Cinemachine.CinemachineImpulseListener>();
            }

            var source = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            }
            if (source != null)
            {
                // Generate a subtle impulse. The glitchy shake was caused by
                // manually modifying Camera.main.transform.localPosition while Cinemachine was active.
                source.GenerateImpulse(0.15f); 
            }
        }
    }

    public class DeathButtonHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action onDown;
        public System.Action onUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            onDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onUp?.Invoke();
        }
    }
}
