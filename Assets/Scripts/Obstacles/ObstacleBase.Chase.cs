using UnityEngine;

/// <summary>
/// Chase (optional, creatures only): when the player comes close, the creature runs at them.
/// If the player gets too far from the creature's home, it gives up, walks home, and goes back
/// to what it was doing (standing guard, wandering or patrolling).
/// </summary>
public abstract partial class ObstacleBase
{
    [Tooltip("Run at the player when they come close.")]
    public bool chasePlayer = false;
    [Tooltip("How close the player must be (in metres) before it starts chasing.")]
    [Range(1f, 50f)] public float chaseStartDistance = 8f;
    [Tooltip("It gives up when the player is this far (in metres) from where the creature started, then walks home.")]
    [Range(2f, 100f)] public float chaseGiveUpDistance = 20f;
    [Tooltip("How fast it runs while chasing.")]
    [Range(0.5f, 20f)] public float chaseSpeed = 4f;

    private enum ChaseState { Off, Chasing, GoingHome }
    private ChaseState chaseState;
    private bool homeSet;
    private Vector3 homeLocal;      // relative to the platform it rides, or a world spot
    private Transform homeParent;

    private Vector3 Home => homeParent != null ? homeParent.TransformPoint(homeLocal) : homeLocal;

    /// <summary>Where it walks back to after giving up (its starting spot; before Play, where it is now).</summary>
    public Vector3 ChaseHome => Application.isPlaying && homeSet ? Home : transform.position;

    /// <summary>The platform (or nothing) the creature's home spot moves with. Platform creatures override this.</summary>
    protected virtual Transform HomeSurface => null;

    /// <summary>True while chasing or walking home (then the normal movement waits).</summary>
    protected bool UpdateChase()
    {
        if (!chasePlayer || payloadType != PayloadType.Damage || defeated) return false;

#if HAS_KINEMATIC_CC
        KinematicCharacterController.KinematicCharacterMotor player = FindPlayer();
        if (player == null) return false;
        Vector3 playerPos = player.TransientPosition;
#else
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged == null) return false;
        Vector3 playerPos = tagged.transform.position;
#endif
        if (!homeSet)
        {
            homeParent = HomeSurface;
            homeLocal = homeParent != null ? homeParent.InverseTransformPoint(transform.position) : transform.position;
            homeSet = true;
        }

        Vector3 home = Home;
        float playerFromHome = Flat(playerPos - home).magnitude;
        float playerFromMe = Flat(playerPos - transform.position).magnitude;

        switch (chaseState)
        {
            case ChaseState.Off:
                if (playerFromMe <= chaseStartDistance && playerFromHome <= chaseGiveUpDistance) chaseState = ChaseState.Chasing;
                break;
            case ChaseState.Chasing:
                if (playerFromHome > chaseGiveUpDistance) chaseState = ChaseState.GoingHome;
                break;
            case ChaseState.GoingHome:
                // Turn round and chase again if the player comes back
                if (playerFromMe <= chaseStartDistance && playerFromHome <= chaseGiveUpDistance) chaseState = ChaseState.Chasing;
                else if (Flat(home - transform.position).magnitude < 0.5f) chaseState = ChaseState.Off;
                break;
        }

        if (chaseState == ChaseState.Chasing)
        {
            // Stop just short of the player, so it doesn't jitter on top of them
            if (playerFromMe > 0.6f) StepTowards(playerPos, chaseSpeed);
            else FaceTowards(playerPos);
            return true;
        }
        if (chaseState == ChaseState.GoingHome)
        {
            StepTowards(home, Mathf.Max(1f, chaseSpeed * 0.6f));
            return true;
        }
        return false;
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    protected void FaceTowards(Vector3 target)
    {
        Vector3 dir = Flat(target - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * 10f);
    }

    /// <summary>
    /// One step towards a spot. On the ground it follows the terrain's height.
    /// Platform creatures override this so they don't run off the edge.
    /// </summary>
    protected virtual void StepTowards(Vector3 target, float speed)
    {
        FaceTowards(target);
        Vector3 next = Vector3.MoveTowards(Flat(transform.position), Flat(target), speed * Time.deltaTime);
        float y = transform.position.y;
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null) y = terrain.SampleHeight(next) + terrain.transform.position.y;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic) { body.isKinematic = true; body.useGravity = false; }
        transform.position = new Vector3(next.x, y, next.z);
    }
}
