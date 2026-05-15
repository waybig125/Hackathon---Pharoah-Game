using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {
        [Header("Pickup Item Data")]
        [Space]
        [SerializeField]
        private GameObject pickupItemPanel;
        [SerializeField]
        private Text itemText;
        [SerializeField]
        private Image itemImage;

        private GameObject currentPickUpItem;

        private void PickUpItemInput()
        {
            if (IsReloading()) return;

            if (GameManager.instace.mobileControl)
            {
                if (CrossPlatformInputManager.GetButtonDown("Pickup"))
                {
                    if (currentPickUpItem != null) animator.SetTrigger(animIDPickUp);
                }
            }
            else 
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (currentPickUpItem != null) animator.SetTrigger(animIDPickUp);
                }
            }            
        }

        private void OnItemEquip() 
        {
            if (isAimming) Aim(false);

            if (currentPickUpItem != null) 
            {                
                if (currentPickUpItem.CompareTag("Weapon")) 
                {                    
                    Weapon currentPickUpWeapon = currentPickUpItem.GetComponent<Weapon>();                    
                    OnWeaponPickUp(currentPickUpWeapon);
                    HidePickUpMenu();
                }
            }
        }

        private void OnWeaponPickUp(Weapon pickUpWeapon) 
        {            
            if (pickUpWeapon.data.weaponType == WeaponType.PrimaryWeapon) 
            {                
                EquipPickedWeapon(pickUpWeapon, PlayerWeaponSettings.PrimaryWeaponSlots);
            }
            else if (pickUpWeapon.data.weaponType == WeaponType.SecondaryWeapon)
            {
                EquipPickedWeapon(pickUpWeapon, PlayerWeaponSettings.SecondaryWeaponSlot);
            }
        }

        private void EquipPickedWeapon(Weapon pickUpWeapon, Transform weaponSlot) 
        {
            //check if player has weapon in hands
            if (currentEquipedWeapon != null)
            {                
                //check if pick up weapon and equiped weapons are same
                if (currentEquipedWeapon.data.weaponType == pickUpWeapon.data.weaponType)
                {
                    //if both are same then drop current weapon and equip picked up weapon
                    Dropweapon(currentEquipedWeapon.transform);
                    EquipWeapon(pickUpWeapon, PlayerWeaponSettings.WeaponHolder,
                                   pickUpWeapon.data.WeaponHolderTransform.Position,
                                   pickUpWeapon.data.WeaponHolderTransform.Rotation);
                }
                else //check if player has same type of weapon in slot
                {
                    if (weaponSlot.GetActiveChildTransform() != null)
                    {
                        //drop weapon from slot and equip picked up weapon in slot
                        Dropweapon(weaponSlot.GetActiveChildTransform());
                        if (weaponSlot.name == PlayerWeaponSettings.PrimaryWeaponSlots.name)
                        {
                            EquipWeapon(pickUpWeapon, PlayerWeaponSettings.PrimaryWeaponSlots,
                                       pickUpWeapon.data.WeaponSlotTransform.Position,
                                       pickUpWeapon.data.WeaponSlotTransform.Rotation);
                        }
                        else
                        {
                            EquipWeapon(pickUpWeapon, PlayerWeaponSettings.SecondaryWeaponSlot,
                                       pickUpWeapon.data.WeaponSlotTransform.Position,
                                       pickUpWeapon.data.WeaponSlotTransform.Rotation);
                        }
                    }
                    else
                    {
                        //equip weapon in weapon slot
                        if (weaponSlot.name == PlayerWeaponSettings.PrimaryWeaponSlots.name) 
                        {
                            EquipWeapon(pickUpWeapon, PlayerWeaponSettings.PrimaryWeaponSlots,
                                       pickUpWeapon.data.WeaponSlotTransform.Position,
                                       pickUpWeapon.data.WeaponSlotTransform.Rotation);
                        }
                        else 
                        {
                            EquipWeapon(pickUpWeapon, PlayerWeaponSettings.SecondaryWeaponSlot,
                                       pickUpWeapon.data.WeaponSlotTransform.Position,
                                       pickUpWeapon.data.WeaponSlotTransform.Rotation);
                        }                       
                    }
                }
            }
            else //if player had no weapon in hands 
            {
                //check if player already has weapon in slot
                if (weaponSlot.GetActiveChildTransform() != null)
                {
                    //drop weapon from slot and equip picked up weapon in slot
                    Dropweapon(weaponSlot.GetActiveChildTransform());
                    if (weaponSlot.name == PlayerWeaponSettings.PrimaryWeaponSlots.name)
                    {
                        EquipWeapon(pickUpWeapon, PlayerWeaponSettings.PrimaryWeaponSlots,
                                   pickUpWeapon.data.WeaponSlotTransform.Position,
                                   pickUpWeapon.data.WeaponSlotTransform.Rotation);
                    }
                    else
                    {
                        EquipWeapon(pickUpWeapon, PlayerWeaponSettings.SecondaryWeaponSlot,
                                   pickUpWeapon.data.WeaponSlotTransform.Position,
                                   pickUpWeapon.data.WeaponSlotTransform.Rotation);
                    }
                }
                else //equip weapon in hands
                {
                    EquipWeapon(pickUpWeapon, PlayerWeaponSettings.WeaponHolder,
                                    pickUpWeapon.data.WeaponHolderTransform.Position,
                                    pickUpWeapon.data.WeaponHolderTransform.Rotation);
                }
            }
        }

        private void EquipWeapon(Weapon equipWeapon,Transform weaponSlotTransform, Vector3 position, Vector3 rotation) 
        {
            if (equipWeapon.data.weaponType == WeaponType.PrimaryWeapon)
            {
                primaryWeaponImage.enabled = true;
                primaryWeaponImage.sprite = equipWeapon.gameObject.GetItemImage();
                primaryWeaponAmmo.enabled = true;
                primaryWeaponAmmo.text = equipWeapon.currentAmmo + "/" + equipWeapon.totalAmmo;
            }
            else if (equipWeapon.data.weaponType == WeaponType.SecondaryWeapon)
            {
                secondaryWeaponImage.enabled = true;
                secondaryWeaponImage.sprite = equipWeapon.gameObject.GetItemImage();
                secondaryWeaponAmmo.enabled = true;
                secondaryWeaponAmmo.text = equipWeapon.currentAmmo + "/" + equipWeapon.totalAmmo;
            }

            currentPickUpItem = null;
            equipWeapon.gameObject.layer = LayerMask.NameToLayer("Equiped");
            Destroy(equipWeapon.gameObject.GetComponent<Rigidbody>());

            equipWeapon.transform.SetParent(weaponSlotTransform);
            equipWeapon.transform.localPosition = position;
            equipWeapon.transform.localRotation = Quaternion.Euler(rotation);
        }

        private void Dropweapon(Transform weaponToDrop) 
        {
            weaponToDrop.SetParent(null);
            weaponToDrop.gameObject.layer = LayerMask.NameToLayer("PickUpItem");
            weaponToDrop.gameObject.AddComponent<Rigidbody>().AddForce(transform.up * 100 * Time.deltaTime, ForceMode.Impulse);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("PickUpItem"))
            {
                currentPickUpItem = other.gameObject;
                ShowPickUpMenu();
            }
        }

        private void OnTriggerStay(Collider other)
        {      
            if (other.gameObject.layer == LayerMask.NameToLayer("PickUpItem"))
            {
                currentPickUpItem = other.gameObject;
                ShowPickUpMenu();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("PickUpItem"))
            {
                currentPickUpItem = null;
                HidePickUpMenu();
            }
        }

        private void ShowPickUpMenu() 
        {
            if (currentPickUpItem != null) 
            {
                pickupItemPanel.SetActive(true);
                itemText.text = currentPickUpItem.GetItemName() == null ? "" : currentPickUpItem.GetItemName();
                itemImage.sprite = currentPickUpItem.GetItemImage() == null ? null : currentPickUpItem.GetItemImage();
            }
        }

        private void HidePickUpMenu()
        {
            pickupItemPanel.SetActive(false);
            itemText.text = " ";
            itemImage.sprite = null;
        }
    }
}
