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
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Transform firePoint;

        [Header("Pool Tags")]
        [SerializeField] private string sulfurPoolTag = "SulfurProjectile";
        [SerializeField] private string mercuryPoolTag = "MercuryProjectile";
        [SerializeField] private string saltPoolTag = "SaltProjectile";

        private float nextFireTime;

        private void Update()
        {
            HandleShooting();
            HandleModeSwitch();
        }

        private void HandleShooting()
        {
            if (MobileInputManager.Instance.IsFiring && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }

        private void HandleModeSwitch()
        {
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

            ObjectPooler.Instance.SpawnFromPool(tag, firePoint.position, firePoint.rotation);
        }
    }
}
