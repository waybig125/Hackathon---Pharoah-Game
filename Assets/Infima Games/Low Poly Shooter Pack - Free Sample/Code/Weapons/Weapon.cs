// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Weapon. This class handles most of the things that weapons need.
    /// </summary>
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Firing")]

        [Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
        [SerializeField] 
        private bool automatic;
        
        [Tooltip("How fast the projectiles are.")]
        [SerializeField]
        private float projectileImpulse = 400.0f;

        [Tooltip("Amount of shots this weapon can shoot in a minute. It determines how fast the weapon shoots.")]
        [SerializeField] 
        private int roundsPerMinutes = 200;

        [Tooltip("Mask of things recognized when firing.")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("Maximum distance at which this weapon can fire accurately. Shots beyond this distance will not use linetracing for accuracy.")]
        [SerializeField]
        private float maximumDistance = 500.0f;

        [Header("Animation")]

        [Tooltip("Transform that represents the weapon's ejection port, meaning the part of the weapon that casings shoot from.")]
        [SerializeField]
        private Transform socketEjection;

        [Header("Resources")]

        [Tooltip("Casing Prefab.")]
        [SerializeField]
        private GameObject prefabCasing;
        
        [Tooltip("Projectile Prefab. This is the prefab spawned when the weapon shoots.")]
        [SerializeField]
        private GameObject prefabProjectile;
        
        [Tooltip("The AnimatorController a player character needs to use while wielding this weapon.")]
        [SerializeField] 
        public RuntimeAnimatorController controller;

        [Tooltip("Weapon Body Texture.")]
        [SerializeField]
        private Sprite spriteBody;
        
        [Header("Audio Clips Holster")]

        [Tooltip("Holster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipHolster;

        [Tooltip("Unholster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipUnholster;
        
        [Header("Audio Clips Reloads")]

        [Tooltip("Reload Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReload;
        
        [Tooltip("Reload Empty Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReloadEmpty;
        
        [Header("Audio Clips Other")]

        [Tooltip("AudioClip played when this weapon is fired without any ammunition.")]
        [SerializeField]
        private AudioClip audioClipFireEmpty;

        #endregion

        #region FIELDS

        /// <summary>
        /// Weapon Animator.
        /// </summary>
        private Animator animator;
        /// <summary>
        /// Attachment Manager.
        /// </summary>
        private WeaponAttachmentManagerBehaviour attachmentManager;

        /// <summary>
        /// Amount of ammunition left.
        /// </summary>
        private int ammunitionCurrent;

        #region Attachment Behaviours
        
        /// <summary>
        /// Equipped Magazine Reference.
        /// </summary>
        private MagazineBehaviour magazineBehaviour;
        /// <summary>
        /// Equipped Muzzle Reference.
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;

        #endregion

        /// <summary>
        /// The GameModeService used in this game!
        /// </summary>
        private IGameModeService gameModeService;
        /// <summary>
        /// The main player character behaviour component.
        /// </summary>
        private CharacterBehaviour characterBehaviour;

        /// <summary>
        /// The player character's camera.
        /// </summary>
        private Transform playerCamera;

        // --- HACKATHON: Professional Spring Recoil ---
        private Vector3 targetRecoilPos;
        private Vector3 targetRecoilRot;
        private Vector3 currentRecoilPos;
        private Vector3 currentRecoilRot;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilSnappiness = 20f;
        [SerializeField] private float recoilReturnSpeed = 10f;
        
        // The stable "Home" position to prevent drift
        private Vector3 originalLocalPos;
        private Quaternion originalLocalRot;
        
        #endregion

        #region UNITY
        
        protected override void Awake()
        {
            //Get Animator.
            animator = GetComponent<Animator>();
            //Get Attachment Manager.
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            //Cache the game mode service. We only need this right here, but we'll cache it in case we ever need it again.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            //Cache the player character.
            characterBehaviour = gameModeService.GetPlayerCharacter();
            //Cache the world camera. We use this in line traces.
            playerCamera = characterBehaviour.GetCameraWorld().transform;

            // Capture the stable home state
            originalLocalPos = transform.localPosition;
            originalLocalRot = transform.localRotation;
        }
        protected override void Start()
        {
            #region Cache Attachment References
            
            //Get Magazine.
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            //Get Muzzle.
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            #endregion

            //Max Out Ammo.
            ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            // --- Perlin Noise Sway/Recovery ---
            float swayAmount = 0.5f;
            float swaySpeed = 1.5f;
            float perlinX = (Mathf.PerlinNoise(Time.time * swaySpeed, 0f) - 0.5f) * swayAmount;
            float perlinY = (Mathf.PerlinNoise(0f, Time.time * swaySpeed) - 0.5f) * swayAmount;
            Vector3 noiseRot = new Vector3(perlinX, perlinY, 0f);

            // 1. Pull target back to zero (the spring tension)
            targetRecoilPos = Vector3.Lerp(targetRecoilPos, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
            targetRecoilRot = Vector3.Lerp(targetRecoilRot, Vector3.zero, recoilReturnSpeed * Time.deltaTime);

            // 2. Snap current values towards target (the movement)
            currentRecoilPos = Vector3.Slerp(currentRecoilPos, targetRecoilPos, recoilSnappiness * Time.fixedDeltaTime);
            currentRecoilRot = Vector3.Slerp(currentRecoilRot, targetRecoilRot, recoilSnappiness * Time.fixedDeltaTime);

            // 3. Apply ONLY the recoil offset to the ORIGINAL home position/rotation
            // This ensures the gun NEVER drifts away from its starting point
            transform.localPosition = originalLocalPos + currentRecoilPos;
            transform.localRotation = originalLocalRot * Quaternion.Euler(currentRecoilRot + noiseRot);
        }

        #endregion

        #region GETTERS

        public override Animator GetAnimator() => animator;
        
        public override Sprite GetSpriteBody() => spriteBody;

        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;

        public override AudioClip GetAudioClipReload() => audioClipReload;
        public override AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;

        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
        
        public override AudioClip GetAudioClipFire() => muzzleBehaviour != null ? muzzleBehaviour.GetAudioClipFire() : null;
        
        public override int GetAmmunitionCurrent() => ammunitionCurrent;

        public override int GetAmmunitionTotal() => magazineBehaviour != null ? magazineBehaviour.GetAmmunitionTotal() : 0;

        public override bool IsAutomatic() => automatic;
        public override float GetRateOfFire() => roundsPerMinutes;
        
        public override bool IsFull() => magazineBehaviour != null && ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #region METHODS

        public override void Reload()
        {
            //Play Reload Animation.
            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);
        }
        public override void Fire(float spreadMultiplier = 1.0f)
        {
            //We need a muzzle in order to fire this weapon!
            if (muzzleBehaviour == null)
                return;
            
            //Make sure that we have a camera cached, otherwise we don't really have the ability to perform traces.
            if (playerCamera == null)
                return;

            //Get Muzzle Socket. This is the point we fire from.
            Transform muzzleSocket = muzzleBehaviour.GetSocket();
            
            //Play the firing animation instantly (classic snappy feel)
            // But we play it softly to avoid the "jumping everywhere" glitch
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            
            // --- HACKATHON: Professional Spring Kick ---
            targetRecoilPos -= new Vector3(0, 0, 0.12f); // Sharp kick back
            targetRecoilRot += new Vector3(-4f, Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f)); // Kick up + random horizontal

            //Reduce ammunition! We just shot, so we need to get rid of one!
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            //Play all muzzle effects.
            muzzleBehaviour.Effect();
            
            // Determine hit point via screen-center raycast (Perfect Aim)
            Vector3 targetPoint = playerCamera.position + playerCamera.forward * maximumDistance;
            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, maximumDistance, mask))
            {
                targetPoint = hit.point;
            }

            // Calculate rotation from muzzle to actual hit point
            Quaternion rotation = Quaternion.LookRotation(targetPoint - muzzleSocket.position);
                
            //Spawn projectile from the muzzle socket (classic position — no offset clipping issues)
            GameObject projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);
            
            // Add velocity to the projectile. We use a high impulse for a "tracer" feel.
            var rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = projectile.transform.forward * projectileImpulse;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            // --- HACKATHON: Dynamic Muzzle Flash Light ---
            GameObject flash = new GameObject("MuzzleFlashLight");
            flash.transform.position = muzzleSocket.position;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.3f);
            light.range = 8f;
            light.intensity = 5f;
            Destroy(flash, 0.05f); // Super fast flash
        }

        public override void FillAmmunition(int amount)
        {
            //Update the value by a certain amount.
            ammunitionCurrent = amount != 0 ? Mathf.Clamp(ammunitionCurrent + amount, 
                0, GetAmmunitionTotal()) : magazineBehaviour.GetAmmunitionTotal();
        }

        public override void EjectCasing()
        {
            //Spawn casing prefab at spawn point.
            if(prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}