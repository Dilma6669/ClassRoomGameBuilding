using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

/// <summary>
/// Level tab helpers: brush size and strength sliders for the terrain brushes, and friendly sky names.
/// </summary>
public partial class GameBuilderWindow
{
    private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// Finds the size and strength of the terrain brush being used right now.
    /// • Terrain Tools package brushes (the '...Ovl' tools) keep them in their 'm_commonUI' (one per brush).
    /// • Unity's own brushes (e.g. Paint Grass) share the Terrain Inspector's brushSize / brushStrength.
    /// Both are internal to Unity, so this uses reflection and simply finds nothing if Unity changes them.
    /// </summary>
    private static bool FindActiveBrush(out object owner, out PropertyInfo size, out PropertyInfo strength)
    {
        owner = null;
        size = strength = null;
        try
        {
            System.Type manager = typeof(Editor).Assembly.GetType("UnityEditor.EditorTools.EditorToolManager");
            object tool = manager?.GetProperty("activeTool", AnyMember)?.GetValue(null);
            if (tool == null || !IsTerrainTool(tool.GetType())) return false;

            // Terrain Tools package
            PropertyInfo ui = tool.GetType().GetProperty("m_commonUI", AnyMember);
            if (ui != null)
            {
                owner = ui.GetValue(tool);
                if (owner == null) return false;
                size = owner.GetType().GetProperty("brushSize", AnyMember);
                strength = owner.GetType().GetProperty("brushStrength", AnyMember);
                return size != null && strength != null;
            }

            // Unity's built-in brushes: the open Terrain Inspector holds the values
            System.Type inspectorType = System.Type.GetType("UnityEditor.TerrainInspector, UnityEditor.TerrainModule");
            if (inspectorType == null) return false;
            owner = Resources.FindObjectsOfTypeAll(inspectorType).FirstOrDefault();
            if (owner == null) return false;
            size = inspectorType.GetProperty("brushSize", AnyMember);
            strength = inspectorType.GetProperty("brushStrength", AnyMember);
            return size != null && strength != null;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static bool IsTerrainTool(System.Type type)
    {
        for (System.Type t = type; t != null; t = t.BaseType)
        {
            if (t.Name.StartsWith("TerrainPaintTool")) return true;
        }
        return false;
    }

    /// <summary>Brush size and strength sliders, under the brush buttons.</summary>
    private void DrawBrushSliders(Terrain terrain)
    {
        bool found = FindActiveBrush(out object owner, out PropertyInfo sizeProp, out PropertyInfo strengthProp);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (!found)
            {
                EditorGUILayout.LabelField("🖌 Pick a brush above, then you can change its size and strength here.", EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Slider("Brush size", 25f, 1f, 100f);
                    EditorGUILayout.Slider("Brush strength (%)", 50f, 1f, 100f);
                }
                return;
            }

            float maxSize = terrain != null && terrain.terrainData != null
                ? Mathf.Clamp(Mathf.Min(terrain.terrainData.size.x, terrain.terrainData.size.z) / 2f, 20f, 300f)
                : 100f;

            float size = Mathf.Round(System.Convert.ToSingle(sizeProp.GetValue(owner)) * 10f) / 10f;
            float strength = Mathf.Round(System.Convert.ToSingle(strengthProp.GetValue(owner)) * 100f);

            EditorGUI.BeginChangeCheck();
            size = EditorGUILayout.Slider(new GUIContent("Brush size", "How wide the brush is (in metres)."), size, 0.5f, maxSize);
            strength = EditorGUILayout.Slider(new GUIContent("Brush strength (%)", "How much each stroke changes the ground. Low = gentle, high = fast."), strength, 1f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    sizeProp.SetValue(owner, size);
                    strengthProp.SetValue(owner, strength / 100f);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Couldn't change the brush: " + e.Message);
                }
                SceneView.RepaintAll();
            }
        }
    }

    /// <summary>Keeps the brush sliders up to date when the brush is changed in the Scene view.</summary>
    private void OnInspectorUpdate()
    {
        if (tab == Tab.Level && ToolManager.activeToolType != null && IsTerrainTool(ToolManager.activeToolType)) Repaint();
    }

    /// <summary>'SkyBox01Material' becomes 'Sky 1 (picture)'; Unity's procedural sky becomes 'Sky (plain, follows the sun)'.</summary>
    private static string FriendlySkyName(Material sky)
    {
        string name = sky.name;
        name = Regex.Replace(name, "(Material|Mat)$", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, "^Sky ?Box", "Sky", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");               // CamelCase -> Camel Case
        name = Regex.Replace(name, "([A-Za-z])0*([0-9]+)", "$1 $2");          // Sky01 -> Sky 1
        name = name.Replace('_', ' ').Trim();
        if (name.Length == 0) name = "Sky";

        string shader = sky.shader != null ? sky.shader.name : "";
        if (shader.Contains("Procedural")) name += "  (plain, follows the sun)";
        else if (shader.Contains("Cubemap") || shader.Contains("6 Sided") || shader.Contains("Panoramic")) name += "  (picture)";
        return name;
    }
}
