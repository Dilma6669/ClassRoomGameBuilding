# Class Game Builder: YouTube Tutorial Plan

A recording script for a single tutorial video (about 26 minutes) that walks students through the project.
Each **chapter** below becomes a YouTube chapter marker. Record in this order, then fix the
timestamps in the description once the video is edited.

---

## 1. Paste this into the YouTube description

YouTube turns these lines into clickable chapter markers automatically. The rules are:
the first one must be `00:00`, there must be at least 3, and each chapter must be at least 10 seconds long.
The times below are estimates. Update them to match your finished video.

```
00:00 Intro: what we're making
00:45 Opening the project & the Hierarchy tour
02:15 Making the ground (Terrain)
04:00 Adding the player (Create Character)
05:45 Controls: move, jump, sprint & camera
06:45 Floating & moving platforms
09:00 Obstacles, enemies & pickups
12:30 Your own 3D models & animations
16:30 Trigger zones: messages & changing level
18:15 Checkpoints
19:15 Game Settings: title, menu & death screen
20:45 Writing the story & backstories for the Library
22:45 The world border (no falling off the map)
23:45 Adding a new level
24:30 Building the game so anyone can play it
25:45 Wrap-up & challenges
```

---

## 2. Recording checklist

- [ ] Unity open at **1920x1080**, Editor layout reset (Window > Layouts > Default)
- [ ] Zoom the Inspector text if possible (it's small on video)
- [ ] Start from a fresh copy of **Level01** (it's the empty template)
- [ ] Close Discord, email and other notifications
- [ ] Have **Level02** ready to show as a "finished example"
- [ ] Have one model ready to drag in (e.g. a free animated animal from quaternius.com or a Mixamo character), already downloaded but NOT yet in the project, so you can show the whole import live

---

## 3. Chapter-by-chapter script

> **Say:** lines are suggested narration. **Do:** lines are what to show on screen.

### 00:00 Intro: what we're making
- **Show:** 15 seconds of Level02 gameplay: jumping across platforms, getting bounced, dying, the death screen, the Library.
- **Say:** "In this video we're going to build our own 3D platformer level from scratch. You won't need to write any code. Everything is done with buttons in the Inspector."

### 00:45 Opening the project & the Hierarchy tour
- **Do:** Open the project from Unity Hub, then open `Assets/Scenes/Level01`.
- **Do:** Click through each object in the Hierarchy:
  - **GameController** holds the game settings and makes checkpoints
  - **LightingController** holds the sun/lights
  - **TerrainController** makes the ground
  - **PlayerController** makes the player
  - **PlatformController** makes floating platforms
  - **CanvasController / TriggerController** handle on-screen messages and trigger zones
- **Say:** "Every 'Controller' has a big green button in the Inspector. That's how we build things."

### 02:15 Making the ground (Terrain)
- **Do:** Select **TerrainController**, then click **⛰️ Create Terrain**.
- **Do:** Show the terrain sculpting tools (raise/lower, smooth, paint texture) for about 30 seconds.
- **Do:** Show the Terrain Populator: **➕ Create Single Obstacle** and **🎲 Scatter Obstacles** to drop things on the ground. **🗑️ Clear All Obstacles** removes them.
- **Say:** "Don't move the terrain itself. It snaps back into place on purpose."

### 04:00 Adding the player (Create Character)
- **Do:** Select **PlayerController**, then click **🧙‍♂️ Create Character**.
- **Do:** Move the PlayerController to where the player should start.
- **Do:** Show the **PlayerLogic** settings: Move Speed, Jump Height, Sprint, Stamina.
- **Say:** "Change Jump Height and press Play to feel the difference. Small changes make a big difference!"

### 05:45 Controls: move, jump, sprint & camera
- **Do:** Press Play and demonstrate:
  - **W A S D**: move
  - **Space**: jump
  - **Left Shift**: sprint (watch the yellow stamina bar)
  - **Mouse**: look around
  - **Mouse wheel**: zoom
  - **Right-click**: first-person toggle
  - **C**: crouch
  - **Esc**: open the menu
- **Say:** "Falling from too high hurts. Watch the green health bar."

### 06:45 Floating & moving platforms
- **Do:** Select **PlatformController**, then click **🏗️ Create New Platform**.
- **Do:** Change size, rotation and the starting offset. Turn on movement and show a platform moving back and forth.
- **Do:** **📋 Duplicate Platform Setup** to make a copy quickly. **❌ Delete Platform Setup** removes one.
- **Do:** Press Play and ride a moving platform.
- **Say:** "Make sure the gaps are jumpable! Test every jump yourself."

### 09:00 Obstacles, enemies & pickups
- **Do:** On a platform, click **➕ Create Single Obstacle**, then select it.
- **Do:** Walk through the main settings:
  - **Movement Type**
    - *Static*: stays still
    - *Patrol*: walks back and forth
    - *Random Wander*: wanders around
  - **Payload Type**: what happens when the player touches it
    - *Damage*: hurts the player
    - *Health Booster*: heals
    - *Stamina Booster*: refills stamina
    - *Invincibility Buff*: no damage for a while
    - *Jump Buff*: jump higher
    - *Sprint Buff*: run faster
  - **Destroy On Trigger**: turns it into a one-time pickup
  - **Is Bouncy**: makes it a trampoline that launches the player
- **Do:** Use **🎲 Scatter Obstacles** to fill a platform with enemies.
- **Do:** Show **Sit On Ground** (on by default): obstacles of any size sit on top of the ground instead of half inside it.
- **Say:** "Mix enemies and pickups so the level is hard but fair."

### 12:30 Your own 3D models & animations
- **Do:** Show the **Assets/MyModels** folder and the **HOW TO ADD MODELS** help file inside it.
- **Do:** Drag a downloaded model file (`.fbx` or `.glb`) from Windows Explorer into **MyModels**. Put its texture images (`.png`) in the same folder.
  > **Where to get free models:**
  > - **quaternius.com**: free animated animals and monsters (any rig works)
  > - **kenney.nl**: free characters and objects
  > - **mixamo.com**: free animated people. Put these in **MyModels/Humanoid**, and tick **"In Place"** when downloading walking and running animations.
- **Do (player):** Select **PlayerController**, then drag the model into **Player Model**. Press Play: it walks, runs, jumps and falls by itself.
- **Do (one obstacle):** Select an obstacle, then drag the model into **Model** on its **Custom Model** component.
- **Do (lots of obstacles):** On a Platform or Terrain populator, drag the model into **Custom Model (3D model)** *before* clicking **🎲 Scatter**.
- **Do:** Show the sliders:
  - **Size**: bigger or smaller
  - **Turn**: if it faces backwards
  - **Raise / Lower**: if it floats or sinks
- **Do:** Open **🏃 Animations** and show that **Idle / Walk / Run / Jump / Fall** filled in by themselves. Explain that animations are matched by name. If one is missing, click **🔍 Find Animations Again** or drag a clip into the box.
- **Do:** Click **📸 Add to Library (with picture)**. Show the new Library entry with its picture.
- **Say:** "The model is just the costume. The obstacle still hurts, heals, or bounces you exactly the same."

### 16:30 Trigger zones: messages & changing level
- **Do:** Select **TriggerController**, then click **Create Trigger**. Move it with the offset sliders.
- **Do:** Type a message (e.g. "Watch out for the spinning rocks!"). Press Play and walk into it.
- **Do:** Tick **Change Scene** and pick a **Target Scene** so the trigger becomes a level exit.
- **Say:** "A trigger at the end of your level can send the player to the next level."

### 18:15 Checkpoints
- **Do:** Point the Scene view at a good spot. Select **GameController** and click **🚩 Create Checkpoint**. You can also use **Game > Create Checkpoint**.
- **Do:** Press Play and run through the flag. It turns green and shows **"Checkpoint!"**.
- **Do:** Die on purpose. The death screen says **"Back to your checkpoint..."** and the player restarts at the flag.
- **Say:** "Dying puts you back at the last flag, but the whole level resets. Restart Level in the pause menu goes all the way back to the start."

### 19:15 Game Settings: title, menu & death screen
- **Do:** Select **GameController** and click **⚙️ Game Settings & Library**. You can also use the top menu bar: **Game > Settings and Library**.
- **Do:** Change **Game Title** and **Subtitle**. Press Play and show the main menu: **Play / Library / Quit Game**.
- **Do:** Press **Esc** mid-game to show the pause menu: **Resume / Restart Level / Library / Quit Game**.
- **Do:** Change **Death Title**, **Death Message** and **Restart Delay**. Die and show the death screen. Click **OK** or wait.
- **Say:** "Set Restart Delay to 0 if you want the game to wait until the player clicks OK. Untick Use Checkpoints if you want every death to start the level over."

### 20:45 Writing the story & backstories for the Library
- **Do:** In **Game > Settings and Library**, open **Library Entries**.
- **Do:** Show an entry's fields:
  - **Name**
  - **Category**: Story, Character, Creature or Object
  - **Picture**: optional
  - **Backstory**
- **Do:** Add a **Story** chapter (e.g. "Chapter 3: The Cave"). Story chapters show in list order, so drag them to reorder.
- **Do:** Add a creature's backstory. Point out that **📸 Add to Library** on a model does the picture for you.
- **Do:** Press Play, then **Library**, and show the tabs: **Story / All / Characters / Creatures / Objects**.
- **Say:** "Every creature and object should have a backstory. Who are they? Why are they here? And what's the story of the whole game?"

### 22:45 The world border (no falling off the map)
- **Do:** Press Play and run to the edge of the map. Show the invisible wall, then bounce up high to hit the sky limit.
- **Do:** In Game Settings, show **Border Padding**, **Sky Limit Height** and **Fall Kill Depth**.
- **Say:** "The border sizes itself to fit your terrain and platforms automatically. If you somehow fall out of the world, you die and restart."

### 23:45 Adding a new level
- **Do:** Duplicate Level01 in the Project window (Ctrl+D) and rename it **Level03**.
- **Do:** Open it, then **Game > Add Open Scene to Build**.
- **Say:** "The first level in the build list is where the game starts. Use a Trigger with Change Scene to link levels together."

### 24:30 Building the game so anyone can play it
- **Do:** **Game > Build Game (Windows)**. Wait for it to finish, then click **Show in Explorer**.
- **Do:** Double-click the `.exe` and play the built game.
- **Say:** "To share your game, copy the WHOLE folder, not just the .exe. The game needs the other files next to it."

### 25:45 Wrap-up & challenges
- **Say:** "Here are some challenges to try:"
  1. Build a level with at least 5 platforms, 1 moving platform, a checkpoint and a trigger message.
  2. Give your player and at least 2 creatures their own 3D models.
  3. Add each creature to the Library with a picture, and write its backstory.
  4. Write the first chapter of your game's story.
  5. Get a friend to play your built game. Can they finish it?

---

## 4. Suggested video title & thumbnail

- **Title:** "Build Your Own 3D Platformer in Unity (No Coding!) | Class Game Builder Tutorial"
- **Thumbnail:** your animated player mid-jump over a gap, with big text: "NO CODE!"
