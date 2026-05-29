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

        private bool isLowHealthPlaying = false;
        private AudioSource lowHealthAudioSource;
        private AudioSource heartbeatAudioSource;

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

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            
            TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_player_grunt");
            
            if (damageOverlay == null) FindDamageOverlay();
            if (damageOverlay != null)
            {
                damageOverlay.DOKill();
                damageOverlay.color = new Color(1f, 1f, 1f, 0.65f);
                damageOverlay.DOFade(0f, 0.5f).SetEase(Ease.OutQuad);
            }
            ShakeCamera();

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
        }

        private void Update()
        {
            if (isDead) return;

            // Low health audio logic
            float healthPct = currentHealth / maxHealth;
            if (healthPct < 0.3f && !isLowHealthPlaying)
            {
                isLowHealthPlaying = true;
                if (lowHealthAudioSource != null && lowHealthAudioSource.clip != null) lowHealthAudioSource.Play();
                if (heartbeatAudioSource != null && heartbeatAudioSource.clip != null) heartbeatAudioSource.Play();
            }
            else if (healthPct >= 0.3f && isLowHealthPlaying)
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
            var source = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();
            }
            if (source != null)
            {
                source.GenerateImpulse(0.4f);
            }
            StartCoroutine(ShakeTransformCoroutine());
        }

        private IEnumerator ShakeTransformCoroutine()
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;
            Vector3 originalLocalPos = cam.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.2f;
            float magnitude = 0.15f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                cam.transform.localPosition = originalLocalPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cam.transform.localPosition = originalLocalPos;
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
