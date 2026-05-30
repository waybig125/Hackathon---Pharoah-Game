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
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            
            //Reduce ammunition! We just shot, so we need to get rid of one!
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            //Play all muzzle effects.
            muzzleBehaviour.Effect();
            
            // --- HACKATHON: HITSCAN FIRE LOGIC (No Fake Projectiles) ---
            Vector3 hitPoint = playerCamera.position + playerCamera.forward * maximumDistance;
            
            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, maximumDistance, mask))
            {
                hitPoint = hit.point;

                // Check for enemy hit
                var zombie = hit.collider.GetComponentInParent<TheAlchemistsCrypt.AI.ZombieAI>();
                if (zombie != null)
                {
                    float relativeY = hitPoint.y - zombie.transform.position.y;
                    bool isHeadshot = (hit.collider.name.ToLower().Contains("head") || hit.collider.name.ToLower().Contains("skull") || relativeY >= 1.4f);
                    float damage = isHeadshot ? 15f : 5f;
                    zombie.TakeDamage(damage);
                }
            }
                
            // Draw a high-speed procedural Tracer line
            DrawTracerLine(muzzleSocket.position, hitPoint);

            // Trigger Camera Recoil
            var camLook = playerCamera.GetComponentInParent<CameraLook>();
            if (camLook != null)
            {
                camLook.ApplyRecoilKick(-4f, Random.Range(-1.5f, 1.5f));
            }
        }

        private void DrawTracerLine(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("HitscanTracer");
            tracerObj.transform.position = start;
            var lr = tracerObj.AddComponent<LineRenderer>();
            
            // Fast, tapered glowing line
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            
            Material tracerMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            tracerMat.EnableKeyword("_EMISSION");
            tracerMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f));
            tracerMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 3f);
            lr.material = tracerMat;

            // Animate it fading out extremely quickly
            StartCoroutine(FadeTracer(lr));
        }

        private System.Collections.IEnumerator FadeTracer(LineRenderer lr)
        {
            float duration = 0.06f; // Extremely snappy
            float elapsed = 0f;
            Color initialColor = lr.material.GetColor("_BaseColor");

            while (elapsed < duration)
            {
                if (lr == null) break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                lr.startColor = new Color(1f, 1f, 1f, alpha);
                lr.endColor = new Color(1f, 1f, 1f, alpha);
                lr.material.SetColor("_BaseColor", new Color(initialColor.r, initialColor.g, initialColor.b, alpha));
                yield return null;
            }
            if (lr != null) Destroy(lr.gameObject);
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