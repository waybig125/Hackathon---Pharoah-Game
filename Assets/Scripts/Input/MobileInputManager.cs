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
                float h = 0f;
                float v = 0f;
                if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) v += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) v -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) h += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                
                // Only override if there is actual keyboard input (allows testing UI in editor)
                if (h != 0 || v != 0) MovementInput = new Vector2(h, v).normalized;
                
                // For look input, we still need mouse movement, but if axes are missing, we can try/catch or use a different approach.
                // However, Mouse X and Mouse Y are usually present. If they fail, we catch it.
                try 
                {
                    LookInput = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                }
                catch (System.ArgumentException) 
                {
                    // Fallback to no mouse look if axes are completely broken
                }

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
