using UnityEngine;

/// <summary>
/// Little white icons for the music mode button, drawn in code (the game's font has no such symbols):
/// In order = a straight arrow, Repeat song = a circular arrow, Shuffle = two crossing arrows.
/// </summary>
public static class MusicIcons
{
    private const int Size = 64;
    private const float Line = 4.5f; // half the line thickness, in pixels

    private static Sprite inOrder, repeat, shuffle;

    public static Sprite InOrder => inOrder != null ? inOrder : inOrder = Make(InOrderShape);
    public static Sprite Repeat => repeat != null ? repeat : repeat = Make(RepeatShape);
    public static Sprite Shuffle => shuffle != null ? shuffle : shuffle = Make(ShuffleShape);

    // ---- The shapes: each returns how far a pixel is from the drawing (0 = on it)

    private static float InOrderShape(Vector2 p)
    {
        Vector2 a = new Vector2(10, 32), b = new Vector2(42, 32);
        return Mathf.Min(Segment(p, a, b) - Line, ArrowHead(p, b, Vector2.right));
    }

    private static float RepeatShape(Vector2 p)
    {
        // A circle with a gap at the top right; the arrow head points round the circle
        Vector2 c = new Vector2(32, 30);
        const float r = 17f, gapFrom = 0f, gapTo = 70f;
        Vector2 end = c + new Vector2(r, 0); // the arc ends at 0 degrees, travelling upwards
        return Mathf.Min(Arc(p, c, r, gapTo, 360f + gapFrom) - Line, ArrowHead(p, end + Vector2.up * 2f, Vector2.up));
    }

    private static float ShuffleShape(Vector2 p)
    {
        // Like a music player's shuffle sign: two paths start flat, cross in the middle, end in flat arrows
        const float low = 17f, high = 47f, thin = 3.2f;
        Vector2 s1 = new Vector2(4, low), k1 = new Vector2(16, low), k2 = new Vector2(34, high), e1 = new Vector2(44, high);
        Vector2 s2 = new Vector2(4, high), j1 = new Vector2(16, high), j2 = new Vector2(34, low), e2 = new Vector2(44, low);
        float pathUp = Mathf.Min(Segment(p, s1, k1), Mathf.Min(Segment(p, k1, k2), Segment(p, k2, e1)));
        float pathDown = Mathf.Min(Segment(p, s2, j1), Mathf.Min(Segment(p, j1, j2), Segment(p, j2, e2)));
        float lines = Mathf.Min(pathUp, pathDown) - thin;
        float heads = Mathf.Min(ArrowHead(p, e1, Vector2.right, 8f, 12f), ArrowHead(p, e2, Vector2.right, 8f, 12f));
        return Mathf.Min(lines, heads);
    }

    // ---- Drawing helpers

    private static Sprite Make(System.Func<Vector2, float> shape)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float d = shape(new Vector2(x + 0.5f, y + 0.5f));
                pixels[y * Size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)); // soft edge
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>Distance to a circle's edge between two angles (degrees, counter-clockwise).</summary>
    private static float Arc(Vector2 p, Vector2 centre, float radius, float fromDeg, float toDeg)
    {
        Vector2 d = p - centre;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        while (angle < fromDeg) angle += 360f;
        if (angle <= toDeg) return Mathf.Abs(d.magnitude - radius);
        Vector2 start = centre + new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * radius;
        Vector2 end = centre + new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad)) * radius;
        return Mathf.Min(Vector2.Distance(p, start), Vector2.Distance(p, end));
    }

    /// <summary>A filled triangle pointing along 'dir', with its back edge centred on 'at'.</summary>
    private static float ArrowHead(Vector2 p, Vector2 at, Vector2 dir, float halfWidth = 10f, float length = 13f)
    {
        Vector2 side = new Vector2(-dir.y, dir.x) * halfWidth;
        Vector2 tip = at + dir * length, left = at + side, right = at - side;
        if (InTriangle(p, tip, left, right)) return 0f;
        return Mathf.Min(Segment(p, tip, left), Mathf.Min(Segment(p, left, right), Segment(p, right, tip)));
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
        return (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
