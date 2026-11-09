using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
using KinematicCharacterController.Examples;
#endif

/// <summary>
/// Runs the game flow: main menu at start, Esc to pause, death screen and level restart.
/// It creates itself automatically when the game starts, so you don't need to add it to a scene.
/// Settings are in Game > Settings and Library.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, StartMenu, Paused, Dead, LevelNotReady }

    public static GameManager Instance { get; private set; }
    public GameState State { get; private set; } = GameState.Playing;
    public GameSettings Settings { get; private set; }

    private static bool hasShownStartMenu;

    // Last checkpoint reached (survives the level reloading after a death)
    private static bool hasCheckpoint;
    private static string checkpointScene;
    private static Vector3 checkpointPosition;
    private static Quaternion checkpointRotation;

    private GameMenuUI ui;
    private float deathTimer;
    private bool isLoadingScene;
    private readonly List<Behaviour> disabledPlayerInputs = new List<Behaviour>();

    // ------------------------------------------------------------------ Startup

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Needed when "Enter Play Mode Options" skips domain reload
        Instance = null;
        hasShownStartMenu = false;
        hasCheckpoint = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("GameManager (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Settings = GameSettings.Load();
        ui = gameObject.AddComponent<GameMenuUI>();
        ui.Build(this, Settings);

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneReady(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) OnSceneReady(scene);
    }

    private void OnSceneReady(Scene scene)
    {
        isLoadingScene = false;
        disabledPlayerInputs.Clear();
        EnsureEventSystem();
        bool hasPlayer = DisableUnfinishedPlayers();

        // A checkpoint only counts in the level it was reached in
        if (hasCheckpoint && checkpointScene != scene.path) hasCheckpoint = false;
        if (hasCheckpoint) MovePlayerToCheckpoint();

        if (Settings.useWorldBorder)
        {
            WorldBorder.CreateFor(scene, Settings);
        }

        if (!hasPlayer)
        {
            // Without a player the level would just sit still and look frozen, so say why instead
            hasShownStartMenu = true;
            State = GameState.LevelNotReady;
            FreezeGame(true);
            ui.ShowLevelNotReady(scene.buildIndex != 0 && SceneManager.sceneCountInBuildSettings > 0);
            return;
        }

        if (Settings.showMenuOnStart && !hasShownStartMenu)
        {
            hasShownStartMenu = true;
            OpenMenu(GameState.StartMenu);
        }
        else
        {
            hasShownStartMenu = true;
            Resume();
        }
    }

    // ------------------------------------------------------------------ Every frame

    private void Update()
    {
        if (EscapePressed()) HandleEscape();

        if (State == GameState.Dead && Settings.restartDelay > 0f && !isLoadingScene)
        {
            deathTimer -= Time.unscaledDeltaTime;
            ui.SetDeathCountdown(deathTimer);
            if (deathTimer <= 0f) RespawnAfterDeath();
        }
    }

    private void LateUpdate()
    {
        // While any menu is open, keep the mouse free so buttons can be clicked
        if (State != GameState.Playing)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void HandleEscape()
    {
        switch (State)
        {
            case GameState.Playing:
                OpenMenu(GameState.Paused);
                break;
            case GameState.Paused:
                if (ui.IsLibraryOpen) CloseLibrary();
                else Resume();
                break;
            case GameState.StartMenu:
                if (ui.IsLibraryOpen) CloseLibrary();
                break;
            case GameState.Dead:
                break; // wait for OK or the countdown
        }
    }

    // ------------------------------------------------------------------ Menu actions

    public void OpenMenu(GameState menuState)
    {
        State = menuState;
        FreezeGame(true);
        ui.ShowMenu(menuState == GameState.StartMenu);
    }

    public void CloseLibrary()
    {
        if (State == GameState.StartMenu || State == GameState.Paused)
        {
            ui.ShowMenu(State == GameState.StartMenu);
        }
    }

    /// <summary>The big top button on the menu: Play / Resume, or Back to Start when the level isn't ready.</summary>
    public void PressPlayButton()
    {
        if (State == GameState.LevelNotReady) LoadFirstLevel();
        else Resume();
    }

    /// <summary>Go to the first level in the build (used when a level has no player yet).</summary>
    public void LoadFirstLevel()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;
        hasCheckpoint = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);
    }

    public void Resume()
    {
        State = GameState.Playing;
        ui.HideAll();
        FreezeGame(false);
    }

    /// <summary>Restart from the very beginning (forgets any checkpoint). Used by the pause menu.</summary>
    public void RestartLevel()
    {
        hasCheckpoint = false;
        ReloadLevel();
    }

    /// <summary>After dying: reload the level, putting the player at the last checkpoint if there is one.</summary>
    public void RespawnAfterDeath()
    {
        ReloadLevel();
    }

    private void ReloadLevel()
    {
        if (isLoadingScene) return;
        isLoadingScene = true;

        ui.HideAll();
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
#if UNITY_EDITOR
            // Scene isn't in Build Settings yet - still allow restarting while testing in the Editor
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.name);
#endif
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ Death

    /// <summary>Called by Health when the player's health reaches zero.</summary>
    public static void NotifyPlayerDied()
    {
        if (Instance != null) Instance.HandlePlayerDeath();
    }

    private void HandlePlayerDeath()
    {
        if (State == GameState.Dead || isLoadingScene) return;

        State = GameState.Dead;
        deathTimer = Settings.restartDelay;
        FreezeGame(true);
        ui.ShowDeath(Settings.restartDelay, hasCheckpoint);
    }

    // ------------------------------------------------------------------ Checkpoints

    /// <summary>Called by a Checkpoint flag when the player touches it.</summary>
    public static void ReachCheckpoint(Checkpoint checkpoint)
    {
        if (Instance == null || !Instance.Settings.useCheckpoints) return;

        hasCheckpoint = true;
        checkpointScene = checkpoint.gameObject.scene.path;
        checkpointPosition = checkpoint.SpawnPosition;
        checkpointRotation = checkpoint.SpawnRotation;
        Instance.ui.ShowToast(checkpoint.message);
    }

    private static void MovePlayerToCheckpoint()
    {
        // Turn the matching flag green again after the reload
        foreach (Checkpoint flag in FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            flag.SetReached(Vector3.Distance(flag.SpawnPosition, checkpointPosition) < 0.1f);
        }

#if HAS_KINEMATIC_CC
        KinematicCharacterMotor motor = FindAnyObjectByType<KinematicCharacterMotor>();
        if (motor != null)
        {
            motor.SetPositionAndRotation(checkpointPosition, checkpointRotation);
            return;
        }
#endif
        Health player = FindAnyObjectByType<Health>();
        if (player != null) player.transform.SetPositionAndRotation(checkpointPosition, checkpointRotation);
    }

    // ------------------------------------------------------------------ Helpers

    private void FreezeGame(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;
        AudioListener.pause = freeze;

        if (freeze)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Wait one frame so the player's own Start() (which locks the mouse) has run first
            StartCoroutine(DisablePlayerInputNextFrame());
        }
        else
        {
            StopAllCoroutines();
            EnablePlayerInput();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private IEnumerator DisablePlayerInputNextFrame()
    {
        yield return null;
        if (State == GameState.Playing) yield break;

#if HAS_KINEMATIC_CC
        // The player script locks the mouse when you click, which would break the menu buttons
        foreach (ExamplePlayer player in FindObjectsByType<ExamplePlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player.enabled)
            {
                player.enabled = false;
                disabledPlayerInputs.Add(player);
            }
        }
#endif
    }

    private void EnablePlayerInput()
    {
        foreach (Behaviour input in disabledPlayerInputs)
        {
            if (input != null) input.enabled = true;
        }
        disabledPlayerInputs.Clear();
    }

    /// <summary>
    /// A level with a player script but no character yet (e.g. the empty template) would spam
    /// red errors every frame. Switch that script off and leave one friendly hint instead.
    /// </summary>
    /// <summary>Turns off players that have no character yet. Returns true if the level has a playable player.</summary>
    private static bool DisableUnfinishedPlayers()
    {
#if HAS_KINEMATIC_CC
        bool hasPlayer = false;
        foreach (ExamplePlayer player in FindObjectsByType<ExamplePlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player.Character == null || player.CharacterCamera == null)
            {
                player.enabled = false;
                Debug.LogWarning("👋 This level has no player character yet. Select 'PlayerController' in the Hierarchy and click '🧙‍♂️ Create Character'.", player);
            }
            else
            {
                hasPlayer = true;
            }
        }
        return hasPlayer;
#else
        return true;
#endif
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem (auto)", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
