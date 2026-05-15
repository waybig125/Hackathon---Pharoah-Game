using UnityEngine;
using Cinemachine;
using UnityStandardAssets.CrossPlatformInput;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {        
        [Space]
        [Header("Aim Data")]
        [SerializeField]
        private CinemachineVirtualCamera aimCamera;

        private bool isAimming;
        private int priorityBoostAmount = 10;

        private void AimInput()
        {
            if (currentEquipedWeapon == null) 
            {
                if (isAimming) Aim(false);
                return; 
            }
            if (IsReloading()) return;
            if (isSwitching) return;

            if (GameManager.instace.mobileControl)
            {
                if (CrossPlatformInputManager.GetButtonDown("Aim"))
                {
                    if (!isAimming)
                    {
                        Aim(true);
                    }
                    else
                    {
                        Aim(false);
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (!isAimming)
                    {
                        Aim(true);
                    }
                    else
                    {
                        Aim(false);
                    }
                }
            }
        }

        public void Aim(bool state)
        {
            isAimming = state;
            animator.SetBool(animIDAim, state);

            if (state)
            {
                animator.SetLayerWeight(animator.GetLayerIndex("WeaponAim"), 1);
                aimCamera.Priority += priorityBoostAmount;
            }
            else
            {
                animator.SetLayerWeight(animator.GetLayerIndex("WeaponAim"), 0);
                aimCamera.Priority -= priorityBoostAmount;
            }
        }
    }
}
