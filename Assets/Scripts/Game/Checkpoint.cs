using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
#endif

/// <summary>
/// A checkpoint flag. When the player touches it, the flag turns green.
/// If the player dies after that, the level restarts with the player back at this flag.
/// Create one with the '🚩 Create Checkpoint' button on GameController (or Game > Create Checkpoint).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Message shown on screen when the player reaches this checkpoint.")]
    public string message = "Checkpoint!";

    private static readonly Color NotReachedColor = new Color(0.9f, 0.25f, 0.2f);
    private static readonly Color ReachedColor = new Color(0.2f, 0.85f, 0.35f);

    private bool reached;

    public Vector3 SpawnPosition => transform.position + Vector3.up * 0.3f;
    public Quaternion SpawnRotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnEnable()
    {
        UpdateColor();
    }

    public void SetReached(bool value)
    {
        reached = value;
        UpdateColor();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying || reached || !IsPlayer(other)) return;

        // Only one flag is green at a time - the latest one
        foreach (Checkpoint flag in FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            flag.SetReached(false);
        }
        SetReached(true);
        GameManager.ReachCheckpoint(this);
    }

    private static bool IsPlayer(Collider other)
    {
        if (other.GetComponentInParent<Health>() != null) return true;
#if HAS_KINEMATIC_CC
        if (other.GetComponentInParent<KinematicCharacterMotor>() != null) return true;
#endif
        return other.CompareTag("Player");
    }

    private void UpdateColor()
    {
        Transform flag = transform.Find("Flag");
        if (flag == null) return;
        Renderer r = flag.GetComponent<Renderer>();
        if (r == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor("_Color", reached ? ReachedColor : NotReachedColor);
        block.SetColor("_BaseColor", reached ? ReachedColor : NotReachedColor);
        r.SetPropertyBlock(block);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.25f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
