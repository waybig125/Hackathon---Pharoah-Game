using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "WeirdBrothers/FSM/Actions/Patrol")]
public class PartrolAction : FSMAction
{
    public override void Execute(BaseStateMachine stateMachine)
    {
        if (stateMachine.Animator.GetBool(AIConstants.IsAttacking))
            return;

        if (stateMachine.Agent.remainingDistance > stateMachine.Data.StopDistance)
        {
            return;
        }

        var _moveToPosition = Vector3.zero;

        if (RandomPoint(stateMachine.transform.position, stateMachine.Data.WonderRange, out _moveToPosition))
        {
            stateMachine.Agent.updateRotation = true;
            stateMachine.Agent.SetDestination(_moveToPosition);
        }
    }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
}
