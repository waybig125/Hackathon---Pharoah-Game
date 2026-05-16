using UnityEngine;

namespace TheAlchemistsCrypt.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float lookSensitivity = 1.5f;

        [Header("References")]
        [SerializeField] private Transform playerCamera;

        private Rigidbody rb;
        private float verticalLookRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            gameObject.tag = "Player"; // Force tag for AI
            
            if (!Application.isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleRotation();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance == null) return;

            Vector2 input = TheAlchemistsCrypt.Input.MobileInputManager.Instance.MovementInput;
            Vector3 moveDirection = (transform.forward * input.y + transform.right * input.x).normalized;
            
            Vector3 targetVelocity = moveDirection * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y; 
            
            rb.linearVelocity = targetVelocity;
        }

        private void HandleRotation()
        {
            if (TheAlchemistsCrypt.Input.MobileInputManager.Instance == null) return;

            Vector2 lookInput = TheAlchemistsCrypt.Input.MobileInputManager.Instance.LookInput;
            
            // Horizontal rotation (Player Body)
            transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

            // Vertical rotation (Camera)
            verticalLookRotation -= lookInput.y * lookSensitivity;
            // Video-recommended clamp values: -30 to 45
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -30f, 45f); 
            
            if (playerCamera != null)
                playerCamera.localEulerAngles = new Vector3(verticalLookRotation, 0, 0);
        }
    }
}
