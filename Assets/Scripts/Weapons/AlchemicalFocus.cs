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
            UpdateWeaponColor();

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("--- RUNTIME LIGHT CHECK ---");
                var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
                foreach (var l in lights)
                {
                    var path = l.name;
                    var t = l.transform.parent;
                    while (t != null)
                    {
                        path = t.name + "/" + path;
                        t = t.parent;
                    }
                    sb.AppendLine($"Light Path: {path} | Type: {l.type} | Range: {l.range} | Intensity: {l.intensity} | Enabled: {l.enabled} | Color: {l.color} | Position: {l.transform.position}");
                }

                sb.AppendLine("\n--- PLAYER HIERARCHY ---");
                var player = GameObject.Find("Player");
                if (player == null)
                {
                    var character = UnityEngine.Object.FindAnyObjectByType<InfimaGames.LowPolyShooterPack.Character>(FindObjectsInactive.Include);
                    if (character != null) player = character.gameObject;
                }

                if (player != null)
                {
                    sb.AppendLine("Player Name: " + player.name);
                    DumpTransform(player.transform, "", sb);
                }
                else
                {
                    sb.AppendLine("Player not found in scene.");
                }

                System.IO.File.WriteAllText("Assets/lights_log.txt", sb.ToString());

                // Find and log TestRoot
                var tr = GameObject.Find("TestRoot");
                if (tr == null) tr = GameObject.Find("Player/TestRoot");
                if (tr == null)
                {
                    // Search all GameObjects for any containing TestRoot
                    foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                    {
                        if (go.name.Contains("TestRoot")) { tr = go; break; }
                    }
                }
                if (tr != null)
                {
                    var trSb = new System.Text.StringBuilder();
                    trSb.AppendLine("TestRoot path: " + tr.name);
                    var parent = tr.transform.parent;
                    while (parent != null)
                    {
                        trSb.AppendLine("Parent: " + parent.name);
                        parent = parent.parent;
                    }
                    trSb.AppendLine("Components on TestRoot:");
                    foreach (var c in tr.GetComponents<Component>())
                    {
                        if (c != null) trSb.AppendLine("  " + c.GetType().Name);
                    }
                    trSb.AppendLine("Children of TestRoot:");
                    foreach (Transform child in tr.transform)
                    {
                        trSb.AppendLine("  " + child.name);
                        foreach (var c in child.GetComponents<Component>())
                        {
                            if (c != null) trSb.AppendLine("    [Comp] " + c.GetType().Name);
                        }
                    }
                    System.IO.File.WriteAllText("Assets/testroot_log.txt", trSb.ToString());
                }
                else
                {
                    System.IO.File.WriteAllText("Assets/testroot_log.txt", "TestRoot not found in scene");
                }
            }
            catch (System.Exception ex)
            {
                System.IO.File.WriteAllText("Assets/lights_log_error.txt", ex.ToString());
            }
        }

        private void OnDisable()
        {
            isReloading = false;
        }

        private void Start()
        {
            currentAmmo = maxAmmo;
            lastMode = currentMode;
            ResolveFirePoint();
            SyncInventoryWeapon();
            UpdateWeaponColor();
        }

        private void Update()
        {
            ResolveFirePoint();
            HandleShooting();
            HandleModeSwitch();

            if (currentMode != lastMode)
            {
                lastMode = currentMode;
                SyncInventoryWeapon();
                UpdateWeaponColor();
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_element_switch");

                // Play voice lines immediately on element switch
                string[] voiceClips = null;
                switch (currentMode)
                {
                    case FireMode.Sulfur:
                        voiceClips = new string[] { "Voice/vo_sulfur_01", "Voice/vo_sulfur_02" };
                        break;
                    case FireMode.Mercury:
                        voiceClips = new string[] { "Voice/vo_mercury_01", "Voice/vo_mercury_02" };
                        break;
                    case FireMode.Salt:
                        voiceClips = new string[] { "Voice/vo_salt_01", "Voice/vo_salt_02" };
                        break;
                }
                if (voiceClips != null)
                {
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(voiceClips[UnityEngine.Random.Range(0, voiceClips.Length)]);
                }
            }
        }

        private void ResolveFirePoint()
        {
            if (firePoint == null || !firePoint.gameObject.activeInHierarchy)
            {
                var inventory = GetComponentInParent<InfimaGames.LowPolyShooterPack.Inventory>();
                if (inventory == null)
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player != null) inventory = player.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>();
                }
                if (inventory != null)
                {
                    var equipped = inventory.GetEquipped();
                    if (equipped != null)
                    {
                        var muzzle = equipped.transform.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Muzzle>(true);
                        if (muzzle != null)
                        {
                            firePoint = muzzle.GetSocket() != null ? muzzle.GetSocket() : muzzle.transform;
                        }
                        else
                        {
                            foreach (var t in equipped.transform.GetComponentsInChildren<Transform>(true))
                            {
                                if (t.name.Contains("Muzzle") || t.name.Contains("muzzle"))
                                {
                                    firePoint = t;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SyncInventoryWeapon()
        {
            var character = GetComponentInParent<InfimaGames.LowPolyShooterPack.Character>();
            if (character == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) character = player.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Character>();
            }
            if (character != null)
            {
                var inv = character.GetInventory();
                if (inv != null && inv.GetEquippedIndex() != (int)currentMode)
                {
                    character.EquipWeapon((int)currentMode);
                }
            }
            else
            {
                var inventory = GetComponentInParent<InfimaGames.LowPolyShooterPack.Inventory>();
                if (inventory == null)
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player != null) inventory = player.GetComponentInChildren<InfimaGames.LowPolyShooterPack.Inventory>();
                }
                if (inventory != null && inventory.GetEquippedIndex() != (int)currentMode)
                {
                    inventory.Equip((int)currentMode);
                }
            }
        }

        public void UpdateWeaponColor()
        {
            Color glowColor = Color.red; // default Sulfur
            switch (currentMode)
            {
                case FireMode.Sulfur: glowColor = new Color(1.0f, 0.55f, 0.05f); break; // Bright orange-gold
                case FireMode.Mercury: glowColor = new Color(0.0f, 0.9f, 1.0f); break; // Cyan/blue
                case FireMode.Salt: glowColor = new Color(0.8f, 0.2f, 1.0f); break; // Majestic royal violet/purple
            }

            // Add or update a dynamic glowing Point Light on the firePoint to illuminate the surroundings with the element's color!
            if (firePoint != null)
            {
                Light l = firePoint.GetComponent<Light>();
                if (l == null) l = firePoint.gameObject.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = glowColor;
                l.intensity = 8.0f;
                l.range = 8.0f;
                l.shadows = LightShadows.None; // Optimize shadows for mobile performance
                l.enabled = false; // Disabled by default to prevent permanent light below player
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

            bool isMousePressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
            if (isMousePressed && UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                isMousePressed = false;
            }

            bool isFiringInput = (MobileInputManager.Instance != null && MobileInputManager.Instance.IsFiring) || isMousePressed;
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
            // Simple keyboard switch for desktop testing using modern Input System APIs
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit1Key.wasPressedThisFrame) currentMode = FireMode.Sulfur;
                if (kb.digit2Key.wasPressedThisFrame) currentMode = FireMode.Mercury;
                if (kb.digit3Key.wasPressedThisFrame) currentMode = FireMode.Salt;
            }
        }

        public void SetMode(FireMode mode, bool initiatedByCharacter = false)
        {
            if (currentMode == mode) return;
            currentMode = mode;
            if (initiatedByCharacter)
            {
                lastMode = currentMode;
                UpdateWeaponColor();
                TheAlchemistsCrypt.Gameplay.AudioManager.PlaySFX("sfx/sfx_element_switch");

                // Play voice lines immediately on element switch
                string[] voiceClips = null;
                switch (currentMode)
                {
                    case FireMode.Sulfur:
                        voiceClips = new string[] { "Voice/vo_sulfur_01", "Voice/vo_sulfur_02" };
                        break;
                    case FireMode.Mercury:
                        voiceClips = new string[] { "Voice/vo_mercury_01", "Voice/vo_mercury_02" };
                        break;
                    case FireMode.Salt:
                        voiceClips = new string[] { "Voice/vo_salt_01", "Voice/vo_salt_02" };
                        break;
                }
                if (voiceClips != null)
                {
                    TheAlchemistsCrypt.Gameplay.AudioManager.PlayVoiceLine(voiceClips[UnityEngine.Random.Range(0, voiceClips.Length)]);
                }
            }
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

            // Flash muzzle light briefly when firing
            if (firePoint != null)
            {
                Light l = firePoint.GetComponent<Light>();
                if (l != null)
                {
                    StartCoroutine(FlashMuzzleLight(l));
                }
            }

            if (ObjectPooler.Instance != null && firePoint != null)
            {
                Camera mainCam = Camera.main;
                Quaternion spawnRotation = firePoint.rotation;
                if (mainCam != null)
                {
                    Vector3 targetPoint = mainCam.transform.position + mainCam.transform.forward * 100f;
                    // Raycast to aim at what the player is looking at, ignoring Player layer
                    if (Physics.Raycast(mainCam.transform.position, mainCam.transform.forward, out RaycastHit hit, 500f, ~LayerMask.GetMask("Player")))
                    {
                        targetPoint = hit.point;
                    }
                    spawnRotation = Quaternion.LookRotation(targetPoint - firePoint.position);
                }

                GameObject spawned = ObjectPooler.Instance.SpawnFromPool(tag, firePoint.position, spawnRotation);
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

        private IEnumerator FlashMuzzleLight(Light l)
        {
            l.enabled = true;
            yield return new WaitForSeconds(0.08f);
            if (l != null) l.enabled = false;
        }

        private void DumpTransform(Transform t, string indent, System.Text.StringBuilder sb)
        {
            sb.AppendLine($"{indent}- {t.name} (Position: {t.localPosition}, Active: {t.gameObject.activeSelf})");
            foreach (var comp in t.GetComponents<Component>())
            {
                if (comp != null && comp != t)
                {
                    sb.AppendLine($"{indent}  [Comp] {comp.GetType().Name}");
                }
            }
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransform(t.GetChild(i), indent + "  ", sb);
            }
        }
    }
}
