using Cinemachine;
using UnityEngine;
using WeirdBrothers.IKHepler;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour,IDamageable
    {
        [Header("Player Data")]
        [Space]
        public PlayerData data;

        //CrossHair
        [Header("Player CrossHair Data")]
        public CrossHairSettings crossHairSettings;

        [HideInInspector] public bool IsTakingDamage;

        private Animator animator;
        private RectTransform crossHair;
        private Camera playerCamera;        
        private Rigidbody playerRigidbody;
        private float time;
        private CinemachinePOV pov;
        private RuntimeAnimatorController defaultAnimator;

        //Animatino ids
        private int animIDAim;
        private int animIDGroundCheckDistance;
        private int animIDGrounded;
        private int animIDJump;
        private int animIDPickUp;
        private int animIDReload;
        private int animIDSpeed;
        private int animIDWeaponIndex;
        private int animIDHor;
        private int animIDVer;
        private int animIDPrimarySwitch;
        private int animIDPrimarySwitch2;
        private int animIDSecodarySwitch;
        private int animIDSecodarySwitch2;

        private Health health;

        private void Awake()
        {
            AssignComponents();
            AssignDefaultData();
            AssignAnimationIDs();
        }

        private void AssignComponents()
        {
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();
            joystick = GameObject.FindObjectOfType<FixedJoystick>();
            playerCamera = Camera.main;
            playerRigidbody = GetComponent<Rigidbody>();
            pov = aimCamera.GetCinemachineComponent<CinemachinePOV>();
            defaultAnimator = animator.runtimeAnimatorController;
        }

        private void AssignDefaultData() 
        {
            CinemachineCore.GetInputAxis = HandleAxisInputDelegate;
            HidePickUpMenu();
            health.CurrrentHealth = 100;
            primaryWeaponImage.enabled = false;
            primaryWeaponAmmo.enabled = false;
            secondaryWeaponImage.enabled = false;
            secondaryWeaponAmmo.enabled = false;
        }

        private void AssignAnimationIDs()
        {
            animIDAim = Animator.StringToHash("aim");
            animIDGroundCheckDistance = Animator.StringToHash("groundCheckDistance");
            animIDGrounded = Animator.StringToHash("grounded");
            animIDJump = Animator.StringToHash("jump");
            animIDPickUp = Animator.StringToHash("pickUp");
            animIDReload = Animator.StringToHash("reload");
            animIDSpeed = Animator.StringToHash("speed");
            animIDWeaponIndex = Animator.StringToHash("weaponIndex");
            animIDHor= Animator.StringToHash("hor");
            animIDVer = Animator.StringToHash("ver");
            animIDPrimarySwitch = Animator.StringToHash("primarySwitch");
            animIDPrimarySwitch2 = Animator.StringToHash("primarySwitch2");
            animIDSecodarySwitch = Animator.StringToHash("secodarySwitch");
            animIDSecodarySwitch2 = Animator.StringToHash("secodarySwitch2");
        }

        private void Update()
        {
            AimPosition();
            AimInput();
            CheckForEquipedWeapon();
            GroundChecker();
            FireInput();
            JumpInput();
            MovementInput();
            PickUpItemInput();
            ReloadInput();
            SwitchInput();          
            

            GetCrossHair();
            if (crossHair)
            {
                crossHairSettings.CrossHairSpread = Mathf.Clamp(crossHairSettings.CrossHairSpread, crossHairSettings.MinSpread, crossHairSettings.MaxSpread);
                crossHair.sizeDelta = new Vector2(crossHairSettings.CrossHairSpread,
                    crossHairSettings.CrossHairSpread);
            }

            if (currentEquipedWeapon)
            {
                if (time > 0)
                {
                    pov.m_VerticalAxis.Value -= currentEquipedWeapon.data.VerticalRecoil;
                    pov.m_HorizontalAxis.Value -= Random.Range(-currentEquipedWeapon.data.HorizontalRecoil, currentEquipedWeapon.data.HorizontalRecoil);
                    time -= Time.deltaTime;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isAimming)
            {
                Vector3 aimDirection = playerCamera.transform.forward;
                aimDirection.y = 0;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(aimDirection), 20f);
            }
        }

        private void LateUpdate()
        {
            PositionSpine();
        }

        private void GetCrossHair() 
        {
            crossHair = crossHairSettings.CrossHairTransform.GetActiveChildTransform().GetComponent<RectTransform>();
        }

        private void PlayAudioClip(AudioClip clip, AudioSource source)
        {
            if (source && clip)
            {
                source.PlayOneShot(clip);
            }
        }

        public void ApplyDamage(float damage, Vector3 damagePoint)
        {
            if (health.CurrrentHealth < 0)
                return;
            health.ApplyDamage(damage);
        }
    }   
}
