using System.Collections.Generic;
using UnityEngine;

/// <summary>Marks something made by a scatter tool, so "Clear" only removes scattered things.</summary>
[DisallowMultipleComponent]
public class ScatterTag : MonoBehaviour
{
    public string group;
}

