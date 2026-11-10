using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// A teleporter pad. Step on it and you appear at the pad it 'Sends you to'.
/// Make a pair with Game Builder > Signs & Story > 🌀 Make a teleporter pair (each pad sends you to the other one).
/// A pad can sit on a platform too: it moves and spins with it.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class Teleporter : MonoBehaviour
{

    [Tooltip("The pad (or anything else) you arrive at. Drag another teleporter here. Empty = this pad does nothing (a 'landing only' pad).")]
    [ShowAs("Sends you to")]
    public Transform sendsYouTo;

    [Tooltip("The pad's glowing colour.")]
    public Color colour = new Color(0.55f, 0.35f, 1f);

    [Tooltip("Keep your speed when you come out (fun with Speed Boost!). Off = you arrive standing still.")]
    [ShowAs("Keep your speed")]
    public bool keepYourSpeed = false;

    [Tooltip("Arrive this high above the pad (metres). Go big to drop in from the sky!")]
    [ShowAs("Arrive this high above (m)")]
    [Range(0f, 300f)] public float arriveAbove = 0.5f;

    [Tooltip("Sparkles at both ends when you teleport.")]
    [ShowAs("Sparkles")]
    public Sparkles.Effect effect = Sparkles.Effect.Swirl;

    [Tooltip("Words that pop up when you teleport. Leave empty for none.")]
    [ShowAs("Message")]
    public string message = "Whoosh!";

    [Tooltip("Seconds before you can use it again.")]
    [ShowAs("Wait before using again (s)")]
    [Range(0.2f, 10f)] public float waitBeforeAgain = 1f;

    // The pad the player just arrived on ignores them until they step off it (so you don't bounce straight back)
    private static Teleporter arrivedOn;
    private static float lastTeleport = -100f;

    private static MaterialPropertyBlock block;

    private void OnEnable() => ApplyColour();

#if UNITY_EDITOR
    private void OnValidate() => ApplyColour();
#endif

    /// <summary>Colours the pad's parts (the glow follows the colour too).</summary>
    public void ApplyColour()
    {
        if (block == null) block = new MaterialPropertyBlock();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            r.GetPropertyBlock(block);
            Color c = r.name == "Ring" ? Color.Lerp(colour, Color.white, 0.5f) : colour;
            c.a = SeeThrough.AlphaFor(r);
            block.SetColor("_Color", c);
            block.SetColor("_BaseColor", c);
            block.SetColor("_EmissionColor", c * 1.5f);
            r.SetPropertyBlock(block);
        }
        Glow glow = GetComponent<Glow>();
        if (glow != null) glow.colour = colour;
    }

    /// <summary>Where the player appears when sent here.</summary>
    public Vector3 ArrivePoint => transform.position + transform.up * (0.3f + arriveAbove);

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying || !IsPlayer(other)) return;
        if (arrivedOn == this) return;          // just came out of this one
        if (sendsYouTo == null) return;
        if (Time.time - lastTeleport < waitBeforeAgain) return;
        Send(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Application.isPlaying || !IsPlayer(other)) return;
        if (arrivedOn == this) arrivedOn = null;
    }

    private void Send(Collider player)
    {
        lastTeleport = Time.time;
        Teleporter target = sendsYouTo.GetComponent<Teleporter>();
        Vector3 to = target != null ? target.ArrivePoint : sendsYouTo.position + Vector3.up * (0.5f + arriveAbove);
        arrivedOn = target != null && target.arriveAbove < 2f ? target : null; // dropping in from high up never touches it

        Sparkles.Play(effect, transform.position + Vector3.up, colour, 1.5f, 1.2f);
        Sparkles.Play(effect, to + Vector3.up * 0.5f, target != null ? target.colour : colour, 1.5f, 1.2f);
        SoundManager.Play(SoundEvent.Bounce, to);
        if (!string.IsNullOrEmpty(message)) PickupMessage.Show(message, colour);

#if HAS_KINEMATIC_CC
        KinematicCharacterMotor motor = player.GetComponentInParent<KinematicCharacterMotor>();
        if (motor == null) motor = FindAnyObjectByType<KinematicCharacterMotor>();
        if (motor != null)
        {
            Vector3 speed = motor.BaseVelocity;
            motor.ForceUnground();
            motor.SetPosition(to);
            motor.BaseVelocity = keepYourSpeed ? speed : Vector3.zero;
            return;
        }
#endif
        Health h = player.GetComponentInParent<Health>();
        if (h != null) h.transform.position = to;
    }

    private static bool IsPlayer(Collider other)
    {
#if HAS_KINEMATIC_CC
        if (other.GetComponentInParent<KinematicCharacterMotor>() != null) return true;
#endif
        return other.GetComponentInParent<Health>() != null || other.CompareTag("Player");
    }

    private void OnDrawGizmos()
    {
        if (sendsYouTo == null) return;
        Gizmos.color = colour;
        Gizmos.DrawLine(transform.position + Vector3.up, sendsYouTo.position + Vector3.up);
    }
}
