using UnityEditor;
using UnityEngine;

/// <summary>
/// Creature & pickup Inspector: the normal settings, but hides ones that don't apply
/// (jump-on settings on pickups, bounce settings when not bouncy, sparkle options when sparkles are off).
/// </summary>
[CustomEditor(typeof(ObstacleBase), true)]
[CanEditMultipleObjects]
public class ObstacleBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ObstacleBase o = (ObstacleBase)target;
        bool many = targets.Length > 1; // with several selected, show everything

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script") continue;

            if (!many)
            {
                if ((prop.name == "jumpsToDefeat" || prop.name == "stompBounce") && o.payloadType != ObstacleBase.PayloadType.Damage) continue;
                if ((prop.name == "triggerRadius" || prop.name == "launchForce" || prop.name == "upwardBias" || prop.name == "momentumTransfer") && !o.isBouncy) continue;
                if ((prop.name == "sparkleStyle" || prop.name == "customSparkleColour" || prop.name == "sparkleAmount" || prop.name == "sparkleSize") && !o.sparkles) continue;
                if (prop.name == "sparkleColour" && (!o.sparkles || !o.customSparkleColour)) continue;
            }

            if (prop.name == "sparkles")
                EditorGUILayout.PropertyField(prop, new GUIContent("Sparkle Burst", "A burst of sparkles when it's picked up or beaten."));
            else if (prop.name == "jumpsToDefeat")
                EditorGUILayout.PropertyField(prop, new GUIContent("Jumps On Head To Beat", prop.tooltip));
            else
                EditorGUILayout.PropertyField(prop, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
