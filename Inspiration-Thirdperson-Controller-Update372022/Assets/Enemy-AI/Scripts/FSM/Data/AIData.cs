using UnityEngine;

[CreateAssetMenu(fileName = "AIData", menuName = "WeirdBrothers/FSM/Data/AIData")]
public class AIData : ScriptableObject
{
	[Space]
	[Header("Animation Data")]
	[Range(0, 1f)]
	public float StartAnimTime = 0.1f;
	[Range(0, 1f)]
	public float StopAnimTime = 0.15f;

	[Space]
	[Header("Attack data")]
	public float Damage;
	[Space]
	public string[] Attacks;

	[Space]
	public float Health;

	[Space]
	[Header("Enemy Detection data")]
	[Range(0, 360)]
	public float Angle;
	public float Radius;	
	public float StopDistance;
	public float WonderRange = 10;
	public LayerMask TargetLayer;
	public LayerMask ObstructionMask;

	[Space]
	[Header("Movement data")]
	public float Speed;
}
