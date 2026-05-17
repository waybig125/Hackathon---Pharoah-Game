using UnityEngine;
using UnityEngine.InputSystem;

namespace TheAlchemistsCrypt.Input
{
    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance;

        [Header("Settings")]
        [SerializeField] private float joystickDeadzone = 0.1f;
        public bool InvertJoystickX = false;
        public bool InvertJoystickY = false;
        
        private InputAction moveAction;
        
        // Output values
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsTouchActive { get; private set; } // Track if a finger is on the look zone
        public bool IsFiring { get; private set; }
        public bool WasFiringPressed { get; set; } // Trigger for semi-auto
        public bool IsAiming { get; private set; }
        public bool IsJumping { get; set; }
        public bool IsJumpHeld { get; private set; }
        public float JumpStartTime { get; private set; }
        public bool IsCrouching { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsSwappingWeapon { get; set; }
        public bool IsReloading { get; set; }

        private void Awake()
        {
            Instance = this;
            
            // New Input System: Bind to Gamepad left stick (simulated by OnScreenStick)
            moveAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
            
            // Auto-attach MobileHUDButtons
            if (gameObject.GetComponent<TheAlchemistsCrypt.UI.MobileHUDButtons>() == null)
            {
                gameObject.AddComponent<TheAlchemistsCrypt.UI.MobileHUDButtons>();
            }

            // Auto-attach AtmosphereManager
            if (gameObject.GetComponent<TheAlchemistsCrypt.Environment.AtmosphereManager>() == null)
            {
                gameObject.AddComponent<TheAlchemistsCrypt.Environment.AtmosphereManager>();
            }

            // Auto-attach MummySpawner for clean runtime spawning
            if (gameObject.GetComponent<TheAlchemistsCrypt.AI.MummySpawner>() == null)
            {
                gameObject.AddComponent<TheAlchemistsCrypt.AI.MummySpawner>();
            }

            // Silence Depth Surface Warnings by ensuring Main Camera settings are mobile-friendly
            var cam = Camera.main;
            if (cam != null) {
                #if UNITY_URP
                // Universal Additional Camera Data can be adjusted here if needed
                #endif
            }
        }

        private void OnEnable() => moveAction?.Enable();
        private void OnDisable() => moveAction?.Disable();

        private void Update()
        {
            Vector2 finalMove = Vector2.zero;

            // 1. Read virtual on-screen joystick
            if (moveAction != null)
                finalMove = moveAction.ReadValue<Vector2>();

            // 2. Read Keyboard WASD/Arrow keys
            Vector2 keyboardInput = Vector2.zero;
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                float x = 0;
                float y = 0;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                keyboardInput = new Vector2(x, y).normalized;
            }

            // Fallback to legacy inputs if needed
            if (keyboardInput.sqrMagnitude < 0.01f)
            {
                float x = UnityEngine.Input.GetAxisRaw("Horizontal");
                float y = UnityEngine.Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f)
                {
                    keyboardInput = new Vector2(x, y).normalized;
                }
            }

            // 3. Blend inputs gracefully
            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                SetMovement(keyboardInput);
            }
            else
            {
                SetMovement(finalMove);
            }

            // 4. Update Touch Active State
            bool touchDetected = UnityEngine.Input.touchCount > 0;
            if (finalMove.sqrMagnitude > 0.001f) touchDetected = true;
            
            IsTouchActive = touchDetected;
        }

        public void SetMovement(Vector2 input)
        {
            if (input.magnitude < joystickDeadzone)
                MovementInput = Vector2.zero;
            else
            {
                float x = InvertJoystickX ? -input.x : input.x;
                float y = InvertJoystickY ? -input.y : input.y;
                MovementInput = new Vector2(x, y);
            }
        }

        public Vector2 GetMovement()
        {
            return MovementInput;
        }

        public void SetLook(Vector2 input)
        {
            // Accumulate input so it's not lost between frames
            LookInput += input;
        }

        public Vector2 ConsumeLook()
        {
            Vector2 temp = LookInput;
            LookInput = Vector2.zero;
            return temp;
        }

        // Keep NotifyTouchActive for UI zones that want to force the state
        public void NotifyTouchActive(bool active)
        {
            // We can combine this with the global count if needed, but the Update loop is more robust
        }

        public void SetFiring(bool state)
        {
            if (state && !IsFiring) WasFiringPressed = true;
            IsFiring = state;
        }

        public void SetAiming(bool state)
        {
            IsAiming = state;
        }

        public void SetJumping(bool state)
        {
            IsJumping = state;
            IsJumpHeld = state;
            if (state) JumpStartTime = Time.time;
        }

        public void SetCrouching(bool state)
        {
            IsCrouching = state;
        }

        public void SetSprinting(bool state)
        {
            IsSprinting = state;
        }

        public void SetSwappingWeapon()
        {
            IsSwappingWeapon = true;
        }
    }
}
