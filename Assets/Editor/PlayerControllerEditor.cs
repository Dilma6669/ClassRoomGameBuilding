using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerController controller = (PlayerController)target;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };

        EditorGUILayout.Space(15);

        // Big Green Create Character Button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Bright green
        if (GUILayout.Button("🧙‍♂️ Create Character", buttonStyle))
        {
            controller.CreateCharacterMesh();
        }

        GUI.backgroundColor = Color.white;

        DrawPlayerModelSection(controller);
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

        if (cm != null && cm.model != null && GUILayout.Button("⚙️ Size, animations & Library picture..."))
        {
            Selection.activeGameObject = cm.gameObject;
        }
    }
}