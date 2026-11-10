using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// List order in the Game Builder: ▲ ▼ on the selected row, and drag-and-drop of rows onto
/// folders, onto other rows (goes just above that row), or onto the 'no folder' strip.
/// The order and folder are saved in the object's hidden ListFolder label.
/// </summary>
public partial class GameBuilderWindow
{
    private const string DragDataKey = "GameBuilderList";
    [System.NonSerialized] private List<Component> dragItems;
    [System.NonSerialized] private string dragKey;
    [System.NonSerialized] private Rect? dropTarget;
    [System.NonSerialized] private Vector2 dragFrom;

    private static int OrderOf(Component c)
    {
        ListFolder f = c != null ? c.GetComponent<ListFolder>() : null;
        return f != null ? f.order : int.MaxValue / 2;
    }

    /// <summary>Your order first (▲ ▼ / drag), then A to Z ('Sign 2' before 'Sign 10').</summary>
    private static List<Component> Ordered(IEnumerable<Component> things)
    {
        List<Component> list = things.Where(c => c != null).ToList();
        list.Sort((a, b) =>
        {
            int byOrder = OrderOf(a).CompareTo(OrderOf(b));
            return byOrder != 0 ? byOrder : EditorUtility.NaturalCompare(a.name, b.name);
        });
        return list;
    }

    /// <summary>One row: click to pick it (Ctrl/Shift-click to add), drag to move it. The picked row gets ▲ ▼.</summary>
    private void DrawListRow(string key, Component item, List<Component> group, string folder, bool isSelected, string text, float indent,
        List<Component> chosen, List<Component> all)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (indent > 0f) GUILayout.Space(indent);
            GUIStyle style = isSelected ? rowSelectedStyle : rowStyle;
            GUIContent content = new GUIContent((isSelected ? "▶ " : "") + text, "Click to pick it. Drag it onto a folder, or onto another row, to move it.");
            Rect r = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true));
            RowControl(key, r, item, content, style, chosen);

            if (isSelected && chosen.Count <= 1)
            {
                int i = group.IndexOf(item);
                using (new EditorGUI.DisabledScope(i <= 0))
                {
                    if (GUILayout.Button(new GUIContent("▲", "Move it up"), GUILayout.Width(26)))
                    {
                        MoveItems(new List<Component> { item }, folder, group[i - 1], all, key);
                        GUIUtility.ExitGUI();
                    }
                }
                using (new EditorGUI.DisabledScope(i < 0 || i >= group.Count - 1))
                {
                    if (GUILayout.Button(new GUIContent("▼", "Move it down"), GUILayout.Width(26)))
                    {
                        MoveItems(new List<Component> { item }, folder, i + 2 < group.Count ? group[i + 2] : null, all, key);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            HandleDrop(key, r, folder, item, all, false); // dropped onto a row: it goes just above that row
        }
    }

    private void RowControl(string key, Rect r, Component item, GUIContent content, GUIStyle style, List<Component> chosen)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && r.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    dragFrom = e.mousePosition;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id && (e.mousePosition - dragFrom).magnitude > 6f)
                {
                    GUIUtility.hotControl = 0;
                    // Dragging one of several picked rows moves all of them
                    List<Component> moving = chosen.Contains(item) && chosen.Count > 1 ? new List<Component>(chosen) : new List<Component> { item };
                    dragItems = moving;
                    dragKey = key;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[0]; // only for this list, not for the Scene or Hierarchy
                    DragAndDrop.SetGenericData(DragDataKey, key);
                    DragAndDrop.StartDrag(moving.Count == 1 ? item.name : moving.Count + " things");
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                    if (r.Contains(e.mousePosition))
                    {
                        // Ctrl/Shift-click adds it to the picked ones (to move several at once)
                        if (e.control || e.shift || e.command)
                        {
                            List<Object> now = Selection.objects.ToList();
                            if (now.Contains(item.gameObject)) now.Remove(item.gameObject); else now.Add(item.gameObject);
                            Selection.objects = now.ToArray();
                        }
                        else SelectAndFrame(item.gameObject);
                    }
                }
                break;

            case EventType.Repaint:
                style.Draw(r, content, r.Contains(e.mousePosition), GUIUtility.hotControl == id, false, false);
                break;
        }
    }

    /// <summary>
    /// A place rows can be dropped. box = a folder or the 'no folder' strip (it lights up);
    /// otherwise a row (a line shows it will go just above it).
    /// </summary>
    private void HandleDrop(string key, Rect r, string folder, Component before, List<Component> all, bool box)
    {
        Event e = Event.current;
        bool ours = dragItems != null && dragKey == key && (DragAndDrop.GetGenericData(DragDataKey) as string) == key;

        if (ours && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && r.Contains(e.mousePosition))
        {
            bool ontoItself = before != null && dragItems.Contains(before);
            DragAndDrop.visualMode = ontoItself ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Move;
            dropTarget = r;
            if (e.type == EventType.DragPerform && !ontoItself)
            {
                DragAndDrop.AcceptDrag();
                List<Component> moving = dragItems;
                dragItems = null;
                dropTarget = null;
                MoveItems(moving, folder, before, all, key);
            }
            e.Use();
            Repaint();
        }

        if (e.type == EventType.Repaint && ours && dropTarget.HasValue && dropTarget.Value == r)
        {
            Color c = new Color(0.4f, 0.8f, 1f);
            if (box) EditorGUI.DrawRect(r, new Color(c.r, c.g, c.b, 0.25f));
            else EditorGUI.DrawRect(new Rect(r.x, r.y - 1f, r.width, 3f), c);
        }
    }

    /// <summary>
    /// Puts things into a folder ("" = no folder), just above 'before' (or at the end if null),
    /// then renumbers that group's order. One Undo step.
    /// </summary>
    private void MoveItems(List<Component> moving, string folder, Component before, List<Component> all, string key)
    {
        moving = Ordered(moving.Where(m => m != null)); // they keep their order among themselves
        List<Component> group = Ordered(all.Where(c => c != null && FolderOf(c) == folder && !moving.Contains(c)));
        int at = before != null && group.Contains(before) ? group.IndexOf(before) : group.Count;
        group.InsertRange(at, moving);

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(folder == "" ? "Move in list" : "Move into folder " + folder);
        for (int i = 0; i < group.Count; i++)
        {
            ListFolder f = group[i].GetComponent<ListFolder>();
            if (f == null)
            {
                f = Undo.AddComponent<ListFolder>(group[i].gameObject);
                f.hideFlags = HideFlags.HideInInspector;
            }
            Undo.RecordObject(f, "Move in list");
            f.folder = folder;
            f.order = (i + 1) * 10;
            EditorUtility.SetDirty(f);
        }
        Undo.CollapseUndoOperations(undoGroup);
        if (folder != "") foldouts["folder/" + key + "/" + folder] = true; // show where it went
        Repaint();
    }
}
