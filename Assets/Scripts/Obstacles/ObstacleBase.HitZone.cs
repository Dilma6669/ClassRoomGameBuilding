using UnityEngine;

/// <summary>
/// The hit zone: an upright cylinder that is the creature's (or pickup's) body, fitted to its model.
/// • 'Body'  – solid, with a flat top you can land on (that's how you jump on a creature).
/// • 'Touch' – a slightly wider trigger round the SIDES only (its top is level with the body's top),
///             so walking into it hurts / picks it up, but landing on top doesn't.
/// It replaces the old invisible balls, which were much bigger than flat or thin models.
/// Sizes are stored for objectScale 1, so the zone grows and shrinks with Object Scale.
/// </summary>
public abstract partial class ObstacleBase
{
    public const string HitZoneName = "Hit Zone (auto)";

    public enum HitZoneShape { Automatic, Cylinder, Ball }
    [Tooltip("Automatic: bounce balls and pickups are a Ball, creatures a Cylinder.\n" +
             "Cylinder: a flat top you can land on (best for jumping on creatures).\n" +
             "Ball: round, so you bounce off it at all sorts of angles depending on where you hit it.")]
    public HitZoneShape hitZoneShape = HitZoneShape.Automatic;

    /// <summary>Is the hit zone a ball (sphere) rather than an upright cylinder?</summary>
    public bool HitZoneIsBall => hitZoneShape == HitZoneShape.Ball ||
                                 (hitZoneShape == HitZoneShape.Automatic && (isBouncy || payloadType != PayloadType.Damage));

    [Tooltip("Fit the hit zone to the model automatically (recommended). Untick to set the width and height yourself.")]
    public bool fitHitZoneToModel = true;
    [Tooltip("How wide the hit zone is (at Object Scale 1).")]
    [Min(0.05f)] public float hitZoneWidth = 1f;
    [Tooltip("How tall the hit zone is (at Object Scale 1).")]
    [Min(0.05f)] public float hitZoneHeight = 1f;
    [Tooltip("How far past its sides the player gets hurt (or picks it up), in metres. A creature's top never hurts, so you can land on it. Bounce balls and pickups also reach this far above their top, so landing on one launches you or picks it up.")]
    [Range(0f, 20f)] public float touchDistance = 0.3f;

    // Where the zone sits (bottom centre, at Object Scale 1). Set by the automatic fit.
    [SerializeField, HideInInspector] private Vector3 hitZoneBottom = Vector3.zero;
    [SerializeField, HideInInspector] private bool hitZoneMeasured;

    private static Mesh cylinderMesh;
    private float nextEditorFit;

    private static Mesh CylinderMesh
    {
        get
        {
            if (cylinderMesh == null) cylinderMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            return cylinderMesh;
        }
    }

    // Unity's built-in cylinder is 2 m across and 2 m tall; read it from the mesh rather than guess
    private static float CylinderWidth => CylinderMesh != null ? Mathf.Max(0.01f, CylinderMesh.bounds.size.x) : 1f;
    private static float CylinderHeight => CylinderMesh != null ? Mathf.Max(0.01f, CylinderMesh.bounds.size.y) : 2f;

    /// <summary>The solid part, while the hit zone is in use (else null).</summary>
    public Collider HitZoneBody { get; private set; }

    /// <summary>Makes (or updates) the hit zone and switches the old ball colliders off.</summary>
    public void UpdateHitZone(bool refit)
    {
        if (CylinderMesh == null) return; // can't happen, but keep the old balls if it does

        Transform zone = transform.Find(HitZoneName);
        if (zone == null)
        {
            zone = new GameObject(HitZoneName).transform;
            zone.SetParent(transform, false);
            refit = true;
        }
        zone.localPosition = Vector3.zero;
        zone.localRotation = Quaternion.identity;
        SetIfDifferent(zone, Vector3.zero, Vector3.one * objectScale);

        if ((fitHitZoneToModel && refit) || !hitZoneMeasured) FitHitZone(zone);

        float w = Mathf.Max(0.05f, hitZoneWidth);
        float h = Mathf.Max(0.05f, hitZoneHeight);
        // touchDistance is in metres; the zone is scaled by objectScale
        float reach = touchDistance / Mathf.Max(0.01f, objectScale);

        bool ball = HitZoneIsBall;
        Collider body, touch;
        if (ball)
        {
            // A round ball fitted to the model (as wide as it is on average), with the touch ball 'reach' bigger all round
            float d = Mathf.Max(0.05f, (w + h) * 0.5f);
            Vector3 middle = hitZoneBottom + Vector3.up * (h / 2f);
            SphereCollider b = BallPart(zone, "Body", false);
            SetIfDifferent(b.transform, middle, Vector3.one);
            SetRadius(b, d / 2f);
            SphereCollider t = BallPart(zone, "Touch", true);
            SetIfDifferent(t.transform, middle, Vector3.one);
            SetRadius(t, d / 2f + Mathf.Max(reach, 0.3f / Mathf.Max(0.01f, objectScale)));
            body = b;
            touch = t;
        }
        else
        {
            MeshCollider b = Part(zone, "Body", false);
            SetIfDifferent(b.transform, hitZoneBottom + Vector3.up * (h / 2f), new Vector3(w / CylinderWidth, h / CylinderHeight, w / CylinderWidth));

            MeshCollider t = Part(zone, "Touch", true);
            // Wider by 'reach' on every side, same bottom.
            // Creatures: top level with the body's top (a tiny bit lower), so landing on their head never hurts.
            // Bounce balls and pickups: it reaches above the top too, so landing on one launches you / picks it up.
            bool topCounts = isBouncy || payloadType != PayloadType.Damage;
            float above = Mathf.Max(reach, 0.3f / Mathf.Max(0.01f, objectScale));
            float touchHeight = topCounts ? h + above : Mathf.Max(0.05f, h - 0.02f);
            SetIfDifferent(t.transform, hitZoneBottom + Vector3.up * (touchHeight / 2f), new Vector3((w + reach * 2f) / CylinderWidth, touchHeight / CylinderHeight, (w + reach * 2f) / CylinderWidth));
            body = b;
            touch = t;
        }
        // Only the shape in use is switched on
        UseOnly(zone.Find("Body"), ball);
        UseOnly(zone.Find("Touch"), ball);
        if (!body.enabled) body.enabled = true;
        bool touchOn = isBouncy || payloadType != PayloadType.None;
        if (touch.enabled != touchOn) touch.enabled = touchOn;

        HitZoneBody = body;
        physicalMeshCollider = body;
        triggerMeshCollider = touch;

        // The old balls (on the shape) are no longer used
        Transform visual = transform.Find("⚠️ DO NOT TOUCH");
        if (visual != null)
        {
            MeshCollider oldBall = visual.GetComponent<MeshCollider>();
            if (oldBall != null && oldBall.enabled) oldBall.enabled = false;
            Transform oldTrigger = visual.Find("TriggerCollider");
            MeshCollider oldTouch = oldTrigger != null ? oldTrigger.GetComponent<MeshCollider>() : null;
            if (oldTouch != null && oldTouch.enabled) oldTouch.enabled = false;
        }
    }

    /// <summary>A ball-shaped part of the hit zone (sphere collider).</summary>
    private static SphereCollider BallPart(Transform zone, string name, bool trigger)
    {
        Transform t = zone.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(zone, false);
        }
        SphereCollider c = t.GetComponent<SphereCollider>();
        if (c == null) c = t.gameObject.AddComponent<SphereCollider>();
        if (c.center != Vector3.zero) c.center = Vector3.zero;
        if (c.isTrigger != trigger) c.isTrigger = trigger;
        return c;
    }

    private static void SetRadius(SphereCollider c, float radius)
    {
        if (Mathf.Abs(c.radius - radius) > 0.0001f) c.radius = radius;
    }

    /// <summary>Switches off the shape that isn't in use (the ball's sphere, or the cylinder's mesh).</summary>
    private static void UseOnly(Transform part, bool ball)
    {
        if (part == null) return;
        SphereCollider s = part.GetComponent<SphereCollider>();
        MeshCollider m = part.GetComponent<MeshCollider>();
        if (s != null && !ball && s.enabled) s.enabled = false;
        if (m != null && ball && m.enabled) m.enabled = false;
        if (s != null && ball && !s.enabled) s.enabled = true;
        if (m != null && !ball && !m.enabled) m.enabled = true;
    }

    private static MeshCollider Part(Transform zone, string name, bool trigger)
    {
        Transform t = zone.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(zone, false);
        }
        MeshCollider c = t.GetComponent<MeshCollider>();
        if (c == null) c = t.gameObject.AddComponent<MeshCollider>();
        if (c.sharedMesh != CylinderMesh) c.sharedMesh = CylinderMesh;
        if (!c.convex) c.convex = true; // needed for triggers and moving bodies
        if (c.isTrigger != trigger) c.isTrigger = trigger;
        return c;
    }

    /// <summary>Only touch the transform when something really changed (so the level isn't marked as changed for nothing).</summary>
    private static void SetIfDifferent(Transform t, Vector3 localPosition, Vector3 localScale)
    {
        if ((t.localPosition - localPosition).sqrMagnitude > 0.000001f) t.localPosition = localPosition;
        if ((t.localScale - localScale).sqrMagnitude > 0.000001f) t.localScale = localScale;
        if (t.localRotation != Quaternion.identity) t.localRotation = Quaternion.identity;
    }

    /// <summary>Measures what you can see (the model, or the shape if there's no model) and fits the cylinder round it.</summary>
    private void FitHitZone(Transform zone)
    {
        bool found = false;
        Bounds b = new Bounds();
        Matrix4x4 toZone = zone.worldToLocalMatrix;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(zone)) continue;

            Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) continue;
            Bounds local = mesh.bounds;
            Matrix4x4 m = toZone * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!found) { b = new Bounds(p, Vector3.zero); found = true; }
                else b.Encapsulate(p);
            }
        }
        if (!found) return;

        // Long or wide models (a snake, a shark) get the average of their length and width
        hitZoneWidth = Mathf.Max(0.05f, (b.size.x + b.size.z) / 2f);
        hitZoneHeight = Mathf.Max(0.05f, b.size.y);
        hitZoneBottom = new Vector3(b.center.x, b.min.y, b.center.z);
        hitZoneMeasured = true;
    }

    /// <summary>In the editor: keep the zone fitted while the model, size or height is being changed.</summary>
    private void EditorKeepHitZoneFitted()
    {
        if (Time.realtimeSinceStartup < nextEditorFit) return;
        nextEditorFit = Time.realtimeSinceStartup + 0.5f;
#if UNITY_EDITOR
        KeepSameSizeAsCharacter();
#endif
        RefitAndMarkChanged();
    }

    private void RefitAndMarkChanged()
    {
        float w = hitZoneWidth, h = hitZoneHeight;
        Vector3 bottom = hitZoneBottom;
        bool hadZone = transform.Find(HitZoneName) != null;
        UpdateHitZone(true);
#if UNITY_EDITOR
        bool changed = !hadZone || Mathf.Abs(w - hitZoneWidth) > 0.0001f || Mathf.Abs(h - hitZoneHeight) > 0.0001f || (bottom - hitZoneBottom).sqrMagnitude > 0.0000001f;
        if (changed && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>Called from OnValidate: changes from the Inspector show straight away (objects can't be made inside OnValidate itself).</summary>
    private void RefitSoon()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject.scene.IsValid()) RefitAndMarkChanged();
        };
#endif
    }

}
