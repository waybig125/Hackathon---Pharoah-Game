using UnityEngine;
using WeirdBrothers;

[CreateAssetMenu(menuName = "WeirdBrothers/FSM/Actions/Attack")]
public class AttackAction : FSMAction
{
    public override void Execute(BaseStateMachine stateMachine)
    {
        if (stateMachine.Animator.GetBool(AIConstants.IsAttacking))
            return;

        if (stateMachine.Data.Attacks.Length > 0) 
        {
            stateMachine.transform.LookAtTargetWithoutChaningY(stateMachine.Target.position);

            if (stateMachine.Data.Attacks.Length == 1)
                stateMachine.Animator.SetTrigger(stateMachine.Data.Attacks[0]);
            else
                stateMachine.Animator.SetTrigger(stateMachine.Data.Attacks[Random.Range(0, stateMachine.Data.Attacks.Length)]);
        }
    }
}
