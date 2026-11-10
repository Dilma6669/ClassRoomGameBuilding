using UnityEditor;
using UnityEngine;

/// <summary>
/// The one look for every open/close section in the Game Builder (and the Inspectors it shows):
/// a muted teal bar across the width with a brighter strip on the left, then the arrow + bold title.
/// Change the colours here and every fold-out bar changes with it.
/// </summary>
public static class FoldoutBar
{
    private static readonly Color DarkBar = new Color(0.19f, 0.33f, 0.37f);
    private static readonly Color DarkStrip = new Color(0.33f, 0.58f, 0.62f);
    private static readonly Color LightBar = new Color(0.72f, 0.82f, 0.84f);
    private static readonly Color LightStrip = new Color(0.35f, 0.6f, 0.65f);

    private static GUIStyle titleStyle;

    public static bool Draw(bool open, string label)
    {
        if (titleStyle == null) titleStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        Rect rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            bool dark = EditorGUIUtility.isProSkin;
            EditorGUI.DrawRect(rect, dark ? DarkBar : LightBar);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), dark ? DarkStrip : LightStrip);
        }
        Rect inside = new Rect(rect.x + 7, rect.y + 2, rect.width - 7, rect.height - 4);
        return EditorGUI.Foldout(inside, open, label, true, titleStyle);
    }
}
