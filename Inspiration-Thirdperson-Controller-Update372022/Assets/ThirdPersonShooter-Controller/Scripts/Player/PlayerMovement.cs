using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {
        [Header("GroundChecker Data")]
        [Space]
        [SerializeField]
        private GroundCheckerSettings groundChecker;
        
        private bool isGrounded;
        private float horizontal;
        private float vertical;
        private float speed;
        private Vector3 desiredMoveDirection;
        private float playerVeloticy;
        private FixedJoystick joystick;

        #region Ground Checker

        private void GroundChecker() 
        {
            isGrounded = Physics.CheckSphere(groundChecker.GroundcheckerTransform.position,
                                                data.GroundCheckDistance,
                                                data.GroundCheckLayer);
            animator.SetBool(animIDGrounded, isGrounded);

            if (isGrounded) return;

            RaycastHit hit;
            if(Physics.Raycast(groundChecker.GroundcheckerTransform.position, Vector3.down ,out hit, 100, data.GroundCheckLayer)) 
            {
                animator.SetFloat(animIDGroundCheckDistance, hit.distance);
            }
            else 
            {
                animator.SetFloat(animIDGroundCheckDistance, 100f);
            }
        }

        #endregion

        #region Movement

        private void MovementInput() 
        {
            if (GameManager.instace.mobileControl)
            {
                horizontal = joystick.Horizontal;
                vertical = joystick.Vertical;
            }
            else 
            {
                horizontal = Input.GetAxis("Horizontal");
                vertical = Input.GetAxis("Vertical");
            }            

            speed = new Vector2(horizontal, vertical).sqrMagnitude;
            speed = Mathf.Clamp(speed, 0, 1);

            if (speed > data.AllowPlayerMovement)
            {
                crossHairSettings.CrossHairSpread = Mathf.Lerp(crossHairSettings.CrossHairSpread,
                                                                crossHairSettings.MaxSpread,
                                                                Time.deltaTime * speed);

                if (isGrounded)
                {
                    if (isAimming)
                    {                        
                        animator.SetFloat(animIDHor, horizontal, data.StartAnimTime, Time.deltaTime);
                        animator.SetFloat(animIDVer, vertical, data.StartAnimTime, Time.deltaTime);
                    }
                    else 
                    {
                        animator.SetFloat(animIDSpeed, speed, data.StartAnimTime, Time.deltaTime);
                    }                    
                }
                Movement();

            }
            else 
            {
                crossHairSettings.CrossHairSpread = Mathf.Lerp(crossHairSettings.CrossHairSpread,
                                                                crossHairSettings.MinSpread,
                                                                Time.deltaTime * 5f);
                animator.SetFloat(animIDSpeed, 0,
                                        data.StopAnimTime, Time.deltaTime);
                animator.SetFloat(animIDHor, 0, data.StopAnimTime, Time.deltaTime);
                animator.SetFloat(animIDVer, 0, data.StopAnimTime, Time.deltaTime);
            }
        }

        private void Movement() 
        {
            var forward = playerCamera.transform.forward;
            var right = playerCamera.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            desiredMoveDirection = forward * vertical + right * horizontal;

            if (isAimming) playerVeloticy = data.AimMovement;            
            else playerVeloticy = data.MoveSpeed;

            if (!isAimming)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                                    Quaternion.LookRotation(desiredMoveDirection),
                                                    data.DesiredRotationSpeed);
            }
            transform.Translate(desiredMoveDirection * Time.deltaTime * playerVeloticy,
                                    Space.World);
        }

        #endregion

        #region Jump Input

        private void JumpInput() 
        {
            if (!isGrounded) return;

            if (GameManager.instace.mobileControl)
            {
                if (CrossPlatformInputManager.GetButtonDown("Jump"))
                {
                    OnJump();
                }
            }
            else 
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    OnJump();
                }
            }            
        }

        private void OnJump()
        {
            animator.SetTrigger(animIDJump);            
            float jumpForce = data.MoveSpeed * Mathf.Abs(Physics.gravity.y) * data.JumpForce;
            jumpForce = Mathf.Sqrt(jumpForce);            
            playerRigidbody.AddForce(transform.up * data.JumpForce,
                ForceMode.VelocityChange);

            crossHairSettings.CrossHairSpread = Mathf.Lerp(crossHairSettings.CrossHairSpread,
                                                    crossHairSettings.MaxSpread,
                                                    Time.deltaTime * jumpForce);
        }

        #endregion        
    }
}
