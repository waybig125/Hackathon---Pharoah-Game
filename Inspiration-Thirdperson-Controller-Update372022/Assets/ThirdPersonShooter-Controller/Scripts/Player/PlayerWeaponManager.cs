using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using WeirdBrothers.AnimationHelper;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {
        [Header("Player Weapon Data")]
        [Space]
        [SerializeField]
        private PlayerWeaponSettings PlayerWeaponSettings;

        [Header("Player Weapon UI")]
        [Space]
        [SerializeField]
        private Image primaryWeaponImage;
        [SerializeField]
        private Text primaryWeaponAmmo;
        [SerializeField]
        private Image secondaryWeaponImage;
        [SerializeField]
        private Text secondaryWeaponAmmo;

        private Vector3 aimPosition;
        private Weapon currentEquipedWeapon;
        private AudioSource weaponAudioSource;

        private void CheckForEquipedWeapon() 
        {
            Transform weaponTranform = PlayerWeaponSettings.WeaponHolder.GetActiveChildTransform();
            if (weaponTranform)
            {
                currentEquipedWeapon = weaponTranform.GetComponent<Weapon>();
                weaponAudioSource = weaponTranform.GetComponent<AudioSource>();
                animator.SetLayerWeight(animator.GetLayerIndex("WeaponHolder"), 1);
                animator.SetFloat(animIDWeaponIndex, currentEquipedWeapon.data.WeaponIndex);

                if (currentEquipedWeapon.animator) animator.runtimeAnimatorController = currentEquipedWeapon.animator;
                else animator.runtimeAnimatorController = defaultAnimator;
            }
            else 
            {
                currentEquipedWeapon = null;
                weaponAudioSource = null;
                animator.SetLayerWeight(animator.GetLayerIndex("WeaponHolder"), 0);
                animator.SetFloat(animIDWeaponIndex, 0);
            }
        }

        private void AimPosition()
        {
            if (currentEquipedWeapon == null) return;
            if (!isAimming) return;

            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, currentEquipedWeapon.data.Range))
            {
                aimPosition = hit.point;
            }
            else
                aimPosition = Vector3.zero;
        }

        #region Fire Input

        private void FireInput() 
        {
            if (currentEquipedWeapon == null) return;
            if (!isAimming) return;

            if (GameManager.instace.mobileControl)
            {
                if (currentEquipedWeapon.data.fireMode == FireMode.Auto)
                {
                    if (CrossPlatformInputManager.GetButton("Fire"))
                    {
                        OnFire();
                    }
                }
                else if (currentEquipedWeapon.data.fireMode == FireMode.Single)
                {
                    if (CrossPlatformInputManager.GetButtonDown("Fire"))
                    {
                        OnFire();
                    }
                }
            }
            else
            {
                if (currentEquipedWeapon.data.fireMode == FireMode.Auto)
                {
                    if (Input.GetMouseButton(0))
                    {
                        OnFire();
                    }
                }
                else if (currentEquipedWeapon.data.fireMode == FireMode.Single)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        OnFire();
                    }
                }
            }
        }

        private void OnFire() 
        {
            if (IsReloading()) return;

            if (Time.time > currentEquipedWeapon.nextFire) 
            {
                currentEquipedWeapon.nextFire = Time.time + currentEquipedWeapon.data.FireRate;
                if (currentEquipedWeapon.currentAmmo <= 0) 
                {
                    PlayAudioClip(currentEquipedWeapon.data.EmptySound, weaponAudioSource);
                    return;
                }

                GenerateRecoil();
                crossHairSettings.CrossHairSpread += currentEquipedWeapon.data.CrossHairSpread;
                currentEquipedWeapon.currentAmmo--;
                currentEquipedWeapon.OnCaseOut();
                currentEquipedWeapon.muzzelFlash.Play();
                PlayAudioClip(currentEquipedWeapon.data.FireSound, weaponAudioSource);

                if (currentEquipedWeapon.data.weaponType == WeaponType.PrimaryWeapon)
                    primaryWeaponAmmo.text = currentEquipedWeapon.currentAmmo + "/" + currentEquipedWeapon.totalAmmo;
                else
                    secondaryWeaponAmmo.text = currentEquipedWeapon.currentAmmo + "/" + currentEquipedWeapon.totalAmmo;

                Vector3 aimDir = (aimPosition - currentEquipedWeapon.firePoint.position).normalized;

                RaycastHit hit;
                if (aimPosition != Vector3.zero)
                {
                    Vector3 targetPosition = aimPosition;
                    Vector3 directionTowardsEnemy = targetPosition - currentEquipedWeapon.firePoint.position;
                    if (Physics.Raycast(currentEquipedWeapon.firePoint.position, CalculatelateSpread(currentEquipedWeapon.data.WeaponSpread, directionTowardsEnemy), out hit, currentEquipedWeapon.data.Range, PlayerWeaponSettings.DamageLayer))
                    {
                        OnTargetHit(hit);
                    }
                }
            }
        }

        private void OnTargetHit(RaycastHit hit)
        {            
            hit.transform.gameObject.ApplyDamage(currentEquipedWeapon.data.Damage, hit.point);
            if (hit.transform.gameObject.IsInLayerMask(PlayerWeaponSettings.BulletHoleLayer))
            {
                GameObject bulletHole = Instantiate(PlayerWeaponSettings.BulletHole, hit.point + (hit.normal * 0.01f), Quaternion.FromToRotation(-Vector3.forward, hit.normal));
                bulletHole.transform.SetParent(hit.transform);
                Destroy(bulletHole, 10);
            }
        }
        private void GenerateRecoil()
        {
           time = currentEquipedWeapon.data.Duration;
        }

        private Vector3 CalculatelateSpread(float inaccuracy, Vector3 direction)
        {
            direction.x += Random.Range(-inaccuracy, inaccuracy);
            direction.y += Random.Range(-inaccuracy, inaccuracy);
            direction.z += Random.Range(-inaccuracy, inaccuracy);
            return direction;
        }

        #endregion

        #region Realod Input

        private void ReloadInput()
        {
            if (currentEquipedWeapon == null) return;
            if (isSwitching) return;

            if (GameManager.instace.mobileControl)
            {
                if (CrossPlatformInputManager.GetButtonDown("Reload"))
                {
                    OnReload();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    OnReload();
                }
            }
        }

        private void OnReload()
        {            
            if (IsReloading())
                return;
            if (currentEquipedWeapon.currentAmmo == currentEquipedWeapon.data.MagSize)
                return;            
            if (currentEquipedWeapon.totalAmmo <= 0)
                return;           

            animator.SetTrigger(animIDReload);
        }

        private bool IsReloading()
        {
            if (animator.AnimatorIsPlaying("Reload", animator.GetLayerIndex("WeaponAim")))
                return true;

            if (animator.AnimatorIsPlaying("Reload", animator.GetLayerIndex("WeaponHolder")))
                return true;

            return false;
        }

        private void UpdateBulletOnReload()
        {
            OnMagIn();

            if (currentEquipedWeapon.totalAmmo <= 0) return;

            //new
            int bulletsToLoad = currentEquipedWeapon.data.MagSize - currentEquipedWeapon.currentAmmo;
            int bulletsToDeduct = currentEquipedWeapon.totalAmmo >= bulletsToLoad ? bulletsToLoad : currentEquipedWeapon.totalAmmo;

            currentEquipedWeapon.totalAmmo -= bulletsToDeduct;
            currentEquipedWeapon.currentAmmo += bulletsToDeduct;

            if(currentEquipedWeapon.data.weaponType==WeaponType.PrimaryWeapon) 
                primaryWeaponAmmo.text = currentEquipedWeapon.currentAmmo + "/" + currentEquipedWeapon.totalAmmo;
            else
                secondaryWeaponAmmo.text = currentEquipedWeapon.currentAmmo + "/" + currentEquipedWeapon.totalAmmo;
        }

        private void OnMagOut()
        {
            PlayAudioClip(currentEquipedWeapon.data.MagOutSound, weaponAudioSource);

            if (currentEquipedWeapon.mag == null) return;
            currentEquipedWeapon.mag.SetParent(PlayerWeaponSettings.MagHolder);            
        }

        private void OnMagIn()
        {
            PlayAudioClip(currentEquipedWeapon.data.MagInSound, weaponAudioSource);

            if (currentEquipedWeapon.mag == null) return;
            currentEquipedWeapon.mag.SetParent(currentEquipedWeapon.transform);
            currentEquipedWeapon.mag.localPosition = currentEquipedWeapon.data.MagTransform.Position;
            currentEquipedWeapon.mag.localRotation = Quaternion.Euler(currentEquipedWeapon.data.MagTransform.Rotation);            
        }

        private void DropMag()
        {
            if (currentEquipedWeapon.mag == null) return;
            currentEquipedWeapon.mag.gameObject.SetActive(false);
            GameObject weaponMag = Instantiate(currentEquipedWeapon.mag.gameObject, currentEquipedWeapon.mag.position, currentEquipedWeapon.mag.rotation);
            weaponMag.transform.SetParent(null);
            weaponMag.SetActive(true);
            weaponMag.AddComponent<Rigidbody>();
            weaponMag.AddComponent<MeshCollider>().convex = true;

            Destroy(weaponMag, 10f);
        }

        private void PickNewMag()
        {
            if (currentEquipedWeapon.mag == null) return;
            currentEquipedWeapon.mag.gameObject.SetActive(true);
        }

        private void OnBolt()
        {
            PlayAudioClip(currentEquipedWeapon.data.BoltSound, weaponAudioSource);
        }

        #endregion
    }
}
