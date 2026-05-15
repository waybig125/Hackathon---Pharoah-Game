using UnityEngine;

namespace WeirdBrothers 
{    
    [System.Serializable]
    public class GroundCheckerSettings 
    {
        public Transform GroundcheckerTransform;        
    }

    [System.Serializable]
    public class CrossHairSettings
    {
        public Transform CrossHairTransform;
        [HideInInspector] public float CrossHairSpread;
        public float MinSpread;
        public float MaxSpread;
    }

    [System.Serializable]
    public class PlayerWeaponSettings
    {
        public Transform WeaponHolder;
        public Transform MagHolder;
        public Transform PrimaryWeaponSlots;
        public Transform SecondaryWeaponSlot;
        public LayerMask DamageLayer;
        public GameObject BulletHole;
        public LayerMask BulletHoleLayer;        
    }

    [System.Serializable]
    public class WeaponPositionData
    {
        public Vector3 Position;
        public Vector3 Rotation;
    }

    //enums
    public enum WeaponType
    {   
        PrimaryWeapon,
        SecondaryWeapon
    }

    public enum FireMode
    {
        Single,
        Auto
    }
}