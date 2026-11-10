using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The Game Builder's own look: a faint dark blue background with slightly lighter blue boxes,
/// so it stands out from the rest of Unity. Buttons keep their normal colours.
/// Unity's box styles are shared by every window, so they're only changed while the Game Builder
/// draws itself, then put straight back.
/// </summary>
public partial class GameBuilderWindow
{
    private static readonly Color ThemeBackground = new Color(0.165f, 0.19f, 0.25f);
    private static readonly Color ThemeBox = new Color(0.205f, 0.235f, 0.305f);
    private static readonly Color ThemeBoxEdge = new Color(0.27f, 0.31f, 0.40f);
    private static readonly Color ThemeBar = new Color(0.13f, 0.15f, 0.205f);

    [System.NonSerialized] private Texture2D boxTexture, barTexture;

    private struct SavedLook
    {
        public GUIStyleState state;
        public Texture2D background;
        public Texture2D[] scaled;
    }

    private struct SavedBorder
    {
        public GUIStyle style;
        public RectOffset border;
    }

    private readonly List<SavedLook> savedLooks = new List<SavedLook>();
    private readonly List<SavedBorder> savedBorders = new List<SavedBorder>();

    /// <summary>Only for Unity's dark look (the light look keeps Unity's normal colours).</summary>
    private static bool UseTheme => EditorGUIUtility.isProSkin;

    private void BeginTheme()
    {
        if (!UseTheme) return;
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ThemeBackground);

        if (boxTexture == null) boxTexture = MakeTexture(ThemeBox, ThemeBoxEdge);
        if (barTexture == null) barTexture = MakeTexture(ThemeBar, ThemeBar);

        Restyle(EditorStyles.helpBox, boxTexture, false);
        Restyle(EditorStyles.toolbar, barTexture, false);
        Restyle(EditorStyles.toolbarButton, barTexture, false);
    }

    /// <summary>Puts Unity's styles back exactly as they were. Always runs, even after an error.</summary>
    private void EndTheme()
    {
        for (int i = savedLooks.Count - 1; i >= 0; i--)
        {
            savedLooks[i].state.background = savedLooks[i].background;
            savedLooks[i].state.scaledBackgrounds = savedLooks[i].scaled;
        }
        foreach (SavedBorder b in savedBorders) b.style.border = b.border;
        savedLooks.Clear();
        savedBorders.Clear();
    }

    private void Restyle(GUIStyle style, Texture2D texture, bool alsoWhenOpen)
    {
        savedBorders.Add(new SavedBorder { style = style, border = new RectOffset(style.border.left, style.border.right, style.border.top, style.border.bottom) });
        style.border = new RectOffset(2, 2, 2, 2);
        Swap(style.normal, texture);
        if (alsoWhenOpen) Swap(style.onNormal, texture);
    }

    private void Swap(GUIStyleState state, Texture2D texture)
    {
        savedLooks.Add(new SavedLook { state = state, background = state.background, scaled = state.scaledBackgrounds });
        state.background = texture;
        state.scaledBackgrounds = new Texture2D[0];
    }

    /// <summary>A small flat square with a 1-pixel edge, stretched to fit any box.</summary>
    private static Texture2D MakeTexture(Color fill, Color edge)
    {
        const int size = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onEdge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = onEdge ? edge : fill;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
