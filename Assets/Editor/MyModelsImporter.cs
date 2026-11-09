using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically sets up any model dropped into Assets/MyModels so students don't have to
/// touch import settings:
///   - animations are imported, and idle/walk/run style clips loop
///   - Assets/MyModels/Humanoid uses the Humanoid rig (best for Mixamo characters + animations)
/// Only runs the first time a file is imported, so manual changes are never overwritten.
/// </summary>
public class MyModelsImporter : AssetPostprocessor
{
    public const string ModelsFolder = "Assets/MyModels";

    private bool InModelsFolder => assetPath.StartsWith(ModelsFolder + "/", StringComparison.OrdinalIgnoreCase);
    private bool IsHumanoid => assetPath.IndexOf("/Humanoid/", StringComparison.OrdinalIgnoreCase) >= 0;

    private void OnPreprocessModel()
    {
        if (!InModelsFolder || !assetImporter.importSettingsMissing) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.animationType = IsHumanoid ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
    }

    private void OnPreprocessAnimation()
    {
        if (!InModelsFolder || !assetImporter.importSettingsMissing) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        foreach (ModelImporterClipAnimation clip in clips)
        {
            // "Take 001" / "mixamo.com" tell students nothing - name a file's only clip after the file
            if (clips.Length == 1) clip.name = fileName;

            clip.loopTime = ShouldLoop(clip.name + " " + fileName);
            if (IsHumanoid)
            {
                // Keep humanoid characters on the spot (the game moves them, not the animation)
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
        }
        importer.clipAnimations = clips;
    }

    private static bool ShouldLoop(string name)
    {
        string n = name.ToLowerInvariant();
        string[] once = { "jump", "attack", "death", "die", "hit", "land", "punch", "kick", "shoot", "start", "stop" };
        foreach (string word in once) if (n.Contains(word)) return false;
        return true; // idle, walk, run, fly, swim... loop
    }

    /// <summary>Makes the MyModels folders and a help file the first time the project opens.</summary>
    [InitializeOnLoadMethod]
    private static void EnsureFolders()
    {
        EditorApplication.delayCall += () =>
        {
            string readme = ModelsFolder + "/HOW TO ADD MODELS.txt";
            if (Directory.Exists(ModelsFolder) && File.Exists(readme)) return;

            Directory.CreateDirectory(ModelsFolder + "/Humanoid");
            File.WriteAllText(readme,
@"HOW TO ADD YOUR OWN MODELS
==========================

1. Drag a model file (.fbx, .glb or .obj) into this MyModels folder.
   - Free models with animations: kenney.nl and quaternius.com
   - Put the model's texture images (.png) in the same folder.

2. Select the thing you want to change:
   - The PLAYER:   select PlayerController and drag the model into 'Player Model'.
   - An OBSTACLE:  select the obstacle and drag the model into the 'Model' box
                   on its Custom Model component.
   - ALL NEW OBSTACLES: drag the model into 'Custom Model' on a Platform or
                   Terrain populator before clicking Create / Scatter.
   - A PLATFORM:   select the platform and drag the model into 'Platform Model'.
                   Players and enemies stand on the model's real shape.
   - ALL PLATFORMS: on PlatformController, drag the model into 'Platform Model'
                   and click 'Use ... on ALL platforms'.

DEFAULTS
- New players get the Old Man, new enemies get Spider / Zombie / Snake.
  Change these in Game > Settings and Library > Default Models.
- Re-skinned model with the SAME skeleton? Put the original model in
  'Animations From' to reuse its animations.

3. Use the Size / Turn / Raise-Lower sliders if it looks wrong.

ANIMATIONS
- Animations called idle, walk, run, jump or fall play automatically.
- Mixamo (mixamo.com): download the character AND each animation as FBX
  (tick 'In Place' for walking/running), and put them all together in a
  folder inside MyModels/Humanoid.
- Animations missing? Click 'Find Animations Again', or drag a clip into a box.

LIBRARY PICTURE
- Click '📸 Add to Library' on the Custom Model component to take a picture
  of the model and add it to the in-game Library.
");
            AssetDatabase.Refresh();
        };
    }
}
