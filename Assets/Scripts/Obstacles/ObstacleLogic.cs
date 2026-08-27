using UnityEngine;

[ExecuteAlways]
public class ObstacleLogic : MonoBehaviour
{
    [Header("Physics Setup")]
    public bool enableRigidbody = false;

    private Rigidbody rb;
    private Transform childFolder;

    private void Awake()
    {
        FindChildFolder();
        ToggleRigidbody();
    }

    private void Start()
    {
        FindChildFolder();
        ToggleRigidbody();
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

    private void OnValidate()
    {
        FindChildFolder();
        ToggleRigidbody();
    }

    private void ToggleRigidbody()
    {
        rb = GetComponentInChildren<Rigidbody>();

        GameObject targetTargetGo = childFolder != null ? childFolder.gameObject : gameObject;

        if (enableRigidbody)
        {
            if (rb == null)
            {
                rb = targetTargetGo.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }
}