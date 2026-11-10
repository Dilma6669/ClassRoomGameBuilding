using UnityEngine;

/// <summary>
/// Which folder this thing is in, in the Game Builder's lists (e.g. 'Level 1 jumps', 'Boss area').
/// Only a label: the object isn't moved in the Hierarchy, so platforms keep their creatures and nothing
/// about how it plays changes. Hidden in the Inspector; set it in the Game Builder ('📁 Folder').
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public class ListFolder : MonoBehaviour
{
    public string folder = "";

    /// <summary>Its place in the list (smaller = higher). Set by ▲ ▼ and drag-and-drop in the Game Builder.</summary>
    public int order = int.MaxValue / 2;
}
