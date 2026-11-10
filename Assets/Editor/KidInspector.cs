using UnityEditor;
using UnityEngine;

/// <summary>
/// Helpers that make Inspectors friendlier for the class:
/// • DrawDefault: Unity's normal Inspector, but without the greyed 'Script' box when drawn inside the Game Builder.
/// • ShowAsDrawer: draws a setting under its [ShowAs("...")] name (keeps [Range] sliders and [Tooltip]s).
/// </summary>
public static class KidInspector
{
    /// <summary>Like DrawDefaultInspector(), minus the 'Script' box in the Game Builder. Returns true if something changed.</summary>
    public static bool DrawDefault(SerializedObject serializedObject)
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        SerializedProperty it = serializedObject.GetIterator();
        bool enterChildren = true;
        while (it.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (it.name == "m_Script")
            {
                if (GameBuilderWindow.DrawingInGameBuilder) continue;
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(it, true);
                continue;
            }
            EditorGUILayout.PropertyField(it, true);
        }
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        return changed;
    }
}

[CustomPropertyDrawer(typeof(ShowAsAttribute))]
public class ShowAsDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowAsAttribute showAs = (ShowAsAttribute)attribute;
        label = new GUIContent(showAs.label, label.tooltip);
        RangeAttribute range = (RangeAttribute)System.Attribute.GetCustomAttribute(fieldInfo, typeof(RangeAttribute));

        label = EditorGUI.BeginProperty(position, label, property);
        switch (property.propertyType)
        {
            case SerializedPropertyType.Float:
                if (range != null) EditorGUI.Slider(position, property, range.min, range.max, label);
                else property.floatValue = EditorGUI.FloatField(position, label, property.floatValue);
                break;
            case SerializedPropertyType.Integer:
                if (range != null) EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
                else property.intValue = EditorGUI.IntField(position, label, property.intValue);
                break;
            case SerializedPropertyType.Boolean:
                property.boolValue = EditorGUI.Toggle(position, label, property.boolValue);
                break;
            case SerializedPropertyType.String:
                property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
                break;
            case SerializedPropertyType.Enum:
                property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, property.enumDisplayNames);
                break;
            case SerializedPropertyType.Color:
                property.colorValue = EditorGUI.ColorField(position, label, property.colorValue);
                break;
            case SerializedPropertyType.ObjectReference:
                EditorGUI.ObjectField(position, property, label);
                break;
            default:
                EditorGUI.LabelField(position, label.text, "(can't show this here)");
                break;
        }
        EditorGUI.EndProperty();
    }
}
