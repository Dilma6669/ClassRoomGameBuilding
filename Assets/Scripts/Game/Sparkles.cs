using UnityEngine;

/// <summary>
/// Little bursts of glowing particles: when something is picked up, a creature is defeated,
/// a platform crumbles or a bouncy platform launches you. Made in code, nothing to set up.
/// </summary>
public static class Sparkles
{
    public enum Style
    {
        Pickup,    // swirls upwards
        Poof,      // sprays out in every direction
        Ring,      // a flat ring around your feet
        Fountain,  // shoots straight up and falls back down
        Explosion, // a big fast blast
        Twinkle,   // stars flash all around it, then vanish
        Sparks,    // fast bright streaks that fly out and drop, like welding
        Shimmer    // a wave of tiny glitter flickering all around it
    }

    /// <summary>A sparkle choice for settings: None, or one of the burst styles (with friendly names).</summary>
    public enum Effect { None, Swirl, Spray, Ring, Fountain, Explosion, Twinkle, Sparks, Shimmer }

    /// <summary>Plays an Effect (does nothing for None).</summary>
    public static void Play(Effect effect, Vector3 position, Color colour, float amount = 1f, float size = 1f)
    {
        if (effect == Effect.None) return;
        Style style;
        switch (effect)
        {
            case Effect.Swirl: style = Style.Pickup; break;
            case Effect.Spray: style = Style.Poof; break;
            case Effect.Ring: style = Style.Ring; break;
            case Effect.Fountain: style = Style.Fountain; break;
            case Effect.Explosion: style = Style.Explosion; break;
            case Effect.Twinkle: style = Style.Twinkle; break;
            case Effect.Sparks: style = Style.Sparks; break;
            default: style = Style.Shimmer; break;
        }
        Burst(position, colour, style, amount, size);
    }

    /// <summary>Fades in and out a few times, so each bit of glitter flickers.</summary>
    public static Gradient FlickerGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.15f, 0.35f),
                          new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0.2f, 0.75f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    /// <summary>Draw each particle stretched along the way it's flying, so it looks like a streak.</summary>
    public static void MakeStreaks(ParticleSystemRenderer renderer)
    {
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.06f;
        renderer.lengthScale = 1f;
    }

    private static Material material;

    /// <param name="amount">1 = normal, 2 = twice as many sparkles.</param>
    /// <param name="size">1 = normal, 2 = twice as big.</param>
    public static void Burst(Vector3 position, Color colour, Style style, float amount = 1f, float size = 1f)
    {
        if (!Application.isPlaying) return;

        GameObject go = new GameObject("Sparkles (auto)");
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy; // tidies itself up
        main.maxParticles = 1000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f * size, 0.4f * size);
        main.startColor = new ParticleSystem.MinMaxGradient(colour, Color.Lerp(colour, Color.white, 0.6f));

        ParticleSystem.ShapeModule shape = ps.shape;
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        int count;
        switch (style)
        {
            case Style.Pickup:
                count = 45;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                main.gravityModifier = -0.15f;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(2f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
                velocity.orbitalY = new ParticleSystem.MinMaxCurve(5f); // the swirl
                break;
            case Style.Ring:
                count = 30;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.4f;
                shape.rotation = new Vector3(90f, 0f, 0f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 6f);
                main.gravityModifier = 0f;
                break;
            case Style.Fountain:
                count = 50;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                shape.radius = 0.15f;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
                main.gravityModifier = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                break;
            case Style.Explosion:
                count = 90;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
                main.gravityModifier = 0.4f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f * size, 0.6f * size);
                ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;
                slow.enabled = true;
                slow.limit = 2f;
                slow.dampen = 0.15f; // fast at first, then drifts
                break;
            case Style.Twinkle:
                count = 35;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1f;
                main.startSpeed = 0f;
                main.gravityModifier = 0f;
                main.duration = 0.6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f * size, 0.6f * size);
                break;
            case Style.Sparks:
                count = 60;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.1f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
                main.gravityModifier = 2f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f * size, 0.12f * size);
                // Hot sparks: mostly white-yellow, a touch of the chosen colour
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), Color.Lerp(colour, Color.white, 0.3f));
                break;
            case Style.Shimmer:
                count = 120;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.4f);
                main.gravityModifier = -0.05f;
                main.duration = 0.8f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f * size, 0.15f * size);
                break;
            default: // Poof
                count = 50;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
                main.gravityModifier = 0.8f;
                break;
        }

        ParticleSystem.EmissionModule emission = ps.emission;
        int total = Mathf.Clamp(Mathf.RoundToInt(count * amount), 1, 1000);
        if (style == Style.Twinkle || style == Style.Shimmer)
        {
            emission.rateOverTime = total / main.duration; // one after another, not all at once
        }
        else
        {
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)total) });
        }

        // Fade out and shrink
        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        fade.color = style == Style.Shimmer ? FlickerGradient() : g;
        ParticleSystem.SizeOverLifetimeModule shrink = ps.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = style == Style.Twinkle
            ? new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)))
            : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (style == Style.Sparks) MakeStreaks(renderer);

        ps.Play();
    }

    public static Material GetMaterial()
    {
        if (material != null) return material;
        material = Resources.Load<Material>("SparkleMaterial"); // made by Game > ... (Editor/SparkleAssets.cs)
        if (material == null) material = new Material(Shader.Find("Sprites/Default"));
        return material;
    }

    /// <summary>A colour that suits each kind of pickup or creature.</summary>
    public static Color ColourFor(ObstacleBase.PayloadType payload)
    {
        switch (payload)
        {
            case ObstacleBase.PayloadType.HealthBooster: return new Color(1f, 0.3f, 0.4f);
            case ObstacleBase.PayloadType.StaminaBooster: return new Color(1f, 0.9f, 0.2f);
            case ObstacleBase.PayloadType.InvincibilityBuff: return new Color(0.3f, 0.8f, 1f);
            case ObstacleBase.PayloadType.JumpBuff: return new Color(0.4f, 1f, 0.4f);
            case ObstacleBase.PayloadType.SprintBuff: return new Color(1f, 0.6f, 0.2f);
            case ObstacleBase.PayloadType.Damage: return new Color(0.75f, 0.45f, 1f);
        }
        return Color.white;
    }
}
