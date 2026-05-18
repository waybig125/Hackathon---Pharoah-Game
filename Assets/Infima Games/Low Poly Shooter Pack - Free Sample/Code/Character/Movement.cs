// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 30.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 60.0f;


        [Tooltip("How fast the player moves while crouching."), SerializeField]
        private float speedCrouching = 15.0f;

        [Tooltip("How high the player jumps."), SerializeField]
        private float jumpForce = 20.0f;

        #endregion

        #region PROPERTIES

        //Velocity.
        private Vector3 Velocity
        {
            //Getter.
            get => rigidBody.linearVelocity;
            set 
            {
                if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)) return;
                rigidBody.linearVelocity = value;
            }
        }

        #endregion

        #region FIELDS

        /// <summary>
        /// Attached Rigidbody.
        /// </summary>
        private Rigidbody rigidBody;
        /// <summary>
        /// Attached CapsuleCollider.
        /// </summary>
        private CapsuleCollider capsule;
        /// <summary>
        /// Attached AudioSource.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// True if the character is currently grounded.
        /// </summary>
        private bool grounded;

        /// <summary>
        /// Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// The player character's equipped weapon.
        /// </summary>
        private WeaponBehaviour equippedWeapon;

        /// <summary>
        /// Array of RaycastHits used for ground checking.
        /// </summary>
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake.
        /// </summary>
        protected override void Awake()
        {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

            // Hack to fix city materials at runtime
            GameObject city = GameObject.Find("EgyptianCity");
            if (city != null && city.GetComponent<CityMaterialFixer>() == null)
            {
                city.AddComponent<CityMaterialFixer>();
            }

        #if UNITY_EDITOR
            if (audioClipWalking == null) audioClipWalking = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Audio/SFX/Character/Movement/S_CH_Loop_Walking.wav");
            if (audioClipRunning == null) audioClipRunning = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Audio/SFX/Character/Movement/S_CH_Loop_Running.wav");
        #endif
        }

        protected override  void Start()
        {
            // De-parent any environment colliders that were accidentally nested under Player
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "GroundPlane" || child.name == "DesertTerrain" || child.name.Contains("Terrain") || child.name.Contains("Plane"))
                {
                    child.SetParent(null);
                    Debug.Log($"Movement: De-parented {child.name} to scene root to prevent player collision locking!");
                }
            }

            //Rigidbody Setup.
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            //Cache the CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();
            
            // Set robust defaults immediately to avoid initial offset clipping
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            // Prevent sticking to walls/ground by applying a zero-friction PhysicMaterial
            PhysicsMaterial zeroFrictionMat = new PhysicsMaterial("ZeroFrictionPlayer");
            zeroFrictionMat.dynamicFriction = 0f;
            zeroFrictionMat.staticFriction = 0f;
            zeroFrictionMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            zeroFrictionMat.bounciness = 0f;
            zeroFrictionMat.bounceCombine = PhysicsMaterialCombine.Minimum;
            capsule.sharedMaterial = zeroFrictionMat;

            //Audio Source Setup.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;
        }

        /// Checks if the character is on the ground using robust spherecast.
        private bool CheckGrounded()
        {
            if (capsule == null) return false;
            float radius = capsule.radius * 0.9f;
            // Use local coordinates to prevent axis-aligned bounding box (AABB) rotation scaling
            Vector3 localBottom = capsule.center + Vector3.down * (capsule.height * 0.5f - radius);
            Vector3 origin = transform.TransformPoint(localBottom);
            float maxDistance = 0.4f; // Reliable ground reach distance

            int hits = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                if (groundHits[i].collider != null && groundHits[i].collider != capsule)
                {
                    // Clean up array
                    for (int j = 0; j < groundHits.Length; j++) groundHits[j] = new RaycastHit();
                    return true;
                }
            }
            return false;
        }

        protected override void FixedUpdate()
        {
            // Check grounded status before movement/jumping
            grounded = CheckGrounded();

            //Move.
            MoveCharacter();
            
            //Jump
            ProcessJumping();
        }

        private void ProcessJumping()
        {
            bool isMobileJumping = TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsJumping;
            bool isDesktopJumping = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed);
            
            // Consume the jump input immediately so it never triggers unintended auto-jumps later
            if (isMobileJumping)
            {
                if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
                {
                    TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsJumping = false;
                }
            }

            if (grounded && (isMobileJumping || isDesktopJumping))
            {
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
            
            // Advanced Variable Jump Feature (Hold to jump higher)
            if (!grounded && TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsJumpHeld)
            {
                // If held for less than 0.3 seconds, apply extra continuous force
                if (Time.time - TheAlchemistsCrypt.Input.MobileInputManager.Instance.JumpStartTime < 0.3f)
                {
                    rigidBody.AddForce(Vector3.up * (jumpForce * 2.0f * Time.fixedDeltaTime), ForceMode.Acceleration);
                }
            }
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override  void Update()
        {
            //Get the equipped weapon!
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            
            //Play Sounds!
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        private void MoveCharacter()
        {
            #region Calculate Movement Velocity

            //Get Movement Input!
            Vector2 frameInput = playerCharacter.GetInputMovement();

            //Calculate local-space direction by using the player's input.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);
            
            //Running and Crouching speed calculation.
            bool isCrouching = TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsCrouching;
            
            if (isCrouching)
            {
                movement *= speedCrouching;
                capsule.height = Mathf.Lerp(capsule.height, 1.0f, Time.fixedDeltaTime * 10f);
                capsule.center = Vector3.Lerp(capsule.center, new Vector3(0, 0.5f, 0), Time.fixedDeltaTime * 10f);
            }
            else if(playerCharacter.IsRunning())
            {
                movement *= speedRunning;
                capsule.height = Mathf.Lerp(capsule.height, 1.8f, Time.fixedDeltaTime * 10f);
                capsule.center = Vector3.Lerp(capsule.center, new Vector3(0, 0.9f, 0), Time.fixedDeltaTime * 10f);
            }
            else
            {
                //Multiply by the normal walking speed.
                movement *= speedWalking;
                capsule.height = Mathf.Lerp(capsule.height, 1.8f, Time.fixedDeltaTime * 10f);
                capsule.center = Vector3.Lerp(capsule.center, new Vector3(0, 0.9f, 0), Time.fixedDeltaTime * 10f);
            }

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion
            
            // Update Velocity. Preserve Y velocity!
            // Extremely robust safety check for NaN/Infinity to prevent physics crashes
            float curY = Velocity.y;
            if (float.IsNaN(curY) || float.IsInfinity(curY)) curY = 0;

            if (!float.IsNaN(movement.x) && !float.IsNaN(movement.z) && !float.IsInfinity(movement.x) && !float.IsInfinity(movement.z))
            {
                Velocity = new Vector3(movement.x, curY, movement.z);
            }
            else
            {
                Velocity = new Vector3(0, curY, 0);
            }
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds()
        {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                //Select the correct audio clip to play.
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                //Play it!
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            //Pause it if we're doing something like flying, or not moving!
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}