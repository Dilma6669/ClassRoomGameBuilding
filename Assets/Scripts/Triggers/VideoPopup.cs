using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a video on screen when a sign (TriggerLogic) is set to "Video".
/// Big mode: full screen, the game pauses, with a Skip button (Esc skips too).
/// Small mode: a box in the corner while the game keeps going.
/// It builds itself while the game runs, so nothing needs adding to a scene.
/// </summary>
public class VideoPopup : MonoBehaviour
{
    public static VideoPopup Current { get; private set; }
    public static bool IsPlayingFullScreen => Current != null && Current.fullScreen;

    private VideoPlayer player;
    private RenderTexture texture;
    private Action onFinished;
    private bool fullScreen;
    private bool finished;

    /// <summary>Show a video. onFinished runs when it ends or is skipped.</summary>
    public static void Play(VideoClip clip, bool pauseGame, Action onFinished)
    {
        if (clip == null)
        {
            onFinished?.Invoke();
            return;
        }

        // Only one video at a time: a new one replaces the old one
        if (Current != null) Current.Finish();

        GameObject go = new GameObject("Video Popup (auto)");
        Current = go.AddComponent<VideoPopup>();
        Current.Build(clip, pauseGame, onFinished);
    }

    /// <summary>Stop the video that's playing (Skip button, Esc key).</summary>
    public static void SkipCurrent()
    {
        if (Current != null) Current.Finish();
    }

    private void Build(VideoClip clip, bool pauseGame, Action finishedCallback)
    {
        fullScreen = pauseGame;
        onFinished = finishedCallback;

        GameSettings settings = GameSettings.Load();
        TMP_FontAsset font = settings != null ? settings.bodyFont : null;

        // Canvas above everything else (the menus use 500)
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        gameObject.layer = 5;

        // The box the video sits in
        RectTransform frame;
        if (fullScreen)
        {
            frame = UIBuilder.Image(transform, "Black Background", Color.black).rectTransform;
            UIBuilder.Stretch(frame);
        }
        else
        {
            Image box = UIBuilder.Image(transform, "Corner Box", new Color(0f, 0f, 0f, 0.85f));
            frame = box.rectTransform;
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(1f, 1f);
            frame.anchoredPosition = new Vector2(-30f, -130f); // below the health bar
            frame.sizeDelta = new Vector2(640f, 360f);
            box.raycastTarget = false;
        }

        // The picture
        texture = new RenderTexture(Mathf.Max(16, (int)clip.width), Mathf.Max(16, (int)clip.height), 0);
        RawImage picture = UIBuilder.Create("Video", frame).gameObject.AddComponent<RawImage>();
        picture.texture = texture;
        picture.raycastTarget = false;
        UIBuilder.Stretch(picture.rectTransform, 6, 6, 6, fullScreen ? 120 : 6);
        AspectRatioFitter fit = picture.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;

        if (fullScreen)
        {
            Button skip = UIBuilder.Button(frame, "Skip  >>", font, 40, UIBuilder.ButtonColor, Finish);
            RectTransform rt = (RectTransform)skip.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30f, 25f);
            rt.sizeDelta = new Vector2(260f, 80f);
        }

        // The sound: ignoreListenerPause so it still plays while the game is paused
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;
        audioSource.spatialBlend = 0f;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.clip = clip;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime; // keeps playing while the game is paused
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = clip.audioTrackCount;
        for (ushort i = 0; i < clip.audioTrackCount; i++)
        {
            player.EnableAudioTrack(i, true);
            player.SetTargetAudioSource(i, audioSource);
        }
        player.loopPointReached += _ => Finish();
        player.errorReceived += (_, message) =>
        {
            Debug.LogWarning("🎬 The video couldn't play: " + message);
            Finish();
        };

        if (fullScreen && GameManager.Instance != null) GameManager.Instance.BeginVideo();

        player.Play();
    }

    private void Update()
    {
        // The small corner video waits while the pause menu (or death screen) is open
        if (fullScreen || player == null || finished) return;
        bool gamePaused = Time.timeScale == 0f;
        if (gamePaused && player.isPlaying) player.Pause();
        else if (!gamePaused && !player.isPlaying && player.isPrepared) player.Play();
    }

    public void Finish()
    {
        if (finished) return;
        finished = true;

        if (player != null) player.Stop();
        if (Current == this) Current = null;
        if (fullScreen && GameManager.Instance != null) GameManager.Instance.EndVideo();

        Action callback = onFinished;
        onFinished = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
