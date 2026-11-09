using System.Collections.Generic;
using UnityEngine;

/// <summary>Scatter settings for platforms (lives on the PlatformController).</summary>
[DisallowMultipleComponent]
public class PlatformScatter : MonoBehaviour
{
    [Range(1, 200)] public int howMany = 15;
    [Tooltip("Random width and length (metres).")]
    public Vector2 sizeRange = new Vector2(2.5f, 5f);
    [Tooltip("Random thickness (metres).")]
    public Vector2 thicknessRange = new Vector2(0.5f, 1f);
    [Tooltip("Random height above the ground (metres).")]
    public Vector2 heightRange = new Vector2(2f, 8f);
    [Tooltip("Give each platform a random turn.")]
    public bool randomTurn = true;

    [Header("Movement (picked at random from the ticked ones)")]
    public bool still = true;
    public bool sideToSide = true;
    public bool upAndDown = true;
    public bool spinning = false;
    public bool growing = false;
    [Tooltip("How far moving platforms travel (metres).")]
    public Vector2 moveDistanceRange = new Vector2(3f, 8f);
    [Tooltip("How fast moving platforms go.")]
    public Vector2 speedRange = new Vector2(1f, 3f);

    [Header("Look")]
    [Tooltip("3D models to pick from at random. Leave empty to use the normal platform look.")]
    public List<GameObject> models = new List<GameObject>();

    public ScatterPlace place = new ScatterPlace { area = ScatterArea.AroundSceneView, maxSlope = 90f, spacing = 3f, keepAwayFromStart = 5f };
}

