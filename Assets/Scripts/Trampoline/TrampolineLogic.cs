using UnityEngine;

[ExecuteAlways]
public class TrampolineLogic : MonoBehaviour
{
    [Header("Trampoline Settings")]
    [Range(1f, 100f)] public float jumpForce = 25f;

    [Header("Scale Controls")]
    [Range(0.1f, 10f)] public float uniformScale = 1f;

    private Transform childFolder;

    private void Awake()
    {
        FindChildFolder();
    }

    private void Start()
    {
        FindChildFolder();
    }

    private void FindChildFolder()
    {
        if (childFolder != null) return;

        childFolder = transform.Find("⚠️ DO NOT TOUCH");

        if (childFolder == null && transform.childCount > 0)
        {
            childFolder = transform.GetChild(0);
        }
    }

    private void Update()
    {
        FindChildFolder();

        // Apply scale directly to child folder geometry
        if (childFolder != null)
        {
            childFolder.localScale = Vector3.one * uniformScale;
        }
    }
}