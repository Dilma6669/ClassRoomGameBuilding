using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds and shows the Main/Pause menu, the Library and the Death screen.
/// Everything is created from code, so it works in every level without setup.
/// Change the words and Library entries in Game > Settings and Library.
/// </summary>
public class GameMenuUI : MonoBehaviour
{
    private GameManager manager;
    private GameSettings settings;
    private TMP_FontAsset titleFont;
    private TMP_FontAsset bodyFont;

    // Screens
    private GameObject menuScreen;
    private GameObject libraryScreen;
    private GameObject deathScreen;
    private GameObject soundScreen;

    // Sound widgets
    private Slider musicSlider;
    private Slider effectsSlider;
    private TextMeshProUGUI nowPlaying;
    private TextMeshProUGUI pauseButtonLabel;
    private TextMeshProUGUI orderButtonLabel;
    private Button repeatButton;
    private Button shuffleButton;
    private TextMeshProUGUI repeatButtonLabel;
    private RectTransform trackListContent;
    private readonly List<Image> trackImages = new List<Image>();
    private float lastPreviewTime;

    // Menu widgets
    private TextMeshProUGUI menuTitle;
    private TextMeshProUGUI menuSubtitle;
    private TextMeshProUGUI playButtonLabel;
    private GameObject playButton;
    private GameObject restartButton;

    // Library widgets
    private RectTransform libraryListContent;
    private ScrollRect libraryDetailScroll;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailCategory;
    private Image detailPicture;
    private LayoutElement detailPictureLayout;
    private TextMeshProUGUI detailBackstory;
    private readonly List<Image> tabImages = new List<Image>();
    private readonly List<Image> listItemImages = new List<Image>();
    private int currentTab; // index into TabNames (0 = Story, opened first)

    private static readonly string[] TabNames = { "Story", "All", "Characters", "Creatures", "Objects" };

    private static bool TabShows(int tab, LibraryCategory category)
    {
        switch (tab)
        {
            case 0: return category == LibraryCategory.Story;
            case 1: return category != LibraryCategory.Story; // everything except story chapters
            case 2: return category == LibraryCategory.Character;
            case 3: return category == LibraryCategory.Creature;
            case 4: return category == LibraryCategory.Object;
            default: return false;
        }
    }

    // Death widgets
    private TextMeshProUGUI deathCountdown;
    private bool deathFromCheckpoint;

    // "Checkpoint!" pop-up
    private CanvasGroup toastGroup;
    private TextMeshProUGUI toastText;
    private Coroutine toastRoutine;

    public bool IsLibraryOpen => libraryScreen != null && libraryScreen.activeSelf;
    public bool IsSoundOpen => soundScreen != null && soundScreen.activeSelf;

    public void Build(GameManager gameManager, GameSettings gameSettings)
    {
        manager = gameManager;
        settings = gameSettings;
        titleFont = settings.titleFont;
        bodyFont = settings.bodyFont;

        GameObject canvasGO = new GameObject("GameMenuCanvas", typeof(RectTransform));
        canvasGO.layer = 5;
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // draw on top of the HUD and dialog canvases

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        menuScreen = BuildMenuScreen(canvasGO.transform);
        libraryScreen = BuildLibraryScreen(canvasGO.transform);
        deathScreen = BuildDeathScreen(canvasGO.transform);
        soundScreen = BuildSoundScreen(canvasGO.transform);
        BuildToast(canvasGO.transform);

        HideAll();
    }

    // ------------------------------------------------------------------ Public API

    public void ShowMenu(bool isStartMenu)
    {
        HideAll();
        menuTitle.text = settings.gameTitle;
        menuSubtitle.text = isStartMenu ? settings.subtitle : "Paused";
        playButtonLabel.text = isStartMenu ? "Play" : "Resume";
        restartButton.SetActive(!isStartMenu);
        playButton.SetActive(true);
        menuScreen.SetActive(true);
    }

    /// <summary>Shown when a level has no player character yet, so it doesn't just look frozen.</summary>
    public void ShowLevelNotReady(bool canGoBackToStart)
    {
        ShowMenu(true);
        menuSubtitle.text = "This level has no player yet!";
        playButtonLabel.text = "Back to Start";
        playButton.SetActive(canGoBackToStart);
    }

    public void ShowLibrary()
    {
        HideAll();
        libraryScreen.SetActive(true);
        SelectTab(currentTab);
    }

    public void ShowDeath(float countdownSeconds, bool fromCheckpoint)
    {
        HideAll();
        deathFromCheckpoint = fromCheckpoint;
        deathCountdown.gameObject.SetActive(countdownSeconds > 0f);
        SetDeathCountdown(countdownSeconds);
        deathScreen.SetActive(true);
    }

    public void SetDeathCountdown(float secondsLeft)
    {
        if (deathCountdown == null) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
        deathCountdown.text = (deathFromCheckpoint ? "Back to your checkpoint in " : "Restarting in ") + seconds + "...";
    }

    public void HideAll()
    {
        if (menuScreen != null) menuScreen.SetActive(false);
        if (libraryScreen != null) libraryScreen.SetActive(false);
        if (deathScreen != null) deathScreen.SetActive(false);
        if (soundScreen != null) soundScreen.SetActive(false);

        // Stop a menu button staying "selected" (Space/Enter would press it again)
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // ------------------------------------------------------------------ Menu

    private GameObject BuildMenuScreen(Transform canvas)
    {
        Image overlay = UIBuilder.Image(canvas, "MenuScreen", new Color(0, 0, 0, 0.6f));
        UIBuilder.Stretch(overlay.rectTransform);

        Image panel = UIBuilder.Image(overlay.transform, "Panel", UIBuilder.PanelColor);
        RectTransform panelRT = panel.rectTransform;
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(640, 0);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 40);
        layout.spacing = 16;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        menuTitle = UIBuilder.Text(panel.transform, "Title", settings.gameTitle, 72, titleFont, UIBuilder.TextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(menuTitle, 110);

        menuSubtitle = UIBuilder.Text(panel.transform, "Subtitle", settings.subtitle, 30, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(menuSubtitle, 56);

        Button play = UIBuilder.Button(panel.transform, "Play", bodyFont, 32, UIBuilder.AccentColor, () => manager.PressPlayButton());
        UIBuilder.Layout(play, 72);
        playButtonLabel = play.GetComponentInChildren<TextMeshProUGUI>();
        playButton = play.gameObject;

        Button restart = UIBuilder.Button(panel.transform, "Restart Level", bodyFont, 30, UIBuilder.ButtonColor, () => manager.RestartLevel());
        UIBuilder.Layout(restart, 64);
        restartButton = restart.gameObject;

        Button library = UIBuilder.Button(panel.transform, "Library", bodyFont, 30, UIBuilder.ButtonColor, () => ShowLibrary());
        UIBuilder.Layout(library, 64);

        Button sound = UIBuilder.Button(panel.transform, "Sound & Music", bodyFont, 30, UIBuilder.ButtonColor, () => ShowSound());
        UIBuilder.Layout(sound, 64);

        Button quit = UIBuilder.Button(panel.transform, "Quit Game", bodyFont, 30, UIBuilder.DangerColor, () => manager.QuitGame());
        UIBuilder.Layout(quit, 64);

        TextMeshProUGUI hint = UIBuilder.Text(panel.transform, "Hint", "Press Esc during the game to open this menu", 22, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(hint, 40);

        return overlay.gameObject;
    }

    // ------------------------------------------------------------------ Library

    private GameObject BuildLibraryScreen(Transform canvas)
    {
        Image overlay = UIBuilder.Image(canvas, "LibraryScreen", new Color(0, 0, 0, 0.7f));
        UIBuilder.Stretch(overlay.rectTransform);

        Image panel = UIBuilder.Image(overlay.transform, "Panel", UIBuilder.PanelColor);
        UIBuilder.Stretch(panel.rectTransform, 80, 80, 60, 60);
        Transform p = panel.transform;

        // Header
        TextMeshProUGUI header = UIBuilder.Text(p, "Header", "Library", 64, titleFont, UIBuilder.TextColor, TextAlignmentOptions.Left);
        RectTransform headerRT = header.rectTransform;
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0, 1);
        headerRT.offsetMin = new Vector2(40, -110);
        headerRT.offsetMax = new Vector2(-300, -20);

        Button back = UIBuilder.Button(p, "Back", bodyFont, 28, UIBuilder.ButtonColor, () => manager.CloseLibrary());
        RectTransform backRT = (RectTransform)back.transform;
        backRT.anchorMin = backRT.anchorMax = backRT.pivot = new Vector2(1, 1);
        backRT.anchoredPosition = new Vector2(-40, -32);
        backRT.sizeDelta = new Vector2(200, 64);

        // Category tabs
        RectTransform tabs = UIBuilder.Create("Tabs", p);
        tabs.anchorMin = new Vector2(0, 1);
        tabs.anchorMax = new Vector2(1, 1);
        tabs.pivot = new Vector2(0, 1);
        tabs.offsetMin = new Vector2(40, -184);
        tabs.offsetMax = new Vector2(-40, -124);
        HorizontalLayoutGroup tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 12;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = true;

        for (int i = 0; i < TabNames.Length; i++)
        {
            int index = i;
            Button tab = UIBuilder.Button(tabs, TabNames[i], bodyFont, 26, UIBuilder.ButtonColor, () => SelectTab(index));
            UIBuilder.Layout(tab, -1, 220);
            tabImages.Add(tab.GetComponent<Image>());
        }

        // Left: list of entries
        Image listBG = UIBuilder.Image(p, "ListBackground", UIBuilder.PanelLightColor);
        RectTransform listRT = listBG.rectTransform;
        listRT.anchorMin = new Vector2(0, 0);
        listRT.anchorMax = new Vector2(0, 1);
        listRT.pivot = new Vector2(0, 0.5f);
        listRT.offsetMin = new Vector2(40, 40);
        listRT.offsetMax = new Vector2(40 + 440, -204);
        libraryListContent = UIBuilder.ScrollArea(listBG.transform, "List", out _, 8, 12);
        UIBuilder.Stretch((RectTransform)libraryListContent.parent.parent);

        // Right: details for the selected entry
        Image detailBG = UIBuilder.Image(p, "DetailBackground", UIBuilder.PanelLightColor);
        UIBuilder.Stretch(detailBG.rectTransform, 40 + 440 + 24, 40, 204, 40);

        RectTransform detailContent = UIBuilder.ScrollArea(detailBG.transform, "Detail", out libraryDetailScroll, 12, 32);
        UIBuilder.Stretch((RectTransform)detailContent.parent.parent);

        detailName = UIBuilder.Text(detailContent, "Name", "", 54, titleFont, UIBuilder.TextColor, TextAlignmentOptions.Left);
        detailCategory = UIBuilder.Text(detailContent, "Category", "", 26, bodyFont, UIBuilder.AccentColor, TextAlignmentOptions.Left);

        detailPicture = UIBuilder.Image(detailContent, "Picture", Color.white);
        detailPicture.preserveAspect = true;
        detailPictureLayout = UIBuilder.Layout(detailPicture, 320);

        detailBackstory = UIBuilder.Text(detailContent, "Backstory", "", 30, bodyFont, UIBuilder.TextColor, TextAlignmentOptions.TopLeft);

        return overlay.gameObject;
    }

    private void SelectTab(int index)
    {
        currentTab = index;
        for (int i = 0; i < tabImages.Count; i++)
        {
            tabImages[i].color = i == index ? UIBuilder.AccentColor : UIBuilder.ButtonColor;
        }
        RebuildList();
    }

    private void RebuildList()
    {
        // Remove old list buttons
        for (int i = libraryListContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = libraryListContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        listItemImages.Clear();

        List<LibraryEntry> visible = new List<LibraryEntry>();
        foreach (LibraryEntry entry in settings.libraryEntries)
        {
            if (entry == null) continue;
            if (TabShows(currentTab, entry.category)) visible.Add(entry);
        }

        if (visible.Count == 0)
        {
            TextMeshProUGUI empty = UIBuilder.Text(libraryListContent, "Empty", "Nothing here yet!", 26, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Center);
            UIBuilder.Layout(empty, 60);
            ShowEntry(null);
            return;
        }

        for (int i = 0; i < visible.Count; i++)
        {
            LibraryEntry entry = visible[i];
            int itemIndex = i;
            Button item = UIBuilder.Button(libraryListContent, entry.name, bodyFont, 28, UIBuilder.ButtonColor, null, TextAlignmentOptions.Left);
            UIBuilder.Layout(item, 64);
            Image img = item.GetComponent<Image>();
            listItemImages.Add(img);
            item.onClick.AddListener(() =>
            {
                HighlightListItem(itemIndex);
                ShowEntry(entry);
            });
        }

        HighlightListItem(0);
        ShowEntry(visible[0]);
    }

    private void HighlightListItem(int index)
    {
        for (int i = 0; i < listItemImages.Count; i++)
        {
            listItemImages[i].color = i == index ? UIBuilder.AccentColor : UIBuilder.ButtonColor;
        }
    }

    private void ShowEntry(LibraryEntry entry)
    {
        if (entry == null)
        {
            detailName.text = "Library";
            detailCategory.text = "";
            detailPicture.gameObject.SetActive(false);
            detailBackstory.text = currentTab == 0
                ? "The story hasn't been written yet!\n\nTo add a chapter, open Game > Settings and Library in the Unity menu bar, add a Library Entry, and set its Category to Story. Chapters appear in list order."
                : "There are no entries in this section yet.\n\nTo add some, open Game > Settings and Library in the Unity menu bar and add to the Library Entries list.";
        }
        else
        {
            detailName.text = entry.name;
            detailCategory.text = entry.category.ToString().ToUpperInvariant();
            detailPicture.sprite = entry.picture;
            detailPicture.gameObject.SetActive(entry.picture != null);
            detailBackstory.text = entry.backstory;
        }

        if (detailPictureLayout != null) detailPictureLayout.preferredHeight = 320;
        if (libraryDetailScroll != null) libraryDetailScroll.verticalNormalizedPosition = 1f;
    }

    // ------------------------------------------------------------------ Sound & Music

    public void ShowSound()
    {
        HideAll();
        musicSlider.SetValueWithoutNotify(SoundManager.MusicVolume);
        effectsSlider.SetValueWithoutNotify(SoundManager.EffectsVolume);
        RebuildTrackList();
        soundScreen.SetActive(true);
        RefreshSoundScreen();
    }

    private GameObject BuildSoundScreen(Transform canvas)
    {
        Image overlay = UIBuilder.Image(canvas, "SoundScreen", new Color(0, 0, 0, 0.7f));
        UIBuilder.Stretch(overlay.rectTransform);

        Image panel = UIBuilder.Image(overlay.transform, "Panel", UIBuilder.PanelColor);
        RectTransform panelRT = panel.rectTransform;
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(900, 940);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 32, 36);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Transform p = panel.transform;

        TextMeshProUGUI title = UIBuilder.Text(p, "Title", "Sound & Music", 60, titleFont, UIBuilder.TextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(title, 84);

        UIBuilder.Layout(UIBuilder.Text(p, "MusicLabel", "Music volume", 28, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Left), 36);
        musicSlider = UIBuilder.Slider(p, "MusicSlider", SoundManager.MusicVolume, v => SoundManager.MusicVolume = v);
        UIBuilder.Layout(musicSlider, 56);

        UIBuilder.Layout(UIBuilder.Text(p, "EffectsLabel", "Sound effects volume", 28, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Left), 36);
        effectsSlider = UIBuilder.Slider(p, "EffectsSlider", SoundManager.EffectsVolume, v =>
        {
            SoundManager.EffectsVolume = v;
            // A little sound so you can hear how loud it is (not on every tiny move)
            if (Time.unscaledTime - lastPreviewTime > 0.25f && SoundManager.Instance != null)
            {
                lastPreviewTime = Time.unscaledTime;
                SoundManager.Instance.PreviewEffects();
            }
        });
        UIBuilder.Layout(effectsSlider, 56);

        nowPlaying = UIBuilder.Text(p, "NowPlaying", "", 30, bodyFont, UIBuilder.TextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(nowPlaying, 56);

        // Music player buttons
        RectTransform row = UIBuilder.Create("MusicButtons", p);
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        UIBuilder.Layout(row, 64, -1, 0); // don't grow (the song list gets the spare space)

        UIBuilder.Button(row, "< Back", bodyFont, 26, UIBuilder.ButtonColor, () => { if (SoundManager.Instance != null) SoundManager.Instance.PreviousTrack(); RefreshSoundScreen(); });
        Button pause = UIBuilder.Button(row, "Pause", bodyFont, 26, UIBuilder.AccentColor, () => { if (SoundManager.Instance != null) SoundManager.Instance.ToggleMusicPause(); RefreshSoundScreen(); });
        pauseButtonLabel = pause.GetComponentInChildren<TextMeshProUGUI>();
        UIBuilder.Button(row, "Next >", bodyFont, 26, UIBuilder.ButtonColor, () => { if (SoundManager.Instance != null) SoundManager.Instance.NextTrack(); RefreshSoundScreen(); });
        // Shuffle: play the songs in a mixed-up order (lit up when it's on; off = in order)
        shuffleButton = UIBuilder.Button(row, "Shuffle: Off", bodyFont, 26, UIBuilder.ButtonColor, () => CycleOrder());
        orderButtonLabel = shuffleButton.GetComponentInChildren<TextMeshProUGUI>();
        // Repeat: keep playing the same song (lit up when it's on)
        repeatButton = UIBuilder.Button(row, "Repeat: Off", bodyFont, 26, UIBuilder.ButtonColor, () =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.RepeatSong = !SoundManager.Instance.RepeatSong;
            RefreshSoundScreen();
        });
        repeatButtonLabel = repeatButton.GetComponentInChildren<TextMeshProUGUI>();

        // The songs: click one to play it
        Image listBG = UIBuilder.Image(p, "TrackListBackground", UIBuilder.PanelLightColor);
        UIBuilder.Layout(listBG, -1, -1, 1);
        trackListContent = UIBuilder.ScrollArea(listBG.transform, "Tracks", out _, 6, 10);
        UIBuilder.Stretch((RectTransform)trackListContent.parent.parent);

        Button back = UIBuilder.Button(p, "Done", bodyFont, 30, UIBuilder.AccentColor, () => manager.CloseLibrary());
        UIBuilder.Layout(back, 64);

        return overlay.gameObject;
    }

    private void CycleOrder()
    {
        SoundManager sm = SoundManager.Instance;
        if (sm == null) return;
        sm.Order = sm.Order == GameSounds.MusicOrder.Shuffle ? GameSounds.MusicOrder.InOrder : GameSounds.MusicOrder.Shuffle;
        RefreshSoundScreen();
    }

    private void RebuildTrackList()
    {
        for (int i = trackListContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = trackListContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        trackImages.Clear();

        SoundManager sm = SoundManager.Instance;
        if (sm == null || sm.TrackCount == 0)
        {
            TextMeshProUGUI empty = UIBuilder.Text(trackListContent, "Empty", "No music yet!\n(Add songs in Game Builder > Sounds > Music)", 26, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Center);
            UIBuilder.Layout(empty, 90);
            return;
        }

        for (int i = 0; i < sm.TrackCount; i++)
        {
            int index = i;
            Button item = UIBuilder.Button(trackListContent, (i + 1) + ".  " + sm.TrackName(i), bodyFont, 26, UIBuilder.ButtonColor, () =>
            {
                SoundManager.Instance.PlayTrack(index);
                RefreshSoundScreen();
            }, TextAlignmentOptions.Left);
            UIBuilder.Layout(item, 54);
            trackImages.Add(item.GetComponent<Image>());
        }
    }

    private void RefreshSoundScreen()
    {
        SoundManager sm = SoundManager.Instance;
        if (sm == null || nowPlaying == null) return;

        bool hasMusic = sm.TrackCount > 0 && sm.Sounds.playMusic;
        if (!hasMusic) nowPlaying.text = sm.TrackCount == 0 ? "No music in this game yet" : "Music is switched off for this game";
        else if (sm.CurrentTrack < 0) nowPlaying.text = "Music stopped";
        else nowPlaying.text = (sm.IsMusicPaused ? "Paused: " : "Now playing: ") + sm.TrackName(sm.CurrentTrack);

        pauseButtonLabel.text = sm.IsMusicPaused ? "Play" : "Pause";
        bool shuffle = sm.Order == GameSounds.MusicOrder.Shuffle;
        orderButtonLabel.text = shuffle ? "Shuffle: On" : "Shuffle: Off";
        shuffleButton.GetComponent<Image>().color = shuffle ? UIBuilder.AccentColor : UIBuilder.ButtonColor;
        bool repeat = sm.RepeatSong;
        repeatButtonLabel.text = repeat ? "Repeat: On" : "Repeat: Off";
        repeatButton.GetComponent<Image>().color = repeat ? UIBuilder.AccentColor : UIBuilder.ButtonColor;

        for (int i = 0; i < trackImages.Count; i++)
        {
            trackImages[i].color = i == sm.CurrentTrack ? UIBuilder.AccentColor : UIBuilder.ButtonColor;
        }
    }

    private float nextSoundRefresh;

    private void Update()
    {
        // Songs change by themselves, so keep "Now playing" up to date
        if (IsSoundOpen && Time.unscaledTime > nextSoundRefresh)
        {
            nextSoundRefresh = Time.unscaledTime + 0.25f;
            RefreshSoundScreen();
        }
    }

    // ------------------------------------------------------------------ Pop-up message ("Checkpoint!")

    private void BuildToast(Transform canvas)
    {
        Image bg = UIBuilder.Image(canvas, "Toast", new Color(0.10f, 0.12f, 0.17f, 0.85f));
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -60);
        rt.sizeDelta = new Vector2(520, 84);
        bg.raycastTarget = false;

        toastText = UIBuilder.Text(bg.transform, "Text", "", 44, titleFont, new Color(0.45f, 1f, 0.6f), TextAlignmentOptions.Center);
        UIBuilder.Stretch(toastText.rectTransform, 16, 16, 0, 0);

        toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
    }

    public void ShowToast(string message)
    {
        if (toastGroup == null) return;
        toastText.text = message;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        float t = 0f;
        while (t < 0.2f) { t += Time.unscaledDeltaTime; toastGroup.alpha = t / 0.2f; yield return null; }
        toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.6f);
        t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / 0.5f; yield return null; }
        toastGroup.alpha = 0f;
        toastRoutine = null;
    }

    // ------------------------------------------------------------------ Death screen

    private GameObject BuildDeathScreen(Transform canvas)
    {
        Image overlay = UIBuilder.Image(canvas, "DeathScreen", new Color(0.25f, 0f, 0f, 0.65f));
        UIBuilder.Stretch(overlay.rectTransform);

        Image panel = UIBuilder.Image(overlay.transform, "Panel", UIBuilder.PanelColor);
        RectTransform panelRT = panel.rectTransform;
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(760, 0);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 40);
        layout.spacing = 18;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = UIBuilder.Text(panel.transform, "Title", settings.deathTitle, 80, titleFont, new Color(1f, 0.45f, 0.45f), TextAlignmentOptions.Center);
        UIBuilder.Layout(title, 110);

        TextMeshProUGUI message = UIBuilder.Text(panel.transform, "Message", settings.deathMessage, 30, bodyFont, UIBuilder.TextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(message);

        deathCountdown = UIBuilder.Text(panel.transform, "Countdown", "", 26, bodyFont, UIBuilder.MutedTextColor, TextAlignmentOptions.Center);
        UIBuilder.Layout(deathCountdown, 40);

        Button ok = UIBuilder.Button(panel.transform, "OK", bodyFont, 32, UIBuilder.AccentColor, () => manager.RespawnAfterDeath());
        UIBuilder.Layout(ok, 72);

        return overlay.gameObject;
    }
}
