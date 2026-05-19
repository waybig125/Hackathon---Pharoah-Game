using System;
using System.Collections;
using UnityEngine;
using TheAlchemistsCrypt.Input;
using TheAlchemistsCrypt.Core;

namespace TheAlchemistsCrypt.Weapons
{
    public class AlchemicalFocus : MonoBehaviour
    {
        public enum FireMode { Sulfur, Mercury, Salt }

        [Header("Weapon Settings")]
        [SerializeField] private FireMode currentMode = FireMode.Sulfur;
        public FireMode CurrentMode => currentMode;
        [SerializeField] private float fireRate = 0.25f; // Faster firing for better alchemical gameplay feel
        [SerializeField] private Transform firePoint;

        [Header("Ammo Settings")]
        [SerializeField] private int maxAmmo = 30;
        public int MaxAmmo => maxAmmo;
        [SerializeField] private int currentAmmo = 30;
        public int CurrentAmmo => currentAmmo;
        [SerializeField] private float reloadDuration = 1.2f;

        [Header("Pool Tags")]
        [SerializeField] private string sulfurPoolTag = "SulfurProjectile";
        [SerializeField] private string mercuryPoolTag = "MercuryProjectile";
        [SerializeField] private string saltPoolTag = "SaltProjectile";

        private float nextFireTime;
        private bool isReloading = false;
        public bool IsReloading => isReloading;

        private FireMode lastMode;

        private void OnEnable()
        {
            isReloading = false;
        }

        private void OnDisable()
        {
            isReloading = false;
        }

        private void Start()
        {
            currentAmmo = maxAmmo;
            lastMode = currentMode;
            UpdateWeaponColor();
        }

        private void Update()
        {
            HandleShooting();
            HandleModeSwitch();

            if (currentMode != lastMode)
            {
                lastMode = currentMode;
                UpdateWeaponColor();
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_element_switch");
            }
        }

        private void UpdateWeaponColor()
        {
            Color glowColor = Color.red; // default Sulfur
            switch (currentMode)
            {
                case FireMode.Sulfur: glowColor = new Color(1.0f, 0.3f, 0.0f); break; // Fiery orange
                case FireMode.Mercury: glowColor = new Color(0.0f, 0.9f, 1.0f); break; // Cyan/blue
                case FireMode.Salt: glowColor = new Color(0.8f, 0.2f, 1.0f); break; // Majestic royal violet/purple
            }

            // Find all renderers in children to apply element coloring
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", glowColor);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", glowColor);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.SetColor("_EmissionColor", glowColor * 2.5f); // Make it glow!
                        m.EnableKeyword("_EMISSION");
                    }
                }
            }

            // Add or update a dynamic glowing Point Light on the firePoint to illuminate the surroundings with the element's color!
            if (firePoint != null)
            {
                Light l = firePoint.GetComponent<Light>();
                if (l == null) l = firePoint.gameObject.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = glowColor;
                l.intensity = 8.0f;
                l.range = 5.0f;
            }
        }

        private void HandleShooting()
        {
            if (isReloading) return;

            // Trigger reload if out of ammo or reload input is pressed using modern Input System API
            bool reloadKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            if (currentAmmo <= 0 || (MobileInputManager.Instance != null && MobileInputManager.Instance.IsReloading) || reloadKeyPressed)
            {
                StartCoroutine(ReloadCoroutine());
                return;
            }

            bool isFiringInput = (MobileInputManager.Instance != null && MobileInputManager.Instance.IsFiring) || 
                                 (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed);
            if (isFiringInput && Time.time >= nextFireTime)
            {
                if (currentAmmo > 0)
                {
                    Shoot();
                    currentAmmo--;
                    nextFireTime = Time.time + fireRate;
                }
            }
        }

        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            if (MobileInputManager.Instance != null) MobileInputManager.Instance.IsReloading = false;
            
            yield return new WaitForSeconds(reloadDuration);
            
            currentAmmo = maxAmmo;
            isReloading = false;
            Debug.Log("Alchemical Focus Reloaded!");
        }

        private void HandleModeSwitch()
        {
            // Mobile Swap Logic
            if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsSwappingWeapon)
            {
                int next = ((int)currentMode + 1) % 3;
                currentMode = (FireMode)next;
                MobileInputManager.Instance.IsSwappingWeapon = false; // Reset the trigger
                Debug.Log($"Mobile: Switched to {currentMode}");
            }

            // Simple keyboard switch for desktop testing using modern Input System APIs
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit1Key.wasPressedThisFrame) currentMode = FireMode.Sulfur;
                if (kb.digit2Key.wasPressedThisFrame) currentMode = FireMode.Mercury;
                if (kb.digit3Key.wasPressedThisFrame) currentMode = FireMode.Salt;
            }
        }

        public void SetMode(FireMode mode)
        {
            currentMode = mode;
        }

        private void Shoot()
        {
            string tag = sulfurPoolTag;
            switch (currentMode)
            {
                case FireMode.Mercury: tag = mercuryPoolTag; break;
                case FireMode.Salt: tag = saltPoolTag; break;
            }

            switch (currentMode)
            {
                case FireMode.Sulfur: TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_sulfur_shot"); break;
                case FireMode.Mercury: TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_mercury_shot"); break;
                case FireMode.Salt: TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_salt_shot"); break;
            }

            if (ObjectPooler.Instance != null && firePoint != null)
            {
                GameObject spawned = ObjectPooler.Instance.SpawnFromPool(tag, firePoint.position, firePoint.rotation);
                if (spawned != null)
                {
                    Projectile proj = spawned.GetComponent<Projectile>();
                    if (proj == null) proj = spawned.GetComponentInChildren<Projectile>();
                    if (proj != null)
                    {
                        proj.element = (Projectile.ElementType)currentMode;
                    }
                }
            }
        }
    }
}
