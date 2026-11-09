using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One place for students to change the menu, death screen, world border and Library.
/// Open it from the Unity menu bar: Game > Settings and Library.
/// The asset lives at Assets/Resources/GameSettings.asset so it is included in builds.
/// </summary>
[CreateAssetMenu(menuName = "Game/Game Settings", fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
    public const string ResourcePath = "GameSettings";

    [Header("Main Menu")]
    public string gameTitle = "Our Class Game";
    [TextArea(1, 3)] public string subtitle = "Made by our class";
    [Tooltip("Show the main menu when the game first starts.")]
    public bool showMenuOnStart = true;

    [Header("Death Screen")]
    public string deathTitle = "You Died!";
    [TextArea(2, 4)] public string deathMessage = "Don't give up! Have another go.";
    [Tooltip("Seconds before the level restarts by itself. Set to 0 to wait until the player clicks OK.")]
    [Range(0f, 30f)] public float restartDelay = 5f;

    [Header("Checkpoints")]
    [Tooltip("After dying, start again from the last checkpoint flag the player touched (instead of the very beginning).")]
    public bool useCheckpoints = true;

    [Header("World Border (stops the player falling off the map)")]
    public bool useWorldBorder = true;
    [Tooltip("Extra space between the edge of the level and the invisible walls.")]
    [Range(0f, 50f)] public float borderPadding = 1f;
    [Tooltip("How high above the tallest part of the level the invisible ceiling sits.")]
    [Range(5f, 500f)] public float skyLimitHeight = 60f;
    [Tooltip("If the player somehow falls this far below the lowest part of the level, they die.")]
    [Range(1f, 200f)] public float fallKillDepth = 30f;

    [Header("Default Models (used when you click Create Character or create enemies)")]
    [Tooltip("The model every new player gets. Swap it on PlayerController > Player Model.")]
    public GameObject defaultPlayerModel;
    [Tooltip("Model for new platforms (e.g. a floating rock). Leave empty for plain cylinders.")]
    public GameObject defaultPlatformModel;
    [Tooltip("Model for new enemies (Damage) that WANDER around.")]
    public GameObject defaultWanderEnemy;
    [Tooltip("Model for new enemies (Damage) that PATROL back and forth.")]
    public GameObject defaultPatrolEnemy;
    [Tooltip("Model for new enemies (Damage) that stay STILL.")]
    public GameObject defaultStaticEnemy;

    /// <summary>The default model for a new obstacle, or null to keep its plain shape (e.g. pickups).</summary>
    public GameObject DefaultObstacleModel(ObstacleBase.PayloadType payload, ObstacleSpawnType movement)
    {
        if (payload != ObstacleBase.PayloadType.Damage) return null; // pickups and buffs keep their shapes
        switch (movement)
        {
            case ObstacleSpawnType.Wander: return defaultWanderEnemy;
            case ObstacleSpawnType.Patrol: return defaultPatrolEnemy;
            default: return defaultStaticEnemy;
        }
    }

    [Header("Fonts (optional - leave empty to use the default font)")]
    public TMP_FontAsset titleFont;
    public TMP_FontAsset bodyFont;

    [Header("Library - the game's story, plus backstories for characters, creatures and objects")]
    [Tooltip("Story entries are shown as chapters in the Story tab, in list order. Use Category = Story for them.")]
    public List<LibraryEntry> libraryEntries = new List<LibraryEntry>();

    /// <summary>Loads the settings asset, or makes a temporary default one if it's missing.</summary>
    public static GameSettings Load()
    {
        GameSettings settings = Resources.Load<GameSettings>(ResourcePath);
        if (settings == null)
        {
            Debug.LogWarning("⚠️ GameSettings asset not found in a Resources folder. Using defaults. Open Game > Settings and Library to create it.");
            settings = CreateInstance<GameSettings>();
            settings.libraryEntries = DefaultLibrary();
        }
        return settings;
    }

    /// <summary>Starter entries so the Library isn't empty. Students should rewrite these!</summary>
    public static List<LibraryEntry> DefaultLibrary()
    {
        return new List<LibraryEntry>
        {
            StoryChapter("Chapter 1: The Fall",
                "Long ago, the floating islands drifted peacefully above a sea of mountains.\n\nThen one stormy night, the great crystal that held them in the sky cracked. Platforms began to wander, strange creatures appeared, and the people of the islands fled.\n\n(Replace this with your class's story!)"),
            StoryChapter("Chapter 2: The Hero Wakes",
                "When the storm cleared, one person was left behind: the Hero. With no memory of who they were, they set off to find the pieces of the crystal and put the sky back together.\n\nEvery level is one step closer to the truth..."),
            new LibraryEntry
            {
                name = "The Hero",
                category = LibraryCategory.Character,
                backstory = "Nobody knows where the Hero came from. One morning they simply woke up at the edge of the map with a strange urge to jump on things.\n\nThey can sprint (hold Left Shift) until they run out of breath, and they've learned the hard way that falling from very high up hurts.\n\n(Replace this with your own backstory!)"
            },
            new LibraryEntry
            {
                name = "Platform Patroller",
                category = LibraryCategory.Creature,
                backstory = "Patrollers walk the same path back and forth, day after day. They were once the guards of a floating castle, and they still haven't been told it fell down years ago.\n\nTouching one hurts, so time your jumps carefully."
            },
            new LibraryEntry
            {
                name = "Wanderer",
                category = LibraryCategory.Creature,
                backstory = "Wanderers have no idea where they're going and they don't care. They drift around their home area looking for something they lost a long time ago.\n\nThey bump into anyone who gets in the way."
            },
            new LibraryEntry
            {
                name = "Health Booster",
                category = LibraryCategory.Object,
                backstory = "A glowing snack left behind by a kind traveller. Grab one to get some health back."
            },
            new LibraryEntry
            {
                name = "Stamina Booster",
                category = LibraryCategory.Object,
                backstory = "Smells like energy drink and bad decisions. Refills your stamina so you can keep sprinting."
            },
            new LibraryEntry
            {
                name = "Jump & Sprint Buffs",
                category = LibraryCategory.Object,
                backstory = "Ancient charms that make your legs feel like springs for a short time. Jump buffs make you jump higher. Sprint buffs make you run faster."
            },
            new LibraryEntry
            {
                name = "Invincibility Charm",
                category = LibraryCategory.Object,
                backstory = "While this charm's magic lasts, nothing can hurt you. Use the time wisely!"
            },
            new LibraryEntry
            {
                name = "Bouncy Obstacle",
                category = LibraryCategory.Object,
                backstory = "Some rocks here are strangely rubbery. Run into one and you'll be launched into the air. Great for reaching high places, terrible for your dignity."
            },
        };
    }

    public static LibraryEntry StoryChapter(string title, string text)
    {
        return new LibraryEntry { name = title, category = LibraryCategory.Story, backstory = text };
    }
}

// Keep new categories at the END so existing entries keep their category
public enum LibraryCategory
{
    Character,
    Creature,
    Object,
    Story
}

[System.Serializable]
public class LibraryEntry
{
    public string name = "New Entry";
    public LibraryCategory category = LibraryCategory.Creature;
    [Tooltip("Optional picture shown in the Library (a Sprite).")]
    public Sprite picture;
    [TextArea(4, 12)] public string backstory = "Write the backstory here...";
}
