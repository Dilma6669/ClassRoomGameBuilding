using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// See-through objects show a preview in the editor with temporary materials. Before a level is saved,
/// the normal materials go back (so the saved level is never broken), then the preview comes back.
/// </summary>
[InitializeOnLoad]
public static class SeeThroughSaveGuard
{
    static SeeThroughSaveGuard()
    {
        EditorSceneManager.sceneSaving += (scene, path) =>
        {
            foreach (SeeThrough s in SeeThrough.Active.ToList()) if (s != null && s.gameObject.scene == scene) s.Restore();
        };
        EditorSceneManager.sceneSaved += scene =>
        {
            foreach (SeeThrough s in SeeThrough.Active.ToList()) if (s != null && s.gameObject.scene == scene && s.isActiveAndEnabled) s.Apply();
        };
    }
}
