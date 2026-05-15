using UnityEngine;
using WeirdBrothers;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeirdBrothers/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Space]
    [Header("Weapon UI")]
    public Sprite WeaponImage;
    public string WeaponName;

    [Space]
    [Header("Weapon Equip")]
    public WeaponPositionData WeaponHolderTransform;
    public WeaponPositionData WeaponSlotTransform;    
    public WeaponPositionData MagTransform;

    [Space]
    [Header("Weapon Data")]
    public float Damage;
    public float FireRate;
    public int MagSize;
    public float Range;        
    public int WeaponIndex;
    public GameObject BulletCase;
    public float BulletEjectingSpeed = 5;
    public WeaponType weaponType;
    public FireMode fireMode = FireMode.Auto;

    [Space]
    [Header("Weapon Recoil")]
    public float Duration;
    public float HorizontalRecoil;
    public float VerticalRecoil;
    public float CrossHairSpread;
    public float WeaponSpread;

    [Space]
    [Header("Weapon Sounds")]
    public AudioClip BoltSound;    
    public AudioClip EmptySound;
    public AudioClip FireSound;
    public AudioClip MagInSound;
    public AudioClip MagOutSound;
}
