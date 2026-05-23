using UnityEngine;

public abstract class EnemyBehavior : MonoBehaviour
{
    protected EnemyCore core;

    public virtual void Initialize(EnemyCore coreRef)
    {
        core = coreRef;
    }
    public abstract void HandleBehavior(bool canSeePlayer);
    public virtual bool OverrideClamp() => false;
}
