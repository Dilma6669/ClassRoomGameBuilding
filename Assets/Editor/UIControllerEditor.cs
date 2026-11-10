using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TriggerController))]
public class UIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        KidInspector.DrawDefault(serializedObject);

        TriggerController controller = (TriggerController)target;

        GUILayout.Space(10);

        // Big and green like 'Create Checkpoint' (green = make something new)
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 45
        };
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button(new GUIContent("💬 Create Pop-up Sign", "Makes an invisible zone: when the player walks into it, a message or video pops up."), buttonStyle))
        {
            controller.CreateTrigger();
        }
        GUI.backgroundColor = Color.white;
    }
}