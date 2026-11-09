using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sparkles that keep floating around something (a platform, a tree, a pickup...): magic dust, fireflies, embers.
/// Choose where they come from: the top, the sides and/or the bottom.
/// They appear when the game is playing. Add it with the "✨ Add sparkles around it" button in the Game Builder.
/// </summary>
[DisallowMultipleComponent]
public class AmbientSparkles : MonoBehaviour
{
    public enum Style { FloatUp, Swirl, Fireflies, Fountain, Twinkle, Explosions, Tornado, Rain, Sparks, Shimmer }

    [Tooltip("Float Up: rise gently. Swirl: circle around it. Fireflies: drift about. Fountain: spray out and fall.\n" +
             "Twinkle: stars that flash where they are. Explosions: a pop every second or so. Tornado: a rising spiral.\n" +
             "Rain: sparkles fall down. Sparks: fast bright streaks that fly out and drop, like welding.\n" +
             "Shimmer: a haze of tiny glitter flickering all over it.")]
    public Style style = Style.FloatUp;

    [Header("Where they come from")]
    public bool fromTop = true;
    public bool fromSides = true;
    public bool fromBottom = false;
    [Tooltip("From one point in the middle, spreading out. Good for small things like pickups.")]
    public bool fromCentre = false;

    [Header("Look")]
    public Color colour = new Color(1f, 0.9f, 0.5f);
    [Tooltip("A second colour: each sparkle is somewhere between the two.")]
    public Color secondColour = Color.white;
    [Tooltip("How many sparkles each second.")]
    [Range(1f, 100f)] public float amount = 20f;
    [Tooltip("How big the sparkles are (1 = normal).")]
    [Range(0.3f, 4f)] public float size = 1f;
    [Tooltip("Fit the sparkle area to the object's size automatically.")]
    public bool fitToObject = true;
    [Tooltip("How wide the sparkle area is (metres), when not fitting automatically.")]
    [Range(0.2f, 20f)] public float areaSize = 1.5f;

    private enum Place { Top, Sides, Bottom, Centre }

    private readonly List<ParticleSystem> systems = new List<ParticleSystem>();
    private readonly List<Vector3> offsets = new List<Vector3>();

    private void OnEnable()
    {
        if (Application.isPlaying) Build();
    }

    private void OnDisable()
    {
        foreach (ParticleSystem ps in systems) if (ps != null) Destroy(ps.gameObject);
        systems.Clear();
        offsets.Clear();
    }

    private void LateUpdate()
    {
        // The sparkles live outside this object (so its scale doesn't stretch them) and follow it
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i] != null) systems[i].transform.position = transform.TransformPoint(offsets[i]);
        }
    }

    private void Build()
    {
        // Size and centre from what you can see
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool found = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        float across = fitToObject ? Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.3f, 25f) : areaSize;

        List<Place> places = new List<Place>();
        if (fromTop) places.Add(Place.Top);
        if (fromSides) places.Add(Place.Sides);
        if (fromBottom) places.Add(Place.Bottom);
        if (fromCentre) places.Add(Place.Centre);
        if (places.Count == 0) return;

        foreach (Place place in places)
        {
            Vector3 at = bounds.center;
            if (place == Place.Top) at.y = bounds.max.y + 0.1f;
            if (place == Place.Bottom) at.y = bounds.min.y - 0.1f;
            systems.Add(MakeSystem(place, at, across, amount / places.Count));
            offsets.Add(transform.InverseTransformPoint(at));
        }
    }

    private ParticleSystem MakeSystem(Place place, Vector3 position, float across, float rate)
    {
        GameObject go = new GameObject("Sparkles (auto) - " + name + " " + place);
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f * size, 0.35f * size);
        main.startColor = new ParticleSystem.MinMaxGradient(colour, secondColour);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        // Top and bottom: a flat disc the size of the object. Sides: a ring just outside it.
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.rotation = new Vector3(90f, 0f, 0f);
        bool sides = place == Place.Sides;
        shape.radius = sides ? across * 1.15f + 0.3f : across * 0.9f;
        shape.radiusThickness = sides ? 0f : 1f;

        if (place == Place.Centre)
        {
            // One point in the middle, gently spreading out in every direction
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.rotation = Vector3.zero;
            shape.radius = 0.05f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        }

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        ParticleSystem.NoiseModule noise = ps.noise;
        float upOrDown = place == Place.Bottom ? -1f : 1f;
        switch (style)
        {
            case Style.FloatUp:
                SetVelocity(velocity, ParticleSystemSimulationSpace.World, new ParticleSystem.MinMaxCurve(0.3f * upOrDown, 0.9f * upOrDown), 0f);
                break;
            case Style.Swirl:
                SetVelocity(velocity, ParticleSystemSimulationSpace.Local, new ParticleSystem.MinMaxCurve(0.4f * upOrDown), 2f);
                break;
            case Style.Fireflies:
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                noise.enabled = true;
                noise.strength = 0.6f;
                noise.frequency = 0.4f;
                break;
            case Style.Fountain:
                main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
                main.gravityModifier = 0.6f;
                if (place == Place.Centre)
                {
                    shape.shapeType = ParticleSystemShapeType.Cone; // straight up, then falls
                    shape.angle = 20f;
                    shape.rotation = new Vector3(-90f, 0f, 0f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
                }
                else if (sides)
                {
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f); // a ring sprays outwards
                    SetVelocity(velocity, ParticleSystemSimulationSpace.World, new ParticleSystem.MinMaxCurve(2f), 0f);
                }
                else
                {
                    SetVelocity(velocity, ParticleSystemSimulationSpace.World, new ParticleSystem.MinMaxCurve(3f * upOrDown, 5f * upOrDown), 0f);
                }
                break;
            case Style.Twinkle:
                // Stars that pop up where they are, flash, and vanish
                main.startSpeed = 0f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f * size, 0.5f * size);
                ParticleSystem.SizeOverLifetimeModule flash = ps.sizeOverLifetime;
                flash.enabled = true;
                AnimationCurve pop = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f));
                flash.size = new ParticleSystem.MinMaxCurve(1f, pop);
                break;
            case Style.Explosions:
                // Every second or so: a pop of sparkles flying out and falling
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.rotation = Vector3.zero;
                shape.radius = 0.1f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
                main.gravityModifier = 0.5f;
                emission.rateOverTime = 0f;
                int perPop = Mathf.Clamp(Mathf.RoundToInt(rate * 1.5f), 5, 300);
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)perPop, (short)perPop, 0, 1.5f) }); // 0 cycles = forever
                break;
            case Style.Tornado:
                // A spiral that spins round and rises
                main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
                SetVelocity(velocity, ParticleSystemSimulationSpace.Local, new ParticleSystem.MinMaxCurve(1.5f * upOrDown), 4f);
                velocity.radial = new ParticleSystem.MinMaxCurve(-0.15f);
                break;
            case Style.Rain:
                // Sparkles gently falling down
                SetVelocity(velocity, ParticleSystemSimulationSpace.World, new ParticleSystem.MinMaxCurve(-1.5f, -2.5f), 0f);
                break;
            case Style.Shimmer:
                // A haze of tiny glitter flickering on and off all over it
                main.startSpeed = 0f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f * size, 0.14f * size);
                emission.rateOverTime = rate * 4f;
                SetVelocity(velocity, ParticleSystemSimulationSpace.World, new ParticleSystem.MinMaxCurve(0.1f, 0.25f), 0f);
                if (place == Place.Centre) { shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = Mathf.Max(0.4f, across); shape.radiusThickness = 1f; }
                break;
            case Style.Sparks:
                // Little crackles of fast streaks that fly out and drop, now and then
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.rotation = Vector3.zero;
                if (place == Place.Centre) shape.radius = 0.05f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f * size, 0.1f * size);
                main.gravityModifier = 1.5f;
                emission.rateOverTime = 0f;
                int perCrackle = Mathf.Clamp(Mathf.RoundToInt(rate * 0.5f), 4, 200);
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)perCrackle, (short)(perCrackle * 2), 0, 0.4f) });
                break;
        }

        // Twinkle in and out
        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        if (style == Style.Shimmer) fade.color = Sparkles.FlickerGradient();

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Sparkles.GetMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (style == Style.Sparks) Sparkles.MakeStreaks(renderer);

        ps.Play();
        return ps;
    }

    private static void SetVelocity(ParticleSystem.VelocityOverLifetimeModule v, ParticleSystemSimulationSpace space, ParticleSystem.MinMaxCurve up, float orbit)
    {
        // Unity needs x, y and z set the same way (all constants, or all random-between-two)
        bool twoValues = up.mode == ParticleSystemCurveMode.TwoConstants;
        v.enabled = true;
        v.space = space;
        v.x = twoValues ? new ParticleSystem.MinMaxCurve(0f, 0f) : new ParticleSystem.MinMaxCurve(0f);
        v.y = up;
        v.z = twoValues ? new ParticleSystem.MinMaxCurve(0f, 0f) : new ParticleSystem.MinMaxCurve(0f);
        if (orbit != 0f) v.orbitalY = new ParticleSystem.MinMaxCurve(orbit);
    }
}
