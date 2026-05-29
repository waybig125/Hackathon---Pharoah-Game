using UnityEngine;

[CreateAssetMenu(menuName = "WeirdBrothers/FSM/Decisions/Enemy Detection")]
public class EnemyDetectionDecision : Decision
{
    public override bool Decide(BaseStateMachine state)
    {
        Collider[] rangeChecks = Physics.OverlapSphere(state.Eyes.position, state.Data.Radius, state.Data.TargetLayer);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - state.transform.position).normalized;

            if (Vector3.Angle(state.transform.forward, directionToTarget) < state.Data.Angle / 2)
            {
                float distanceToTarget = Vector3.Distance(state.Eyes.position, target.position);
                Debug.DrawRay(state.Eyes.position, directionToTarget, Color.white);
                if (!Physics.Raycast(state.Eyes.position, directionToTarget, distanceToTarget, state.Data.ObstructionMask))
                {
                    state.Target = rangeChecks[0].transform;
                    return true;
                }
                else
                {
                    state.Target = null;
                    return false;
                }
            }
            else
            {
                state.Target = null;
                return false;
            }
        }
        else if (state.Target != null)
        {
            state.Target = null;
            return false;
        }

        return false;
    }
}
