using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The '✔ Confirm text' button used under typed text (Library entries, game title, death screen...):
/// finishes the typing and saves the file straight away, then shows '✔ Saved!' for a moment.
/// </summary>
public static class ConfirmText
{
    public const string Label = "✔ Confirm text";
    private static readonly Dictionary<string, double> savedAt = new Dictionary<string, double>();

    /// <summary>Finish typing and save the asset (or mark the scene to be saved).</summary>
    public static void Save(SerializedObject so, string key)
    {
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
        EditorGUIUtility.editingTextField = false;
        so.ApplyModifiedProperties();
        foreach (Object target in so.targetObjects)
        {
            EditorUtility.SetDirty(target);
            if (EditorUtility.IsPersistent(target)) AssetDatabase.SaveAssetIfDirty(target);
            else if (!Application.isPlaying && target is Component c && c.gameObject.scene.IsValid())
            {
                // Something in the level (e.g. a checkpoint): save the level
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(c.gameObject.scene);
            }
        }
        savedAt[key] = EditorApplication.timeSinceStartup;
    }

    /// <summary>True for a couple of seconds after that button was clicked.</summary>
    public static bool JustSaved(string key)
    {
        return savedAt.TryGetValue(key, out double t) && EditorApplication.timeSinceStartup - t < 2.5;
    }

    public const float Width = 170f, Height = 24f;

    /// <summary>
    /// The small Confirm button, centred under the text box (so it doesn't look like the big action buttons),
    /// with '✔ Saved!' under it for a moment. Returns true when clicked.
    /// </summary>
    public static bool CentredButton(string key)
    {
        bool clicked;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            clicked = GUILayout.Button(Label, GUILayout.Width(Width), GUILayout.Height(Height));
            GUILayout.FlexibleSpace();
        }
        if (JustSaved(key))
        {
            EditorGUILayout.LabelField("✔ Saved!", new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter });
            if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.Repaint();
        }
        return clicked;
    }

    /// <summary>The button, for Editors that lay things out automatically.</summary>
    public static void Button(SerializedObject so, string key)
    {
        if (CentredButton(key)) Save(so, key);
    }
}

/// <summary>
/// How a Library entry looks: name, type, picture (with a preview of it), backstory, and a Confirm button.
/// Every entry uses this, including new ones made with the + button.
/// </summary>
[CustomPropertyDrawer(typeof(LibraryEntry))]
public class LibraryEntryDrawer : PropertyDrawer
{
    private const float Preview = 96f;

    private static float Line => EditorGUIUtility.singleLineHeight;
    private static float Gap => EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = Line + Gap; // the fold-out line with the entry's name
        if (!property.isExpanded) return h;

        h += (Line + Gap) * 3; // name, type, picture
        if (property.FindPropertyRelative("picture").objectReferenceValue != null) h += Preview + Gap;
        h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("backstory"), true) + Gap;
        h += 24 + Gap; // Confirm button
        if (ConfirmText.JustSaved(property.serializedObject.targetObject.GetInstanceID() + property.propertyPath)) h += Line;
        return h + 4;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty name = property.FindPropertyRelative("name");
        SerializedProperty category = property.FindPropertyRelative("category");
        SerializedProperty picture = property.FindPropertyRelative("picture");
        SerializedProperty backstory = property.FindPropertyRelative("backstory");

        Rect r = new Rect(position.x, position.y, position.width, Line);
        string title = string.IsNullOrEmpty(name.stringValue) ? "(no name)" : name.stringValue;
        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, title, true, EditorStyles.foldout);
        if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

        EditorGUI.indentLevel++;
        r.y += Line + Gap;
        EditorGUI.PropertyField(r, name);
        r.y += Line + Gap;
        EditorGUI.PropertyField(r, category, new GUIContent("Type"));
        r.y += Line + Gap;
        EditorGUI.PropertyField(r, picture);

        // A preview of the chosen picture, under its box
        if (picture.objectReferenceValue is Sprite sprite)
        {
            r.y += Line + Gap;
            Rect box = new Rect(position.x + EditorGUIUtility.labelWidth + 2, r.y, Preview, Preview);
            EditorGUI.DrawRect(box, new Color(0.12f, 0.13f, 0.16f));
            Texture2D tex = sprite.texture;
            if (tex != null)
            {
                Rect uv = sprite.textureRect;
                Rect coords = new Rect(uv.x / tex.width, uv.y / tex.height, uv.width / tex.width, uv.height / tex.height);
                float aspect = uv.width / Mathf.Max(1f, uv.height);
                Rect fit = aspect >= 1f
                    ? new Rect(box.x, box.y + (box.height - box.height / aspect) / 2f, box.width, box.height / aspect)
                    : new Rect(box.x + (box.width - box.width * aspect) / 2f, box.y, box.width * aspect, box.height);
                GUI.DrawTextureWithTexCoords(fit, tex, coords);
            }
            r.y += Preview - Line;
        }

        r.y += Line + Gap;
        float storyHeight = EditorGUI.GetPropertyHeight(backstory, true);
        EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, storyHeight), backstory, true);
        r.y += storyHeight + Gap;
        EditorGUI.indentLevel--;

        Rect button = new Rect(position.x + (position.width - ConfirmText.Width) / 2f, r.y, ConfirmText.Width, ConfirmText.Height); // small, centred
        if (GUI.Button(button, ConfirmText.Label)) ConfirmText.Save(property.serializedObject, property.serializedObject.targetObject.GetInstanceID() + property.propertyPath);
        if (ConfirmText.JustSaved(property.serializedObject.targetObject.GetInstanceID() + property.propertyPath))
        {
            EditorGUI.LabelField(new Rect(position.x, button.yMax + Gap, position.width, Line), "✔ Saved!", new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter });
            if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.Repaint();
        }
        EditorGUI.EndProperty();
    }
}

/// <summary>
/// Game Settings: Unity's normal layout, plus a '✔ Confirm text' button under each group of typed text
/// (game title + subtitle, death title + message). Library entries get theirs from LibraryEntryDrawer.
/// </summary>
[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : TextConfirmEditor
{
    private static readonly string[] FontsFirst = { "titleFont", "bodyFont" };

    /// <summary>
    /// Shown in other Game Builder tabs instead (Level: world border, Checkpoints: on/off,
    /// Player / Platforms / Creatures: default models), so Signs & Story only has the story things.
    /// </summary>
    private static readonly HashSet<string> ShownInOtherTabs = new HashSet<string>
    {
        "useCheckpoints", "useWorldBorder", "borderPadding", "skyLimitHeight", "fallKillDepth",
        "defaultPlayerModel", "defaultPlatformModel", "defaultWanderEnemy", "defaultPatrolEnemy", "defaultStaticEnemy"
    };

    protected override List<SerializedProperty> Properties()
    {
        List<SerializedProperty> all = base.Properties();
        // The fonts go first, just under the script (they change every title and text in the game)
        List<SerializedProperty> fonts = all.FindAll(p => System.Array.IndexOf(FontsFirst, p.name) >= 0);
        List<SerializedProperty> rest = all.FindAll(p => System.Array.IndexOf(FontsFirst, p.name) < 0 && p.name != "m_Script");
        if (GameBuilderWindow.DrawingInGameBuilder) rest.RemoveAll(p => ShownInOtherTabs.Contains(p.name));

        List<SerializedProperty> ordered = all.FindAll(p => p.name == "m_Script");
        ordered.AddRange(fonts);
        ordered.AddRange(rest);
        return ordered;
    }
}

/// <summary>Checkpoint flags: their message gets a '✔ Confirm text' button (it saves the level).</summary>
[CustomEditor(typeof(Checkpoint))]
public class CheckpointEditor : TextConfirmEditor { }

/// <summary>Unity's normal layout, with a '✔ Confirm text' button after each group of text boxes.</summary>
public class TextConfirmEditor : Editor
{
    /// <summary>The properties to draw, in order (override to hide or move some).</summary>
    protected virtual List<SerializedProperty> Properties()
    {
        List<SerializedProperty> list = new List<SerializedProperty>();
        SerializedProperty it = serializedObject.GetIterator();
        bool enterChildren = true;
        while (it.NextVisible(enterChildren))
        {
            enterChildren = false;
            list.Add(it.Copy());
        }
        return list;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        List<SerializedProperty> props = Properties();
        for (int i = 0; i < props.Count; i++)
        {
            SerializedProperty p = props[i];
            if (p.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(p);
                continue;
            }
            EditorGUILayout.PropertyField(p, true);

            // After the last text box in a group, a Confirm button
            if (p.propertyType == SerializedPropertyType.String)
            {
                bool nextIsText = i + 1 < props.Count && props[i + 1].propertyType == SerializedPropertyType.String;
                if (!nextIsText) ConfirmText.Button(serializedObject, target.GetInstanceID() + p.propertyPath);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
