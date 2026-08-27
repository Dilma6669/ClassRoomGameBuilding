using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class CharacterController : MonoBehaviour
{
    // Store known children instance IDs so we can spot new additions
    private HashSet<int> knownChildIDs = new HashSet<int>();

    private void OnEnable()
    {
        CacheExistingChildren();
    }

    private void Update()
    {
        // Only run in the Editor when not playing
        if (Application.isPlaying) return;

        // Check if a new child was added
        if (transform.childCount > knownChildIDs.Count)
        {
            foreach (Transform child in transform)
            {
                int id = child.gameObject.GetInstanceID();

                // If this child wasn't in our list, it's brand new!
                if (!knownChildIDs.Contains(id))
                {
                    // Reset local transform
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;

                    // Add to tracked set so we don't reset it again when moved
                    knownChildIDs.Add(id);
                }
            }
        }
        // Clean up set if a child was deleted
        else if (transform.childCount < knownChildIDs.Count)
        {
            CacheExistingChildren();
        }
    }

    private void CacheExistingChildren()
    {
        knownChildIDs.Clear();
        foreach (Transform child in transform)
        {
            knownChildIDs.Add(child.gameObject.GetInstanceID());
        }
    }
}