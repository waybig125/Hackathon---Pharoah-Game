// Copyright 2021, Infima Games. All Rights Reserved.

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// Main Character Component. This component handles the most important functions of the character, and interfaces
	/// with basically every part of the asset, it is the hub where it all converges.
	/// </summary>
	[RequireComponent(typeof(CharacterKinematics))]
	public sealed class Character : CharacterBehaviour
	{
		#region FIELDS SERIALIZED

		[Header("Inventory")]
		
		[Tooltip("Inventory.")]
		[SerializeField]
		private InventoryBehaviour inventory;

		[Header("Cameras")]

		[Tooltip("Normal Camera.")]
		[SerializeField]
		private Camera cameraWorld;

		[Header("Animation")]

		[Tooltip("Determines how smooth the locomotion blendspace is.")]
		[SerializeField]
		private float dampTimeLocomotion = 0.15f;

		[Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
		[SerializeField]
		private float dampTimeAiming = 0.3f;
		
		[Header("Animation Procedural")]
		
		[Tooltip("Character Animator.")]
		[SerializeField]
		private Animator characterAnimator;

		#endregion

		#region FIELDS

		/// <summary>
		/// True if the character is aiming.
		/// </summary>
		private bool aiming;
		/// <summary>
		/// True if the character is running.
		/// </summary>
		private bool running;
		/// <summary>
		/// True if the character has its weapon holstered.
		/// </summary>
		private bool holstered;
		
		/// <summary>
		/// Last Time.time at which we shot.
		/// </summary>
		private float lastShotTime;
		
		/// <summary>
		/// Overlay Layer Index. Useful for playing things like firing animations.
		/// </summary>
		private int layerOverlay;
		/// <summary>
		/// Holster Layer Index. Used to play holster animations.
		/// </summary>
		private int layerHolster;
		/// <summary>
		/// Actions Layer Index. Used to play actions like reloading.
		/// </summary>
		private int layerActions;

		/// <summary>
		/// Character Kinematics. Handles all the IK stuff.
		/// </summary>
		private CharacterKinematics characterKinematics;
		
		/// <summary>
		/// The currently equipped weapon.
		/// </summary>
		private WeaponBehaviour equippedWeapon;
		/// <summary>
		/// The equipped weapon's attachment manager.
		/// </summary>
		private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
		
		/// <summary>
		/// The scope equipped on the character's weapon.
		/// </summary>
		private ScopeBehaviour equippedWeaponScope;
		/// <summary>
		/// The magazine equipped on the character's weapon.
		/// </summary>
		private MagazineBehaviour equippedWeaponMagazine;
		
		/// <summary>
		/// True if the character is reloading.
		/// </summary>
		private bool reloading;
		
		/// <summary>
		/// True if the character is inspecting its weapon.
		/// </summary>
		private bool inspecting;

		/// <summary>
		/// True if the character is in the middle of holstering a weapon.
		/// </summary>
		private bool holstering;

		/// <summary>
		/// Movement Axis Values.
		/// </summary>
		private Vector2 axisMovement;
		/// <summary>
		/// Look Axis Values.
		/// </summary>
		private Vector2 axisLook;
		/// <summary>
		/// True if the player is holding the aiming button.
		/// </summary>
		private bool holdingButtonAim;
		/// <summary>
		/// True if the player is holding the running button.
		/// </summary>
		private bool holdingButtonRun;
		/// <summary>
		/// True if the player is holding the firing button.
		/// </summary>
		private bool holdingButtonFire;

		/// <summary>
		/// If true, the tutorial text should be visible on screen.
		/// </summary>
		private bool tutorialTextVisible;

		/// <summary>
		/// True if the game cursor is locked! Used when pressing "Escape" to allow developers to more easily access the editor.
		/// </summary>
		private bool cursorLocked;
		private Vector2 cachedMovement;

		#endregion

		#region CONSTANTS

		/// <summary>
		/// Aiming Alpha Value.
		/// </summary>
		private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");

		/// <summary>
		/// Hashed "Movement".
		/// </summary>
		private static readonly int HashMovement = Animator.StringToHash("Movement");

		#endregion

		#region UNITY

		protected override void Awake()
		{
			#region Lock Cursor

			//Always make sure that our cursor is locked when the game starts!
			cursorLocked = true;
			//Update the cursor's state.
			UpdateCursorState();

			#endregion

			//Cache the CharacterKinematics component.
			characterKinematics = GetComponent<CharacterKinematics>();

			//Initialize Inventory.
			inventory.Init();

			//Refresh!
			RefreshWeaponSetup();
		}
		protected override void Start()
		{
			//Cache a reference to the holster layer's index.
			layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
			//Cache a reference to the action layer's index.
			layerActions = characterAnimator.GetLayerIndex("Layer Actions");
			//Cache a reference to the overlay layer's index.
			layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
		}

		protected override void Update()
		{
            // --- HACKATHON MOBILE & DESKTOP FAIL-SAFE INJECTION ---
            bool mobileFiring = false, mobileAiming = false, mobileRunning = false;
            bool isTouchActive = false;

            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
            {
                var mob = TheAlchemistsCrypt.Input.MobileInputManager.Instance;
                mobileFiring = mob.IsFiring;
                mobileAiming = mob.IsAiming;
                mobileRunning = mob.IsSprinting;
                
                // Robust Gating: Use manager's state
                isTouchActive = mob.IsTouchActive;
                
                // Atomic events
                if (mob.IsSwappingWeapon) {
                    if (inventory != null) StartCoroutine(Equip(inventory.GetNextIndex()));
                    mob.IsSwappingWeapon = false;
                }
                if (mob.IsReloading) {
                    if (CanReload()) PlayReloadAnimation();
                    mob.IsReloading = false;
                }

                // Cache movement: use virtual stick or keyboard WASD if active, else native axisMovement
                Vector2 mobMove = mob.GetMovement();
                cachedMovement = mobMove.sqrMagnitude > 0.01f ? mobMove : axisMovement;
            }
            else {
                // Fallback to desktop movement if no mobile manager
                cachedMovement = axisMovement;
            }

            holdingButtonFire = mobileFiring;
            holdingButtonAim = mobileAiming;
            holdingButtonRun = mobileRunning;

            // Only process Keyboard/Mouse if touch isn't currently controlling the game
            if (!isTouchActive)
            {
                if (Mouse.current != null) {
                    if (Mouse.current.leftButton.isPressed) holdingButtonFire = true;
                    if (Mouse.current.rightButton.isPressed) holdingButtonAim = true;
                }
                if (Keyboard.current != null) {
                    if (Keyboard.current.leftShiftKey.isPressed) holdingButtonRun = true;
                    if (Keyboard.current.rKey.wasPressedThisFrame && CanReload()) PlayReloadAnimation();
                    if (Keyboard.current.qKey.wasPressedThisFrame && inventory != null) 
                        StartCoroutine(Equip(inventory.GetNextIndex()));
                }
            }

			//Match Aim.
			aiming = holdingButtonAim && CanAim();
			//Match Run.
			running = holdingButtonRun && CanRun();

			//Holding the firing button.
			if (holdingButtonFire)
			{
				bool isPunching = equippedWeapon.GetComponent<PunchCombat>() != null;
                bool canFire = CanPlayAnimationFire() && (equippedWeapon.HasAmmunition() || isPunching);
				if (canFire && (equippedWeapon.IsAutomatic() || isPunching))
				{
					if (Time.time - lastShotTime > 60.0f / (isPunching ? 120.0f : equippedWeapon.GetRateOfFire()))
						Fire();
				}
                else if (canFire && !equippedWeapon.IsAutomatic())
                {
                    bool triggerDown = false;
                    if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.WasFiringPressed)
                    {
                        // Stricter semi-auto gate: ensure enough time has passed since last shot
                        float minInterval = 60.0f / equippedWeapon.GetRateOfFire();
                        if (Time.time - lastShotTime > minInterval)
                        {
                            triggerDown = true;
                            TheAlchemistsCrypt.Input.MobileInputManager.Instance.WasFiringPressed = false;
                        }
                    }
                    if (!isTouchActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) triggerDown = true;

                    if (triggerDown)
                        Fire();
                }
			}

			UpdateAnimator();
		}

		protected override void LateUpdate()
		{
			//We need a weapon for this!
			if (equippedWeapon == null)
				return;

			//Weapons without a scope should not be a thing! Ironsights are a scope too!
			if (equippedWeaponScope == null)
				return;
			
			//Make sure that we have a kinematics component!
			if(characterKinematics != null)
			{
				//Compute.
				characterKinematics.Compute();
			}
		}
		
		#endregion

		#region GETTERS

		public override Camera GetCameraWorld() => cameraWorld;

		public WeaponBehaviour GetEquippedWeapon() => equippedWeapon;

		public override InventoryBehaviour GetInventory() => inventory;
		
		public override bool IsCrosshairVisible() => !aiming && !holstered;
		public override bool IsRunning() 
		{
			if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null && TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsSprinting)
				return true;
			return running;
		}
		
		public override bool IsAiming() => aiming;
		public override bool IsCursorLocked() => cursorLocked;
		
		public override bool IsTutorialTextVisible() => tutorialTextVisible;
		
		public override Vector2 GetInputMovement()
		{
			return cachedMovement;
		}

		public override Vector2 GetInputLook()
		{
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null &&
                TheAlchemistsCrypt.Input.MobileInputManager.Instance.IsTouchActive)
            {
                // Let the mobile manager handle the accumulation/consumption logic
                return TheAlchemistsCrypt.Input.MobileInputManager.Instance.ConsumeLook();
            }

            // Fallback for desktop/editor
            Vector2 look = axisLook;
            axisLook = Vector2.zero;
			return look;
		}

		#endregion

		#region METHODS

		/// <summary>
		/// Updates all the animator properties for this frame.
		/// </summary>
		private void UpdateAnimator()
		{
			// Use cachedMovement to avoid the "double consumption" bug
			float movementMagnitude = Mathf.Clamp01(Mathf.Abs(cachedMovement.x) + Mathf.Abs(cachedMovement.y));
			characterAnimator.SetFloat(HashMovement, movementMagnitude, dampTimeLocomotion, Time.deltaTime);
			
			//Update the aiming value, but use interpolation. This makes sure that things like firing can transition properly.
			characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);

			//Update Animator Aiming.
			const string boolNameAim = "Aim";
			characterAnimator.SetBool(boolNameAim, aiming);
			
			//Update Animator Running.
			const string boolNameRun = "Running";
			characterAnimator.SetBool(boolNameRun, running);
		}
		
		/// <summary>
		/// Plays the inspect animation.
		/// </summary>
		private void Inspect()
		{
			//State.
			inspecting = true;
			//Play.
			characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
		}
		
		/// <summary>
		/// Fires the character's weapon.
		/// </summary>
		private void Fire()
		{
			//Save the shot time, so we can calculate the fire rate correctly.
			lastShotTime = Time.time;
			
			// --- HACKATHON PUNCH INJECTION ---
			var punch = equippedWeapon.GetComponent<PunchCombat>();
			if (punch != null)
			{
				punch.Punch();
			}
			else
			{
				//Fire the weapon! Make sure that we also pass the scope's spread multiplier if we're aiming.
				equippedWeapon.Fire();
			}
			// ----------------------------------

			//Play firing animation.
			const string stateName = "Fire";
			characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);
		}

		private void PlayReloadAnimation()
		{
			#region Animation

			//Get the name of the animation state to play, which depends on weapon settings, and ammunition!
			string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
			//Play the animation state!
			characterAnimator.Play(stateName, layerActions, 0.0f);

			//Set.
			reloading = true;

			#endregion

			//Reload.
			equippedWeapon.Reload();
		}

		/// <summary>
		/// Equip Weapon Coroutine.
		/// </summary>
		private IEnumerator Equip(int index = 0)
		{
			//Only if we're not holstered, holster. If we are already, we don't need to wait.
			if(!holstered)
			{
				//Holster.
				SetHolstered(holstering = true);
				//Wait.
				yield return new WaitUntil(() => holstering == false);
			}
			//Unholster. We do this just in case we were holstered.
			SetHolstered(false);
			//Play Unholster Animation.
			characterAnimator.Play("Unholster", layerHolster, 0);
			
			//Equip The New Weapon.
			inventory.Equip(index);
			//Refresh.
			RefreshWeaponSetup();
		}

		/// <summary>
		/// Refresh all weapon things to make sure we're all set up!
		/// </summary>
		private void RefreshWeaponSetup()
		{
			//Make sure we have a weapon. We don't want errors!
			if ((equippedWeapon = inventory.GetEquipped()) == null)
				return;
			
			//Update Animator Controller. We do this to update all animations to a specific weapon's set.
			characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();
			
			//Cache the weapon attachment manager.
			weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
			//Cache the scope.
			equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
			//Cache the magazine.
			equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
		}

		/// <summary>
		/// Updates the cursor state based on the value of cursorLocked.
		/// </summary>
		private void UpdateCursorState()
		{
			//Update lock state.
			Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
			//Update visible state.
			Cursor.visible = !cursorLocked;
		}

		#endregion

		#region INPUT

		/// <summary>
		/// OnMove.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnMove(InputValue value)
		{
			//Save the movement input.
			axisMovement = value.Get<Vector2>();
		}

		/// <summary>
		/// OnLook.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnLook(InputValue value)
		{
			//Save the look input.
			axisLook = value.Get<Vector2>();
		}

		/// <summary>
		/// OnJump.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnJump(InputValue value)
		{
			//Don't jump if we're running.
			if (running)
				return;
			
			// Set the jumping state in the MobileInputManager so Movement.cs can process it
			if (TheAlchemistsCrypt.Input.MobileInputManager.Instance != null)
			{
				TheAlchemistsCrypt.Input.MobileInputManager.Instance.SetJumping(value.isPressed);
			}
		}

		/// <summary>
		/// OnInventoryNext.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnInventoryNext(InputValue value)
		{
			//Next weapon!
			StartCoroutine(Equip(inventory.GetNextIndex()));
		}

		/// <summary>
		/// OnInventoryPrevious.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnInventoryPrevious(InputValue value)
		{
			//Previous weapon!
			StartCoroutine(Equip(inventory.GetLastIndex()));
		}

		/// <summary>
		/// OnHolster.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnHolster(InputValue value)
		{
			//Set holstered!
			SetHolstered(!holstered);
		}

		/// <summary>
		/// OnAim.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnAim(InputValue value)
		{
			//Hold.
			holdingButtonAim = value.isPressed;
		}

		/// <summary>
		/// OnFire.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnFire(InputValue value)
		{
			//Hold.
			holdingButtonFire = value.isPressed;

			//Check.
			if (holdingButtonFire && CanPlayAnimationFire())
			{
				//Fire!
				if(!equippedWeapon.IsAutomatic())
					Fire();
			}
		}

		/// <summary>
		/// OnRun.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnRun(InputValue value)
		{
			//Hold.
			holdingButtonRun = value.isPressed;
		}

		/// <summary>
		/// OnReload.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnReload(InputValue value)
		{
			//Reload.
			if (CanReload())
				PlayReloadAnimation();
		}

		/// <summary>
		/// OnInspect.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnInspect(InputValue value)
		{
			//Inspect.
			if (CanInspect())
				Inspect();
		}

		/// <summary>
		/// OnLockCursor.
		/// </summary>
		/// <param name="value">Value.</param>
		public void OnLockCursor(InputValue value)
		{
			//Flip the value!
			cursorLocked = !cursorLocked;
			//Update the cursor's state.
			UpdateCursorState();
		}

		#endregion

		#region ANIMATION

		public override void EjectCasing()
		{
			//Eject casings.
			if(equippedWeapon != null)
				equippedWeapon.EjectCasing();
		}

		public override void FillAmmunition(int amount)
		{
			//Fill ammunition.
			if(equippedWeapon != null)
				equippedWeapon.FillAmmunition(amount);
		}

		public override void SetActiveMagazine(int active)
		{
			//Set active magazine.
		}

		public override void AnimationEndedReload()
		{
			//Reload finish.
			reloading = false;
		}

		public override void AnimationEndedInspect()
		{
			//Inspect finish.
			inspecting = false;
		}

		public override void AnimationEndedHolster()
		{
			//Holster finish.
			holstering = false;
		}

		#endregion

		#region HELPER METHODS

		/// <summary>
		/// Sets the value of holstered.
		/// </summary>
		private void SetHolstered(bool value = true)
		{
			//Set value.
			holstered = value;
			
			//Update Animator.
			const string boolName = "Holstered";
			characterAnimator.SetBool(boolName, holstered);
		}

		/// <summary>
		/// Returns true if the character can aim.
		/// </summary>
		private bool CanAim() => !reloading && !inspecting && !holstered && !holstering;
		/// <summary>
		/// Returns true if the character can run.
		/// </summary>
		private bool CanRun()
        {
            Vector2 move = GetInputMovement();
            // Allow sprinting if there's any significant movement, even diagonal
            return move.magnitude > 0.1f && move.y > -0.1f && !aiming && !inspecting && !reloading && !holstered && !holstering;
        }

		/// <summary>
		/// Returns true if the character can inspect its weapon.
		/// </summary>
		private bool CanInspect() => !reloading && !inspecting && !aiming && !holstered && !holstering;
		/// <summary>
		/// Returns true if the character can reload its weapon.
		/// </summary>
		private bool CanReload() => !reloading && !inspecting && !aiming && !holstered && !holstering && (equippedWeapon != null && !equippedWeapon.IsFull());

		/// <summary>
		/// Returns true if the character can play the fire animation.
		/// </summary>
		private bool CanPlayAnimationFire() => !reloading && !inspecting && !holstered && !holstering;

		#endregion
	}
}
