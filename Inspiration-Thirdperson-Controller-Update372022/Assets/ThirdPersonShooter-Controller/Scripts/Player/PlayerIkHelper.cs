using UnityEngine;
using WeirdBrothers.IKHepler;

namespace WeirdBrothers 
{
    public partial class PlayerController : MonoBehaviour
    {
        [Space]
        [Header("Player IK")]
        [SerializeField]
        private UpperBodySettingsIK upperBodySettingsIK;

        private Vector3 spineRotation = Vector3.zero;

        private void PositionSpine() 
        {
            if (isAimming) 
            {
                if (spineRotation == Vector3.zero)
                {
                    spineRotation = upperBodySettingsIK.SpineRotation;
                }
                upperBodySettingsIK.Spine.LookAt(upperBodySettingsIK.LookAt);
                upperBodySettingsIK.Spine.Rotate(spineRotation);              
            }            
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (IsReloading()) 
            {
                if (currentEquipedWeapon.currentAmmo == currentEquipedWeapon.data.MagSize) return;
                if (currentEquipedWeapon.magRef == null) return;
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);                
                animator.SetIKPosition(AvatarIKGoal.LeftHand, currentEquipedWeapon.magRef.position);                
                return; 
            }

            if (currentEquipedWeapon != null && animator != null)
            {
                if (currentEquipedWeapon.leftHandRef == null) return;

                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);                
                animator.SetIKPosition(AvatarIKGoal.LeftHand, currentEquipedWeapon.leftHandRef.position);
                return;
            }
        }
    }
}
