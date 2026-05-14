using UnityEngine;

namespace TheAlchemistsCrypt.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSensitivity = 2f;

        [Header("References")]
        [SerializeField] private Transform playerCamera;

        private Rigidbody rb;
        private float verticalLookRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            // Lock cursor if on desktop
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
            Vector2 input = TheAlchemistsCrypt.Input.MobileInputManager.Instance.MovementInput;
            Vector3 moveDirection = (transform.forward * input.y + transform.right * input.x).normalized;
            
            Vector3 targetVelocity = moveDirection * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y; // Maintain gravity
            
            rb.linearVelocity = targetVelocity;
        }

        private void HandleRotation()
        {
            Vector2 lookInput = TheAlchemistsCrypt.Input.MobileInputManager.Instance.LookInput;

            // Horizontal rotation (Player Body)
            transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

            // Vertical rotation (Camera)
            verticalLookRotation -= lookInput.y * lookSensitivity;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
            playerCamera.localEulerAngles = new Vector3(verticalLookRotation, 0, 0);
        }
    }
}
