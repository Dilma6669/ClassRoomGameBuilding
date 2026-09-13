using UnityEngine;

public class TerrainObstacle : ObstacleBase
{
    [Header("Height Alignment")]
    [Tooltip("Adjusts vertical offset of the visual model and colliders above the terrain ground surface.")]
    [Range(-2f, 10f)] public float heightOffset = 0f;

    public Vector3 SpawnCenterPosition { get; private set; }
    public bool IsFalling { get; set; }

    protected override void OnValidate()
    {
        base.OnValidate();
        ApplyVisualHeightOffset();
    }

    protected override void Awake()
    {
        base.Awake();
        ApplyVisualHeightOffset();
    }

    private void Start()
    {
        SpawnCenterPosition = transform.position;
        ApplyVisualHeightOffset();
    }

    private void ApplyVisualHeightOffset()
    {
        // Target the child visual container
        Transform visualChild = transform.Find("⚠️ DO NOT TOUCH");
        if (visualChild != null)
        {
            // Move the visual mesh and attached colliders up relative to the root
            visualChild.localPosition = new Vector3(0f, heightOffset, 0f);
        }
    }

    public void SetKinematicState(bool enableKinematic)
    {
        // Safely check for an optional Rigidbody locally if this specific terrain obstacle uses gravity/falling
        Rigidbody localRb = GetComponent<Rigidbody>();
        if (localRb != null) 
        {
            localRb.isKinematic = enableKinematic;
        }
    }
}