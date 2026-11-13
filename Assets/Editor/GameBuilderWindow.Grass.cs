using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 🏔 Level tab: 🌱 Grass (grass types for the Paint Grass brush, how far it shows, how thick)
/// and ✨ Graphics & effects (Scripts/Lighting/LevelGraphics.cs).
/// </summary>
public partial class GameBuilderWindow
{
    private const string SimpleGrassPath = "Assets/Resources/Simple Grass.png";
    private static readonly string[] GrassModels = { "Grass", "Grass_2", "Grass_Short" };
    private const string GrassModelFolder = "Assets/MyModels/NatureModelsPackTest/FBX/";
    private int grassModelChoice;

    private static readonly Color Green = new Color(0.2f, 0.8f, 0.3f);
    private static readonly Color Red = new Color(0.95f, 0.5f, 0.5f);

    // ------------------------------------------------------------------ 🌱 Grass

    private void DrawGrassBox(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        DetailPrototype[] grass = data.detailPrototypes;

        if (grass.Length == 0)
        {
            EditorGUILayout.HelpBox("This ground has no grass yet. Add a grass type, then click '🌱 Paint Grass' above and paint on the ground.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Click '🌱 Paint Grass' above and paint on the ground (hold Shift to rub it out).\n" +
                                    "Got more than one grass? Pick which one to paint in the Inspector's grass pictures.", MessageType.Info);
            for (int i = 0; i < grass.Length; i++)
            {
                if (DrawGrassType(terrain, i)) GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.Space(4);
        int clicked = ButtonGrid(Columns(2), new[] { "🌱 Add simple grass\n(fastest)", "🌿 Add grass model\n(nicer, slower)" }, new Color?[] { Green, Green });
        grassModelChoice = EditorGUILayout.Popup(new GUIContent("Grass model", "Which 3D grass the 'Add grass model' button uses."), grassModelChoice, GrassModels);
        if (clicked == 0) { AddSimpleGrass(terrain); GUIUtility.ExitGUI(); }
        if (clicked == 1) { AddModelGrass(terrain, GrassModels[grassModelChoice]); GUIUtility.ExitGUI(); }

        // How much it costs: distance and thickness (the two things that make grass slow)
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Lots of grass far away makes the game slow. Keep these small for weak computers.", EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();
        float distance = EditorGUILayout.Slider(new GUIContent("Grass shows up to (m)", "Grass further away than this isn't drawn. Less = faster."), terrain.detailObjectDistance, 10f, 150f);
        float density = EditorGUILayout.Slider(new GUIContent("Grass thickness", "How many tufts are drawn. Less = faster."), terrain.detailObjectDensity, 0.1f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(terrain, "Grass settings");
            terrain.detailObjectDistance = distance;
            terrain.detailObjectDensity = density;
        }

        if (grass.Length > 0 && BigButton("🧹 Remove ALL painted grass", Red)
            && EditorUtility.DisplayDialog("Remove all grass?", "Rub out all the grass painted on this ground? (Ctrl+Z puts it back.)", "Remove it", "Cancel"))
        {
            Undo.RegisterCompleteObjectUndo(data, "Remove all grass");
            int[,] empty = new int[data.detailHeight, data.detailWidth];
            for (int layer = 0; layer < grass.Length; layer++) data.SetDetailLayer(0, 0, layer, empty);
            GUIUtility.ExitGUI();
        }
    }

    /// <summary>One grass type: its name, size and colours, and a delete button. True if it was deleted.</summary>
    private static bool DrawGrassType(Terrain terrain, int index)
    {
        TerrainData data = terrain.terrainData;
        DetailPrototype[] grass = data.detailPrototypes;
        DetailPrototype g = grass[index];
        string name = g.usePrototypeMesh ? (g.prototype != null ? g.prototype.name : "missing model") + " (model)"
                                         : (g.prototypeTexture != null ? g.prototypeTexture.name : "missing picture");

        bool deleted = false;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((g.usePrototypeMesh ? "🌿 " : "🌱 ") + name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("🗑", "Delete this grass type (and the grass painted with it)."), GUILayout.Width(32))
                    && EditorUtility.DisplayDialog("Delete grass?", "Delete '" + name + "' and all the grass painted with it?", "Delete", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(data, "Delete grass");
                    var list = new System.Collections.Generic.List<DetailPrototype>(grass);
                    list.RemoveAt(index);
                    data.detailPrototypes = list.ToArray();
                    deleted = true;
                }
            }
            if (deleted) return true;

            EditorGUI.BeginChangeCheck();
            float minH = g.minHeight, maxH = g.maxHeight;
            EditorGUILayout.MinMaxSlider(new GUIContent("Height (small ↔ big)", "Tufts get a random height between these."), ref minH, ref maxH, 0.1f, 4f);
            float minW = g.minWidth, maxW = g.maxWidth;
            EditorGUILayout.MinMaxSlider(new GUIContent("Width (thin ↔ wide)", "Tufts get a random width between these."), ref minW, ref maxW, 0.1f, 4f);
            Color healthy = EditorGUILayout.ColorField(new GUIContent("Colour", "The main grass colour."), g.healthyColor);
            Color dry = EditorGUILayout.ColorField(new GUIContent("Dry patches colour", "Some patches get this colour instead."), g.dryColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(data, "Change grass");
                g.minHeight = minH; g.maxHeight = maxH; g.minWidth = minW; g.maxWidth = maxW;
                g.healthyColor = healthy; g.dryColor = dry;
                grass[index] = g;
                data.detailPrototypes = grass;
            }
        }
        return false;
    }

    /// <summary>Adds the cheap picture grass, unless the ground already has it. Returns its index.</summary>
    public static int AddSimpleGrass(Terrain terrain)
    {
        Texture2D picture = SimpleGrassPicture();
        TerrainData data = terrain.terrainData;
        DetailPrototype[] grass = data.detailPrototypes;
        for (int i = 0; i < grass.Length; i++)
        {
            if (!grass[i].usePrototypeMesh && grass[i].prototypeTexture == picture) return i;
        }

        DetailPrototype g = new DetailPrototype
        {
            usePrototypeMesh = false,
            prototypeTexture = picture,
            renderMode = DetailRenderMode.Grass, // flat, drawn in big batches: the fastest kind
            useInstancing = false,
            minWidth = 0.6f, maxWidth = 1.1f,
            minHeight = 0.4f, maxHeight = 0.9f,
            noiseSpread = 0.3f,
            healthyColor = new Color(0.55f, 0.75f, 0.35f),
            dryColor = new Color(0.75f, 0.70f, 0.40f),
            density = 1f,
        };
        return AddGrassType(terrain, g, "Add simple grass");
    }

    private static int AddModelGrass(Terrain terrain, string model)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassModelFolder + model + ".fbx");
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Grass model missing", "Couldn't find " + GrassModelFolder + model + ".fbx", "OK");
            return -1;
        }
        DetailPrototype g = new DetailPrototype
        {
            usePrototypeMesh = true,
            prototype = prefab,
            renderMode = DetailRenderMode.VertexLit,
            useInstancing = true, // drawn many at once by the graphics card
            minWidth = 0.8f, maxWidth = 1.3f,
            minHeight = 0.8f, maxHeight = 1.3f,
            noiseSpread = 0.3f,
            healthyColor = Color.white,
            dryColor = new Color(0.85f, 0.85f, 0.7f),
            density = 0.5f, // fewer tufts: each model is 192 triangles
        };
        return AddGrassType(terrain, g, "Add grass model");
    }

    private static int AddGrassType(Terrain terrain, DetailPrototype g, string undoName)
    {
        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(new Object[] { data, terrain }, undoName);
        var list = new System.Collections.Generic.List<DetailPrototype>(data.detailPrototypes) { g };
        data.detailPrototypes = list.ToArray();
        data.RefreshPrototypes();

        // The first grass: gentle 'cheap' settings so a big lawn doesn't slow the game down
        if (list.Count == 1)
        {
            terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, 60f);
            terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, 0.6f);
            terrain.drawTreesAndFoliage = true;
        }
        EditorUtility.SetDirty(data);
        return list.Count - 1;
    }

    /// <summary>The grass picture (made by code the first time: blades of grass on a see-through background).</summary>
    private static Texture2D SimpleGrassPicture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(SimpleGrassPath);
        if (existing != null) return existing;

        const int W = 128, H = 128;
        Color[] pixels = new Color[W * H];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.45f, 0.65f, 0.3f, 0f);
        System.Random random = new System.Random(5);
        for (int blade = 0; blade < 22; blade++)
        {
            float baseX = 8 + (float)random.NextDouble() * (W - 16);
            float height = H * (0.55f + 0.42f * (float)random.NextDouble());
            float bend = ((float)random.NextDouble() - 0.5f) * 30f;
            float width = 3f + (float)random.NextDouble() * 3f;
            float shade = 0.75f + 0.25f * (float)random.NextDouble();
            for (int y = 0; y < height; y++)
            {
                float t = y / height;
                float centre = baseX + bend * t * t;
                float half = width * (1f - t) * 0.5f + 0.4f;
                for (int x = Mathf.FloorToInt(centre - half); x <= Mathf.CeilToInt(centre + half); x++)
                {
                    if (x < 0 || x >= W) continue;
                    float edge = Mathf.Clamp01(half - Mathf.Abs(x - centre) + 0.5f);
                    Color c = Color.Lerp(new Color(0.55f, 0.75f, 0.4f), new Color(0.95f, 1f, 0.8f), t) * shade;
                    c.a = Mathf.Max(pixels[y * W + x].a, edge);
                    pixels[y * W + x] = c;
                }
            }
        }

        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        Directory.CreateDirectory(Path.GetDirectoryName(SimpleGrassPath));
        File.WriteAllBytes(SimpleGrassPath, tex.EncodeToPNG());
        DestroyImmediate(tex);
        AssetDatabase.ImportAsset(SimpleGrassPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SimpleGrassPath);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 128;
        importer.isReadable = true; // the terrain grass needs to read it
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SimpleGrassPath);
    }

    // ------------------------------------------------------------------ ✨ Graphics & effects

    private void DrawGraphicsBox()
    {
        LevelGraphics graphics = Find<LevelGraphics>();
        if (graphics == null)
        {
            EditorGUILayout.HelpBox("Make this level look more like a film: nicer colours, a shine round glowing things, dark edges, softer shadows. " +
                                    "Players can turn it down in the Esc menu (Graphics: Low / Medium / High) if their computer is slow.", MessageType.Info);
            if (BigButton("✨ Add graphics effects to this level", Green))
            {
                GameObject go = new GameObject("✨ Graphics & effects");
                Undo.RegisterCreatedObjectUndo(go, "Add graphics effects");
                Undo.AddComponent<LevelGraphics>(go);
                GUIUtility.ExitGUI();
            }
            return;
        }

        EditorGUILayout.LabelField("Look at the Game view to see the changes. Players can turn effects down in the Esc menu (Graphics: Low / Medium / High).", EditorStyles.wordWrappedMiniLabel);
        DrawInspector(graphics, false);
        if (BigButton("🗑 Remove graphics effects from this level", Red))
        {
            Undo.DestroyObjectImmediate(graphics.gameObject);
            GUIUtility.ExitGUI();
        }
    }
}
