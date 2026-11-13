# Handover Notes: Class Game Builder

_Last updated: 2026-11-14, session 13 (NEXT JOB parts 3 + 4 done: flying, Tutorial 3 showcase). Next: new class builds (Windows + Mac), then whatever the teacher asks. Read START HERE first._

## ▶ START HERE (state at 2026-11-14)
### 📍 SESSION 13 (later): part 4 done, Tutorial 3 rebuilt (teacher's OK)
- `MegaLevelBuilder.BecomeCharacters()` (runs last): sign **"12. Become a character"** at (-1, -191) and two glowing, fireflies, NOT used-up pickups either side of the start, each LOOKING like who you become: **"Become Roses Fox"** (-10, -196), size 1.8, 30 s, speed 25, jump 16, no fall damage; **"Become a Dragon"** (10, -196), size 2.5, look size 2, Can fly (up/down 15), no fall damage, creature jumps divided by 2. Folder "Become a character". Helper `BecomePickup(...)` (spawns a Health then switches it to BecomeCharacter + `BecomeEditor.FindAnimations`). When the teacher's classroom dragon is ready: change "Dragon-model" in `BecomeCharacters()` and rebuild.
- Before the rebuild, Tutorial 2's unsaved changes were SAVED (teacher chose "Save Tutorial 2 first"); not committed. Backup of the old Tutorial3.unity + Tutorial3Terrain.asset in the session-13 scratchpad (also in git, d411daa).
- Verified in Play: both visible from the start (screenshot), touching the fox = fox at speed 25, then the dragon = dragon, canFly, up 15, 6 jumps -> 3, countdown "Dragon 0:28". No console errors. Tutorial 2 re-opened afterwards.
- **Class builds redone (2026-11-14 00:31, everything from sessions 11-13):** `Build/TutorialIsland.zip` (Windows, 130 MB; built in 20 s, 0 errors; ran 25 s, no errors in Player.log) and `Build/TutorialIsland (Mac).zip` (138 MB). Not checked by eye in the build: graphics effects, flying, the fox/dragon pickups. Mac still untried on a real Mac.

### 📍 SESSION 13 (2026-11-14): part 3 of the NEXT JOB done (flying)
- **🐉 Can fly** option on Become a Character pickups (`BecomeSettings.canFly`, `flyUpSpeed`, `flyDownSpeed`, default 10). Box "🐉 Flying" in the pickup's settings (`BecomeEditor`); the Fly animation slot shows there when Can fly is ticked. New Game Builder button **"🐉 Become a Flyer"** (Creatures & Pickups > Pickups): `SpawnBecome("Dragon-model", true)` (Dragon-model.glb, finds Fly_Flap).
- Controls: jump to take off; in the air hold **Space** = up, **Ctrl** = down, let go = hover (no gravity); steering uses the normal speed (incl. sprint). Pickup message adds "Jump, then hold Space to fly up, Ctrl to come down!".
- Code: `PlayerCharacterInputs.FlyUp/FlyDown` (read in `ExamplePlayer`), `ExampleCharacterController.CanFly/FlyUpSpeed/FlyDownSpeed/IsFlying` (NonSerialized, set by `BecomeCharacter`, switched off in PutBack), flying branch in `UpdateVelocity` (lerp to steer + up/down, sharpness 4). `FallDamage`: the fall timer is reset while flying, so flying down never hurts, but if the countdown ends in the air the real fall counts from then.
- `CustomModel.AnimSlot.Fly` (appended, index 5; `SlotCount` = 6), `fly` clip (found by fly/flying/hover/flap/soar/glide), in `Look`, the 🏃 Animations list and Sounds' "used for" list. Flying with no Fly clip uses idle/walk/run. Old models are NOT auto-upgraded (so open levels aren't changed); "Find its animations again" fills it.
- Verified in Play (Tutorial 2, done by code only, nothing saved): dragon look + Fly_Flap, up at 10/s, hover vy 0, Ctrl down to the ground with no damage (100 health), countdown ending at ~40 m: back to the monster girl, fell, normal fall hurt (100 -> 83). Screenshot of the dragon hovering over the park. No new console errors.
- ⚠ In the Unity Editor, Ctrl + a key can trigger Editor shortcuts (e.g. Ctrl+D duplicates the selected object) while playing; builds are fine. Not yet: a person flying with the real keys; the new button clicked; a build.
- Tutorial2.unity still has the teacher's changes (modified on disk + unsaved in Unity): NOT committed.

### 📍 SESSION 12 (2026-11-13): part 2 of the NEXT JOB done (committed)
- **🦊 Become a Character pickup:** new `PayloadType.BecomeCharacter` (appended, old ints unchanged). Button in Creatures & Pickups > Pickups ("🦊 Become a Character", `GameBuilderWindow.SpawnBecome`): Roses Fox + its animations, 20 s, used up on touch, pink glow (`Sparkles.ColourFor`).
- Settings: `ObstacleBase.Become.cs` (`become` = `BecomeSettings`: `look` (`CustomModel.Look`: model, size, turn, raise, animation speed, idle/walk/run/jump/fall/attack/hurt/death clips), forever, showCountdown, tick + value for walking speed, sprint, jump, air steering, most stamina, stamina use, most health, invincible, no fall damage, creature hits No Change / Take Away / Divide By N (never below 1)). Countdown length = `buffDuration`. Box "🦊 Become a character" in the pickup's settings (`Editor/BecomeEditor.cs`); picking a model finds its animations there (AutoFindClips is editor-only, so clips are saved on the pickup).
- Runtime: `Scripts/Player/BecomeCharacter.cs` (added to the player root): remembers the normal look/settings, `CustomModel.SetLook` swaps model + rebuilds the animation graph, countdown text top-centre (flashes in the last 5 s), then "You're back to normal!". Touching another one puts the normal player back first. Dying reloads the level (normal player). Hooks: `Health` (InvincibleNow, `SetMaxHealth`), `FallDamage` (NoFallDamageNow), `ObstacleBase.GetJumpedOn` (`BecomeCharacter.JumpsNeeded`), `PlayerLogic.SettingsChanged`.
- Verified in Play (Tutorial 2, a park Health pickup changed in Play only): fox model + clips Idle/Walk/Run/Jump/Fall (it has no hurt clip), speed 5.61 -> 20 -> 5.61, health 300/300, Divide by 2: 6 jumps -> 3, countdown "Roses Fox 0:06" + message "You are Roses Fox for 6 seconds!", back to the monster girl (one model holder) when it ends. Bug fixed while testing: TMP `canvas` is null once hidden (countdown now uses its parent object).
- Not yet: a person clicking the 🦊 button and the settings box (not clicked: the teacher's Tutorial 2 was open with unsaved changes, so nothing was spawned in it); a build.
- End of session 12: the teacher said "commit anything", so their saved Tutorial2.unity, GameSettings.asset (Spider Library entry), the Spider picture and the TMP font atlas were committed too. Tutorial 2 still had UNSAVED changes open in Unity at that point (not in git). Next: NEXT JOB part 3 (flying), then part 4 (Tutorial 3 showcase), then class builds.

### 📍 SESSION 11 (2026-11-13, late): part 1 of the NEXT JOB done + grass fix (committed)
- **Package:** `com.unity.postprocessing` 3.4.0 (Built-in post-processing v2).
- **✨ Graphics & effects** (🏔 Level tab box): `Scripts/Lighting/LevelGraphics.cs` on a "✨ Graphics & effects" object (button adds/removes it; levels without it are unchanged). Film colours (Gentle = Neutral, Strong = ACES +0.6 exposure), brightness/colourful/contrast/warmth, glow shine (bloom), dark edges (vignette + colour), corner shadows (AO, off by default, High only), shadows reach (default 120 m, Play only). Profile, volume and the camera's PostProcessLayer are made in memory (HideAndDontSave/DontSave), so nothing extra is saved into the scene; shows live in the Game view. PostProcessResources kept on the component (filled in the Editor) so builds include the shaders.
- **Esc menu "Graphics: High/Low/Medium"** button (cycles; PlayerPrefs `GraphicsQuality`): `Scripts/Lighting/GraphicsQuality.cs`. Low = hard shadows 40 m/1 cascade, 1 pixel light, no MSAA, no screen effects, grass 25 m x0.4; Medium = 80 m/2 cascades, 2 lights, no MSAA, bloom fast mode, no AO, grass 45 m x0.7; High = the level/project values. Applied in `GameManager.OnSceneReady`. Quality changes are put back when Play stops (verified: 300 m/4 cascades/4 lights/2x AA after Stop, ProjectSettings unchanged).
- **Grass fix (teacher: "add grass isn't working"):** cause = the ground had NO grass types, so the Paint Grass brush had nothing to paint. Now 🌱 Paint Grass adds "Simple Grass" automatically when there is none, plus a **🌱 Grass** box in the Level tab (`Editor/GameBuilderWindow.Grass.cs`): add simple grass (flat picture `Assets/Resources/Simple Grass.png`, made by code, fastest) or a grass model (Grass/Grass_2/Grass_Short, 192 tris, instanced, half density), per-type height/width/colours/🗑, "Grass shows up to (m)" + "Grass thickness" sliders (first grass sets cheap 60 m / 0.6), 🧹 remove all painted grass. Teacher asked: keep grass light on the CPU.
- Verified: grass rendered on a temporary ground (additive temp scene, closed unsaved); effects screenshots before/after on Tutorial 2's camera (temp scene, removed afterwards, Tutorial 2's unsaved edits untouched); Play test of the Low/Medium/High values. Not yet: a person clicking the new boxes; a build.
- Still to do from the NEXT JOB: parts 2-4 below (become-a-character pickup, flying, Tutorial 3 showcase). Start with part 2.
- Still uncommitted on purpose (teacher's): Tutorial2.unity edits, GameSettings.asset (Spider Library entry), the Spider picture, the TMP font atlas. Consider adding ✨ Graphics + a grass station to the tutorials later (needs the teacher's OK to rebuild).
- Class builds NOT redone since these changes (offer one after part 2-4, or sooner if wanted).

### 🔜 NEXT JOB (asked at the end of session 10; part 1 done in session 11)
Teacher's words, in short. Build every part as OPTIONS (see the memory notes), test in Play, then commit:
1. **Graphics options in the 🏔 Level tab** ("chuck all the graphic options in as options"): post-processing for the Built-in pipeline (needs the `com.unity.postprocessing` package from the Unity registry): colour grading/tonemapping (cheap, biggest win), bloom (makes Glow pickups/teleporters shine), vignette (suits the menacing look), ambient occlusion (costs more: off by default); shadow distance (Ultra uses 300 m + 4 cascades, the heaviest thing; ~120 m looks the same and saves power); plus a **Graphics: Low / Medium / High** choice in the Esc menu so weak laptops run smoothly. Project facts: Linear colour space, Quality "Ultra", soft shadows, 2x MSAA. Teacher cares about no "heavy power consumption lag".
2. **"Become a character" pickup** (a new pickup kind on `ObstacleBase`, button in Creatures & Pickups): touching it turns the player into another model, e.g. **Roses Fox** (`Assets/MyModels/Roses Fox - Rigged & Animated (C).glb`, the teacher's, with animations). Options:
   - which model + its animations (pre-find clips in the editor; `CustomModel.AutoFindClips` is EDITOR-ONLY, so store the clips on the pickup);
   - temporary changes to ALL the player defaults while transformed (speed, sprint, jump, stamina, health/invincible, fall damage... each as "change this" + value);
   - a **countdown timer**, length changeable, **shown on screen**; when it ends, the normal player comes back;
   - "hits to beat creatures": **subtract** N or **divide** by N (creatures' 'jumps to beat').
3. **Flying characters** (dragons etc.): an option on that pickup ("Can fly"). They start on the ground; **Space raises, Ctrl lowers** in the air; needs a new **Fly animation slot** in `CustomModel` (+ the Game Builder/Sounds animation lists). The player is KCC `ExampleCharacterController` (not ExamplePlayer.cs; find where input is read).
4. **Showcase in Tutorial 3 "Playground Madness"** (`Editor/MegaLevelBuilder.cs`), in obvious spots near the start, with signs: a Roses Fox pickup and a Dragon pickup (use `Dragon-model.glb` for now; the teacher is making a classroom dragon model to swap in). Rebuilding Tutorial 3 needs the teacher's OK; back up the scene first. Then new class builds (Windows + Mac) and commit.
- Rebuilt tutorials keep the player via `TutorialLevelBuilder.SetTutorialPlayer` (`TutorialPlayerModel` = "Leane' Scary Monster Girl - Rigged & Animated (C).glb", clips idle zombie_idle_loop_fit, walk zombie_walk_fwd_loop_fit, run jog_fwd_loop_fit, jump ninjajump_idle_loop_fit, the rest automatic). If the teacher changes her again, read Tutorial 2's player and update that list.

### 📍 SESSION 10 (2026-11-13): fixes after the teacher played around
Teacher asked: Scenery in the Library; terrain changes stick even when not saving; see-through not working (in the build); animations moving the model (fixed position by default); what other computers need; Mac build won't open. Mid-task: the player falls through the ground at the start of the tutorial they had sculpted.
- **Ground "Don't Save" really discards:** `Editor/TerrainUnsavedGuard.cs`. Ground lives in its own TerrainData file, so Unity's Don't Save kept it (and any later SaveAssets wrote it). The guard keeps a hidden `HideAndDontSave` copy per TerrainData ("Saved ground copy #<instanceID>", survives domain reloads) taken on sceneOpened/sceneSaved (or lazily in `Watch`); `Watch` (every 0.5 s) marks the level dirty when the ground really changed; `sceneClosing` with a dirty scene and `quitting` put the copy back (`CopyGround`: heights, paint, holes, grass, trees) and `SaveAssetIfDirty`. A ground file replaced on disk (git) drops its copy (`ReimportWatcher`; saves by Unity itself are told apart with `OnWillSaveAssets`). Ignores Play start/stop (`inPlayChange`, cleared on EnteredEditMode; a delayCall there never ran with Unity in the background). Menu toggle **Game > Ground: Undo Unsaved Changes** (EditorPrefs, on). Verified in a scratch level: unsaved sculpt → put back in memory AND on disk; saved sculpt → kept; Play/Stop with a dirty level → untouched.
  - ⚠ Bug found + fixed while testing: before the play-mode guard, stopping Play wrote a stale (sloped) copy back to Tutorial2Terrain.asset. Restored from git again.
- **Tutorial 2 ground restored** (teacher chose this): the teacher had raised the park into a slope (+19 m at the start), burying the player start, 7 signs, power-ups, zoo creatures and the checkpoint → player fell through every time. `Tutorial2Terrain.asset` = HEAD again (sloped copy kept in the session scratchpad only). Tutorials 1 and 3: nothing buried. The teacher's other Tutorial2.unity edits are untouched and uncommitted.
- **Player never starts under the ground:** `GameManager.KeepPlayerAboveGround()` in `OnSceneReady` (start + checkpoint respawn): below the terrain height → put 0.1 m on top + a console warning. Verified in Play (10 m under → on top).
- **Animations stay in place:** `CustomModel.animationMovesModel` (default off, "Animation can move it" in 🏃 Animations). LateUpdate keeps the biggest SkinnedMeshRenderer's `rootBone` over its starting spot (holder space X/Z; Y still moves). Humanoids skipped (already in place). Measured: only the new medieval warrior's clips walk off (its `root` bone moves 1.3 m per walk loop); all class models already animate in place, so they're unaffected. Verified in Play: off = root fixed, on = drifts to 1.1 m.
- **See-through in builds:** cause = a built game only has the Standard shader's "Fade" version if a saved material uses it; the copies made at runtime fell back to solid. Now `SeeThrough.CopyOf` starts from `Assets/Resources/SeeThroughFade.mat` / `SeeThroughFadeGlow.mat` (Fade, + emission). Verified in Play; NOT yet checked in a real build (needs a new class build).
- **Scenery in the Library:** `LibraryCategory.Scenery` (appended, ints unchanged), in-game Library tab "Scenery" (index 5, own empty-page hint). Game Builder > 🌲 Scenery > "📖 Library (one piece)": select a tree/rock → Add to Library (picture + "What is the …?" backstory). `CustomModelEditor.DrawLibrarySection(name, Transform, category, backstory)` + `TakePicture(Transform, name)` are the general versions. Verified: picture of a tree, the tab shows in Play.
- **Mac "won't open":** the app IS ad-hoc signed by Unity (has _CodeSignature); it's Gatekeeper blocking a downloaded app, and macOS 15+ removed right-click > Open. `MacZip.HowToOpen` rewritten: Terminal `xattr -cr "/Applications/<App>.app"` (easiest), or System Settings > Privacy & Security > Open Anyway, and `codesign --force --deep --sign -` if it says damaged. The existing Mac zip still has the OLD note: rebuild to get the new one.
- **Other computers:** all packages come from Unity's registry (download on first open, needs internet); KCC is in Assets; needs Unity 6000.3.20f1 via Unity Hub. Copy the project folder WITHOUT Library/Temp/Logs/Build (or git; this branch is NOT pushed). Mac builds need the Mac Build Support (Mono) module on that PC.
- **Stamina:** sprinting lasts twice as long: `PlayerLogic.staminaDrainRate` default 25 -> 12.5 per second (8 s from full). Old levels still saved with 25 are upgraded once (`settingsVersion`, in Awake + OnValidate); other values are kept.
- **Class builds redone (2026-11-13, after the stamina change):** `Build/TutorialIsland.zip` (Windows, 82 MB; ran 25 s, no errors in Player.log) and `Build/TutorialIsland (Mac).zip` (90 MB, new Mac note inside). They include the teacher's saved Tutorial2.unity edits. The Windows build took ~35 min (new see-through shader versions compiling). See-through in a build still not checked by eye.
- **Later in session 10 (teacher's corrections):**
  - "Scenery library" meant a LIST: the Library Scenery tab/category/box were removed again (GameMenuUI/GameSettings back to before). The general `CustomModelEditor.DrawLibrarySection(name, Transform, ...)` / `TakePicture(Transform, ...)` stay (unused extras).
  - **📋 Scenery List** (Scenery tab, like the Platforms List): `Scripts/Environment/SceneryPiece.cs` (hidden tag with `group`), added by `SceneryPopulator.PlaceAt`; older scenery is tagged by `GameBuilderWindow.TagOldScenery()` in RefreshSceneLists (no Undo, marks the level changed, like NameUnnamedObstacles). Selected piece: Name/Folder, Glow & sparkles, 📍 Move it (onto a platform = into its PlatformSceneryHolder, static flags cleared; onto the ground = back into its group folder via `SceneryFolder`), 🗑 Delete. Icons 🌲/🌿/🪨/🪵. Verified: 425 pieces listed in Tutorial 2, screenshot of the tab.
  - **Tutorials use a student's character:** `TutorialLevelBuilder.SetTutorialPlayer` (public) puts the monster girl on the player (now "Leane' Scary Monster Girl - Rigged & Animated (C).glb": the teacher replaced the file, which got a NEW GUID, so Tutorials 1 and 3 were re-pointed; clips listed in NEXT JOB above). Tutorial 1's welcome sign says "You are a character made by a student!". Change `TutorialPlayerModel` / `TutorialPlayerClips` to swap.
  - **Moody look:** `TutorialLevelBuilder.SetMoodyLook` (public): fog ExponentialSquared 0.004, colour (0.30, 0.30, 0.37), ambient/reflection 0.75, sun 0.8 (0.86, 0.80, 0.82), and `Assets/Tutorial/Moody Sky.mat` (copy of the sky, tint (0.40, 0.38, 0.46), exposure 0.75). Other levels keep their normal sky.
  - All three tutorials rebuilt with the teacher's OK (the old scenes/grounds were backed up in the session scratchpad first). Verified: player model + clips, fog, scenery tags, player 0.2 m above ground in all three; screenshot of the darker park.
  - Committed at the end (448181d): the tutorial scenes, grounds, Moody Sky, Leane's monster girl and Roses Fox .glb. Still not committed (teacher's): GameSettings.asset (Spider Library entry), the Spider picture, the TMP font atlas.
  - Graphics question (cheap wins) answered in chat: post-processing (colour grading/tonemapping, bloom, vignette; AO optional) and a lower shadow distance / graphics-quality option to save power. Not built yet.
- **Class builds redone again (22:15, monster girl + moody look + Scenery List):** `Build/TutorialIsland.zip` (Windows, 88 MB; ran 25 s, no errors) and `Build/TutorialIsland (Mac).zip` (96 MB). Windows build took 14 s this time (shaders cached).

### 📍 SESSION 9 (2026-11-11): what was done
Teacher asked: a scenery-model platform with creatures on it in Tutorial 2; sliders maxed far beyond the old limits; a bonus 3rd level showing everything maxed (enormous spiders/dinosaurs/dragons, ultra-fast racers, ridiculous power-ups, huge spinning sky islands, a very high finish reached by chained boost pads, hundreds of scenery pieces, some on platforms; a stress test). Mid-task they added: teleporters; whacky combinations (bounce off a dinosaur onto a platform, pickups on glowing scenery...); see-through sliders; "Landing on it never hurts" for platforms AND creatures/objects; the level name **Playground Madness**.
- **Slider limits raised** (all `[Range]`s + Game Builder sliders): creature Object Scale 200, creature/pickup speed 500 m/s, roam/patrol 1000 m, chase 500/1000 m and 500 m/s, payload 1000, power-up time 600 s, launch force/bounce strength 500, stomp bounce 300, jumps to beat 100, touch distance 20, Custom Model size 50, platform move speed 1000, spin ±3600°/s, grow min/max 50, crumble 60 s / back 300 s, Platform Model size 50 / raise ±200, sparkles amount/size, glow 20 / range 200, max health 10000, stamina 10000, sprint x20, scatter counts (creatures 2000, platforms 1000, scenery 5000), scatter ranges, 'New creatures are this big' 100, 'New platforms float this high' 500, Height Alignment (fly height) 300, sign zone 200. Platform width/depth kept at 500.
- **Creature scatter groups:** `flyHeightRange` ("🐉 Fly this high (m)", uses `ScatterTools.SetFlyHeight` → `TerrainObstacle.heightOffset`, `ApplyVisualHeightOffset` now public, hit zone refits) and `pickupsWander` ("🏃 They run around" for pickups/bounce balls).
- **Scenery on platforms:** `Scripts/Platforms/PlatformSceneryHolder.cs` ("Scenery on this platform (auto)" under the platform's TargetChild, cancels its stretch every LateUpdate). `SceneryPopulator.PlaceOnPlatform(group, model, platform, point, size)`; Place One clicking a platform puts it ON it (`placeOnPlatforms`, default on, "Click a platform: put it ON the platform"). Platform scenery gets no static flags (it must move). PlatformModel ignores the holder (stays visible when the model changes); the crumbling/bouncy tint skips it. The Scenery tab's "one piece" panel also finds trees on platforms. Teleporter pads on platforms use the same holder.
- **Safe landing ("🪂 Landing on it never hurts"):** `PlatformLogic.safeLanding` (default off; `ScatterTools.SetPlatformType(Bouncy)` turns it on), `ObstacleBase.safeLanding` (🏀 Bounce box), scenery group `safeLanding` + per-piece toggle (adds `Scripts/Game/SafeLanding.cs`). `FallDamage` asks `SafeLanding.IsSafe(GroundCollider)`. Verified: 50 m falls onto a boost pad = 0 damage; a normal 50 m fall = -57.
- **Teleporters:** `Scripts/Game/Teleporter.cs` (Sends you to, colour, keep your speed, arrive this high above (0-300 m, "sky drop"), sparkles, message, wait) + `Editor/TeleporterTools.cs` (`CreatePair`, `CreatePad`; pads: 3 discs + Glow + Tornado sparkles + 35% see-through, trigger BoxCollider on the root; root object "Teleporters"; material `Assets/Resources/TeleporterMaterial.mat`). Game Builder > Signs & Story > 🌀 Teleporters: "Make a teleporter pair" (two clicks), list, settings, see-through, Move it. Arriving pad ignores you until you step off. One-way = empty 'Sends you to'. Verified both ways in Play.
- **See-through:** `Scripts/Game/SeeThrough.cs` ("👻 See-through (%)" slider at the bottom of every Glow & sparkles box: creatures/pickups, platforms, scenery pieces, teleporters). Makes Standard-shader "Fade" copies (keeps texture/colour; glTF/Standard/URP names) as DontSave materials; originals are kept in a serialized list. `Editor/SeeThroughSaveGuard.cs` restores originals on `sceneSaving` and re-applies on `sceneSaved` (verified: 0 empty material slots in saved scenes). Tints (platform types, teleporters) use `SeeThrough.AlphaFor`. 0% removes the component.
- **Tutorial 2 (Adventure Park), rebuilt (teacher OK'd):** park 285 m long, 14 signs. New: **12 Model platforms** (Step Up → "Tree Stump Island" (TreeStump_Moss, 10 m) with a willow ON it, 3 scattered spiders + health; "Moving Log" (WoodLog_Moss, side to side) with a bush and a patrolling fox), **13 Teleporters & see-through** (pad on the ground → "Teleport Ledge" 11 m up with a Super Jump; 60% see-through Ghost Zombie; 70% Glass Platform), **14 Finish** now loads Tutorial3. Verified in Play: things stay on the stump/log, teleport up works, ghost/glass look right.
- **Tutorial 3 "Playground Madness":** `Editor/MegaLevelBuilder.cs` (class name kept), `Assets/Scenes/Tutorial3.unity` (28 MB), `Assets/Tutorial/Tutorial3Terrain.asset` (flat 500x500 valley, normal mountains), menus Game > Tutorial > Open/Build Tutorial 3 (Playground Madness). Seeded (3003). Added to Build Settings after Tutorial2; `TutorialLevelBuilder.TutorialScenes()` puts all three in the class builds. Player has 1000 health. Built: **206 platforms, 319 creatures, 276 pickups/bounce balls, 654 scenery pieces, 6 teleporter pads** (builds in ~4 s).
  - Boost tower: 8 bouncy pads (bounce 57, safe landing, glowing, Explosion land effect; 6 and 8 move), 36 m higher and 13 m further each, a Rest Stop with checkpoint + Health 1000 halfway, FINISH platform ~280 m up with a one-way gold teleporter home.
  - Whacky stations: ring of 20 giant gifts (Health +1000, Shield 2 min, Super Jump/Speed x3 for 60 s, custom messages); bouncy giant dinosaur (size 30, launch 80, straight up) beside "Dino Sky Island" 60 m up (verified ~90 m launch; edge ~3 m from the launch column, needs steering); crystal field (10 glowing Rock_Snow, safe landing, pickups on their tops, flying sharks); spinning 40 m stump carousel (trees, spiders, speed boosts, purple teleporter from the start, verified riding it); invisible bridge (100% see-through steps with fireflies, some crumbling) to a glass platform with a see-through ghost dragon; sky drop teleporter (arrive 150 m above a giant trampoline); trampoline spiders (stomp bounce 70, 50 jumps); 34 sky islands made from rock/stump/log models (12 huge 40-80 m; spin up to 720°/s, fly 150 m at 40 m/s, 150 m elevators, growing x0.4-2.5) with trees on most; 150 scattered platforms; scenery: trees 350, rocks 120, plants 500, giant trees 6-15x, giant rocks 6-25x, glowing firefly trees with health in their tops; stampedes (speed boosts 30-90 m/s, runaway health, floating super jumps, giant bounce balls, a 150 m/s shield conveyor); creatures (40 spiders 10-25x, 16 dinosaurs 25-50x, 24 flying dragons, 70 racers 60-150 m/s, 30 flying seagulls, 8 giant snakes, 50 tiny spiders, a T-Rex chasing at 40 m/s); 90 creatures/pickups on platforms.
  - Verified in Play: ~55 fps in the Editor with ~600 obstacles; 142/148 platform things still on after 104 s (the rest fell off fast spinners); boost pad bounce ~54 m, no damage; teleporters; dino launch.
  - Fixed during testing: pads on platforms were placed by renderer bounds (counted trees → 13 m too high); now `Surface()` raycasts the platform's own colliders.
- **Class builds redone (all 3 tutorial levels):** `Build/TutorialIsland.zip` (Windows, 82 MB) and `Build/TutorialIsland (Mac).zip` (90 MB). The Windows one ran with no errors in Player.log (it seemed to quit after ~5 s: that was the teacher closing it). Mac still untried on a real Mac.
- **Not done / next:** not tested by a person: actually climbing the boost tower and steering onto Dino Sky Island; the Game Builder's 2-click teleporter placing and the see-through slider were not clicked by a person (code paths tested). Tutorial 1 signs don't mention teleporters/see-through (Tutorial 2 does).
- Gotcha: opening a level can mark it changed (the animation 'looked for' delayCall); the Undo history is then empty.

## Earlier sessions
### 📍 STATE AT THE END OF SESSION 8 (2026-11-10, late)
- **Everything is committed** on `feature/menus-models-checkpoints` (not merged, not pushed). Only the usual noise is uncommitted (TMP font atlas, TutorialTerrain.asset, the teacher's GameSettings.asset and Spider Library picture: leave those out).
- **Class builds are current** (both tutorials, all of session 8): `Build/TutorialIsland.zip` (Windows, 78 MB, 0 errors, ran 25 s clean) and `Build/TutorialIsland (Mac).zip` (86 MB, universal, exec bit checked). **The Mac zip has NOT been tried on a real Mac yet**: ask how it went.
- Done this session (details in the sections below): Tutorial 2 "Adventure Park" + Tutorial 1 → 2 link; bigger creatures + New creatures size slider; Adventure Park's own mountains (TerrainShaper); creature attack (turn to face) / death (hold pose, shrink away, sparkles when) / player hurt flash + hurt animation, all with drag-in animation slots; 'Hurts from' 0.3; round bounce balls with real angles; pickup messages; Speed Boost free sprint + Super Jump no fall damage; the whole Game Builder restyled (folding title-bar boxes, emerald/blue); Mac builds.
- **Teacher likes:** small visual tweaks done live while they watch (gaps, widths, colours, titles); options instead of hard changes; measuring things from screenshots before saying "done".
- **Next ideas / open:** web (WebGL) build for Chromebooks (see the Mac/power-up notes for what it needs); maybe a player death animation before the death screen; "Straight up"/pickup texts in more tutorial signs if wanted; the per-platform "More > Scatter" still places things off round platforms.
- **Rules that bit this session:** check `isDirty` and STOP before any tutorial rebuild (it once wiped the teacher's live tweak); Unity must restart after installing a build module; capture timing lags evals by ~1 s; pass emoji in EditorPrefs keys escaped from bash.

### ✅ DONE (session 8, 2026-11-10): Tutorial 2 "Adventure Park" + Tutorial 1 refreshed
Teacher asked for a second tutorial level showing all the features, reached from Tutorial 1's Finish sign. Teacher approved wiping their experiments in Tutorial 1 for the rebuild.
- **Builder:** `Editor/ShowcaseLevelBuilder.cs`, scene `Assets/Scenes/Tutorial2.unity` (`SceneName` "Tutorial2"), own ground `Assets/Tutorial/Tutorial2Terrain.asset` (flat park 100 x 250 m from (0,-80)), menus **Game > Tutorial > Open Tutorial 2 (Adventure Park)** / **Build Tutorial 2 (Adventure Park)** (the Build menu shows a confirm dialog; from scripts call `ShowcaseLevelBuilder.Build()`). Added to Build Settings right after Tutorial. Seeded random (2026), so rebuilds are the same.
- It uses the students' own tools: `ScatterTools.QuickPlatform` with `PlaceAt` + `NewPlatformHeight` (restored afterwards), `ScatterPlatforms` (settings left on the PlatformController's PlatformScatter) and `ScatterAllCreatures` (CreatureScatter on the terrain), populator `CreateSingleObstacle` with JSON borrow/restore, `AddGlow`, AmbientSparkles, land effects, `ApplyPlatformModel`.
- **12 signs**, each station's things in a 📁 folder (signs in "Park signs"; folder names have no numbers so A-Z sorting reads well): 1 Welcome, 2 Power-ups (all 5 in a glowing row across the path), 3 Moving platforms (side to side, spinning, growing, elevator up to "High Ledge" with a "Prize Shield"; the elevator's top = the ledge's top), 4 Stepping stones (6 + "Top Platform" with a Speed Boost), 5 Sky garden (11-12 scattered platforms, 20% crumbling, 15% bouncy, spiders/health/super jumps scattered onto them), 6 Creature zoo (Fox wanderer, Horse patroller, Snake guard, Giant Dinosaur 5x / 3 jumps with a "Jump From Here" platform, Bouncy Spider), 7 Checkpoint, 8 Chaser (zombie, chase 8 m / gives up 15 m / speed 2.5, 2 jumps), 9 Runaway things (wandering Health, wandering bounce ball), 10 Glow & effects (4 low platforms: glow + Sparks, glow + fireflies + Ring, Float Up + Fountain, rock model + Explosion), 11 Make it yours (click to place, Move it, Name/Folder, drag in lists, Ctrl+Z), 12 Finish (no level change; says Esc > Restart Level).
- **Tutorial 1:** helpers in `TutorialLevelBuilder` are now `internal` (Sign, MakePlatform, TopOf, Nudge, FlattenArea, RendererBounds, AddPart, GetSignMaterial) plus `CreateTutorialTerrain(dataPath, rect, height)` and `AddToBuild(scenePath, after)`. Finish (17) now loads Tutorial2 and says so; signs 7 (click to place, float height, Move it) and 11 (click where it goes) refreshed. Rebuilt. `BuildTutorialGame()` now builds Tutorial + Tutorial2.
- **Verified in Play:** pop-ups show on entering; all creatures/pickups stay on the ground/platforms; the chaser closes 6 m in under 1 s and gives damage; the dinosaur is beaten by dropping on it with no damage; walking into Tutorial 1's Finish loads Tutorial2 and play continues (state Playing, no Console errors). Screenshots looked right (sky garden, glow platforms, zoo).
- NOT done: a fresh class build (Game > Tutorial > Build Tutorial Game, then zip `Build/Tutorial`); offer it. Not tested by a person: riding the elevator to the ledge and jumping from the platform onto the dinosaur (measured only: ledge top = elevator top; platform edge about 1.7 m from the dinosaur, 1.6 m higher).
- Gotcha: `internal` members of editor classes can't be reached from `unity command script eval` (it compiles in its own assembly).
- **Follow-up (same session): bigger creatures + new mountains.** Teacher: creatures were far too small next to the player; wanted Adventure Park's terrain to look different.
  - Sizes (objectScale): Adventure Park Fox 2, Horse 3, Snake 4, Bouncy Spider 2, Chasing Zombie 2, sky garden spiders 1.4-1.7, Giant Dinosaur 9 (5 m tall, 8 m long; "Jump From Here" platform 7 m up, 1.6 m from it). Tutorial 1 arena spider/zombie 2; the "Jump On Me" zombie only 1.3.
  - **Why the stomp zombie stays small:** the player's jump lifts the feet only about 1.7 m (KCC JumpUpSpeed 10, gravity 30), so a creature meant to be jumped on from the ground must be under about 1.5 m tall. Bigger ones need a platform to jump from.
  - **New mountains:** `Editor/TerrainShaper.cs` `NewMountains(terrain, seed, valleyRect, valleyHeight)` makes ridged-noise mountains rising away from the valley (max 40% of the terrain height) with gentle hills near it; `CreateTutorialTerrain(..., newMountainsSeed)` runs it before flattening (0 = keep the normal ground). Adventure Park uses seed 7. Possible later: a Game Builder Level-tab button for students ("🎲 New mountains", with Undo).
  - Verified in Play: all creatures on the ground/platforms in both levels, screenshots of the new valley.
- **Follow-up (same session): hurts-from 0.3, creature attacks, player hurt flash.**
  - `ObstacleBase.touchDistance` ("Hurts from (metres away)") default is now **0.3** (was 0.15). Only new creatures get it; existing ones keep their saved value. Both tutorials were rebuilt with it.
  - **Attack:** `CustomModel.attack` clip, auto-found by name (scratch, bite, attack, punch, kick, claw...; Zombie_Scratch, Bite, Kick, Attack for the class models). Models made before this look for one once (`attackLookedFor`/`hurtLookedFor` flags, in OnEnable/Refresh in the Editor, via delayCall). `CustomModel.PlayAttack` plays the Sounds tab's 'it attacks you' animation if one is set, else `attack`. `ObstacleBase.Attack.cs`: `faceYouWhenAttacking` (default on) snaps it round to face the player during the animation (Slerp x25), it doesn't walk meanwhile, then turns back (0.35 s, platform-relative) and carries on. Inspector "⚔ When it hurts you": "Turns to face you" + "Attack animation" drag-in box (teacher asked for drag-in like the other slots, e.g. a Jump when there's no attack one) via `CustomModelEditor.AnimationSlot`.
  - **Player hurt:** `Health` "When you get hurt": Flash when hurt (on), Flash style Colour/Blink, Flash colour (red), Flash for 0.6 s, Flashes per second 8. Colour uses a MaterialPropertyBlock (_Color, _BaseColor, glTFast baseColorFactor); Blink toggles renderers; a hit mid-flash reuses the same renderer list. `CustomModel.hurt` clip auto-found ("hit" first, then hurt/damage...; Old Man = Hit_Chest), played when the Sounds tab has no 'gets hurt' animation; dropdown "Hurt (when you get hurt)" in the player's model Animations.
  - Verified in Play: snake facing 270 turned to 358 (player's side), bit, turned back to 271; player -10 health; red flash visible and cleared; Blink hid and restored the model; Hit_Chest played. No Console errors. Careful in tests: teleporting the player into a creature's body counts as landing on its head (it gets beaten).
  - The open Tutorial 2 had unsaved changes during this, so it wasn't saved or rebuilt again; the player's hurt clip in the saved tutorial levels is filled in when they're next opened and saved (or rebuilt).
- **Follow-up (same session): death animations + shrink away.** `CustomModel.death` clip auto-found ("death", "die", "dead", "defeat", "dying"; all class models have one: Fox 1.2 s, Horse 1.4, Spider 1.3, Snake 2.3, Zombie Death_A 4.5, Dinosaur 4.5). `PlayDeath` (Sounds tab 'it is beaten' first) plays it with `holdLastFrame`: `actionHold` keeps the action weight at 1 and pauses the clip at its end, so it stays lying down. `ObstacleBase.DieThenShrink`: wait for the animation (max 6 s), then if `shrinkAwayWhenBeaten` (default on) wait `waitBeforeShrinking` (0.5 s) and shrink to 0 over `shrinkTime` (1 s), sparkle burst, Destroy. No death animation = the old squash. Inspector "💀 When it's beaten": Death animation dropdown, Shrinks away afterwards, Lies there for, Shrinks over (also a Death dropdown in the model's Animations).
  - Verified in Play (snake): landed 0.4 s, Death held 2.3 s, shrink 3.3-4.2 s, removed 4.3 s; snake 1.3 m -> 0.5 m tall while dead; dinosaur 4.8 m -> 2.8 m, stays visible. No Console errors.
  - The 'looked for' flags (attack/hurt/death) are filled by a delayCall when a level opens, which waits until Unity has focus; in scripted tests call `CustomModel.Refresh()` first, or the saved levels' old creatures have no death clip yet (then they squash).
  - Capture timing gotcha: `capture game_view` runs a second or more after an eval returns; time screenshots with that in mind.
- **Follow-up: new creature size + death sparkles.**
  - Creatures tab slider **"New creatures are this big"** (`ScatterTools.NewCreatureSize`, EditorPrefs `GameBuilder.CreatureSize`, default **2**, 0.5-6). Used by the Wanderer/Patroller/Guard buttons (`Spawn` sets the borrowed populator's `objectScale` for Damage only, then restores it) and by creature scatter groups (their size range is now "1 = this size"; pickups and bounce balls unchanged). A tip under it says the player jumps about 1.7 m. `ShowcaseLevelBuilder.Build()` sets it to 1 while building and restores it. Verified in Play: Patroller objectScale 2, Health Pickup 1, populator left at 1.
  - **Sparkles on death:** for creatures that can be beaten, the Sparkle Burst settings moved into "💀 When it's beaten" (hidden from the general list), plus **"Sparkles when"** (`deathSparkleWhen`: When It Disappears (default, as before) / As Soon As Its Beaten / Both). Verified: "Both" burst at 0.5 s and at removal (4.3 s).
- **Follow-up: Game Builder restyle (teacher-led, many small steps), all committed.**
  - Every tab uses folding title-bar boxes: `ObstacleBaseEditor.BeginBox(title, subtitle, green)` returns open (state in EditorPrefs `GameBuilder.BoxOpen.<title>`, **closed by default**; renaming a title resets it) + `EndBox()`; `GameBuilderWindow.Box/EndBox` wrap them. Click on the bar toggles on the next redraw (changing layout mid-event breaks IMGUI).
  - Colours: **emerald** (0.02, 0.42, 0.32) = defaults ("Default options for spawning creatures/platforms", "Default options for new characters"), lists ("Creatures and Pickups List", "Platforms List", ...), tools (Scatter tools, Scenery tools), General sounds/Music/Starting volumes; **blue** = settings. Title font 15 in a 28 px bar. Button rows that make things keep plain `Section` headings.
  - Creature panel order: Default options → Creatures/Pickups buttons → list → one inset outer box (Name, Model (+glow buttons), Transform settings (Move it…, scale, sit on ground, Y rotation, Height Alignment), When you touch it (What it does / How much / Power-up lasts / Disappears + Bounce), Movement settings (How it moves via `ObstacleBaseEditor.HowItMoves` + Chase), Battle settings (Jump On Top, Hit zone, attack, death + sparkles), Delete) → Scatter tools. Gaps inside the outer box are equal on all sides (measured from screenshots: a GUILayout horizontal wrapper adds ~4 px left / 3 px right; `GUILayout.Space(-8)` trims the first box's top gap).
  - Signs & Story: `BeginGroup/EndGroup` = bordered, non-folding group with a 17 pt title ("💬 Pop-up signs", "🚩 Checkpoints").
  - Levels & Build list moved to `DrawLevelsList()`; New level / 🎮 Test / Share in `DrawNewLevelTestShare()`.
  - Tutorial signs updated (blue stamina bar, new section names) and both tutorials rebuilt (teacher OK'd). Class build zip still to be offered.
  - Emoji in EditorPrefs keys: pass them escaped (`\U0001F4CB`) from bash evals, or the key won't match.
- **Follow-up: round bounce balls + pickup messages.**
  - Hit zone **Shape** (`ObstacleBase.hitZoneShape`: Automatic / Cylinder / Ball; `HitZoneIsBall`). Automatic = Ball for bounce balls and pickups, Cylinder for creatures. Ball = SphereCollider Body + Touch (radius + touch distance, min 0.3 m) on the same "Body"/"Touch" objects; the unused collider type is switched off (`UseOnly`). `physicalMeshCollider`/`triggerMeshCollider` are now `Collider`. Scene drawer draws wire balls.
  - Cylinder touch zones of bounce balls/pickups also reach above the top (teacher found landing on them did nothing after the cylinder change).
  - Bounce direction = from the hit zone's centre to the player's capsule middle (it used the object's BASE, so everything went up). 'Straight up' (upwardBias) 0.5 → side hits ~35°, 0.2 → ~64° from vertical. New bounce balls (button, scatter) get 0.25, the park ball 0.2; Tutorial 1's bounce pad keeps 0.5. Labels: Launch force / Straight up (0-1) / Keep your speed (0-1).
  - **Pickup message** (`Scripts/Game/PickupMessage.cs`, canvas sortingOrder 450, body font bold 72 (the title font went blurry), pickup colour, shows 1.6 s + 0.4 s fade): `showPickupMessage` + `pickupMessage` (empty = automatic, `AutomaticPickupMessage()`), in When you touch it > 💬 Message when picked up. Verified: "+25 Health!" and a custom "Zoom zoom! Stamina full!".
  - Tutorial signs mention them (T1 sign 13, park signs 2 and 9); both tutorials rebuilt. The class zip predates these.
- **Follow-up: power-up options + Mac builds.**
  - Speed Boost `speedBoostFreeSprint` and Super Jump `superJumpNoFallDamage` (both default on, pickup's When you touch it box). `PlayerLogic.FreeSprintNow` / `NoFallDamageNow`; `FallDamage` remembers a protected fall until landing. Verified: 40 m fall with Super Jump 0 damage, without 16. Stamina with real Shift not tested by Claude.
  - **Mac Build Support (Mono)** installed with `unity install-modules --editor-version 6000.3.20f1 -m mac-mono --accept-eula`; **Unity must be restarted** before it can build Mac (IsBuildTargetSupported says true too early, BuildPlayer then fails with "Build target 'StandaloneOSX' not supported").
  - `GameMenuItems.BuildMac/BuildMacApp` (universal x64ARM64 set by reflection), `MacZip.ZipApp` (System.IO.Compression with Unix perms: Contents/MacOS, Frameworks, .dylib = 755; Windows' Compress-Archive loses the exec bit) + 'HOW TO OPEN ON A MAC.txt' (right-click > Open; `xattr -cr` if "damaged"). Buttons: Levels & Build > Share (🪟 / 🍎), Game > Build Game (Mac), Game > Tutorial > Build Tutorial Game for Mac.
  - Built `Build/TutorialIsland (Mac).zip` (86 MB, 0 errors, fat binary CAFEBABE with 2 slices, both levels). NOT run on a real Mac yet.
  - Web build (for Chromebooks) is for later: needs Web Build Support, hosting, a lighter quality setting, and the video sign switched to a URL (VideoClip doesn't work in WebGL).

### ✅ DONE (session 7): list order + drag-and-drop
- `Editor/GameBuilderWindow.ListOrder.cs`. Rows are a custom control (`RowControl`): click selects, Ctrl/Shift-click adds to the selection, dragging past 6 px starts a `DragAndDrop` that carries only generic data (`"GameBuilderList"` = the list key, no objectReferences, so it can't drop into the Scene or Hierarchy). Drop targets (`HandleDrop`): a folder header (into that folder, at the end; it lights up), a row (just above that row, in its folder; a line shows where), and a "⬆ Drop here to take it out of its folder" strip that appears while dragging something from a folder. Dragging one of several selected rows moves them all.
- The selected row (single selection) has ▲ ▼. `MoveItems` puts the things in place and renumbers that group's `ListFolder.order` (10, 20...) in one Undo step. Groups are sorted by `order`, then by name (`Ordered`). Folders are still A to Z.
- `ListFolder` got an `order` field (default int.MaxValue/2 = "not ordered yet", sorted by name).
- Tested by calling MoveItems directly: into a new folder, ▲, drop above a row, drop on the no-folder strip, undo. NOT tested with a real mouse drag.
- ⚠ The teacher was working IN THE OPEN TUTORIAL LEVEL (they made a "Custom Enemies" folder there). Don't test in, reload or discard the open level without asking. Remind them that a Tutorial rebuild wipes changes made there; build in their own level instead.

### ✅ DONE (session 7, very last): names + folders in the lists
- **Unique names:** new creatures/pickups (`Spawn`) and quick platforms get numbered names via `GameBuilderWindow.UniqueName` ("Wanderer", "Wanderer 2"...). New pop-up signs are "Pop-up Sign", "Pop-up Sign 2"... (`TriggerController.CreateTrigger`, was "UITrigger"; nothing looked that name up).
- **Name + 📁 Folder box** at the top of the selected platform / creature / sign / checkpoint panel (`GameBuilderWindow.Folders.cs`, `DrawNameAndFolder`): a DelayedTextField renames it (with Undo); the Folder dropdown offers (no folder) / the existing folders / "➕ New folder…" (an inline name box and a "Make it" button). With several selected, it applies to all of them (Ctrl/Shift-click rows in the list to multi-select).
- **Folders are labels, not Hierarchy folders:** a hidden `ListFolder` component (`Scripts/Game/ListFolder.cs`, `folder` string, HideInInspector) on the object. Parenting is untouched, so platform creatures and populators keep working.
- `DrawList` shows the loose things first, then each folder as "▼ 📁 Name (count)" (click to fold; open by default; the state is in `foldouts["folder/<list>/<name>"]`), with ✏️ rename (inline) and ✕ remove (the things go back to no folder). It scrolls above 9 rows.
- Tested: numbered names, sign names, two folders + undo (in the Tutorial, then discarded). Not tested by a person clicking: the rename box and the dropdown.

### ✅ DONE (session 7, last): Move panel, hand-made moves stick, scenery click-placing, platform height
- **Gizmos:** on by default (once per session). The hit zone / chase drawings (`ObstacleSceneDrawer`) now follow the Gizmos button (teacher: on by default, but hidden when Gizmos is off).
- **📍 Move it… → one Move panel** (`Editor/MovePanel.cs`; teacher's idea: one shared panel instead of position controls in every tab). A `GUILayout.Window` in the Scene view's bottom-left (it can be dragged by its title). It has left-right / up-down / forward-back with − / + (0.5 m) and typed numbers, "Click a new spot" (ScenePlacer), "⬇ Back on the ground (automatic height)" for creatures and pickups, "⬆ Height (m)" above the ground for platforms, and ✔ OK. Opening it switches to the Move tool so the arrows show. It closes itself when something else is selected (compared by owner: ObstacleBase / PlatformLogic, so clicking the model counts as the same thing). The button in the tab is `GameBuilderWindow.DrawPosition` (it highlights while the panel is open).
  - New spot: a platform keeps its height above the ground. A creature on a platform can only move on ITS platform; a ground creature can't go onto a platform. Otherwise a dialog says to delete it and place a new one there.
- **Hand-made moves now stick.** Before Play, `PlatformLogic.Update` and `FollowPlatform.LateUpdate` used to put things back at their offsets every editor update, so dragging a platform or a creature on a platform snapped back. Both now remember what they last applied (`[NonSerialized]`) and, if the transform changed since then, copy it into the offsets (and `rotationY` / `initialYRotation`) instead. FollowPlatform ignores turns caused by the 'Initial Y Rotation' slider. Tested by calling Update/LateUpdate directly (the Editor doesn't tick while Unity is in the background): drag platform +5 m (kept; its creature moved with it), drag creature +0.8 m (kept), Offset Y slider +2 (both went up), rotation slider kept, Back on the ground. `MoveOnPlatform` calls `Physics.SyncTransforms()` first; without it, the creature dropped 2 m too far after a platform's height changed.
- Ground creatures were already fine: wander/patrol/chase take their home from where they are when Play starts.
- **Platforms:** "New platforms float this high (m)" slider (`ScatterTools.NewPlatformHeight`, EditorPrefs, 3 m; an elevator +1.5 m).
- **Scenery "📍 Place One"** uses click-to-place and keeps going (a random model from the group with each click) until Esc (`ScenePlacer.Begin(..., repeat: true)`, `SceneryPopulator.PlaceOneAt`: clicking a tree or rock puts it on the terrain underneath). The Scenery tab has the click-to-place tick box too.
- The Game Builder refreshes its lists when a level opens (`EditorSceneManager.sceneOpened`).
- NOT tested with real mouse clicks (Claude can't click in the Scene view): the ScenePlacer ring/hint and the Move panel's buttons. The code paths behind them were tested by calling them directly.

### ✅ DONE (session 7, later still): click to place, hit zones always visible, Gizmos on
- **📍 Click in the Scene to choose where it goes** (tick box in the Platforms and Creatures tabs, remembered in EditorPrefs `GameBuilder.ClickToPlace`, default ON). With it on, the Quick platform, Creature and Pickup buttons wait for a click in the Scene view (`Editor/ScenePlacer.cs`). A ring follows the mouse (green = ground, blue = a platform) with a hint label; Esc or the Stop button cancels; Alt/right-drag still move the camera. `GameBuilderWindow.PlaceRow` runs the button's action with `placeHit` and `ScatterTools.PlaceAt` set: `LookPoint()` returns the clicked spot (platforms skip the "step sideways if taken" search), and `Spawn` uses the clicked platform (or the ground). On a platform, `MoveOnPlatform` sets the FollowPlatform offsets AND moves the transform at once (FollowPlatform only moves it on the next editor update, so `SitOnPlatformInEditor` would otherwise measure the platform's middle). Placed things are selected without moving the camera. Tested: ground, platform and quick-platform spots landed exactly on the clicked point.
- **Hit zone + chase circles** are drawn by `Editor/ObstacleSceneDrawer.cs` (SceneView.duringSceneGui + Handles), so they show with Gizmos off and when the MODEL (a child) is selected. The teacher couldn't see them because Gizmos was off and clicking a creature in the Scene selects its model. The runtime gizmo code was removed. `ObstacleBase.ChaseHome` is public for this.
- **Gizmos are switched on** once per Unity session (SessionState `GameBuilder.GizmosOn`), so turning them off sticks until Unity restarts. Side effect: the pop-up signs' big see-through trigger bubbles now show too.

### ✅ DONE (session 7, later): creature movement, chase, hit zone
The teacher asked for movement options for creatures AND pickups, an adjustable damage distance (large creatures hurt from too far away and were hard to land on), a visible cylinder hit zone with height/width that's fitted automatically, a plain answer about 'destroy on trigger', and an optional chase.
- **Movement:** the Game Builder's creature panel has "🚶 How it moves": Stays still / Wanders around / Walks (floats) back & forth, for creatures and pickups (`Editor/GameBuilderWindow.Creatures.cs`). It swaps the driver component (Terrain* or Platform* version) as one Undo step and renames "Wanderer" to "Patroller" etc. The driver's settings show below it with `[ShowAs]` labels ("How far it wanders", "Patrol distance", "Slowest/Fastest speed", ranges now 0.1-100 m and 0.1-20 m/s). Tested: swap + undo, and a wandering Health Pickup moved in Play.
- **Chase** (`Scripts/Obstacles/ObstacleBase.Chase.cs`, creatures only, default off): "Starts chasing within" 8 m, "Gives up past (from home)" 20 m, "Chase speed" 4. States: Off -> Chasing -> GoingHome -> Off; the normal driver waits while chasing or going home. Terrain creatures follow the terrain's height. `PlatformObstacle` overrides `StepTowards` (stops at the edge) and `HomeSurface` (the home spot moves with the platform), and chasers count as movers. Scene gizmos: orange = start circle, red = give-up circle. Tested in Play on a moving platform: 5 m -> 0.9 m in 2.5 s, gave up when the player went 45 m away, walked home, then patrolled again.
- **Hit zone** (`ObstacleBase.HitZone.cs`, ObstacleBase is now `partial`): the child "Hit Zone (auto)" (scaled by objectScale like the other children) holds "Body" (solid convex cylinder) and "Touch" (trigger, wider by `touchDistance` metres, its top level with the body's top so landing on top never hurts). It uses Unity's built-in `Cylinder.fbx` (**2 m wide and 2 m tall**: scale by `CylinderWidth/CylinderHeight`). It's fitted to the enabled Mesh/Skinned renderers (width = average of length and width), refitted in the editor every 0.5 s and after OnValidate (via delayCall), and once in Play. The old sphere colliders are switched off; `physicalMeshCollider/triggerMeshCollider` point to the new ones, so stomp detection is unchanged. Older creatures get a zone in Awake, so opening an old level marks it as changed (save it).
  - Inspector (`ObstacleBaseEditor`): "🎯 Hit zone": Fit to the model, Width/Height in metres (stored per objectScale 1), "Hurts from (metres away)" or "Touch distance", and a "Fit it to the model again" button. The unused `triggerRadius` is hidden.
  - Tested in Play: a 2-jump zombie dropped onto from above was beaten with no damage taken; standing 1.22 m from a patroller's centre = no damage, 0.95 m = 10 damage (body radius 0.37 + capsule 0.5 + 0.15 touch).
  - Collider `.bounds` are stale in edit mode until `Physics.SyncTransforms()`; this misled a check once.
- **'Destroy On Trigger'** is now labelled "Disappears after touching": the thing vanishes after the player touches it (a pickup is used up; a creature hurts once, then vanishes).
- Tutorial sign 12 ("Jump on it!") mentions the hit zone, How it moves and Chase; the level was rebuilt. The class zip does NOT have this yet: offer a new build.

### ✅ DONE (session 7, 2026-11-10): "make the Game Builder easy for 12-year-olds" (all six parts)
Checked tab by tab with `capture editor_window --window "Game Builder"`.
1. **No 'Script' box in the Game Builder** (it still shows in Unity's Inspector). `Editor/KidInspector.cs` has `KidInspector.DrawDefault(serializedObject)`, which replaces `DrawDefaultInspector()` in the PlayerController, GameController, EnvironmentController and TriggerController editors. `TextConfirmEditor` skips it too. Components with no editor (PlayerLogic, Health, FallDamage...) are drawn by `GameBuilderWindow.DrawInspector`, which uses `KidInspector.DrawDefault` when the editor is Unity's `GenericInspector`. "Setup Platforms" / "Create New Platform" and the pop-up "Dialog Canvas" field are hidden in the Game Builder only.
2. **Buttons:** "Create Character" becomes a grey "🔧 Fix my character" once an `ExampleCharacter...` child exists. "Create Trigger" is now a big green "💬 Create Pop-up Sign"; "Delete Trigger" is now "🗑️ Delete this sign".
3. **Names:** `ObstacleBase.KindName()` gives the button names ("Wanderer", "Patroller", "Guard", "Health Pickup", "Bounce Ball"...). `ObstacleBase.NameByKind` is called at the end of both populators' `AttachDriverBySpawnType`, so everything they make is named, including the Tutorial builder's creatures. The Game Builder also renames leftover "ObstacleLogic" objects when its lists refresh (`NameUnnamedObstacles`). This rename is deliberately NOT recorded for Undo: an Undo step inside OnHierarchyChange would make Ctrl+Z undo the rename, which would trigger it again, so Ctrl+Z would get stuck. The scene is marked dirty instead. The list shows "(kind)" only when the name doesn't already start with it, e.g. "Jump On Me  (Guard)". Scatter names match (`ScatterTools.KindName`).
4. **Plain-English labels:** new runtime attribute `[ShowAs("...")]` (`Scripts/Game/ShowAsAttribute.cs`) with its drawer in `KidInspector.cs`. The drawer handles `[Range]` itself (sliders stay) and keeps tooltips; it draws by type, without calling PropertyField (no recursion). It's used on PlayerLogic (sprint speed is now a 1-4 slider, plus the stamina and air settings), FallDamage and SceneryPopulator. The pop-up sign labels are set in `TriggerLogicEditor`, and its headers were renamed ("Sign zone", "Where the zone is", "Going to another level"). The Game Builder's label column is wider (`labelWidth`, 32% of the window, 150-230 px, reset to 0 afterwards).
5. **Level tab:** brush size + strength (%) sliders (`Editor/GameBuilderWindow.Brush.cs`). The active tool comes from internal `EditorToolManager.activeTool`. Terrain Tools brushes use the tool's public `m_commonUI.brushSize/brushStrength` (what they actually paint with). Unity's own Paint Grass uses the `TerrainInspector` instance's `brushSize/brushStrength` (instance properties, found with `Resources.FindObjectsOfTypeAll`). Tested by setting and reading back on all 6 brushes. `OnInspectorUpdate` repaints the tab while a terrain tool is active. Sky names are friendly: "Sky 1  (picture)", "Sky  (plain, follows the sun)".
6. **Polish:** 📋 on the two list headings; the top bar says "↩ Made a mistake? Ctrl+Z undoes it".
- The Tutorial's "2. Pop-up signs" text now says "Create Pop-up Sign" / "Go to another level when touched". The Tutorial level was rebuilt, and a fresh class build was made: `Build/TutorialIsland.zip` (76 MB, 0 errors in Player.log). The game title is now "Room 9 Class Game" (teacher's change, committed).
- Gotcha: the **Game > Tutorial > Build Tutorial Level** menu shows a confirm dialog, which blocks `script eval` (the Editor stays unreachable until someone clicks it). From scripts, call `TutorialLevelBuilder.Build()` instead.


- **Session 6 (sounds) + the Game Builder look/Signs/Library/music-mode follow-ups are committed** (see "Follow-up 2026-11-10: Game Builder look" at the end). See "Done 2026-11-10 (session 6): Sounds & Music" below. A fresh Tutorial build WITH sounds was made on 2026-11-10: `Build/Tutorial` + `Build/TutorialIsland.zip` (76 MB), ran with 0 errors.
- **Everything is committed** on `feature/menus-models-checkpoints` (latest commits: sounds & music, tutorial build menu item, tutorial with 17 stations, platform effects, glow/sparkles). Not merged to main, not pushed. The only uncommitted change is `Assets/Tutorial/TutorialTerrain.asset` (it changes on every Tutorial rebuild; harmless).
- **Nothing is in progress.** The teacher's last requests were all finished and verified in Play mode:
  - level order + New/Copy level
  - video pop-ups
  - quick platforms + platform/creature scatter
  - crumbling/bouncy platforms with effects
  - jumping on creatures
  - glow + sparkle effects
  - a 17-station Tutorial Island
  - a Tutorial-only class build
- **The class got a Tutorial Island build** (zip handed over; `Build/` is now empty). If the Tutorial or a feature changes, rebuild it with **Game > Tutorial > Build Tutorial Level**, then **Build Tutorial Game (for the class)**, and offer a new zip (`Compress-Archive` of `Build/Tutorial`).
- **The teacher dropped the YouTube/AI tutorial video**; the playable Tutorial is the class's guide. Any new student-facing feature should get a Game Builder button AND a Tutorial station/sign.
- **Working style that worked:** the teacher sends quick follow-up ideas mid-task (often as several short messages). Fold them in as options (default off/None unless obviously wanted), test in Play mode, then commit. They approved committing on this branch.
- **Likely next asks:** swap the test video for their own clip (in the builder: the `videoClip` load path for the "Watch me!" sign), feedback from students using the Game Builder, a Mixamo character test, more platform ideas.

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
| Build | **Game > Build Game (Windows)** (or Game Builder > Levels & Build) in `Editor/GameMenuItems.cs`; Tutorial-only: **Game > Tutorial > Build Tutorial Game (for the class)** | Output `Build/Windows/` or `Build/Tutorial/` (git-ignored). `Build/` was emptied on 2026-11-10 at the teacher's request; build again when needed. Last builds succeeded with 0 errors. |
| 3D models + animations | `Scripts/Models/CustomModel.cs`, `Editor/CustomModelEditor.cs`, `Editor/MyModelsImporter.cs` | See below. |
| Obstacles sit on the ground | `ObstacleBase.GroundLift`, `TerrainObstacle`, `PlatformObstacle.SitOnPlatformInEditor()` | Was 50% buried (terrain) or wrong for scale != 1 (platforms). New `sitOnGround` toggle (default on). Snap raycast now sorted by distance. |
| Rename `CharacterController` to `PlayerController` | `Scripts/Player/PlayerController.cs`, `Editor/PlayerControllerEditor.cs` | `.meta` GUIDs kept, scene objects renamed (one line each). Create Character now **upgrades** an existing character instead of duplicating it. Inspector has a **Player Model** slot. |
| Removed the dead old obstacle system | deleted `ObstacleLogic.cs` + `ObstacleLogicEditor.cs` | Unused by any scene, prefab or script (verified by GUID search). The prefab is still *named* ObstacleLogic. |
| Default models | `GameSettings` "Default Models", `PlayerController.CreateCharacterMesh`, populators `ApplyCustomModel` | Player = **Old Man**. Damage enemies: Wander = Spider, Patrol = Zombie, Static = Snake. Pickups keep their shapes. Only applied when an object is created. |
| Platform models | `Scripts/Platforms/PlatformModel.cs`, `PlatformLogicEditor` (🪨 slot), `PlatformController.customModel` + 🔁 all-platforms button, `GameSettings.defaultPlatformModel` | Replaces the cylinder and **builds MeshColliders from the model** (players stand on hills). The holder cancels the platform's non-uniform scale, then fits Width/Depth (keep proportions) with the top aligned to the old cylinder top. Follows sliders and moving/rotating/scaling platforms. Tested: player stands on it, pickup sits on it. |
| Platform movers that fall off | `PlatformObstacle` (walking/falling states, `FindGround`, `SwitchSurface`), `FollowPlatform.Detach/AttachTo`, `IObstacleAnchor` on Platform Wander/Patrol drivers | Movers look ahead and turn back at edges. If the ground goes anyway they detach, fall (kinematic, code-driven) and re-anchor to whatever they land or step onto: another platform (ride it) or the terrain (fixed world anchor). They are removed after 10 s of falling. Static obstacles are unchanged. Tested: huge ranges on a 3x3 platform, pushed off, stepping onto a moving platform, dropping onto a static platform. |
| Scenery (trees/rocks) | `Scripts/Environment/SceneryPopulator.cs` + editor; "🌲 Add Scenery Tool" button on TerrainPopulator | Scatter (count, size range, random turn, max slope, edge padding, tilt, sink) or Place One. Solid = MeshColliders from the model. Animators off, static flags set. Tested: 40 scattered within slope limits. |
| "Animations From" | `CustomModel.animationsFrom` | Re-skinned models with the same skeleton reuse another model's animations. |
| Tutorial script with YouTube chapters | `Tutorial/YouTube-Tutorial-Script.md` | **No longer needed** (teacher, 2026-11-10); kept but not maintained. It's missing tonight's features. |

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
- Small open points from 2026-11-10 (details in the session sections below):
  - a pickup on a platform once dropped about 5 m (not reproduced)
  - the old per-platform "Scatter" button in a platform's "More" fold-out still places things off round/turned platforms (the Game Builder scatter is fixed)
  - a very flat creature might count walking into it as a stomp
  - the painted-ground scatter filter is untested (the Tutorial has one terrain layer)
  - the homemade TestVideo logs a harmless colour warning
- Glow lights and floating sparkles cost performance in large numbers (each glow is a point light). Scenery groups warn about this in the tooltip.

## Driving the Editor from Claude (how verification was done)
- The Unity CLI (`unity`, v1.0.0-beta.17) + the **Pipeline package** (`com.unity.pipeline`, added this session) let Claude control the open Editor:
  - `unity command script eval --code '...'`
  - `unity command editor.playmode start|stop`
  - `unity command capture game_view --output-path <png>`
  - `unity command console get`
  - `unity recompile --project-path "E:/unity projects/classroomgamebuilding"`
- After package changes the Editor reloads. If commands fail with PIPELINE_UNREACHABLE, poll `unity status` until ready (or click into the Unity window).
- Tests were done on **unsaved** Level02 changes, then discarded by re-opening the scene via `EditorSceneManager.OpenScene` (no save prompt).
- Lessons from 2026-11-10:
  - **Edit-mode changes made by script are lost when Play starts unless the scene is marked dirty**: use `Undo.*` or `EditorSceneManager.MarkSceneDirty`, or set things in Play mode.
  - After `unity recompile`, wait a few seconds before `script eval` (the domain reload can swallow the first eval silently).
  - Teleport the player with `KinematicCharacterMotor.SetPosition` and call `GameManager.Instance.Resume()` first (the start menu freezes time).
  - Short-lived particles: set `Time.timeScale` low (e.g. 0.08) right after firing them, then `capture game_view`.
  - Selection/delayCall events wait while Unity is in the background; invoke the logic directly in tests.
  - `sed` replacements containing `\n` inside C# string literals inserted REAL newlines (broke compiles twice). Use the Edit tool for anything with `\n`.
  - To test a built .exe, read its `Player.log` (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\SchoolTeachingProject2_BRP\Player.log`). **Don't screenshot the desktop**: it captured the teacher's browser instead of the game.

## Tutorial Island
- **Built:** `Assets/Scenes/Tutorial.unity`, rebuilt by `Editor/TutorialLevelBuilder.cs` via **Game > Tutorial > Build Tutorial Level**. Never hand-edit the scene: change the builder and rebuild. It now has **17 stations** (see "session 5" below), its own terrain copy (`Assets/Tutorial/TutorialTerrain.asset`) and is added to Build Settings after the other levels (order: Level01, Level02, Tutorial).
- Verified: all pop-ups show, creatures stay on the arena, and the bounce ball launches when jumped onto (the teacher also confirmed it in the built game). NOT verified: bouncing by running into a ball's side.
- Fixed during the build: TriggerLogic fade-outs could hide the next sign's message (now one owner at a time), and PlatformLogic.TargetChild now finds the moving part on a just-created platform.
- **FIXED 2026-11-09: "built game jams up at the Finish sign".** Finish loads Level01, the empty template with no character, so no camera rendered and nothing could move. GameManager now has a `LevelNotReady` state: when a level has no playable player it shows the menu with "This level has no player yet!", a **Back to Start** button (loads build index 0; hidden when already there), Library and Quit. Verified in the Editor on Level01. Since then, Finish only changes level if Level01 is in the build (a Tutorial-only build just shows the Finish message).

- Tutorial texts now point at the Game Builder tabs (updated 2026-11-10 and level rebuilt).
- The builder used to delete + re-copy the scene, giving it a **new GUID on every rebuild** (Build Settings kept a stale one). It now overwrites the file in place, so the GUID is stable, and `AddToBuild` re-syncs the entry. Gotcha: `EditorBuildSettings.scenes` keeps a stale GUID if an entry with the same path already exists; remove the entry, assign, then add it back.

## Game Builder window + Student mode (BUILT 2026-11-10, teacher approved the design)
- **Game > 🎮 Game Builder** (`Editor/GameBuilderWindow.cs`). Tabs:
  - 🧙 **Player:** Create Character, model, movement/sprint/stamina/jump, health, fall damage.
  - 🏔 **Level:** terrain brushes (Raise/Lower, Smooth, Flatten, Paint Ground, Stamp, Paint Grass) via `ToolManager.SetActiveTool` (prefers the Terrain Tools `*Ovl` types), plus sun height/direction/colour/brightness, sky material, fog and world border.
  - 🪨 **Platforms:**
    - Quick platforms (Floating/Moving/Elevator/Spinning/Growing/Stepping stones/Crumbling/Bouncy)
    - Scatter platforms
    - a clickable list
    - the selected platform's "Make it:" movement and "Special:" type buttons, settings (incl. Effects), model, glow/sparkles
  - 🕷 **Creatures & Pickups:**
    - one-click Wanderer/Patroller/Guard, Health/Stamina/Shield/Super Jump/Speed Boost pickups and Bounce Ball. Each goes on the selected platform, or on the ground where the Scene view looks.
    - a list, the selected item's settings, glow/sparkles, model, Delete
    - "Scatter lots at once" groups
  - 🌲 **Scenery:** SceneryPopulator (groups with Glow/Sparkles options) + glow/sparkles for a selected piece.
  - 💬 **Signs & Story:** triggers list + edit (incl. Video pop-ups), checkpoints, and the GameSettings inspector (title, menu, Library).
  - 📦 **Levels & Build:** level order (tick, ▲▼, Open, ✕), New level / Copy level, Add level, Play/Stop, Build Game, open the build folder.
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

## Done 2026-11-10 (session 4): special platforms, jump-on creatures, glow & sparkles (commit `ba16c43`)
- **Crumbling / Bouncy platforms:**
  - New `PlatformLogic.platformType` (Normal / Crumbling / Bouncy), with `crumbleAfter` (shake time), `comeBackAfter` (0 = gone for good), `bounceStrength` and `colourIt`.
  - Crumbling = orange tint, Bouncy = green. The tint uses a MaterialPropertyBlock (not saved; re-applied in Update).
  - The runtime behaviour lives in `Scripts/Platforms/SpecialPlatform.cs`, added in `PlatformLogic.Start` when the type isn't Normal. It detects the player via KCC `GroundingStatus.GroundCollider`.
  - Game Builder Platforms tab: 💥 Crumbling and 🟢 Bouncy quick buttons, a "Special:" row on the selected platform, and Crumbling %/Bouncy % in Platform Scatter.
  - Verified: the platform crumbles under the player and comes back after 3 s; bouncy launches about 6 m.
- **Jump on creatures** (`ObstacleBase.jumpsToDefeat`, default 1; 0 = can't be beaten; `stompBounce`):
  - Side contact still hurts as before. Landing on its head bounces the player and counts a hit; at the set number of hits it squashes and vanishes with a sparkle burst.
  - Gotcha: when landing, KCC already grounds the player on the creature's **solid** collider, so the trigger often never fires, or fires with zero velocity and the player "on the ground". The stomp is therefore detected in `ObstacleBase.Update` (player `GroundCollider` is this creature) AND in `OnTriggerEnter` (feet ≥ solid top − 0.25 or standing on it).
  - Verified: 1-, 2- and 3-jump creatures, no damage from the top, 10 damage from the side.
  - Possible edge case: a very flat creature (under 0.25 m tall) might count walking into it as a stomp.
  - Scatter groups have "Jumps on head to beat".
- **Glow** (`Scripts/Game/Glow.cs`, on anything):
  - A point light on the object (visible while editing) plus surface emission in play (instanced materials).
  - Settings: colour, strength, range, pulse, pulse speed.
  - Game Builder "💡 Make it glow" on the selected creature/pickup, platform (goes on the moving TargetChild) and scenery piece.
  - New pickups from the one-click buttons glow automatically. Scatter groups and scenery groups have a Glow option (scenery: "Use these glow & sparkle settings on the ... already placed").
- **Sparkle bursts** (`Scripts/Game/Sparkles.cs`, particles made in code; additive material `Assets/Resources/SparkleMaterial.mat`, so it's always in builds; re-made by `Editor/SparkleAssets.cs` if deleted):
  - Fired on pickup collect, creature defeat, crumble and bounce.
  - Styles: Swirl (pickup default), Spray (defeat default), Ring, Fountain, Explosion, Twinkle, Sparks (stretched streaks), Shimmer.
  - Per-object settings: on/off, style, own colour, amount, size.
  - The obstacle Inspector (`Editor/ObstacleBaseEditor.cs`) hides settings that don't apply.
- **Floating sparkles** (`Scripts/Game/AmbientSparkles.cs`, on anything, play mode only):
  - "✨ Add sparkles around it" button.
  - Styles: Float Up, Swirl, Fireflies, Fountain, Twinkle, Explosions, Tornado, Rain, Sparks, Shimmer.
  - **From:** top / sides / bottom / centre (any mix).
  - colour + second colour, amount, size, fit-to-object or area size.
  - The particle systems live at the scene root and follow the object, so the object's scale doesn't stretch them.
  - Lesson: they must start OUTSIDE solid objects (inside a platform they were invisible). The additive material's `_TintColor` is 0.9 so they show in daylight.
- Tutorial: signs 5 and 7 mention bouncy/crumbling and jumping on heads, and the Tutorial's health ball now glows with centre sparkles.
- Verified in Play with screenshots: all 8 burst styles (in slow motion) and all 10 floating styles side by side, with no console errors or warnings. `Build/Windows` is rebuilt with 0 errors.
- Not done: the YouTube script doesn't cover these yet; glow/sparkles only show in Play (glow's light shows while editing).
- **Follow-up (same day): platform effects.** New `PlatformLogic` "Effects" settings:
  - `landEffect` (any platform, **optional**, default None)
  - `breakEffect` (crumbling, default Spray)
  - `effectColour`, `effectAmount`, `effectSize`
  - `warningFlash` + `flashEvery` (default 1 s) + `flashColour`: while shaking it flashes toward the flash colour and any Glow on its TargetChild flashes too (`Glow.flash`), so players can count down. Crumbling default `crumbleAfter` is now 2 s.
  - `Sparkles.Effect` (None + 8 styles) with `Sparkles.Play()` is the shared "effect choice".
  - `SpecialPlatform` is now also added to Normal platforms that have a land effect.
  - Switching a platform to Bouncy pre-sets landEffect Ring + green; switching to Crumbling pre-sets orange and crumbleAfter ≥ 2.
  - Glow's `pulseSpeed` was renamed to `pulsesPerSecond` (default 0.5).
  - Verified in Play: the flash (light 1.5 → 3.9), break after 2 s, back after 3 s; Sparks landing burst; Bouncy ring; no console errors.

## Done 2026-11-10 (session 5): Tutorial covers everything from this evening
- The course grows from 11 to **17 stations** (`Editor/TutorialLevelBuilder.cs`). The meadow is flattened over 295 m (was 200), so `Assets/Tutorial/TutorialTerrain.asset` really changed this time and is committed. New and renumbered stations:
  - **3 Videos**, plus a **"Watch me!"** sign that plays `Assets/MyVideos/TestVideo.mp4` full screen, once. Replace it with the teacher's clip later: set the `videoClip` load path in the builder.
  - **6 Glow & sparkles** corner, beside the path at x+8..15:
    - a "crystal" (Rock_Snow_3) that glows cyan with Swirl sparkles from its sides
    - an autumn tree with Fireflies
    - a shimmering mossy stump
    - flowers with a centre Fountain
  - **9 Bouncy** platform (low, bounce 22, Ring land effect).
  - **10 Crumbling** path: 3 crumbling platforms 1.5 m up, 6 m apart, 2 s crumble, back after 3 s.
  - **12 Jump on it!**: a still zombie (`AddStompCreature`) needing 2 jumps, with an Explosion burst.
  - **16 Levels**: New empty level / Copy / order / ticks.
  - Updated texts for Pop-ups, Platforms (Quick + Scatter), Creatures (Scatter), and Finish (Build Game).
- Verified in Play:
  - sign texts fit (2 lines)
  - the video plays and Skip returns to play
  - the glow corner is visible
  - bouncy launches, crumbling falls, and the stomp creature is beaten in 2 jumps
- Note: the homemade test clip logs one harmless "Color primaries 0 is unknown" warning when played; a normal camera/phone .mp4 won't.

## Done 2026-11-10 (session 6): Sounds & Music
Teacher asked: a Sounds tab to drag in sounds (attack, enemy attack, run, jump, sprint, damage, fall damage, enemy damage, player death, enemy death), choosing which objects a sound is for, a resizable soundtrack list, and an Esc > sound options screen with music and effects sliders. Mid-task they added: general defaults for player / enemies / items, PLUS pick a model and drag sounds onto its animations, with the model's sound overriding the general one ("if you've got a better way, go for it").
- **Data:** `Assets/Resources/GameSounds.asset` (`Scripts/Sound/GameSounds.cs`), the same for every level.
  - `sounds`: `SoundEntry` list (when, clip, volume, varyPitch, forEverything / onlyFor `SoundTarget` flags, onlyModel, repeatEvery). One general entry per `SoundEvent` is guaranteed by `SoundTools.EnsureAsset`.
  - `modelSounds`: per model, `AnimationSound` (animation clip, sound, volume, repeatEachLoop).
  - soundtrack list, playMusic, order (In order / Shuffle / Repeat one), musicInMenus, starting volumes.
- **Matching (`SoundManager.Pick`):** the most exact wins: onlyModel (+2) > kind filter (+1) > for everything (0); ties are random (variety). A model's own event sounds are SoundEntries with `onlyModel` set (edited only in the model panel; the general lists hide them).
- **Runtime:** `SoundManager` (auto, DontDestroyOnLoad, so music survives deaths/level loads). It has 16 pooled AudioSources; creature sounds are 3D (inaudible past 45 m, so they're skipped beyond that) and player sounds are 2D. The death sound ignores the listener pause. Music ignores the menu pause and ducks during full-screen videos. Volumes and order are in PlayerPrefs (`Sound.*`).
  - `PlayerSounds` (added to the KCC player): Run/Sprint steps (Sprint falls back to Run, faster) and Jump via the new `ExampleCharacterController.OnJumped` action (one line added to the KCC example script).
  - Hooks: `Health` (Damage / FallDamage via the new `TakeDamage(amount, fromFall)` / PlayerDeath), `ObstacleBase` (EnemyAttack, Attack + EnemyDamage on a stomp, EnemyDeath, PickUp, Bounce ball, plus new **CreatureMove** steps), `SpecialPlatform` (Bounce).
  - `CustomModel` plays animation sounds when an animation starts and on each loop (if ticked). If the model has its own walk/run/jump sound, the general Run/Sprint/Jump/CreatureMove sounds are skipped.
- **Esc menu:** a new "Sound & Music" button opens sliders (effects slider plays a preview), Back/Pause/Next, an order button, and a clickable song list. Esc goes back.
- **Game Builder > 🔊 Sounds** (`Editor/GameBuilderWindow.Sounds.cs` + `.ModelSounds.cs`, the window is now `partial`). Also on the menu: **Game > 🔊 Sounds and Music**.
  - General sounds folded by group: 🧙 Player / 👹 Creatures / ⭐ Items & objects. Each has ▶ preview, ✕, "➕ Another sound", and "For every creature" or tick boxes for kinds.
  - 🎭 Sounds for one model: a dropdown of Assets/MyModels (sub-folders as sub-menus, 📖 = has a Library entry) or drag a model in. It lists the animations the game plays (Idle/Walk/Run/Jump/Fall); the other clips (e.g. the Fox's Bark/Howl) are in a fold-out since the game never plays them. Below that are its own creature/player event sounds.
  - Music list: ▶ ▲ ▼ ✕, a drop zone for many songs at once, and "➕ Add a song".
  - "🎵 Fill empty slots with starter sounds": `SoundTools` writes simple beeps/thuds and two short chiptune songs (made in code, free to use) to `Assets/MySounds/Starter Sounds` and `Starter Music`. **They are currently assigned to every slot**; students replace them by dragging in their own sounds.
- **Tutorial:** sign 15 is now "Library & Sounds". The level was rebuilt.
- **Follow-up (same evening): extra animations + 🎯 Touching things.**
  - The general sounds are now grouped 🧙 Player / 🎯 Touching things (walk into a pickup, jump on a creature, bounce) / 👹 Creatures.
  - The model panel has a "⭐ When it's a pickup or bounce ball" part (walk into it / bounce off it).
  - **Extra animations** (ones the game doesn't pick by itself, e.g. the Zombie has about 190) each get a "Play it when" choice: `AnimationMoment` (Never / Now and then while standing / Creature: attacks you, gets hit, is beaten / Player: gets hurt, lands too hard), saved on `AnimationSound.playWhen` (Normal = the 5 auto slots).
    - The UI shows the already set-up ones, then a 🔍 search with the first 12 matches.
  - `CustomModel` has a new action layer (one extra mixer input): `PlayMoment(moment, out playedSound)` plays the clip once with a quick blend, plus its sound. If it had a sound, the general sound for that moment is skipped (`ObstacleBase.PlayOwnMoment`, `Health`). "It is beaten" with an animation plays it, then the creature vanishes (max 3 s) instead of squashing.
  - Hearing range now counts the player's position as well as the camera's (the camera lags after teleports/respawns).
  - `SoundManager.LogSounds = true` writes every sound moment to the Console (testing aid).
  - **Verified in Play by really walking into a pickup** (Pick Up played, pickup collected) **and dropping onto the "Jump On Me" zombie twice**:
    - Attack played on each stomp.
    - The first stomp played the model's own Hit_Head animation and sound instead of Enemy Damage.
    - The second stomp beat it: Death_B played, then it vanished.
    - The zombie patroller's own Zombie_Scratch "attacks you" animation also played. Test data removed afterwards.
  - NOT tested: "now and then" and the player's gets hurt / lands too hard animations (same code path).
- **Verified in Play:** damage, fall damage, jump; creature kind and model matching (model beats kind beats general); a model's idle-animation sound repeats each loop; music auto-advances; Next/Pause/Shuffle; Esc closes the Sound screen; the Sound screen looks right.
  - NOT heard by a person: I can't listen, so the starter sounds' quality is untested.
  - NOT verified: a real stomp/pickup by walking into things (the hooks are the same code paths as the sparkles, which were verified before), and the sounds in a built .exe.
- Lessons:
  - EditorWindow private fields survive a domain reload but Dictionaries don't: mark caches `[NonSerialized]`.
  - In IMGUI, always reserve the rect for an AssetPreview (it loads between the Layout and Repaint passes and breaks the layout).
  - The CLI console buffer keeps old errors; check their timestamps.

## Tutorial build for the class (2026-11-10)
- **Game > Tutorial > Build Tutorial Game (for the class)** (`TutorialLevelBuilder.BuildTutorialGame`): builds ONLY the Tutorial scene to `Build/Tutorial/TutorialIsland.exe`. The class's level order isn't touched. It clears the folder first and deletes Unity's `*DoNotShip` debug folder.
- `Build/TutorialIsland.zip` (76 MB) was made for sharing; `Build/` is git-ignored. On the teacher's request every other build (`Build/Windows` and an old loose build in `Build/`) was deleted to save space, then the teacher moved the zip elsewhere and `Build/Tutorial` was deleted too, so `Build/` is empty. Run Build Game again when the class needs the full game. The exe was launched and ran with 0 errors in its Player.log.
- `TriggerLogic` now skips a "Change Scene" to a level that isn't in the build (warning instead of an error). The Finish sign no longer promises a next level.
- The teacher said the **YouTube video script is no longer needed** (the Tutorial replaces it).

## Open requests
1. **Platforms, more options.** Presets, scatter, crumbling and bouncy are done. Still ideas: a "test this jump" helper, a per-platform colour picker.
2. ~~AI-made tutorial video~~: no longer needed (2026-11-10); the playable Tutorial Island replaces it.
3. Optional: test a real **Mixamo** character + animations in `MyModels/Humanoid` (the teacher offered to supply one).
4. Maybe: "✏️ Rename level" (would have to update triggers' `sceneToLoad` names).

## Assets notes
- Nature pack: `Assets/MyModels/NatureModelsPackTest/FBX` (150 CC0 Quaternius models). Its .blend/.obj copies are in `ModelSources/` (git-ignored; Blender isn't installed). Scenery groups quick-fill from it by name.
- Blender FBX roots carry a -90 X rotation and x100 scale; all model systems wrap the model in a holder instead of overwriting the root transform.
- The teacher plans to keep these rigs/animations and re-skin models later. No rock or platform models exist yet; `defaultPlatformModel` is empty, so new platforms are still cylinders.
- Videos go in `Assets/MyVideos` (.mp4, H.264). Keep them short, because they go into the build.

## Next steps checklist
- [ ] Teacher: class plays the Tutorial Island build; collect feedback (what's confusing, what they want).
- [ ] Teacher: send a real video to replace `TestVideo.mp4` on the Tutorial's "Watch me!" sign (then rebuild the Tutorial and the class build).
- [ ] Teacher: choose the level order for the full game in Levels & Build, then Build Game.
- [ ] Teacher to try the Game Builder with a student and report what's confusing.
- [ ] Open requests above (Mixamo test, more platform ideas).
- Git: commit only when the teacher says so (they approved committing on 2026-11-10). `Assets/Fonts/SueEllenFrancisco-Regular SDF.asset` and `Assets/Tutorial/TutorialTerrain.asset` change by themselves; leave them out of commits.

### Follow-up 2026-11-10: Game Builder look
- **Colour rule for buttons:** green = add something new, blue = big action on what's there (Play, Copy, Fill sounds, sculpt tools), red = delete/clear/Stop playing, grey = small helpers and changes to the selected thing (incl. ✔ Confirm message). The chosen option is highlighted light blue (0.6, 0.85, 1).
- **Theme:** `Editor/GameBuilderWindow.Theme.cs` paints a faint dark blue background, lighter blue `helpBox` boxes and toolbar (dark skin only). Unity's shared styles are swapped only inside `OnGUI` and restored in `finally`. Don't restyle `foldoutHeader`: its ▶ arrow is part of the background picture.
- **Gotcha:** an EditorWindow's private `GUIStyle` fields survive recompiles, so cached styles need `[System.NonSerialized]` or style changes don't show.
- **Signs:** ✔ Confirm message (under the pop-up text) saves the level; `Editor/TriggerLogicEditor.cs`.
- **Sounds tab model groups** fold open/closed; the player group only shows for player models (open level's player, the default player, or a model that already has player sounds).
