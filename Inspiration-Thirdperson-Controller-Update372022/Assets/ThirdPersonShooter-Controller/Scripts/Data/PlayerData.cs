using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "WeirdBrothers/Player/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Space]
    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    [Space]
    [Header("Movement data")]
    public float AimMovement;
	public float AllowPlayerMovement = 0.1f;
    public float DesiredRotationSpeed = 0.1f;
    public float GroundCheckDistance;
	public LayerMask GroundCheckLayer;    
	public float JumpForce;
	public float MoveSpeed;    
}
