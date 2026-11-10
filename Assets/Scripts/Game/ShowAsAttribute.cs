using UnityEngine;

/// <summary>
/// Shows a setting under a plain-English name in the Inspector and the Game Builder,
/// e.g. [ShowAs("Sprint speed (× normal)")]. Works together with [Range] (still a slider) and [Tooltip].
/// The drawer is in Editor/KidInspector.cs.
/// </summary>
public class ShowAsAttribute : PropertyAttribute
{
    public readonly string label;

    public ShowAsAttribute(string label)
    {
        this.label = label;
    }
}
