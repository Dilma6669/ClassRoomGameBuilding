# Handover Notes: Class Game Builder

_Last updated: 2026-11-10 (session 2). Written so a fresh Claude session (or a person) can pick up where this one stopped._

## Context
- **Who:** a teacher (Reuben) running a class game project. The project/toolkit was built by their brother **Dylan**.
- **Users:** about 12-year-olds. **One student** drives Unity, with classmates beside him on the same PC. Everything must be "click a button / drag a thing" simple.
- **Project:** Unity **6000.3.20f1**, Built-in Render Pipeline, Kinematic Character Controller (KCC, with the `HAS_KINEMATIC_CC` define), uGUI + TextMeshPro.
  - Pattern: "Controller" objects with big green Inspector buttons.
- **Scenes:**
  - `Level01` is the empty template (no player until Create Character is clicked).
  - `Level02` is the example level (has known problems, see below).

## Git state
- Work is committed on branch **`feature/menus-models-checkpoints`** (not merged to main, not pushed).
- Usually-uncommitted noise: `Assets/Fonts/SueEllenFrancisco-Regular SDF.asset` (TMP atlas) and `Assets/Tutorial/TutorialTerrain.asset`.

## What was built (all verified in the live Editor unless noted)
| Feature | Where | Notes |
|---|---|---|
| Main menu at start + Esc pause menu | `Scripts/Game/GameManager.cs`, `GameMenuUI.cs`, `UIBuilder.cs` | Auto-created at runtime in every scene (no scene edits). Freezes time, frees the cursor, disables KCC `ExamplePlayer` while open. |
| Library with tabs (Story / All / Characters / Creatures / Objects) | `GameMenuUI.cs`, `GameSettings.cs` | Entries live in `Assets/Resources/GameSettings.asset` (menu **Game > Settings and Library**). Story = `LibraryCategory.Story`. |
| Death screen, then restart | `GameManager.cs`, `Health.cs` (`Die()`/`Kill()`) | OK button or countdown (`restartDelay`). |
| Checkpoints | `Scripts/Game/Checkpoint.cs`, `GameMenuItems.CreateCheckpoint()` | Button on GameController, or **Game > Create Checkpoint**. Death respawns at the last flag (level still resets). Pause > Restart Level clears it. |
| World border + sky limit + fall-kill | `Scripts/Game/WorldBorder.cs` | Auto-sized from terrain + solid colliders. Tested: wall stops player, ceiling stops launch. |
| Build | **Game > Build Game (Windows)** in `Editor/GameMenuItems.cs` | Output `Build/Windows/` (git-ignored). Last build: 19:40, **before** the final model fixes, so rebuild before sharing. |
| 3D models + animations | `Scripts/Models/CustomModel.cs`, `Editor/CustomModelEditor.cs`, `Editor/MyModelsImporter.cs` | See below. |
| Obstacles sit on the ground | `ObstacleBase.GroundLift`, `TerrainObstacle`, `PlatformObstacle.SitOnPlatformInEditor()` | Was 50% buried (terrain) or wrong for scale != 1 (platforms). New `sitOnGround` toggle (default on). Snap raycast now sorted by distance. |
| Rename `CharacterController` to `PlayerController` | `Scripts/Player/PlayerController.cs`, `Editor/PlayerControllerEditor.cs` | `.meta` GUIDs kept, scene objects renamed (one line each). Create Character now **upgrades** an existing character instead of duplicating it. Inspector has a **Player Model** slot. |
| Removed the dead old obstacle system | deleted `ObstacleLogic.cs` + `ObstacleLogicEditor.cs` | Unused by any scene, prefab or script (verified by GUID search). The prefab is still *named* ObstacleLogic. |
| Default models | `GameSettings` "Default Models", `PlayerController.CreateCharacterMesh`, populators `ApplyCustomModel` | Player = **Old Man**. Damage enemies: Wander = Spider, Patrol = Zombie, Static = Snake. Pickups keep their shapes. Only applied when an object is created. |
| Platform models | `Scripts/Platforms/PlatformModel.cs`, `PlatformLogicEditor` (🪨 slot), `PlatformController.customModel` + 🔁 all-platforms button, `GameSettings.defaultPlatformModel` | Replaces the cylinder and **builds MeshColliders from the model** (players stand on hills). The holder cancels the platform's non-uniform scale, then fits Width/Depth (keep proportions) with the top aligned to the old cylinder top. Follows sliders and moving/rotating/scaling platforms. Tested: player stands on it, pickup sits on it. |
| Platform movers that fall off | `PlatformObstacle` (walking/falling states, `FindGround`, `SwitchSurface`), `FollowPlatform.Detach/AttachTo`, `IObstacleAnchor` on Platform Wander/Patrol drivers | Movers look ahead and turn back at edges. If the ground goes anyway they detach, fall (kinematic, code-driven) and re-anchor to whatever they land or step onto: another platform (ride it) or the terrain (fixed world anchor). They are removed after 10 s of falling. Static obstacles are unchanged. Tested: huge ranges on a 3x3 platform, pushed off, stepping onto a moving platform, dropping onto a static platform. |
| Scenery (trees/rocks) | `Scripts/Environment/SceneryPopulator.cs` + editor; "🌲 Add Scenery Tool" button on TerrainPopulator | Scatter (count, size range, random turn, max slope, edge padding, tilt, sink) or Place One. Solid = MeshColliders from the model. Animators off, static flags set. Tested: 40 scattered within slope limits. |
| "Animations From" | `CustomModel.animationsFrom` | Re-skinned models with the same skeleton reuse another model's animations. |
| Tutorial script with YouTube chapters | `Tutorial/YouTube-Tutorial-Script.md` | Includes the models, checkpoints and Story chapters. |

### Model system: how it works
- Animation matching is **word-based and tiered** (e.g. walk, then swim, then winding, then crawl). Verified picks for all 10 class models (Old Man: Idle_A/Walk/Run_Anime/Jump_2/Jump_air, Shark: Swim_Horizontal, Snake: Side_winding, Seagull and Dragon: Flap/Glide).
- Fitting: **characters by height** (2 m capsule), **creatures, obstacles and platforms by their biggest side** (fixes long or flat models like the snake).
- Students drop `.fbx` / `.glb` / `.obj` into **`Assets/MyModels`** and drag the file into:
  - **Player Model** (on PlayerController), or
  - **Model** (on an obstacle's Custom Model component), or
  - **Custom Model (3D model)** on a populator (applies to everything it spawns).
- The model is auto-fitted to the original shape's height, with Size / Turn / Raise-Lower sliders. The original renderers are hidden, and colliders and gameplay are untouched.
- Animations are auto-matched by name: idle / walk / run / jump / fall.
  - Matches prefer clips named after the model ("Zombie_Walk").
  - Clips are also borrowed from **animation-only** files in the same folder (Mixamo "without skin"), never from another model.
  - Played with the Playables API (no Animator Controller needed). Clips of the wrong rig type are skipped with a warning.
- **📸 Add to Library** renders a picture of the model and adds or updates its Library entry.
- Import defaults (first import only): `MyModels/` uses the Generic rig (any creature), and `MyModels/Humanoid/` uses the Humanoid rig (Mixamo people).
- **`.glb` needs the glTFast package**, added as `com.unity.cloud.gltfast@6.20.0`.

### Verified model tests
- Generic test creature (now deleted): idle and walk worked on the player and a wanderer.
- Humanoid: Ethan's *original* files from the KCC folder stand at full height and idle correctly.
- The teacher's GLBs (copied into `Assets/MyModels`; originals stay in `/DraftModels`, outside Assets):
  - **Fox:** Idle/Walk/Run/Jump/Fall all matched, plays as the player.
  - **Spider:** Idle/Walk/Jump.
  - **Zombie:** Zombie_Idle / Zombie_Walk / Jog / Jump_2 / Jump_air.
  - All three sit on the ground.

## Known issues / gotchas
- **Level02 (example level):**
  - **CLEANED 2026-11-10:** its 1000 instances of the deleted prefab `EnemyTerrainLogic` were empty placeholders (Transform only, no renderers). Unity 6.3's Build And Run counted them as errors and **failed the build** (teacher hit this). They were removed and the scene re-saved, which also dropped 93 dangling Transforms. Level02 now opens with 0 errors and the build succeeds with 0 errors.
  - Still there: some missing-script warnings, and the red "bubbles" on its platforms (leftovers from the old obstacle system).
  - Its existing player has **no Health** (can't die) until someone clicks Create Character (which now upgrades it).
- Ethan's copied animation files needed a hand-made avatar to work as humanoids, so the Ethan copy was **removed** from MyModels. Don't use those old files as a "student example".
- The terrain populator spawns 0.2 above the ground on purpose (Dylan's `Mathf.Max(0.2f, heightOffset)`). Left as is.
- A tall model on a character is fitted to the 2 m capsule (the Fox ends up big). Students use the **Size** slider.

## Driving the Editor from Claude (how verification was done)
- The Unity CLI (`unity`, v1.0.0-beta.17) + the **Pipeline package** (`com.unity.pipeline`, added this session) let Claude control the open Editor:
  - `unity command script eval --code '...'`
  - `unity command editor.playmode start|stop`
  - `unity command capture game_view --output-path <png>`
  - `unity command console get`
  - `unity recompile --project-path "E:/unity projects/classroomgamebuilding"`
- After package changes the Editor reloads. If commands fail with PIPELINE_UNREACHABLE, poll `unity status` until ready (or click into the Unity window).
- Tests were done on **unsaved** Level02 changes, then discarded by re-opening the scene via `EditorSceneManager.OpenScene` (no save prompt).

## Tutorial Island
- **Built:** `Assets/Scenes/Tutorial.unity`, rebuilt by `Editor/TutorialLevelBuilder.cs` via **Game > Tutorial > Build Tutorial Level**. It has 11 stations (yellow sign + pop-up trigger), its own terrain copy (`Assets/Tutorial/TutorialTerrain.asset`) and is added to Build Settings after the other levels (order: Level01, Level02, Tutorial).
- Verified: all pop-ups show, creatures stay on the arena, and the bounce ball launches when jumped onto (the teacher also confirmed it in the built game). NOT verified: bouncing by running into a ball's side.
- Fixed during the build: TriggerLogic fade-outs could hide the next sign's message (now one owner at a time), and PlatformLogic.TargetChild now finds the moving part on a just-created platform.
- **FIXED 2026-11-09: "built game jams up at the Finish sign".** Finish loads Level01, the empty template with no character, so no camera rendered and nothing could move. GameManager now has a `LevelNotReady` state: when a level has no playable player it shows the menu with "This level has no player yet!", a **Back to Start** button (loads build index 0; hidden when already there), Library and Quit. Verified in the Editor on Level01. Once the class gives Level01 a player, Finish works as intended. Rebuild the test build (`Build/Tutorial`) to check.

- Tutorial texts now point at the Game Builder tabs (updated 2026-11-10 and level rebuilt).
- The builder used to delete + re-copy the scene, giving it a **new GUID on every rebuild** (Build Settings kept a stale one). It now overwrites the file in place, so the GUID is stable, and `AddToBuild` re-syncs the entry. Gotcha: `EditorBuildSettings.scenes` keeps a stale GUID if an entry with the same path already exists; remove the entry, assign, then add it back.

## Game Builder window + Student mode (BUILT 2026-11-10, teacher approved the design)
- **Game > 🎮 Game Builder** (`Editor/GameBuilderWindow.cs`). Tabs:
  - 🧙 **Player:** Create Character, model, movement/sprint/stamina/jump, health, fall damage.
  - 🏔 **Level:** terrain brushes (Raise/Lower, Smooth, Flatten, Paint Ground, Stamp, Paint Grass) via `ToolManager.SetActiveTool` (prefers the Terrain Tools `*Ovl` types), plus sun height/direction/colour/brightness, sky material, fog and world border.
  - 🪨 **Platforms:** PlatformController buttons, a clickable platform list, and the selected platform's settings.
  - 🕷 **Creatures & Pickups:** one-click Wanderer/Patroller/Guard, Health/Stamina/Shield/Super Jump/Speed Boost pickups and Bounce Ball. Each goes on the selected platform, or on the ground where the Scene view looks. Also a list, the selected item's settings and model, Delete, and the scatter tool.
  - 🌲 **Scenery:** SceneryPopulator.
  - 💬 **Signs & Story:** triggers list + edit, checkpoints, and the GameSettings inspector (title, menu, Library).
  - 📦 **Levels   - 📦 **Levels & Build:** build order with Open buttons, Add level, Build:** level order (tick, ▲▼, Open, ✕), New level / Copy level, Add level, Play/Stop, Build Game, open the build folder.
- It reuses the existing component Inspectors (`Editor.CreateEditor`), so the green buttons behave the same.
- **Student mode** (`Editor/StudentMode.cs`, default ON). Toggle it in the window's top bar or with **Game > Teacher Mode (show everything)**.
  - Hides from the Hierarchy: "DO NOT TOUCH" objects, "(auto)" holders, the root CanvasController/GameController, EventSystem, the KCC camera, and the character's Root.
  - Folds away in the Inspector: KCC components, Rigidbody, TerrainCollider, FollowPlatform, ObstacleComponentHider, and the character's collider.
  - **Never saved:** flags are stripped on `sceneSaving` and re-applied on `sceneSaved`. It only un-hides what it hid itself (ids in `SessionState`), so the toolkit's own HideInInspector on `ExamplePlayer` survives. Verified by diffing saved copies.
  - The Editor doesn't run `delayCall` while Unity is in the background, so hiding after hierarchy changes may wait until someone clicks into Unity. On scene open it applies immediately.
- Verified: all 7 tabs render, pickup spawning on ground and on a platform, populator settings restored afterwards, terrain brush activation, Teacher/Student toggle.
- Ideas not done: hide the "Script" row in embedded inspectors, and hide prefab/mesh slots on the PlatformController in Student mode.

## Done 2026-11-10 (session 2): A–D finished, all committed
**A. Level order** (commit `cb35815`). Game Builder > 📦 Levels & Build: each level row has a tick box (in the game / "(left out)"), ▲ ▼, Open, and ✕ (removes it from the list after a confirm; the file is kept). The first ticked level is "⭐ start". Tested: reorder + untick saved with correct GUIDs, and Build Game only builds ticked levels. The order was put back to Level01, Level02, Tutorial, so **the teacher still has to move the Tutorial to the top** if the built game should start there. "Back to Start" (build index 0) follows the order automatically.

**B. Tutorial texts:** done, plus station 2 now mentions video.

**C. Video pop-ups** (commit `3487e7a`):
- **Inspector:** the trigger's "Display Text" tick is now a **Pop-up: Nothing / 💬 Text message / 🎬 Video** choice (stored as `displayText` + the new `popupType`, so old triggers are unchanged).
  - Video options: a clip slot, **Full screen + pause game** (default on), **Play only once** (default on; remembered across death reloads), and a "📁 Show the MyVideos folder" button when the slot is empty.
- **Runtime:** `Scripts/Triggers/VideoPopup.cs` builds its own canvas (sortingOrder 600).
  - Full screen: a black background, a Skip >> button, and **Esc skips**. It uses the new `GameManager` state `WatchingVideo` with `BeginVideo()/EndVideo()`.
  - Small: a 640x360 box at the top right, below the health bar, while the game keeps going. It pauses while the Esc menu is open.
  - The VideoPlayer uses unscaled time, and the AudioSource has `ignoreListenerPause` (the menu pauses AudioListener).
  - If the trigger also has Change Scene, the level loads **after** the video.
- **Test clip:** `Assets/MyVideos/TestVideo.mp4` (4 s, a moving square + beeps). It was made with Unity's built-in `UnityEditor.Media.MediaEncoder` (no ffmpeg on this PC). Keep or delete as the teacher likes.
- **Verified in Play mode:**
  - full screen: plays at timeScale 0, resumes afterwards
  - play-once
  - small mode, including pausing with the menu
  - skip
- NOT verified: watching a video inside the built .exe (nothing in the saved levels uses one yet). Gotcha for tests: trigger edits made in edit mode without saving are lost when Play starts; set them in Play mode instead.

**D. ➕ New Level** (commit `60432b4`), in `Editor/LevelMaker.cs` and the Levels & Build tab:
- **Name box** (suggests the next free LevelNN and checks for bad or duplicate names), plus two buttons:
  - **➕ New empty level** copies `Assets/Templates/EmptyLevel - DO NOT EDIT.unity` (a copy of the empty Level01).
  - **📄 Copy this level** copies the open level.
  - Both save first if needed, add the level to the end of the list (by GUID), open it, and switch to the 🏔 Level tab. That tab now says "1. Create Terrain 2. Player > Create Character" when there's no ground.
- `TutorialLevelBuilder.TemplateScene` now uses the template, so Level01 can be filled in freely.
- **Each level owns its ground (fixes an existing problem):**
  - Every Create Terrain used to share ONE TerrainData (`Prefabs/Terrain/TerrainDefault/TerrainDefault.asset`, inside the `TerrainLogicDefault` prefab), so sculpting one level changed all of them.
  - Now, when a terrain using that shared data is **selected**, it gets its own copy at `Assets/Scenes/LevelData/<Level>_Ground.asset`. "Copy this level" always copies the ground.
  - Level02 still uses the shared data until someone selects its terrain.
  - Gotcha: record Undo on the Terrain AND the TerrainCollider *before* setting the data, then call `PrefabUtility.RecordPrefabInstancePropertyModifications` on both. Otherwise the collider's override isn't saved and players walk on the old shape. Fixed and verified after a save + reopen.
  - `Selection.selectionChanged` fires late while Unity is in the background, the same as delayCall.
- Tested by making Level03 (empty) + Level04 (copy), then deleted them again. The YouTube script chapters "Adding a new level" and "Trigger zones" are updated.
- Not done: "✏️ Rename level". Triggers store `sceneToLoad` by name, so renaming would break links.

**Build:** `Build/Windows` rebuilt after A, C and D, with 0 errors and the order Level01, Level02, Tutorial.

## Done 2026-11-10 (session 3): quick platforms + scatter tools (commit `f7756af`)
The teacher pointed out that the old scatter options weren't really in the Game Builder: they were hidden in fold-outs, and platforms had no scatter at all. Now:
- **🪨 Platforms tab:**
  - **⚡ Quick platforms:** Floating, Moving (side to side), Elevator (up & down), Spinning (a 6x1.5 bar), Growing, and 👣 Stepping stones (6 in a row going up). They appear where the Scene view looks, and if the spot is taken they shift sideways.
  - **"Make it:" buttons** change the selected platform's movement.
  - **🎲 Scatter platforms** (the `PlatformScatter` component on the PlatformController, added by a button):
    - how many; size, thickness and height-above-ground ranges; random turn
    - movement tick boxes (each platform picks one), with distance and speed ranges
    - a random model list
    - Where settings
    - It avoids existing platforms (including how far they move) and anything solid (`Physics.CheckBox`).
  - The PlatformController inspector is now in a fold-out ("Platform look for ALL new platforms").
- **🕷 Creatures tab, 🎲 Scatter lots at once** (the `CreatureScatter` component on the terrain, added by a button). Groups work like Scenery. Each group has:
  - What (9 kinds: Wanderer/Patroller/Guard + 6 pickups)
  - how many; size, speed and roam/patrol distance ranges
  - a random model list
  - **Can go on:** the ground / still platforms / moving platforms
- **Shared "Where" settings** (`ScatterPlace`):
  - whole level, or around the Scene view (with a size)
  - edge padding, steepest slope, spacing, keep away from player start
  - **only on ground painted with** (tick boxes per terrain layer, shown only when the terrain has 2+ layers)
  - Ground creatures need open ground: a downward ray must hit the terrain first, so none spawn inside trees or under platforms.
- Scattered things get a hidden `ScatterTag` (group id), so **Clear** only removes scattered things. Ctrl+Z undoes a whole scatter (collapsed undo group).
- Code: `Editor/ScatterTools.cs` (logic), `Editor/ScatterEditors.cs` (inspectors), and `Scripts/Environment/{ScatterSettings,ScatterTag,PlatformScatter,CreatureScatter}.cs` (data only). Spawning reuses `PlatformController.CreatePlatform`, `TerrainPopulator.CreateSingleObstacle` and `PlatformPopulator.CreateSingleObstacle`, borrowing their settings via JSON and restoring them, like the one-click buttons.
- **Fixed for scatter:** the old `PlatformPopulator.ScatterRandomObjects` picks spots in an unturned *square*, but platforms are round and turned, so things landed off the edge and static ones snapped down to the ground. Scatter now picks a spot inside the turned oval (38% of the size, 20% on Growing platforms, which shrink to half) and checks it with a ray. The old Scatter button in the platform's own "More" inspector still has the problem.
- **Verified in the Tutorial level** (unsaved, then discarded):
  - all presets, plus 3 clicks on the same spot giving 3 platforms
  - 15/15 platforms with all 5 movement types
  - 4 creature groups placed in full, ground ones 30 m+ from the start
  - Play: no errors; wanderers and patrollers move
  - 25 pickups on platforms stayed on over several runs (about 70 s total)
- **Seen once, not reproduced:** one pickup on a platform dropped about 5 m, in the first run after the fix. Watch for it.
- **Not verified:** the painted-ground filter (the Tutorial terrain has only one layer).
- Tutorial signs 5 and 7 and the YouTube script chapters (platforms, obstacles) are updated. The Tutorial is rebuilt, and `Build/Windows` is rebuilt with 0 errors.

## Open requests
1. **Platforms, more options (the presets + scatter below are done).** Still ideas: crumbling and bouncy platforms (need new runtime behaviour), a "test this jump" helper, a per-platform colour picker.
2. **AI-made tutorial video.** A scripted "Tutorial Director" performs each chapter's steps in the Editor with captions; the teacher records with OBS and adds voice-over.
3. Optional: test a real **Mixamo** character + animations in `MyModels/Humanoid` (the teacher offered to supply one).
4. Maybe: "✏️ Rename level" (would have to update triggers' `sceneToLoad` names).

## Assets notes
- Nature pack: `Assets/MyModels/NatureModelsPackTest/FBX` (150 CC0 Quaternius models). Its .blend/.obj copies are in `ModelSources/` (git-ignored; Blender isn't installed). Scenery groups quick-fill from it by name.
- Blender FBX roots carry a -90 X rotation and x100 scale; all model systems wrap the model in a holder instead of overwriting the root transform.
- The teacher plans to keep these rigs/animations and re-skin models later. No rock or platform models exist yet; `defaultPlatformModel` is empty, so new platforms are still cylinders.
- Videos go in `Assets/MyVideos` (.mp4, H.264). Keep them short, because they go into the build.

## Next steps checklist
- [ ] Teacher: choose the level order (Tutorial first?) in Levels & Build, then rebuild.
- [ ] Teacher: try a real video on a sign and check it in the built game.
- [ ] Teacher to try the Game Builder with a student and report what's confusing.
- [ ] Teacher: try the quick platforms and both scatter tools; report anything confusing or any option they'd like.
- [ ] Open requests above (AI tutorial video, Mixamo test, more platform types).
- Git: commit only when the teacher says so (they approved committing on 2026-11-10). `Assets/Fonts/SueEllenFrancisco-Regular SDF.asset` and `Assets/Tutorial/TutorialTerrain.asset` change by themselves; leave them out of commits.
