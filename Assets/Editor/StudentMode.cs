using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Student mode tidies the Hierarchy and Inspector so students only see what they need:
/// "⚠️ DO NOT TOUCH" folders, "(auto)" holders, the canvas folder, the camera and the character's
/// insides are hidden, and engine/physics components are folded away.
/// Teacher mode shows everything. The hiding is never saved into the scene files.
/// Switch it in the Game Builder window or with Game > Teacher Mode.
/// </summary>
[InitializeOnLoad]
public static class StudentMode
{
    private const string TeacherModeKey = "GameBuilder.TeacherMode";
    private const string MenuPath = "Game/Teacher Mode (show everything)";
    private static bool applyQueued;

    public static bool TeacherMode
    {
        get => EditorPrefs.GetBool(TeacherModeKey, false);
        set
        {
            EditorPrefs.SetBool(TeacherModeKey, value);
            ApplyToOpenScenes();
            EditorApplication.RepaintHierarchyWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    static StudentMode()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => Apply(scene, !TeacherMode);
        // Never save the hiding into the scene file: un-hide just before saving, hide again after
        EditorSceneManager.sceneSaving += (scene, path) => Apply(scene, false);
        EditorSceneManager.sceneSaved += scene => Apply(scene, !TeacherMode);
        EditorApplication.hierarchyChanged += QueueApply;
        EditorApplication.playModeStateChanged += state => QueueApply();
        QueueApply();
    }

    [MenuItem(MenuPath, priority = 1)]
    private static void ToggleTeacherMode() => TeacherMode = !TeacherMode;

    [MenuItem(MenuPath, true)]
    private static bool ToggleTeacherModeValidate()
    {
        Menu.SetChecked(MenuPath, TeacherMode);
        return true;
    }

    private static void QueueApply()
    {
        if (applyQueued) return;
        applyQueued = true;
        EditorApplication.delayCall += () =>
        {
            applyQueued = false;
            ApplyToOpenScenes();
        };
    }

    public static void ApplyToOpenScenes()
    {
        bool hide = !TeacherMode;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) Apply(scene, hide);
        }
    }

    private static void Apply(Scene scene, bool hide)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Walk(root.transform, hide, false);
        }
    }

    private static void Walk(Transform t, bool hide, bool underComponentHider)
    {
        GameObject go = t.gameObject;
        if ((go.hideFlags & HideFlags.DontSave) != 0) return; // temporary editor objects, leave alone

        SetFlag(go, HideFlags.HideInHierarchy, hide && ShouldHideInHierarchy(t));

        // Obstacles have their own "show advanced components" switch (ObstacleComponentHider), so respect it
        bool hider = underComponentHider || go.GetComponent<ObstacleComponentHider>() != null;
        foreach (Component c in go.GetComponents<Component>())
        {
            if (c == null || c is Transform) continue;
            if (IsAdvanced(c, hider)) SetFlag(c, HideFlags.HideInInspector, hide);
        }

        foreach (Transform child in t)
        {
            Walk(child, hide, hider);
        }
    }

    private static bool ShouldHideInHierarchy(Transform t)
    {
        string n = t.name;
        if (n.Contains("DO NOT TOUCH")) return true;
        if (n.Contains("(auto)")) return true;
        if (t.parent == null && (n == "CanvasController" || n == "GameController")) return true;
        if (n.StartsWith("EventSystem")) return true;
#if HAS_KINEMATIC_CC
        if (t.GetComponent<KinematicCharacterController.Examples.ExampleCharacterCamera>() != null) return true;
#endif
        // The character's skeleton/meshes (the player is changed from the Game Builder instead)
        if (n == "Root" && t.parent != null && t.parent.GetComponent<Health>() != null) return true;
        return false;
    }

    private static bool IsAdvanced(Component c, bool underComponentHider)
    {
        string ns = c.GetType().Namespace;
        if (ns != null && ns.StartsWith("KinematicCharacterController")) return true;
        if (c is ObstacleComponentHider || c is FollowPlatform) return true;
        if (c is TerrainCollider) return true;

        if (underComponentHider) return false; // the obstacle's own switch handles physics parts
        if (c is Rigidbody) return true;
#if HAS_KINEMATIC_CC
        if (c is Collider && c.GetComponent<KinematicCharacterController.KinematicCharacterMotor>() != null) return true;
#endif
        return false;
    }

    // Things Student mode hid itself (kept across script reloads), so it never un-hides
    // something the toolkit hides on purpose (e.g. ExamplePlayer)
    private const string HiddenIdsKey = "StudentMode.HiddenIds";
    private static HashSet<int> hiddenByUs;

    private static HashSet<int> HiddenByUs
    {
        get
        {
            if (hiddenByUs == null) hiddenByUs = new HashSet<int>(SessionState.GetIntArray(HiddenIdsKey, new int[0]));
            return hiddenByUs;
        }
    }

    private static void SetFlag(Object o, HideFlags flag, bool on)
    {
        int id = o.GetInstanceID();
        if (on)
        {
            if ((o.hideFlags & flag) != 0) return; // already hidden (by us, or on purpose by the toolkit)
            o.hideFlags |= flag;
            HiddenByUs.Add(id);
        }
        else
        {
            if (!HiddenByUs.Remove(id)) return; // not ours to un-hide
            o.hideFlags &= ~flag;
        }
        SessionState.SetIntArray(HiddenIdsKey, HiddenByUs.ToArray());
    }
}
