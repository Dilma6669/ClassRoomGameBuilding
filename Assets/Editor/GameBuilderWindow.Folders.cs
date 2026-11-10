using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Names and folders for the Game Builder's lists (platforms, creatures & pickups, pop-up signs, checkpoints).
/// A folder is only a label on the object (Scripts/Game/ListFolder.cs); nothing moves in the Hierarchy.
/// </summary>
public partial class GameBuilderWindow
{
    // Typing a new folder's name / renaming a folder (per list)
    [System.NonSerialized] private string newFolderFor, newFolderName = "";
    [System.NonSerialized] private string renamingKey, renamingFolder, renamingTo = "";

    private static string FolderOf(Component c)
    {
        ListFolder f = c != null ? c.GetComponent<ListFolder>() : null;
        return f != null && f.folder != null ? f.folder.Trim() : "";
    }

    private bool FolderOpen(string key, string folder)
    {
        string k = "folder/" + key + "/" + folder;
        return !foldouts.TryGetValue(k, out bool open) || open; // open to start with
    }

    /// <summary>Every selected thing of this kind (clicking a model counts as its creature / platform).</summary>
    private static HashSet<T> SelectedAll<T>() where T : Component
    {
        HashSet<T> all = new HashSet<T>();
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null || !go.scene.IsValid()) continue;
            T c = go.GetComponentInParent<T>(true);
            if (c != null) all.Add(c);
        }
        return all;
    }

    /// <summary>Puts things in a folder ("" = no folder). One Undo step.</summary>
    private static void SetFolder(IEnumerable<Component> things, string folder)
    {
        folder = (folder ?? "").Trim();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(folder == "" ? "Take out of folder" : "Put in folder " + folder);
        foreach (Component c in things)
        {
            if (c == null) continue;
            ListFolder f = c.GetComponent<ListFolder>();
            if (folder == "")
            {
                if (f != null) Undo.DestroyObjectImmediate(f);
                continue;
            }
            if (f == null)
            {
                f = Undo.AddComponent<ListFolder>(c.gameObject);
                f.hideFlags = HideFlags.HideInInspector;
            }
            Undo.RecordObject(f, "Folder");
            f.folder = folder;
            EditorUtility.SetDirty(f);
        }
        Undo.CollapseUndoOperations(group);
    }

    /// <summary>A folder's row in a list: ▼/▶ 📁 name (count), ✏️ rename, ✕ remove. Returns true if it's open.</summary>
    private bool DrawFolderRow(string key, string folder, int count, List<Component> things, List<Component> all)
    {
        string k = "folder/" + key + "/" + folder;
        bool open = FolderOpen(key, folder);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (renamingKey == key && renamingFolder == folder)
            {
                GUILayout.Label("📁", GUILayout.Width(20));
                GUI.SetNextControlName("renameFolder");
                renamingTo = EditorGUILayout.TextField(renamingTo);
                bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                if (GUILayout.Button("✔", GUILayout.Width(26)) || enter)
                {
                    if (renamingTo.Trim() != "") SetFolder(things, renamingTo);
                    renamingKey = renamingFolder = null;
                    GUI.FocusControl(null);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("✕", GUILayout.Width(26))) { renamingKey = renamingFolder = null; GUI.FocusControl(null); }
                return open;
            }

            GUIStyle style = new GUIStyle(rowStyle) { fontStyle = FontStyle.Bold };
            if (GUILayout.Button((open ? "▼ 📁 " : "▶ 📁 ") + folder + "  (" + count + ")", style))
            {
                foldouts[k] = !open;
                open = !open;
            }
            HandleDrop(key, GUILayoutUtility.GetLastRect(), folder, null, all, true); // drop onto a folder = put it in (at the end)
            if (GUILayout.Button(new GUIContent("✏️", "Rename this folder"), GUILayout.Width(28)))
            {
                renamingKey = key;
                renamingFolder = folder;
                renamingTo = folder;
            }
            if (GUILayout.Button(new GUIContent("✕", "Remove this folder (the things in it stay; they just go back to the top of the list)"), GUILayout.Width(26)))
            {
                SetFolder(things, "");
                GUIUtility.ExitGUI();
            }
        }
        return open;
    }

    /// <summary>
    /// At the top of a selected thing's panel: its Name (rename it) and its 📁 Folder.
    /// With several selected, the folder choice goes to all of them.
    /// </summary>
    /// <param name="boxTitle">Set: drawn in a box with this title in its own coloured bar (like '📐 Transform settings').</param>
    private void DrawNameAndFolder<T>(T item, string key, List<T> all, string boxTitle = null) where T : Component
    {
        if (item == null) return;
        List<T> chosen = SelectedAll<T>().ToList();
        if (!chosen.Contains(item)) chosen.Add(item);

        if (boxTitle != null) ObstacleBaseEditor.BeginBox(boxTitle, "Its name, and which folder it's in (in the list above).");
        using (new EditorGUILayout.VerticalScope(boxTitle != null ? GUIStyle.none : EditorStyles.helpBox))
        {
            if (chosen.Count == 1)
            {
                string newName = EditorGUILayout.DelayedTextField(new GUIContent("Name", "Type a new name and press Enter."), item.name);
                if (newName != item.name && newName.Trim() != "")
                {
                    Undo.RecordObject(item.gameObject, "Rename");
                    item.gameObject.name = newName.Trim();
                    RefreshSceneLists();
                }
            }
            else
            {
                EditorGUILayout.LabelField(chosen.Count + " selected", EditorStyles.boldLabel);
            }

            // Folder: No folder / existing folders / New folder...
            List<string> existing = all.Where(c => c != null).Select(FolderOf).Where(f => f != "")
                .Distinct().OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase).ToList();
            string current = chosen.Select(c => FolderOf(c)).Distinct().Count() == 1 ? FolderOf(chosen[0]) : null;
            List<string> options = new List<string> { "(no folder)" };
            options.AddRange(existing);
            options.Add("➕ New folder…");
            int index = current == null ? -1 : current == "" ? 0 : existing.IndexOf(current) + 1;

            EditorGUI.showMixedValue = current == null;
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(new GUIContent("📁 Folder", "Groups things in the list above, so they're easier to find. It doesn't change the game."),
                Mathf.Max(0, index), options.ToArray());
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (picked == options.Count - 1) { newFolderFor = key; newFolderName = ""; }
                else
                {
                    SetFolder(chosen.Cast<Component>(), picked == 0 ? "" : existing[picked - 1]);
                    GUIUtility.ExitGUI();
                }
            }

            if (newFolderFor == key)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    newFolderName = EditorGUILayout.TextField("New folder's name", newFolderName);
                    using (new EditorGUI.DisabledScope(newFolderName.Trim() == ""))
                    {
                        if (GUILayout.Button("Make it", GUILayout.Width(70)))
                        {
                            SetFolder(chosen.Cast<Component>(), newFolderName);
                            newFolderFor = null;
                            GUI.FocusControl(null);
                            GUIUtility.ExitGUI();
                        }
                    }
                    if (GUILayout.Button("✕", GUILayout.Width(26))) { newFolderFor = null; GUI.FocusControl(null); }
                }
            }
            if (chosen.Count == 1)
                EditorGUILayout.LabelField("Tip: Ctrl-click more in the list to put several into a folder at once.", EditorStyles.miniLabel);
        }
        if (boxTitle != null) ObstacleBaseEditor.EndBox();
    }

    /// <summary>'Wanderer' if the name is free, else 'Wanderer 2', 'Wanderer 3'...</summary>
    private static string UniqueName<T>(string wanted, T except) where T : Component
    {
        HashSet<string> taken = new HashSet<string>(FindAll<T>().Where(c => c != null && c != except).Select(c => c.gameObject.name));
        if (!taken.Contains(wanted)) return wanted;
        for (int i = 2; ; i++)
        {
            string name = wanted + " " + i;
            if (!taken.Contains(name)) return name;
        }
    }
}
