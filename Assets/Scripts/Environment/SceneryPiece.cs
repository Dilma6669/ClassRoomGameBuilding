using UnityEngine;

/// <summary>
/// Marks one placed tree, rock or plant, so it shows in the Game Builder's Scenery List.
/// Added automatically (when scenery is placed, or when the Game Builder finds older scenery). Hidden in the Inspector.
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public class SceneryPiece : MonoBehaviour
{
    [Tooltip("The scenery group it came from (Trees, Rocks...).")]
    public string group = "";
}
