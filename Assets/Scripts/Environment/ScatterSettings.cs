using System.Collections.Generic;
using UnityEngine;

// Settings for the Game Builder's scatter tools (platforms, creatures & pickups).
// These components only hold settings; the scattering itself happens in the Editor (Editor/ScatterTools.cs).

public enum ScatterArea { WholeLevel, AroundSceneView }

/// <summary>Where things are allowed to go.</summary>
[System.Serializable]
public class ScatterPlace
{
    [Tooltip("The whole level, or just around where the Scene view is looking.")]
    public ScatterArea area = ScatterArea.WholeLevel;
    [Tooltip("How big the area around the Scene view is (metres across).")]
    [Range(10f, 2000f)] public float areaSize = 80f;
    [Tooltip("Keep this far from the edge of the map.")]
    [Range(0f, 100f)] public float edgePadding = 15f;
    [Tooltip("Don't use ground steeper than this (degrees). 0 = only flat ground, 90 = anywhere.")]
    [Range(0f, 90f)] public float maxSlope = 30f;
    [Tooltip("Keep at least this far apart from each other.")]
    [Range(0f, 200f)] public float spacing = 4f;
    [Tooltip("Keep this far away from where the player starts.")]
    [Range(0f, 100f)] public float keepAwayFromStart = 15f;
    [Tooltip("Only use ground painted with these. Leave all unticked for any ground.")]
    public List<TerrainLayer> onlyOnGround = new List<TerrainLayer>();
}

