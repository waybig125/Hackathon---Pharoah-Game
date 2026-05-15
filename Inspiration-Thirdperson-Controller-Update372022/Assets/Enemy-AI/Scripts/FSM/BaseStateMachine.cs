using UnityEngine;
using WeirdBrothers;
using UnityEngine.AI;
using WeirdBrothers.AIHelper;

public class BaseStateMachine : MonoBehaviour,IDamageable
{
    [Header("AI Data")]
    public AIData Data;

    [Space]
    [SerializeField] private BaseState _initialState;
    public BaseState CurrentState { get; set; }

    [HideInInspector] public NavMeshAgent Agent;
    [HideInInspector] public Animator Animator;
    [HideInInspector] public Transform Eyes;
    [HideInInspector] public Transform Target;

    private float _speedAmount;
    private Health _health;
    private bool _isdead;

    private void Awake()
    {
        AssignComponents();
        AssignDefaultData();

        var skinmesh = GetComponentInChildren<SkinnedMeshRenderer>();
        Debug.Log(skinmesh.material.enableInstancing);

    }

    private void AssignComponents()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        _health = GetComponent<Health>();
    }

    private void AssignDefaultData()
    {
        Agent.speed = Data.Speed;
        Agent.stoppingDistance = Data.StopDistance;

        CurrentState = _initialState;
        Eyes = Animator.GetBoneTransform(HumanBodyBones.Head);
        _health.CurrrentHealth = Data.Health;
    }

    private void Update()
    {
        if (_health.CurrrentHealth <= 0)
        {
            if (!_isdead)
            {
                Agent.isStopped = true;
                Animator.SetTrigger(AIConstants.Dead);
                _isdead = true;
                Destroy(gameObject, 5f);
            }
            return;
        }

        if (CurrentState != null)
            CurrentState.Execute(this);

        if (Agent.remainingDistance > Agent.stoppingDistance) Move(Agent.velocity);
        else Move(Vector3.zero);
    }

    private void Move(Vector3 move)
    {
        _speedAmount = move.sqrMagnitude;
        _speedAmount = Mathf.Clamp(_speedAmount, 0, 1);

        if (_speedAmount <= 0) Animator.SetFloat(AIConstants.Speed, _speedAmount, Data.StopAnimTime, Time.deltaTime);
        else Animator.SetFloat(AIConstants.Speed, _speedAmount, Data.StartAnimTime, Time.deltaTime);
    }

    private void OnAttack(MeleeAttack meleeAttack)
    {
        if (meleeAttack.AttackPart == AttackPart.LeftHand)
        {
            CheckForEnemies(Animator.GetBoneTransform(HumanBodyBones.LeftHand), meleeAttack);
        }
        else if (meleeAttack.AttackPart == AttackPart.RightHand)
        {
            CheckForEnemies(Animator.GetBoneTransform(HumanBodyBones.RightHand), meleeAttack);
        }
    }

    private void CheckForEnemies(Transform obj, MeleeAttack meleeAttack)
    {
        Collider[] hittedEnemies = Physics.OverlapSphere(obj.position, meleeAttack.AttackRadius, Data.TargetLayer);

        foreach (Collider col in hittedEnemies) 
        {
            col.gameObject.ApplyDamage(Data.Damage, transform.position);
        }
    }

    public void ApplyDamage(float damage, Vector3 damagePoint)
    {
        if (_isdead)
            return;

        _health.ApplyDamage(damage);
    }
}
