using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TriggerController))]
public class UIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TriggerController controller = (TriggerController)target;

        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Trigger", GUILayout.Height(35)))
        {
            controller.CreateTrigger();
        }
        GUI.backgroundColor = Color.white;
    }
}