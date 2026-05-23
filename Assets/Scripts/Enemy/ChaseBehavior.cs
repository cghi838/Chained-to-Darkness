using UnityEngine;

public class ChaseBehavior : EnemyBehavior
{
    public override void HandleBehavior(bool canSeePlayer)
    {
        Vector2 target;

        if (canSeePlayer)
        {
            target = core.GetPlayer().position;
        }
        else if (core.isAggro)
        {
            target = core.lastKnownPlayerPosition;
        }
        else
        {
            return;
        }

        core.FaceTarget(target);

        core.MoveToward(target);
    }
}