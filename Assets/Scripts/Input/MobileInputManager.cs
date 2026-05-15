using UnityEngine;

namespace TheAlchemistsCrypt.Input
{
    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance;

        [Header("Settings")]
        [SerializeField] private float joystickDeadzone = 0.1f;
        
        // Output values
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsJumping { get; set; }
        public bool IsJumpHeld { get; private set; }
        public float JumpStartTime { get; private set; }
        public bool IsCrouching { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsSwappingWeapon { get; set; }

        private void Awake()
        {
            Instance = this;
            
            // Auto-attach MobileHUDButtons
            if (gameObject.GetComponent<TheAlchemistsCrypt.UI.MobileHUDButtons>() == null)
            {
                gameObject.AddComponent<TheAlchemistsCrypt.UI.MobileHUDButtons>();
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
                MovementInput = input;
        }

        public void SetLook(Vector2 input)
        {
            LookInput = input;
        }

        public void SetFiring(bool state)
        {
            IsFiring = state;
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
