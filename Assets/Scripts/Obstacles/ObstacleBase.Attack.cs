using UnityEngine;

/// <summary>
/// Attack (creatures only): when it hurts the player it plays its attack animation (if its model has one),
/// quickly turning to face the player while it does. Then it turns back and carries on as before
/// (standing guard, wandering, patrolling or chasing). It doesn't walk during the attack.
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("When it hurts you, it quickly turns to face you while its attack animation plays, then carries on.")]
    public bool faceYouWhenAttacking = true;

    private const float TurnBackTime = 0.35f;
    private float attackTimeLeft;
    private float turnBackLeft;
    private Transform attackTarget;
    private Transform turnParent;            // the platform it rides (it may turn while we attack)
    private Quaternion rotationBeforeAttack; // relative to turnParent, or in the world

    /// <summary>Plays the attack animation and starts facing the player. Returns true if the animation had its own sound.</summary>
    private bool StartAttack(Transform player)
    {
        if (!lookedForModel) { ownModel = GetComponentInChildren<CustomModel>(); lookedForModel = true; }
        if (ownModel == null) return false;

        float length = ownModel.PlayAttack(out bool playedSound);
        if (length > 0f && faceYouWhenAttacking && player != null)
        {
            // Remember how it was facing (only at the start of an attack, not when hit again mid-attack)
            if (attackTimeLeft <= 0f && turnBackLeft <= 0f)
            {
                FollowPlatform follow = GetComponent<FollowPlatform>();
                turnParent = follow != null ? follow.targetPlatform : null;
                rotationBeforeAttack = turnParent != null ? Quaternion.Inverse(turnParent.rotation) * transform.rotation : transform.rotation;
            }
            attackTarget = player;
            attackTimeLeft = length;
            turnBackLeft = 0f;
        }
        return playedSound;
    }

    /// <summary>Turns to face the player while attacking, then back. Returns true while it's busy (so it doesn't walk).</summary>
    private bool UpdateAttack()
    {
        float dt = Time.deltaTime;
        if (attackTimeLeft > 0f)
        {
            attackTimeLeft -= dt;
            if (attackTarget != null)
            {
                Vector3 toPlayer = attackTarget.position - transform.position;
                Vector3 up = turnParent != null ? turnParent.up : Vector3.up;
                toPlayer = Vector3.ProjectOnPlane(toPlayer, up);
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    // A quick snap round (about a tenth of a second)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized, up), dt * 25f);
                }
            }
            if (attackTimeLeft <= 0f) turnBackLeft = TurnBackTime;
            return true;
        }

        if (turnBackLeft > 0f)
        {
            turnBackLeft -= dt;
            Quaternion before = turnParent != null ? turnParent.rotation * rotationBeforeAttack : rotationBeforeAttack;
            transform.rotation = Quaternion.Slerp(transform.rotation, before, dt * 12f);
            return true;
        }
        return false;
    }
}
