using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Makes teleporter pads (Game Builder > Signs & Story > 🌀 Teleporters).</summary>
public static class TeleporterTools
{
    public const string RootName = "Teleporters";
    private const string MaterialPath = "Assets/Resources/TeleporterMaterial.mat";

    private static readonly Color[] Colours =
    {
        new Color(0.55f, 0.35f, 1f),   // purple
        new Color(0.2f, 0.85f, 1f),    // cyan
        new Color(1f, 0.45f, 0.8f),    // pink
        new Color(1f, 0.65f, 0.15f),   // orange
        new Color(0.4f, 1f, 0.45f),    // green
    };

    /// <summary>Two linked pads: each one sends you to the other. Returns the first pad.</summary>
    public static Teleporter CreatePair(RaycastHit first, RaycastHit second) =>
        CreatePair(first.point, PlatformAt(first), second.point, PlatformAt(second));

    public static Teleporter CreatePair(Vector3 a, PlatformLogic onA, Vector3 b, PlatformLogic onB, Color? colour = null)
    {
        Undo.SetCurrentGroupName("Teleporter pair");
        int undoGroup = Undo.GetCurrentGroup();

        int number = Object.FindObjectsByType<Teleporter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length / 2 + 1;
        Color c = colour ?? Colours[(number - 1) % Colours.Length];
        Teleporter padA = CreatePad(a, onA, c, $"Teleporter {number} A");
        Teleporter padB = CreatePad(b, onB, c, $"Teleporter {number} B");
        padA.sendsYouTo = padB.transform;
        padB.sendsYouTo = padA.transform;

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = padA.gameObject;
        return padA;
    }

    /// <summary>The platform a click landed on (null = the ground).</summary>
    public static PlatformLogic PlatformAt(RaycastHit hit) => hit.collider != null ? hit.collider.GetComponentInParent<PlatformLogic>() : null;

    /// <summary>One pad standing at 'point'. On a platform it rides along (moves and spins with it).</summary>
    public static Teleporter CreatePad(Vector3 point, PlatformLogic onPlatform, Color colour, string name)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Teleporter");
        Transform parent = onPlatform != null ? PlatformSceneryHolder.For(onPlatform) : Root();
        go.transform.SetParent(parent, false);
        go.transform.position = point;
        go.transform.rotation = Quaternion.identity;

        Material mat = GetMaterial();
        Part(go.transform, "Pad", new Vector3(0f, 0.06f, 0f), new Vector3(2.6f, 0.06f, 2.6f), mat);
        Part(go.transform, "Ring", new Vector3(0f, 0.1f, 0f), new Vector3(1.8f, 0.06f, 1.8f), mat);
        Part(go.transform, "Ring", new Vector3(0f, 0.14f, 0f), new Vector3(0.9f, 0.06f, 0.9f), mat);

        // Step into this to teleport
        BoxCollider zone = go.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 1.2f, 0f);
        zone.size = new Vector3(2.2f, 2.4f, 2.2f);

        Teleporter t = go.AddComponent<Teleporter>();
        t.colour = colour;

        Glow glow = go.AddComponent<Glow>();
        glow.colour = colour;
        glow.strength = 2f;
        glow.lightRange = 6f;
        glow.pulse = true;
        glow.pulsesPerSecond = 1f;

        AmbientSparkles s = go.AddComponent<AmbientSparkles>();
        s.style = AmbientSparkles.Style.Tornado;
        s.colour = colour;
        s.secondColour = Color.white;
        s.fromTop = false; s.fromSides = false; s.fromBottom = false; s.fromCentre = true;
        s.amount = 25f;

        SeeThrough ghost = go.AddComponent<SeeThrough>();
        ghost.howSeeThrough = 35f; // slightly see-through, like a portal
        ghost.Apply();
        t.ApplyColour();
        return t;
    }

    private static Transform Root()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Teleporter");
        }
        return root.transform;
    }

    private static void Part(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Material GetMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard")) { color = Color.white };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.6f, 0.6f, 0.6f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    public static Teleporter[] All() =>
        Object.FindObjectsByType<Teleporter>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(t => t.name).ToArray();
}
