using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {
        private bool isSwitching;
        
        private void SwitchInput() 
        {
            if (IsReloading()) return;
            if (isSwitching) return;

            if (GameManager.instace.mobileControl)
            {
                if (CrossPlatformInputManager.GetButtonDown("Switch1"))
                {
                    OnSwitch(1);
                }
                else if (CrossPlatformInputManager.GetButtonDown("Switch2"))
                {
                    OnSwitch(2);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    OnSwitch(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    OnSwitch(2);
                }
            }
        }

        private void OnSwitch(int index) 
        {
            if (index == 1)
            {
                if (PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform() != null)
                {
                    if (PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform().GetComponent<Weapon>().data.weaponType == WeaponType.SecondaryWeapon)
                    {
                        if(PlayerWeaponSettings.PrimaryWeaponSlots.GetActiveChildTransform())
                            animator.SetTrigger(animIDSecodarySwitch2);
                        else
                            animator.SetTrigger(animIDSecodarySwitch);
                    }
                    else
                    {
                        animator.SetTrigger(animIDPrimarySwitch);
                    }
                }
                else if (PlayerWeaponSettings.PrimaryWeaponSlots.GetActiveChildTransform() != null)
                {
                    animator.SetTrigger(animIDPrimarySwitch);
                }
            }
            else if (index == 2) 
            {
                if (PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform() != null)
                {
                    if (PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform().GetComponent<Weapon>().data.weaponType == WeaponType.PrimaryWeapon)
                    {
                        if(PlayerWeaponSettings.SecondaryWeaponSlot.GetActiveChildTransform())
                            animator.SetTrigger(animIDPrimarySwitch2);
                        else
                            animator.SetTrigger(animIDPrimarySwitch);
                    }
                    else
                    {
                        animator.SetTrigger(animIDSecodarySwitch);
                    }
                }
                else if (PlayerWeaponSettings.SecondaryWeaponSlot.GetActiveChildTransform() != null)
                {
                    animator.SetTrigger(animIDSecodarySwitch);
                }
            }
        }

        private void OnWeaponSwitch(int index)
        {
            if (PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform() != null)
            {
                Weapon weapon = PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform().GetComponent<Weapon>();
                if (weapon.data.weaponType == WeaponType.PrimaryWeapon)
                {
                    //equip weapon in slot
                    EquipWeapon(weapon, PlayerWeaponSettings.PrimaryWeaponSlots,
                                       weapon.data.WeaponSlotTransform.Position,
                                       weapon.data.WeaponSlotTransform.Rotation);
                }
                else if (weapon.data.weaponType == WeaponType.SecondaryWeapon)
                {
                    EquipWeapon(weapon, PlayerWeaponSettings.SecondaryWeaponSlot,
                                    weapon.data.WeaponSlotTransform.Position,
                                    weapon.data.WeaponSlotTransform.Rotation);
                }
            }
            else if (PlayerWeaponSettings.PrimaryWeaponSlots.GetActiveChildTransform() != null && index==1)
            {
                Weapon weapon = PlayerWeaponSettings.PrimaryWeaponSlots.GetActiveChildTransform().GetComponent<Weapon>();
                if (weapon.data.weaponType == WeaponType.PrimaryWeapon)
                {
                    //equip weapon in slot
                    EquipWeapon(weapon, PlayerWeaponSettings.WeaponHolder,
                                       weapon.data.WeaponHolderTransform.Position,
                                       weapon.data.WeaponHolderTransform.Rotation);
                }
            }
            else if (PlayerWeaponSettings.SecondaryWeaponSlot.GetActiveChildTransform() != null && index == 2)
            {
                Weapon weapon = PlayerWeaponSettings.SecondaryWeaponSlot.GetActiveChildTransform().GetComponent<Weapon>();
                if (weapon.data.weaponType == WeaponType.SecondaryWeapon)
                {
                    //equip weapon in slot
                    EquipWeapon(weapon, PlayerWeaponSettings.WeaponHolder,
                                       weapon.data.WeaponHolderTransform.Position,
                                       weapon.data.WeaponHolderTransform.Rotation);
                }
            }
        }

        private void OnSwitchStart() 
        {
            isSwitching = true;
        }

        private void OnSwitchEnd()
        {
            isSwitching = false;
        }
    }
}
