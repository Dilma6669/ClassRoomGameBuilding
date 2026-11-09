using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Makes the soft round sparkle texture and the glowing (additive) material used by Sparkles.cs.
/// They live in Assets/Resources so they're always included in the built game. Re-made if deleted.
/// </summary>
[InitializeOnLoad]
public static class SparkleAssets
{
    private const string TexturePath = "Assets/Resources/SparkleDot.png";
    private const string MaterialPath = "Assets/Resources/SparkleMaterial.mat";

    static SparkleAssets()
    {
        EditorApplication.delayCall += Ensure;
    }

    public static void Ensure()
    {
        if (File.Exists(TexturePath) && File.Exists(MaterialPath)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        if (!File.Exists(TexturePath))
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // soft glowing dot
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        if (!File.Exists(MaterialPath))
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader) { name = "SparkleMaterial" };
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.9f, 0.9f, 0.9f, 1f)); // brighter than the default so it shows in daylight
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        AssetDatabase.SaveAssets();
    }
}
