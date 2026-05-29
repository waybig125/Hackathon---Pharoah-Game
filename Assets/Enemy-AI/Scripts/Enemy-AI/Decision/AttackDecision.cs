using UnityEngine;

[CreateAssetMenu(menuName = "WeirdBrothers/FSM/Decisions/Attack Decision")]
public class AttackDecision : Decision
{
    public override bool Decide(BaseStateMachine state)
    {
        if (Vector3.Distance(state.transform.position, state.Target.position) <= (state.Data.StopDistance + 0.5f))
            return true;
        else
            return false;
    }
}
