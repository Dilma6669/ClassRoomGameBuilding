using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

[RequireComponent(typeof(SphereCollider))]
[ExecuteAlways]
public class TriggerLogic : MonoBehaviour
{
    [HideInInspector]
    public TextMeshProUGUI uiTextObject;

    [Header("Message Settings")]
    public bool displayText = true;
    [TextArea(2, 5)]
    [Tooltip("The text to display on screen when the player enters this trigger.")]
    public string message = "⚠️ DO NOT TOUCH ⚠️";

    [Tooltip("If true, the message disappears when the player leaves the trigger zone.")]
    public bool clearOnExit = true;

    [Range(0.1f, 3f)]
    [Tooltip("Speed in seconds for the text UI to fade in and out.")]
    public float fadeDuration = 0.5f;

    [Header("Scene Transition Settings")]
    public bool changeScene = false;
    [HideInInspector] public string sceneToLoad = "";

    [Header("Trigger Scale & Rotation")]
    [Range(0.5f, 20f)] public float size = 3f;
    [Range(0f, 360f)] public float rotationY = 0f;

    [Header("Position Offset")]
    [Range(-20f, 20f)] public float offsetX = 0f;
    [Range(-20f, 20f)] public float offsetY = 0f;
    [Range(-20f, 20f)] public float offsetZ = 0f;
    
    private SphereCollider triggerCollider;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    public Canvas canvas;

    private void Awake()
    {
        EnsureColliderSetup();
        CacheUIReferences();
    }

    private void Start()
    {
        EnsureColliderSetup();
    }

    private void EnsureColliderSetup()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.hideFlags = HideFlags.HideInInspector;
        }
    }

    private void CacheUIReferences()
    {
        // Auto-find CanvasDialog by name if canvas is not assigned
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("CanvasDialog");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
        }

        if (canvas == null) return;

        // 1. Find TextMeshProUGUI inside the assigned canvas
        if (uiTextObject == null)
        {
            uiTextObject = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 2. Find CanvasGroup inside the assigned canvas
        if (canvasGroup == null)
        {
            canvasGroup = canvas.GetComponentInChildren<CanvasGroup>(true);
        }
    }

    private void Update()
    {
        EnsureColliderSetup();

        transform.localScale = new Vector3(size, size, size);

        if (!Application.isPlaying)
        {
            transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<KinematicCharacterController.KinematicCharacterMotor>() != null)
        {
            if (displayText)
            {
                CacheUIReferences();

                if (uiTextObject != null)
                {
                    uiTextObject.text = message;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.gameObject.SetActive(true);
                    StartFade(1f);
                }
            }

            if (changeScene && !string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!displayText || !clearOnExit) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<KinematicCharacterController.KinematicCharacterMotor>() != null)
        {
            CacheUIReferences();
            if (canvasGroup != null)
            {
                StartFade(0f);
            }
        }
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (Application.isPlaying)
        {
            fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    public void DeleteTrigger()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.DestroyObjectImmediate(gameObject);
#else
        Destroy(gameObject);
#endif
    }
}