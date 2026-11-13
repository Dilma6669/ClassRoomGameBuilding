using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// Something a creature shoots at the player (ObstacleBase.Shoot.cs): a glowing ball, or any model.
/// Made by code while playing, never saved. It flies (straight, homing, or dropping like a thrown ball),
/// hurts the player when it hits them, and disappears with a sparkle burst when it hits anything solid.
/// </summary>
public class Projectile : MonoBehaviour
{
    private Vector3 velocity;
    private float speed;
    private int damage;
    private float homing;          // 0 = straight, 1 = turns hard towards you
    private float gravity;         // 0 = no drop
    private float timeLeft;
    private float knockback;
    private float radius;
    private Sparkles.Effect hitEffect;
    private Color colour;
    private Transform shooter;
    private Transform target;
    private bool spin;
    private Transform visual;

    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static MaterialPropertyBlock block;

    /// <summary>Shoots one. 'model' empty = a glowing ball. Size = metres across.</summary>
    public static Projectile Fire(ObstacleBase from, Vector3 position, Vector3 velocity, Transform target, int damage, float homing,
        bool drops, float lasts, float knockback, float size, Color colour, GameObject model, bool trail, Sparkles.Effect hitEffect)
    {
        GameObject go = new GameObject("Projectile (auto)");
        go.transform.position = position;
        Projectile p = go.AddComponent<Projectile>();
        p.velocity = velocity;
        p.speed = velocity.magnitude;
        p.damage = damage;
        p.homing = Mathf.Clamp01(homing);
        p.gravity = drops ? Physics.gravity.magnitude : 0f;
        p.timeLeft = Mathf.Max(0.1f, lasts);
        p.knockback = knockback;
        p.radius = Mathf.Max(0.05f, size * 0.5f);
        p.hitEffect = hitEffect;
        p.colour = colour;
        p.shooter = from != null ? from.transform : null;
        p.target = target;
        p.spin = model != null;
        p.MakeLook(size, colour, model, trail);
        if (velocity.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(velocity);
        return p;
    }

    private void MakeLook(float size, Color c, GameObject model, bool trail)
    {
        if (model != null)
        {
            // Any model: copy it, take its colliders and animations away, fit its biggest side to the size
            GameObject holder = new GameObject("Model");
            holder.transform.SetParent(transform, false);
            GameObject copy = Instantiate(model, holder.transform);
            copy.transform.localPosition = Vector3.zero;
            foreach (Collider col in copy.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (Animator a in copy.GetComponentsInChildren<Animator>()) a.enabled = false;
            Bounds b = new Bounds();
            bool found = false;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>())
            {
                if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
            }
            if (found)
            {
                float biggest = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (biggest > 0.0001f) holder.transform.localScale = Vector3.one * (size / biggest);
                copy.transform.position -= (b.center - transform.position) * holder.transform.localScale.x; // centre it
            }
            visual = holder.transform;
        }
        else
        {
            // A glowing ball
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ball.GetComponent<Collider>());
            ball.name = "Ball";
            ball.transform.SetParent(transform, false);
            ball.transform.localScale = Vector3.one * size;
            Renderer r = ball.GetComponent<Renderer>();
            Material glow = Resources.Load<Material>("TeleporterMaterial"); // Standard with emission (kept in builds)
            if (glow != null) r.sharedMaterial = glow;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (block == null) block = new MaterialPropertyBlock();
            block.Clear();
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", c * 2.5f);
            r.SetPropertyBlock(block);
            visual = ball.transform;
        }

        if (trail)
        {
            TrailRenderer t = gameObject.AddComponent<TrailRenderer>();
            t.sharedMaterial = Sparkles.GetMaterial();
            t.time = 0.35f;
            t.startWidth = size * 0.8f;
            t.endWidth = 0f;
            t.minVertexDistance = Mathf.Max(0.05f, size * 0.2f);
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.4f), 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            t.colorGradient = g;
        }
    }

    private Vector3 TargetMiddle()
    {
#if HAS_KINEMATIC_CC
        KinematicCharacterMotor m = target != null ? target.GetComponent<KinematicCharacterMotor>() : null;
        if (m != null) return m.TransientPosition + m.CharacterUp * (m.Capsule != null ? m.Capsule.height * 0.5f : 1f);
#endif
        return target != null ? target.position + Vector3.up : transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        timeLeft -= dt;
        if (timeLeft <= 0f) { Pop(); return; }

        // Homing: turn towards the player (keeps its speed)
        if (homing > 0f && target != null)
        {
            Vector3 want = (TargetMiddle() - transform.position).normalized * Mathf.Max(0.01f, velocity.magnitude);
            velocity = Vector3.RotateTowards(velocity, want, homing * 5f * dt, 0f);
        }
        if (gravity > 0f) velocity += Vector3.down * gravity * dt;

        Vector3 step = velocity * dt;
        float distance = step.magnitude;
        if (distance > 0.00001f)
        {
            // Sweep a ball along the way, so fast shots can't skip through things
            int count = Physics.SphereCastNonAlloc(transform.position, radius, step / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;
                if (shooter != null && col.transform.IsChildOf(shooter)) continue; // not its own body
                if (col.GetComponentInParent<Projectile>() != null) continue;
                float d = hits[i].distance;
                if (d <= 0f && hits[i].point == Vector3.zero) d = 0f; // started inside it
                if (d < nearest) { nearest = d; best = i; }
            }
            if (best >= 0)
            {
                transform.position += step.normalized * Mathf.Max(0f, nearest);
                Hit(hits[best].collider);
                return;
            }
            transform.position += step;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion face = Quaternion.LookRotation(velocity);
                transform.rotation = face;
            }
        }
        if (spin && visual != null) visual.Rotate(0f, 0f, 540f * dt, Space.Self); // models spin as they fly
    }

    private void Hit(Collider col)
    {
#if HAS_KINEMATIC_CC
        KinematicCharacterMotor motor = col.GetComponentInParent<KinematicCharacterMotor>();
        if (motor != null)
        {
            IDamagable d = col.GetComponentInParent<IDamagable>();
            if (d == null) d = motor.GetComponentInChildren<IDamagable>();
            if (d != null) d.TakeDamage(damage);
            if (knockback > 0f)
            {
                Vector3 push = velocity; push.y = 0f;
                push = push.sqrMagnitude > 0.0001f ? push.normalized : Vector3.zero;
                motor.ForceUnground(0.1f);
                motor.BaseVelocity += push * knockback + Vector3.up * knockback * 0.35f;
            }
        }
#endif
        Pop();
    }

    private void Pop()
    {
        if (hitEffect != Sparkles.Effect.None) Sparkles.Play(hitEffect, transform.position, colour, 0.6f, Mathf.Clamp(radius * 2f, 0.4f, 6f));
        Destroy(gameObject);
    }
}
