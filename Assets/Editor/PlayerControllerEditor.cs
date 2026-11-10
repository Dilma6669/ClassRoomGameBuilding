using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        KidInspector.DrawDefault(serializedObject);

        PlayerController controller = (PlayerController)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };

        EditorGUILayout.Space(15);

        // Big Green Create Character Button. Once there is a character, the same button repairs it
        // (CreateCharacterMesh reuses the 'ExampleCharacter...' child instead of making a second one).
        bool hasCharacter = HasCharacter(controller);
        GUI.backgroundColor = hasCharacter ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.2f, 0.8f, 0.3f); // grey = fix, green = create
        GUIContent createLabel = hasCharacter
            ? new GUIContent("🔧 Fix my character", "Puts back anything missing from your character (camera, health, sounds...). Your settings and model are kept.")
            : new GUIContent("🧙‍♂️ Create Character", "Makes your player and its camera in this level.");
        if (GUILayout.Button(createLabel, buttonStyle))
        {
            controller.CreateCharacterMesh();
        }

        GUI.backgroundColor = Color.white;

        DrawPlayerModelSection(controller);
    }

    /// <summary>True when the level already has a character (a child named 'ExampleCharacter...').</summary>
    private static bool HasCharacter(PlayerController controller)
    {
        foreach (Transform child in controller.transform)
        {
            if (child.name.StartsWith("ExampleCharacter")) return true;
        }
        return false;
    }

    /// <summary>A shortcut so students can change the player's look without digging into the hierarchy.</summary>
    private static void DrawPlayerModelSection(PlayerController controller)
    {
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🎨 Player Model", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });

        // The player's body is the child with the Health script on it
        Health body = controller.GetComponentInChildren<Health>(true);
        if (body == null)
        {
            EditorGUILayout.HelpBox("Click 'Create Character' first, then you can give your player a model here.", MessageType.None);
            return;
        }

        CustomModel cm = body.GetComponent<CustomModel>();
        GameObject current = cm != null ? cm.model : null;

        EditorGUILayout.HelpBox("Drag a model from Assets/MyModels into the box below to change how your player looks.", MessageType.Info);
        GameObject chosen = (GameObject)EditorGUILayout.ObjectField("Player Model", current, typeof(GameObject), false);

        if (chosen != current)
        {
            if (cm == null) cm = Undo.AddComponent<CustomModel>(body.gameObject);
            Undo.RecordObject(cm, "Change Player Model");
            cm.model = chosen;
            cm.Refresh();
            EditorUtility.SetDirty(cm);
        }

        // Not needed in the Game Builder: its Player tab has a 'Model size, turn & animations' fold-out for this
        if (cm != null && cm.model != null && !GameBuilderWindow.DrawingInGameBuilder && GUILayout.Button("⚙️ Size, animations & Library picture..."))
        {
            Selection.activeGameObject = cm.gameObject;
        }
    }
}