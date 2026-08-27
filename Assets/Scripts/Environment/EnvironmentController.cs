using UnityEngine;

[ExecuteAlways]
public class EnvironmentController : MonoBehaviour
{
    [Header("Target Terrain Settings")]
    public Vector3 terrainPositionOffset = new Vector3(-500f, -50f, -500f);

    private Terrain childTerrain;

    private void Awake()
    {
        EnsureParentScaleReset();
        FindAndAlignTerrain();
    }

    private void Update()
    {
        EnsureParentScaleReset();
        FindAndAlignTerrain();
    }

    private void EnsureParentScaleReset()
    {
        // Keep the parent GameObject scaled strictly at (1, 1, 1)
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }
    }

    private void FindAndAlignTerrain()
    {
        // Grab child terrain component
        if (childTerrain == null)
        {
            childTerrain = GetComponentInChildren<Terrain>();
        }

        if (childTerrain != null)
        {
            Transform t = childTerrain.transform;

            // Snap position to target offsets if moved
            if (t.localPosition != terrainPositionOffset)
            {
                t.localPosition = terrainPositionOffset;
            }

            // Lock rotation to zero
            if (t.localRotation != Quaternion.identity)
            {
                t.localRotation = Quaternion.identity;
            }

            // Lock child terrain scale to uniform (1, 1, 1)
            if (t.localScale != Vector3.one)
            {
                t.localScale = Vector3.one;
            }
        }
    }
}