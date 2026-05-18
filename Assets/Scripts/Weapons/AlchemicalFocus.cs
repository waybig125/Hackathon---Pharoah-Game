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

        private void Start()
        {
            currentAmmo = maxAmmo;
        }

        private void Update()
        {
            HandleShooting();
            HandleModeSwitch();
        }

        private void HandleShooting()
        {
            if (isReloading) return;

            // Trigger reload if out of ammo or reload input is pressed
            if (currentAmmo <= 0 || (MobileInputManager.Instance != null && MobileInputManager.Instance.IsReloading) || UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(ReloadCoroutine());
                return;
            }

            if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsFiring && Time.time >= nextFireTime)
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
            if (MobileInputManager.Instance == null) return;

            // Mobile Swap Logic
            if (MobileInputManager.Instance.IsSwappingWeapon)
            {
                int next = ((int)currentMode + 1) % 3;
                currentMode = (FireMode)next;
                MobileInputManager.Instance.IsSwappingWeapon = false; // Reset the trigger
                Debug.Log($"Mobile: Switched to {currentMode}");
            }

            // Simple keyboard switch for desktop testing
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) currentMode = FireMode.Sulfur;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) currentMode = FireMode.Mercury;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) currentMode = FireMode.Salt;
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

            if (ObjectPooler.Instance != null && firePoint != null)
            {
                ObjectPooler.Instance.SpawnFromPool(tag, firePoint.position, firePoint.rotation);
            }
        }
    }
}
