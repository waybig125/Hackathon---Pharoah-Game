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

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // Desktop Fallback
            if (Application.isEditor || !Application.isMobilePlatform)
            {
                MovementInput = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
                LookInput = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                IsFiring = UnityEngine.Input.GetMouseButton(0);
            }
            
            // Note: UI Joystick and Touch Zone will set these values directly via methods called from EventSystem
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
    }
}
