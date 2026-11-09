using System.Collections.Generic;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Makes a platform Crumbling (shakes, falls, comes back) or Bouncy (launches the player up).
/// Added by PlatformLogic when the game starts, from its "Special Platform" settings.
/// </summary>
public class SpecialPlatform : MonoBehaviour
{
    private enum State { Solid, Shaking, Gone }

    private PlatformLogic logic;
    private State state = State.Solid;
    private float timer;
    private float goneTime;
    private float bounceCooldown;

    private readonly List<Collider> colliders = new List<Collider>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Transform> visuals = new List<Transform>();
    private readonly List<Vector3> visualHome = new List<Vector3>();

#if HAS_KINEMATIC_CC
    private KinematicCharacterMotor player;
#endif

    private void Start()
    {
        logic = GetComponent<PlatformLogic>();
    }

    private void Update()
    {
        if (logic == null || logic.TargetChild == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        bool standing = PlayerStandingOnMe();

        if (logic.platformType == PlatformLogic.PlatformType.Bouncy)
        {
            bounceCooldown -= dt;
            if (standing && bounceCooldown <= 0f)
            {
                Launch();
                bounceCooldown = 0.3f;
            }
            return;
        }

        if (logic.platformType != PlatformLogic.PlatformType.Crumbling) return;

        switch (state)
        {
            case State.Solid:
                if (standing)
                {
                    CollectParts();
                    state = State.Shaking;
                    timer = logic.crumbleAfter;
                }
                break;

            case State.Shaking:
                timer -= dt;
                float shake = 0.04f + 0.08f * (1f - timer / Mathf.Max(0.01f, logic.crumbleAfter));
                for (int i = 0; i < visuals.Count; i++)
                {
                    if (visuals[i] != null) visuals[i].localPosition = visualHome[i] + Random.insideUnitSphere * shake / Mathf.Max(0.1f, logic.TargetChild.lossyScale.x);
                }
                if (timer <= 0f) Crumble();
                break;

            case State.Gone:
                goneTime += dt;
                // The pieces drop away, then disappear
                if (goneTime < 0.8f)
                {
                    foreach (Transform v in visuals) if (v != null) v.position += Vector3.down * (goneTime * 12f * dt);
                }
                else
                {
                    foreach (Renderer r in renderers) if (r != null) r.enabled = false;
                }

                if (logic.comeBackAfter > 0f && goneTime >= logic.comeBackAfter) ComeBack();
                break;
        }
    }

    private void CollectParts()
    {
        colliders.Clear(); renderers.Clear(); visuals.Clear(); visualHome.Clear();
        Transform target = logic.TargetChild;
        foreach (Collider c in target.GetComponentsInChildren<Collider>()) if (c.enabled && !c.isTrigger) colliders.Add(c);
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) if (r.enabled) renderers.Add(r);
        foreach (Transform child in target)
        {
            visuals.Add(child);
            visualHome.Add(child.localPosition);
        }
    }

    private void Crumble()
    {
        state = State.Gone;
        goneTime = 0f;
        foreach (Collider c in colliders) if (c != null) c.enabled = false;
        Sparkles.Burst(logic.TargetChild.position, PlatformLogic.CrumblingColour, Sparkles.Style.Poof);
    }

    private void ComeBack()
    {
        state = State.Solid;
        for (int i = 0; i < visuals.Count; i++) if (visuals[i] != null) visuals[i].localPosition = visualHome[i];
        foreach (Renderer r in renderers) if (r != null) r.enabled = true;
        foreach (Collider c in colliders) if (c != null) c.enabled = true;
    }

    private bool PlayerStandingOnMe()
    {
#if HAS_KINEMATIC_CC
        if (player == null) player = FindAnyObjectByType<KinematicCharacterMotor>();
        if (player == null) return false;
        CharacterGroundingReport ground = player.GroundingStatus;
        return ground.IsStableOnGround && ground.GroundCollider != null && ground.GroundCollider.transform.IsChildOf(logic.TargetChild);
#else
        return false;
#endif
    }

    private void Launch()
    {
#if HAS_KINEMATIC_CC
        player.ForceUnground();
        Vector3 v = player.BaseVelocity;
        v.y = logic.bounceStrength;
        player.BaseVelocity = v;
        Sparkles.Burst(player.TransientPosition, PlatformLogic.BouncyColour, Sparkles.Style.Ring);
#endif
    }
}
