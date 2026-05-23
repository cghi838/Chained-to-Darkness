using UnityEngine;

public class RangedBehavior : EnemyBehavior
{
    [SerializeField] private float preferredDistance = 4f;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float fireCooldown = 1.5f;

    private float lastShotTime;

    public override void HandleBehavior(bool canSeePlayer)
    {
        if (!core.isAggro)
            return;

        Vector2 playerPos = core.GetPlayer().position;

        core.FaceTarget(playerPos);

        float dist = Vector2.Distance(core.GetRB().position, playerPos);

        /*
        if (dist < preferredDistance)
        {
            core.MoveAway(playerPos);
        }
        else
        {
            //Stays put
        } */

        if (canSeePlayer && Time.time > lastShotTime + fireCooldown)
        {
            Shoot(playerPos);
            lastShotTime = Time.time;
        }
    }

    private void Shoot(Vector2 target)
    {
        GameObject proj = Instantiate(projectile, core.transform.position, Quaternion.identity);
        proj.GetComponent<Bullet>().SetAnchorPosAndRadius(core.transform, core.GetAggroRadius()); // Set anchor position and radius for bullet
        proj.GetComponent<Rigidbody2D>().linearVelocity =
            (target - (Vector2)core.transform.position).normalized * 5f;
    }
}