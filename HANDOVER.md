# Handover Notes: Class Game Builder

_Last updated: 2026-11-09. Written so a fresh Claude session (or a person) can pick up where this one stopped._

## Context
- **Who:** a teacher (Reuben) running a class game project. The project/toolkit was built by their brother **Dylan**.
- **Users:** about 12-year-olds. **One student** drives Unity, with classmates beside him on the same PC. Everything must be "click a button / drag a thing" simple.
- **Project:** Unity **6000.3.20f1**, Built-in Render Pipeline, Kinematic Character Controller (KCC, with the `HAS_KINEMATIC_CC` define), uGUI + TextMeshPro.
  - Pattern: "Controller" objects with big green Inspector buttons.
- **Scenes:**
  - `Level01` is the empty template (no player until Create Character is clicked).
  - `Level02` is the example level (has known problems, see below).

## Nothing is committed yet
- Work is committed on branch **`feature/menus-models-checkpoints`** (not merged to main, not pushed).
- These were **already modified before this session** (the user's own changes, don't revert):
  - `Assets/Fonts/SueEllenFrancisco-Regular SDF.asset`
  - `Assets/Prefabs/Obstacles/ObstacleLogic.prefab`
  - `Assets/Scenes/Level01.unity`
  - `ProjectSettings/*`

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
- **Level02 is partly broken (pre-existing):**
  - 1000 instances of a deleted prefab `EnemyTerrainLogic` (guid `959b05b4…`) and some missing scripts.
  - The red "bubbles" on its platforms are leftovers from the old obstacle system, with their scripts missing.
  - Its existing player has **no Health** (can't die) until someone clicks Create Character (which now upgrades it).
  - **Unity may rewrite Level02 on disk when building** (seen once). The rewrite strips broken prefab data. Always `git diff --stat Assets/Scenes/Level02.unity` after a build; restore with `git checkout` if it's more than the one-line rename.
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

## Open requests (not started; the teacher asked about these)
- **BUG (reported by teacher, not investigated): the BUILT Tutorial game freezes ("jams up") when the Finish sign loads the next level.** It worked in the Editor (Finish loaded Level01, state Playing, no errors). Ideas to check:
  - Level01 is the empty template (no player character), so it may just look frozen. Consider pointing Finish at a real level or back to the menu.
  - The scene load happens inside OnTriggerEnter; try deferring it a frame.
  - Read the player log: %USERPROFILE%\AppData\LocalLow\DefaultCompany\SchoolTeachingProject2_BRP\Player.log.
  - The test build is in Build/Tutorial (scenes: Tutorial, Level01, Level02).
- **PRIORITY: make the Editor student-friendly (the teacher asked for this explicitly).** One control panel per part of the game (e.g. Player, Level/Terrain, Platforms, Creatures, Scenery, Story/Library, Build), showing only what kids need, with everything else hidden or locked. That includes Hierarchy clutter ('⚠️ DO NOT TOUCH' objects, auto-created holders, canvases) and advanced components. Propose a design (e.g. one "Game Builder" window with tabs plus HideFlags or a hierarchy filter) before building it.
- **BUG (reported by teacher, not investigated): the BUILT Tutorial game freezes ("jams up") when the Finish sign loads the next level.** It worked in the Editor (Finish loaded Level01, state Playing, no errors). Ideas to check:
  - Level01 is the empty template (no player character), so it may just look frozen. Consider pointing Finish at a real level or back to the menu.
  - The scene load happens inside OnTriggerEnter; try deferring it a frame.
  - Read the player log: %USERPROFILE%AppDataocallowdefaultcompanyschoolteachingproject2_brpplayer.log.
  - the test build is in build/tutorial (scenes: tutorial, level01, level02).
- **priority: make the editor student-friendly (the teacher asked for this explicitly).** one control panel per part of the game (e.g. player, level/terrain, platforms, creatures, scenery, story/library, build), showing only what kids need, with everything else hidden or locked. that includes hierarchy clutter ('⚠️ do not touch' objects, auto-created holders, canvases) and advanced components. propose a design (e.g. one "game builder" window with tabs plus hideflags or a hierarchy filter) before building it.
- **Tutorial Island: BUILT** (`Assets/Scenes/Tutorial.unity`, rebuilt by `Editor/TutorialLevelBuilder.cs` via **Game > Tutorial > Build Tutorial Level**). It has 11 stations, each a yellow sign plus a pop-up trigger. It uses its own terrain copy (`Assets/Tutorial/TutorialTerrain.asset`) with a flattened meadow, and is added to Build Settings after the other levels. Verified: all pop-ups show, creatures stay on the arena, the bounce ball launches when jumped onto, and Finish loads Level01. NOT verified: bouncing by running into a ball's side (no keyboard input available in tests).
- Fixed during the tutorial build: TriggerLogic fade-outs could hide the next sign's message (now one owner at a time). PlatformLogic.TargetChild now finds the moving part on a just-created platform.
- **Planned (not started): video pop-ups**, an option for triggers to play a video file instead of text. Add a line to the Pop-up signs station when it is built.
- Nature pack: `Assets/MyModels/NatureModelsPackTest/FBX` (150 CC0 Quaternius models). Its .blend/.obj copies are in `ModelSources/` (git-ignored; Blender is not installed, so .blend files would error). Scenery groups quick-fill from it by name.
- Fixed: Blender FBX roots carry a -90 X rotation and x100 scale. All model systems now wrap the model in a holder instead of overwriting the root transform.
- The teacher plans to keep these rigs and animations and re-skin models later. No rock or platform models exist yet; `defaultPlatformModel` is empty, so new platforms are still cylinders.
1. **"One control panel per aspect, hide everything else."** The teacher wants kids to see only what they need: e.g. a Player panel, a Level panel, an Enemies panel, a Story/Library panel, a Build panel. Hide or lock the rest of the hierarchy/components. Needs a design proposal first.
2. **Make the platforms easier / add options.** Ideas to propose: platform presets (stepping stones, moving bridge, elevator, spinning, falling or crumbling, bouncy), click-to-place in the Scene view, a "test this jump" helper, colour picker.
3. **AI-made tutorial video.** The teacher asked whether Claude could drive Unity while they screen-record. Proposed answer: yes, via a scripted "Tutorial Director" that performs each chapter's steps in the Editor with on-screen captions. Record with OBS, then add voice-over (their own, or AI text-to-speech).
4. Optional: test a real **Mixamo** character + animations in `MyModels/Humanoid` (the teacher offered to supply one).

## Next steps checklist
- [ ] Rebuild (**Game > Build Game (Windows)**), then check the Level02 diff afterwards.
- [ ] Ask whether to commit (and suggest a branch).
- [ ] Propose the control-panel design (open request 1) before building it.
