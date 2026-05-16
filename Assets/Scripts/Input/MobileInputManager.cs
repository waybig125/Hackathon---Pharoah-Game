using UnityEngine;

namespace TheAlchemistsCrypt.Input
{
    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance;

        [Header("Settings")]
        [SerializeField] private float joystickDeadzone = 0.1f;
        public bool InvertJoystickX = false;
        public bool InvertJoystickY = false;
        
        // Output values
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
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

            // Silence Depth Surface Warnings by ensuring Main Camera settings are mobile-friendly
            var cam = Camera.main;
            if (cam != null) {
                #if UNITY_URP
                // Universal Additional Camera Data can be adjusted here if needed
                #endif
            }
        }

        private void Update()
        {
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

        public void SetLook(Vector2 input)
        {
            // Accumulate input so it's not lost between frames
            LookInput += input;
        }

        public void ConsumeLook()
        {
            LookInput = Vector2.zero;
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
