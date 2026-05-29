using UnityEngine;
using WeirdBrothers.AIHelper;

[CreateAssetMenu(menuName = "WeirdBrothers/Attacks/MeleeAttack")]
public class MeleeAttack : ScriptableObject
{
    public AttackPart AttackPart;
    public float AttackRadius;
    public bool IsHeavyAttack;
    public Vector3 Force;
    public bool LeftWeaponAttack = false;

    [Header("Attack Clip")]
    public AudioClip AttackClip;
}
