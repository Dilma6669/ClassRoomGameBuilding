using System.Collections.Generic;
using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Makes a platform Crumbling (shakes and flashes, falls, comes back) or Bouncy (launches the player up),
/// and plays its landing sparkles. Added by PlatformLogic when the game starts, from its settings.
/// </summary>
public class SpecialPlatform : MonoBehaviour
{
    private enum State { Solid, Shaking, Gone }

    private PlatformLogic logic;
    private State state = State.Solid;
    private float timer;
    private float goneTime;
    private float bounceCooldown;
    private bool wasStanding;
    private float lastLandEffect = -10f;

    private readonly List<Collider> colliders = new List<Collider>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Transform> visuals = new List<Transform>();
    private readonly List<Vector3> visualHome = new List<Vector3>();
    private MaterialPropertyBlock flashBlock;
    private float flashAmount; // 0..1, shown in LateUpdate

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
        bool justLanded = standing && !wasStanding;
        wasStanding = standing;

        if (justLanded && Time.time - lastLandEffect > 0.3f)
        {
            lastLandEffect = Time.time;
            Sparkles.Play(logic.landEffect, PlayerFeet(), logic.effectColour, logic.effectAmount, logic.effectSize);
        }

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
                float shaken = logic.crumbleAfter - timer; // seconds since it started shaking
                float shake = 0.04f + 0.08f * (shaken / Mathf.Max(0.01f, logic.crumbleAfter));
                for (int i = 0; i < visuals.Count; i++)
                {
                    if (visuals[i] != null) visuals[i].localPosition = visualHome[i] + Random.insideUnitSphere * shake / Mathf.Max(0.1f, logic.TargetChild.lossyScale.x);
                }

                // A flash every few seconds (bright at once, then fading): players can count down to the break
                flashAmount = logic.warningFlash ? 1f - Mathf.Repeat(shaken, logic.flashEvery) / logic.flashEvery : 0f;
                flashAmount *= flashAmount;
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

    private void LateUpdate()
    {
        if (logic == null || logic.platformType != PlatformLogic.PlatformType.Crumbling) return;
        ShowFlash(state == State.Shaking ? flashAmount : 0f);
    }

    /// <summary>Tints the platform towards the flash colour (and makes its glow flash, if it has one).</summary>
    private void ShowFlash(float amount)
    {
        Glow glow = logic.TargetChild.GetComponent<Glow>();
        if (glow != null) glow.flash = amount;

        if (renderers.Count == 0) return;
        if (flashBlock == null) flashBlock = new MaterialPropertyBlock();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Color normal = logic.colourIt ? PlatformLogic.CrumblingColour
                         : (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color") ? r.sharedMaterial.color : Color.white);
            r.GetPropertyBlock(flashBlock);
            Color c = Color.Lerp(normal, logic.flashColour, amount * 0.85f);
            flashBlock.SetColor("_Color", c);
            flashBlock.SetColor("_BaseColor", c);
            r.SetPropertyBlock(flashBlock);
        }
    }

    private void CollectParts()
    {
        colliders.Clear(); renderers.Clear(); visuals.Clear(); visualHome.Clear();
        Transform target = logic.TargetChild;
        foreach (Collider c in target.GetComponentsInChildren<Collider>()) if (c.enabled && !c.isTrigger) colliders.Add(c);
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is ParticleSystemRenderer)) renderers.Add(r);
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
        flashAmount = 0f;
        foreach (Collider c in colliders) if (c != null) c.enabled = false;
        Sparkles.Play(logic.breakEffect, logic.TargetChild.position, logic.effectColour, logic.effectAmount, logic.effectSize);
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

    private Vector3 PlayerFeet()
    {
#if HAS_KINEMATIC_CC
        if (player != null) return player.TransientPosition;
#endif
        return logic.TargetChild.position;
    }

    private void Launch()
    {
#if HAS_KINEMATIC_CC
        player.ForceUnground();
        Vector3 v = player.BaseVelocity;
        v.y = logic.bounceStrength;
        player.BaseVelocity = v;
#endif
    }
}
