using UnityEngine;

[CreateAssetMenu(menuName = "WeirdBrothers/FSM/Actions/Chase")]
public class ChaseAction : FSMAction
{
    public override void Execute(BaseStateMachine stateMachine)
    {
        if (stateMachine.Target == null)
            return;
        if (stateMachine.Animator.GetBool(AIConstants.IsAttacking))
            return;

        stateMachine.Agent.SetDestination(stateMachine.Target.position);
    }
}
