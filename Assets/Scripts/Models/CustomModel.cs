using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
using KinematicCharacterController.Examples;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Swap how something looks for your own 3D model - with animations that play automatically.
///
/// HOW TO USE:
///   1. Put a model file (.fbx, .glb or .obj) in the Assets/MyModels folder.
///   2. Drag it into the "Model" box on this component.
/// That's it! The model is sized to fit, and animations named idle / walk / run / jump / fall
/// are found and played automatically. The object still collides and behaves exactly the same.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class CustomModel : MonoBehaviour
{
    public const string ModelChildPrefix = "Model (auto)";

    public enum AnimSlot { Idle, Walk, Run, Jump, Fall }

    [Tooltip("Drag a 3D model here (.fbx, .glb or .obj from your Assets/MyModels folder, or a prefab).")]
    public GameObject model;

    [Tooltip("Make the model bigger or smaller.")]
    [Range(0.1f, 50f)] public float size = 1f;

    [Tooltip("Turn the model if it faces the wrong way.")]
    [Range(-180f, 180f)] public float turn = 0f;

    [Tooltip("Move the model up or down if it floats above or sinks into the ground.")]
    [Range(-50f, 50f)] public float raiseLower = 0f;

    [Tooltip("Optional: use the animations from ANOTHER model file with the same skeleton (e.g. a re-skinned zombie using Zombie-model's animations). Leave empty to use this model's own animations.")]
    public GameObject animationsFrom;

    [Tooltip("Plays when standing still.")] public AnimationClip idle;
    [Tooltip("Plays when moving.")] public AnimationClip walk;
    [Tooltip("Plays when moving fast (sprinting).")] public AnimationClip run;
    [Tooltip("Plays when going up in the air.")] public AnimationClip jump;
    [Tooltip("Plays when falling.")] public AnimationClip fall;
    [Tooltip("Creatures: plays when it hurts the player (found by name: Scratch, Bite, Attack, Punch, Kick...). An animation picked in Game Builder > Sounds for 'it attacks you' is used instead.")]
    public AnimationClip attack;
    [Tooltip("The player: plays when something hurts you (found by name: Hit, Hurt, Damage...). An animation picked in Game Builder > Sounds for 'gets hurt' is used instead.")]
    public AnimationClip hurt;
    [Tooltip("Creatures: plays when it's beaten, and it stays in the last pose (found by name: Death, Die, Dead...). An animation picked in Game Builder > Sounds for 'it is beaten' is used instead.")]
    public AnimationClip death;

    [Tooltip("Speed up or slow down all animations.")]
    [Range(0.1f, 10f)] public float animationSpeed = 1f;

    [Tooltip("Off (normal): the model stays where it is while it animates, even if an animation walks it forwards. On: animations that walk or jump the model away move it (it snaps back when the animation loops).")]
    public bool animationMovesModel = false;

    // Saved results of fitting the model (so we don't re-measure every frame)
    [SerializeField, HideInInspector] private GameObject builtFrom;
    [SerializeField, HideInInspector] private GameObject clipsFoundFrom;
    [SerializeField, HideInInspector] private bool attackLookedFor; // models set up before 'attack' existed look for one once
    [SerializeField, HideInInspector] private bool hurtLookedFor;
    [SerializeField, HideInInspector] private bool deathLookedFor;
    [SerializeField, HideInInspector] private Transform modelInstance;
    [SerializeField, HideInInspector] private float fitScale = 1f;
    [SerializeField, HideInInspector] private Vector3 modelAnchor;   // bottom-centre of the model, in model space
    [SerializeField, HideInInspector] private Vector3 targetAnchor;  // where that point should sit, in parent space

    public Transform ModelInstance => modelInstance;

    // Runtime animation
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly AnimationClipPlayable[] clipPlayables = new AnimationClipPlayable[5];
    private readonly int[] mixerInput = { -1, -1, -1, -1, -1 };
    private readonly float[] weights = new float[5];
    private AnimSlot currentSlot = AnimSlot.Idle;
    private Vector3 lastPosition;
    private bool isStaticObstacle;
    private PlayerLogic playerLogic;
#if HAS_KINEMATIC_CC
    private KinematicCharacterMotor motor;
#endif

    // ------------------------------------------------------------------ Lifecycle

    private void OnEnable()
    {
#if UNITY_EDITOR
        // Opened in the Editor and set up before the attack/hurt animations existed: look for them now
        if (!Application.isPlaying && (!attackLookedFor || !hurtLookedFor || !deathLookedFor) && model != null && model == builtFrom)
        {
            EditorApplication.delayCall -= DelayedRefresh;
            EditorApplication.delayCall += DelayedRefresh;
        }
#endif
        if (!Application.isPlaying) return;

        if (model != null && modelInstance == null) Rebuild();

        CacheMovementSources();
        lastPosition = transform.position;
        BuildAnimationGraph();
    }

    private void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    private void Update()
    {
        if (!Application.isPlaying || !graph.IsValid()) return;
        UpdateAnimation(Time.deltaTime);
    }

    // Some animations walk the model forwards (its root bone moves), then snap it back when they loop.
    // Keep that bone over its starting spot (up and down still moves, for jumps and crouches).
    private Transform motionBone;
    private Vector3 motionBoneRest; // in model-holder space

    private void LateUpdate()
    {
        if (animationMovesModel || motionBone == null || modelInstance == null || !Application.isPlaying || !graph.IsValid()) return;
        Vector3 p = modelInstance.InverseTransformPoint(motionBone.position);
        p.x = motionBoneRest.x;
        p.z = motionBoneRest.z;
        motionBone.position = modelInstance.TransformPoint(p);
    }

    private void FindMotionBone(bool isHumanoid)
    {
        motionBone = null;
        if (isHumanoid || modelInstance == null) return; // humanoids already stay in place
        SkinnedMeshRenderer biggest = null;
        foreach (SkinnedMeshRenderer smr in modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.rootBone != null && (biggest == null || smr.sharedMesh != null && biggest.sharedMesh != null && smr.sharedMesh.vertexCount > biggest.sharedMesh.vertexCount)) biggest = smr;
        if (biggest == null) return;
        motionBone = biggest.rootBone;
        motionBoneRest = modelInstance.InverseTransformPoint(motionBone.position);
    }

    // ------------------------------------------------------------------ Building the model

    /// <summary>Re-creates the model if it changed, otherwise just re-applies the size/turn/height sliders.</summary>
    public void Refresh()
    {
#if UNITY_EDITOR
        // "Animations From" changed: look for animations again (the model itself stays)
        if (!Application.isPlaying && model != null && model == builtFrom && animationsFrom != clipsFoundFrom)
        {
            ClearClips();
            AutoFindClips();
            MarkDirty();
        }
        // Set up before the 'attack' animation existed: look for one now (the other animations stay as they are)
        else if (!Application.isPlaying && model != null && model == builtFrom && (!attackLookedFor || !hurtLookedFor || !deathLookedFor))
        {
            AnimationClip i = idle, w = walk, r = run, j = jump, f = fall;
            AutoFindClips();
            idle = i; walk = w; run = r; jump = j; fall = f;
            MarkDirty();
        }
#endif
        if (model != builtFrom || (model != null && modelInstance == null) || (model == null && modelInstance != null))
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && model != builtFrom)
            {
                ClearClips();
                AutoFindClips();
            }
#endif
            Rebuild();
        }
        else
        {
            ApplyPlacement();
        }
    }

    /// <summary>Everything about what a character looks like: its model, placement and animations.</summary>
    [System.Serializable]
    public class Look
    {
        public GameObject model;
        public GameObject animationsFrom;
        public float size = 1f, turn, raiseLower, animationSpeed = 1f;
        public bool animationMovesModel;
        public AnimationClip idle, walk, run, jump, fall, attack, hurt, death;
    }

    public Look GetLook() => new Look
    {
        model = model, animationsFrom = animationsFrom, size = size, turn = turn, raiseLower = raiseLower,
        animationSpeed = animationSpeed, animationMovesModel = animationMovesModel,
        idle = idle, walk = walk, run = run, jump = jump, fall = fall, attack = attack, hurt = hurt, death = death,
    };

    /// <summary>Changes to another look while playing (a 'become a character' pickup) and starts its animations.</summary>
    public void SetLook(Look look)
    {
        model = look.model; animationsFrom = look.animationsFrom;
        size = look.size; turn = look.turn; raiseLower = look.raiseLower;
        animationSpeed = look.animationSpeed; animationMovesModel = look.animationMovesModel;
        idle = look.idle; walk = look.walk; run = look.run; jump = look.jump; fall = look.fall;
        attack = look.attack; hurt = look.hurt; death = look.death;
        Rebuild();
        if (!Application.isPlaying) return;
        CacheMovementSources();
        lastPosition = transform.position;
        BuildAnimationGraph();
    }

    public void Rebuild()
    {
        DestroyModelInstances();

        if (model == null)
        {
            SetOriginalRenderers(true);
            builtFrom = null;
            MarkDirty();
            return;
        }

        // The model goes inside a holder. We size/turn/move the HOLDER, so the model keeps its own
        // built-in rotation and scale (FBX files from Blender have a 90 degree turn and x100 scale on them).
        GameObject holder = new GameObject(ModelChildPrefix + " - " + model.name);
        GameObject instance = CreateInstance(model);
        instance.name = model.name;
        instance.transform.SetParent(holder.transform, false); // keeps the model's own local turn/scale

        // Measure the model with the holder at the world origin, unrotated and unscaled
        Bounds modelBounds = MeasureRenderers(holder.transform, null, out bool hasRenderers);
        if (!hasRenderers) modelBounds = new Bounds(Vector3.zero, Vector3.one);

        // Turn off the model's own physics so it can't get in the player's way
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Transform parent = GetVisualParent();
        GetTargetArea(parent, out Vector3 targetBottomCentre, out float targetHeight, out bool isCharacter);

        holder.transform.SetParent(parent, false);
        holder.layer = gameObject.layer;

        // Characters stand up, so match their height. Creatures and objects (a long snake, a wide shark)
        // match their biggest side, so they fit the space of the shape they replace.
        float modelSize = isCharacter ? modelBounds.size.y : Mathf.Max(modelBounds.size.x, modelBounds.size.y, modelBounds.size.z);
        fitScale = targetHeight / Mathf.Max(modelSize, 0.001f);
        modelAnchor = new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
        targetAnchor = targetBottomCentre;
        modelInstance = holder.transform;
        builtFrom = model;

        ApplyPlacement();
        SetOriginalRenderers(false);

        Animator animator = instance.GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = null; // our code drives the animations
        }

        // Show the idle pose in the editor instead of a T-pose.
        // (Not for humanoid rigs: sampling them outside Play mode drops the body to the ground origin.)
        bool isHumanoid = animator != null && animator.avatar != null && animator.avatar.isHuman;
        if (!Application.isPlaying && idle != null && !isHumanoid) idle.SampleAnimation(instance, 0f);

        MarkDirty();
    }

    /// <summary>Applies the Size / Turn / Raise-Lower sliders.</summary>
    public void ApplyPlacement()
    {
        if (modelInstance == null) return;

        Quaternion rotation = Quaternion.Euler(0f, turn, 0f);
        float scale = fitScale * size;
        modelInstance.localRotation = rotation;
        modelInstance.localScale = Vector3.one * scale;
        // Keep the bottom-centre of the model on the target spot, whatever the size or turn
        modelInstance.localPosition = targetAnchor - rotation * (modelAnchor * scale) + Vector3.up * raiseLower;
    }

    private GameObject CreateInstance(GameObject source)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(source))
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(source);
        }
#endif
        return Instantiate(source);
    }

    private void DestroyModelInstances()
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t != transform && t.name.StartsWith(ModelChildPrefix)) toDestroy.Add(t.gameObject);
        }
        foreach (GameObject go in toDestroy)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        modelInstance = null;
    }

    /// <summary>Where the model goes: the character's mesh root, the obstacle's visual, or this object.</summary>
    private Transform GetVisualParent()
    {
#if HAS_KINEMATIC_CC
        ExampleCharacterController character = GetComponent<ExampleCharacterController>();
        if (character != null && character.MeshRoot != null) return character.MeshRoot;
#endif
        Transform obstacleVisual = transform.Find("⚠️ DO NOT TOUCH");
        if (obstacleVisual != null) return obstacleVisual;
        return transform;
    }

    /// <summary>The space the model should fill, measured from the original shape.</summary>
    private void GetTargetArea(Transform parent, out Vector3 bottomCentre, out float height, out bool isCharacter)
    {
        isCharacter = false;
#if HAS_KINEMATIC_CC
        CapsuleCollider capsule = GetComponent<KinematicCharacterMotor>() != null ? GetComponent<CapsuleCollider>() : null;
        if (capsule != null)
        {
            isCharacter = true;
            height = capsule.height;
            Vector3 bottomWorld = transform.TransformPoint(new Vector3(0f, capsule.center.y - height * 0.5f, 0f));
            bottomCentre = parent.InverseTransformPoint(bottomWorld);
            return;
        }
#endif
        MeshFilter filter = parent.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            Bounds b = filter.sharedMesh.bounds;
            height = Mathf.Max(b.size.x, b.size.y, b.size.z);
            bottomCentre = new Vector3(b.center.x, b.min.y, b.center.z);
            return;
        }

        Bounds world = MeasureRenderers(transform, modelInstance, out bool found);
        if (found)
        {
            height = Mathf.Max(world.size.x, world.size.y, world.size.z) / Mathf.Max(parent.lossyScale.y, 0.001f);
            bottomCentre = parent.InverseTransformPoint(new Vector3(world.center.x, world.min.y, world.center.z));
            return;
        }

        height = 1f;
        bottomCentre = Vector3.zero;
    }

    private static Bounds MeasureRenderers(Transform root, Transform exclude, out bool found)
    {
        Bounds bounds = new Bounds();
        found = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (exclude != null && r.transform.IsChildOf(exclude)) continue;
            Bounds rb = WorldBoundsFromMesh(r);
            if (!found) { bounds = rb; found = true; }
            else bounds.Encapsulate(rb);
        }
        return bounds;
    }

    /// <summary>
    /// Bounds from the mesh data itself. Renderer.bounds can be out of date for skinned
    /// (animated) meshes in the editor, which made models the wrong size.
    /// </summary>
    private static Bounds WorldBoundsFromMesh(Renderer r)
    {
        Mesh mesh = null;
        if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
        else
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
        }
        if (mesh == null) return r.bounds;

        Bounds local = mesh.bounds;
        Matrix4x4 m = r.transform.localToWorldMatrix;
        Bounds world = new Bounds(m.MultiplyPoint3x4(local.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            world.Encapsulate(m.MultiplyPoint3x4(corner));
        }
        return world;
    }

    /// <summary>Hide the original shape (capsule, rock...) while a model is showing. Colliders stay on.</summary>
    private void SetOriginalRenderers(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (modelInstance != null && r.transform.IsChildOf(modelInstance)) continue;
            if (r.name.StartsWith(ModelChildPrefix) || IsInsideModel(r.transform)) continue;
            r.enabled = visible;
        }
    }

    private bool IsInsideModel(Transform t)
    {
        for (Transform p = t; p != null && p != transform; p = p.parent)
        {
            if (p.name.StartsWith(ModelChildPrefix)) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ Animation (play mode)

    private void CacheMovementSources()
    {
        playerLogic = GetComponentInParent<PlayerLogic>();
#if HAS_KINEMATIC_CC
        motor = GetComponent<KinematicCharacterMotor>();
#endif
        isStaticObstacle = GetComponent<PlatformStaticDriver>() != null || GetComponent<TerrainStaticDriver>() != null;
    }

    private AnimationClip GetClip(AnimSlot slot)
    {
        switch (slot)
        {
            case AnimSlot.Idle: return idle;
            case AnimSlot.Walk: return walk;
            case AnimSlot.Run: return run;
            case AnimSlot.Jump: return jump;
            default: return fall;
        }
    }

    private void BuildAnimationGraph()
    {
        if (graph.IsValid()) graph.Destroy();
        if (modelInstance == null) return;

        // The Animator lives on the model itself (inside the holder), where the animations expect it
        Animator animator = modelInstance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Transform modelRoot = modelInstance.childCount > 0 ? modelInstance.GetChild(0) : modelInstance;
            animator = modelRoot.gameObject.AddComponent<Animator>();
        }
        animator.applyRootMotion = false;

        // Humanoid characters need humanoid animations (and the other way round), or the pose breaks
        bool isHumanoid = animator.avatar != null && animator.avatar.isHuman;
        AnimationClip[] usable = new AnimationClip[5];
        int clipCount = 0;
        for (int i = 0; i < 5; i++)
        {
            AnimationClip clip = GetClip((AnimSlot)i);
            if (clip == null) continue;
            if (clip.humanMotion != isHumanoid)
            {
                Debug.LogWarning($"⚠️ The {(AnimSlot)i} animation '{clip.name}' doesn't match the model '{model.name}' (one is Humanoid and the other isn't), so it was skipped.", this);
                continue;
            }
            usable[i] = clip;
            clipCount++;
        }
        if (clipCount == 0) return;

        graph = PlayableGraph.Create(name + " Animations");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime); // pauses with the game
        mixer = AnimationMixerPlayable.Create(graph, clipCount + 1); // + 1 for extra animations (bark, bite...)
        actionInput = clipCount;
        actionWeight = 0f;
        actionTimeLeft = 0f;
        isHumanoidRig = isHumanoid;
        FindMotionBone(isHumanoid);

        int input = 0;
        for (int i = 0; i < 5; i++)
        {
            mixerInput[i] = -1;
            AnimationClip clip = usable[i];
            if (clip == null) continue;

            clipPlayables[i] = AnimationClipPlayable.Create(graph, clip);
            graph.Connect(clipPlayables[i], 0, mixer, input);
            mixerInput[i] = input;
            input++;
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        currentSlot = PickFallback(AnimSlot.Idle);
        for (int i = 0; i < 5; i++) weights[i] = i == (int)currentSlot ? 1f : 0f;
        ApplyWeights();
        graph.Play();
    }

    private void UpdateAnimation(float dt)
    {
        if (dt <= 0f) return;

        Vector3 velocity = (transform.position - lastPosition) / dt;
        lastPosition = transform.position;
        bool grounded = true;

#if HAS_KINEMATIC_CC
        if (motor != null)
        {
            velocity = motor.BaseVelocity; // ignores moving-platform motion
            grounded = motor.GroundingStatus.IsStableOnGround;
        }
#endif
        float horizontalSpeed = isStaticObstacle ? 0f : new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float runAbove = playerLogic != null ? playerLogic.moveSpeed * 1.15f : 6f;

        AnimSlot target;
        if (!grounded && (jump != null || fall != null))
        {
            target = velocity.y > 0.5f ? AnimSlot.Jump : AnimSlot.Fall;
        }
        else if (horizontalSpeed < 0.3f)
        {
            target = AnimSlot.Idle;
        }
        else if (run != null && (walk == null || horizontalSpeed > runAbove))
        {
            target = AnimSlot.Run;
        }
        else
        {
            target = AnimSlot.Walk;
        }

        target = PickFallback(target);

        if (target != currentSlot)
        {
            // Start jumps from the beginning
            if (target == AnimSlot.Jump && clipPlayables[(int)AnimSlot.Jump].IsValid())
            {
                clipPlayables[(int)AnimSlot.Jump].SetTime(0);
            }
            currentSlot = target;
            PlayAnimationSound(true);
        }
        else
        {
            PlayAnimationSound(false);
        }

        // Smoothly blend between animations
        float total = 0f;
        for (int i = 0; i < 5; i++)
        {
            weights[i] = Mathf.MoveTowards(weights[i], i == (int)currentSlot ? 1f : 0f, dt * 8f);
            total += weights[i];
        }
        if (total > 0f) for (int i = 0; i < 5; i++) weights[i] /= total;
        UpdateAction(dt);
        ApplyWeights();
    }

    // ------------------------------------------------------------------ Animation sounds (Game Builder > Sounds > one model)

    private int lastLoop = -1;

    /// <summary>The clip that plays for a slot (after falling back to a similar one if it's missing).</summary>
    public AnimationClip ClipPlayingFor(AnimSlot slot) => GetClip(PickFallback(slot));

    /// <summary>Does this model have its own sound for the animation that plays in this slot?</summary>
    public bool HasOwnSound(AnimSlot slot)
    {
        return graph.IsValid() && SoundManager.FindAnimationSound(model, ClipPlayingFor(slot)) != null;
    }

    /// <summary>Plays the sound set for the current animation when it starts (and each time it loops, if wanted).</summary>
    private void PlayAnimationSound(bool justStarted)
    {
        AnimationClipPlayable playable = clipPlayables[(int)currentSlot];
        AnimationClip clip = GetClip(currentSlot);
        if (!playable.IsValid() || clip == null || clip.length <= 0f) return;

        int loop = Mathf.FloorToInt((float)(playable.GetTime() / clip.length));
        bool looped = loop != lastLoop;
        lastLoop = loop;
        if (!justStarted && !looped) return;

        AnimationSound sound = SoundManager.FindAnimationSound(model, clip);
        if (sound == null || (!justStarted && !sound.repeatEachLoop)) return;
        SoundManager.PlayAnimationSound(sound, transform.position, GetComponentInParent<ObstacleBase>() == null);
    }

    /// <summary>If the wanted animation is missing, use the closest one that exists.</summary>
    private AnimSlot PickFallback(AnimSlot wanted)
    {
        AnimSlot[] order;
        switch (wanted)
        {
            case AnimSlot.Fall: order = new[] { AnimSlot.Fall, AnimSlot.Jump, AnimSlot.Idle, AnimSlot.Walk, AnimSlot.Run }; break;
            case AnimSlot.Jump: order = new[] { AnimSlot.Jump, AnimSlot.Fall, AnimSlot.Idle, AnimSlot.Walk, AnimSlot.Run }; break;
            case AnimSlot.Run: order = new[] { AnimSlot.Run, AnimSlot.Walk, AnimSlot.Idle, AnimSlot.Jump, AnimSlot.Fall }; break;
            case AnimSlot.Walk: order = new[] { AnimSlot.Walk, AnimSlot.Run, AnimSlot.Idle, AnimSlot.Jump, AnimSlot.Fall }; break;
            default: order = new[] { AnimSlot.Idle, AnimSlot.Walk, AnimSlot.Run, AnimSlot.Jump, AnimSlot.Fall }; break;
        }
        foreach (AnimSlot slot in order) if (mixerInput[(int)slot] >= 0) return slot;
        return wanted;
    }

    private void ApplyWeights()
    {
        for (int i = 0; i < 5; i++)
        {
            if (mixerInput[i] < 0) continue;
            mixer.SetInputWeight(mixerInput[i], weights[i] * (1f - actionWeight));
            clipPlayables[i].SetSpeed(animationSpeed);
        }
        if (actionInput >= 0 && actionInput < mixer.GetInputCount()) mixer.SetInputWeight(actionInput, actionWeight);
    }

    // ------------------------------------------------------------------ Extra animations (bark, bite, death...)

    private int actionInput = -1;
    private AnimationClipPlayable actionPlayable;
    private float actionWeight;
    private bool actionHold; // a death animation: stays in its last pose
    private float actionTimeLeft;
    private bool isHumanoidRig;
    private float nowAndThenTimer = -1f;

    /// <summary>
    /// Plays the model's extra animation chosen for this moment (Game Builder > Sounds > one model), with its sound.
    /// Returns how long it lasts (0 if there is none). playedSound: it had its own sound (so skip the general one).
    /// </summary>
    public float PlayMoment(AnimationMoment moment, out bool playedSound)
    {
        playedSound = false;
        if (!Application.isPlaying || model == null) return 0f;

        List<AnimationSound> choices = new List<AnimationSound>();
        foreach (ModelSounds m in GameSounds.Load().modelSounds)
        {
            if (m == null || m.model != model) continue;
            foreach (AnimationSound a in m.animations)
                if (a != null && a.animation != null && a.playWhen == moment) choices.Add(a);
        }
        if (choices.Count == 0) return 0f;

        AnimationSound pick = choices[Random.Range(0, choices.Count)];
        if (pick.sound != null)
        {
            SoundManager.PlayAnimationSound(pick, transform.position, GetComponentInParent<ObstacleBase>() == null);
            playedSound = true;
        }
        return PlayAction(pick.animation, moment == AnimationMoment.IsBeaten); // a death pose stays
    }

    /// <summary>
    /// A creature hurts the player: plays the 'it attacks you' animation picked in Game Builder > Sounds,
    /// or else its own attack animation (found by name). Returns how long it lasts (0 = none).
    /// </summary>
    public float PlayAttack(out bool playedSound)
    {
        float length = PlayMoment(AnimationMoment.AttacksYou, out playedSound);
        if (length > 0f || !Application.isPlaying) return length;
        return PlayAction(attack);
    }

    /// <summary>
    /// A creature is beaten: plays the 'it is beaten' animation picked in Game Builder > Sounds, or else its own death
    /// animation, and stays in the last pose. Returns how long it lasts (0 = none).
    /// </summary>
    public float PlayDeath(out bool playedSound)
    {
        float length = PlayMoment(AnimationMoment.IsBeaten, out playedSound);
        if (length > 0f || !Application.isPlaying) return length;
        return PlayAction(death, true);
    }

    /// <summary>The player got hurt and nothing was picked in Game Builder > Sounds: plays the model's own hurt animation.</summary>
    public float PlayHurt()
    {
        if (!Application.isPlaying) return 0f;
        return PlayAction(hurt);
    }

    private float PlayAction(AnimationClip clip, bool holdLastFrame = false)
    {
        if (!graph.IsValid() || actionInput < 0 || clip == null || clip.length <= 0f) return 0f;
        if (clip.humanMotion != isHumanoidRig) return 0f; // would break the pose

        if (actionPlayable.IsValid())
        {
            graph.Disconnect(mixer, actionInput);
            actionPlayable.Destroy();
        }
        actionPlayable = AnimationClipPlayable.Create(graph, clip);
        actionPlayable.SetSpeed(animationSpeed);
        graph.Connect(actionPlayable, 0, mixer, actionInput);
        float length = clip.length / Mathf.Max(0.1f, animationSpeed);
        actionTimeLeft = length;
        actionHold = holdLastFrame;
        return length;
    }

    private void UpdateAction(float dt)
    {
        actionTimeLeft -= dt;
        if (actionHold)
        {
            // A death animation: blend in and stay in its last pose (no blending back to standing)
            actionWeight = Mathf.MoveTowards(actionWeight, 1f, dt * 8f);
            if (actionTimeLeft <= 0f && actionPlayable.IsValid() && actionPlayable.GetSpeed() != 0.0)
            {
                AnimationClip clip = actionPlayable.GetAnimationClip();
                if (clip != null) actionPlayable.SetTime(Mathf.Max(0f, clip.length - 0.02f));
                actionPlayable.SetSpeed(0.0);
            }
            return;
        }
        // Blend in quickly, and back out just before it ends
        float target = actionTimeLeft > 0.15f ? 1f : 0f;
        actionWeight = Mathf.MoveTowards(actionWeight, target, dt * 8f);

        // "Now and then": an extra animation every few seconds while standing still
        if (currentSlot == AnimSlot.Idle && actionTimeLeft <= 0f)
        {
            if (nowAndThenTimer < 0f) nowAndThenTimer = Random.Range(4f, 10f);
            nowAndThenTimer -= dt;
            if (nowAndThenTimer <= 0f)
            {
                nowAndThenTimer = -1f;
                PlayMoment(AnimationMoment.NowAndThen, out _);
            }
        }
        else
        {
            nowAndThenTimer = -1f;
        }
    }

    // ------------------------------------------------------------------ Editor helpers

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (EditorUtility.IsPersistent(this)) return; // don't build models inside prefab assets in the Project window
        EditorApplication.delayCall -= DelayedRefresh;
        EditorApplication.delayCall += DelayedRefresh;
    }

    private void DelayedRefresh()
    {
        if (this == null || Application.isPlaying) return;
        Refresh();
    }

    public void ClearClips()
    {
        idle = walk = run = jump = fall = attack = hurt = death = null;
        attackLookedFor = hurtLookedFor = deathLookedFor = false;
    }

    /// <summary>
    /// Looks for animation clips in the model file, and in other files in the same folder
    /// (handy for Mixamo, where every animation is its own file).
    /// </summary>
    public void AutoFindClips()
    {
        if (model == null) return;
        clipsFoundFrom = animationsFrom;

        // Animations come from this model, or from the "Animations From" model if one is set
        GameObject source = animationsFrom != null ? animationsFrom : model;
        string modelPath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(modelPath)) return;

        string folder = System.IO.Path.GetDirectoryName(modelPath).Replace('\\', '/');
        // The model that PLAYS the animations decides humanoid vs not
        Avatar modelAvatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<Avatar>().FirstOrDefault();
        bool humanoid = modelAvatar != null && modelAvatar.isHuman;

        List<AnimationClip> clips = new List<AnimationClip>();
        AddClipsFrom(modelPath, clips);

        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == modelPath || System.IO.Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue;
            // Only borrow from animation-only files (e.g. Mixamo "without skin"), never from another creature's model
            GameObject other = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (other != null && other.GetComponentInChildren<Renderer>(true) != null) continue;
            AddClipsFrom(path, clips);
        }

        // A humanoid character can only use humanoid animations (others would break its pose)
        clips.RemoveAll(c => c.humanMotion != humanoid);

        if (idle == null) idle = BestClip(clips, source.name, "idle", "stand", "breath");
        if (walk == null) walk = BestClip(clips, source.name, "walk", "swim", "winding", "slither", "crawl", "trot");
        if (run == null) run = BestClip(clips, source.name, "run", "sprint", "jog", "gallop");
        if (jump == null) jump = BestClip(clips, source.name, "jump", "flap", "takeoff");
        if (fall == null) fall = BestClip(clips, source.name, "fall", "inair", "in_air", "air", "glide");
        if (attack == null) attack = BestClip(clips, source.name, "scratch", "bite", "attack", "punch", "kick", "claw", "strike", "sting", "peck", "swipe", "slash");
        if (hurt == null) hurt = BestClip(clips, source.name, "hit", "hurt", "damage", "flinch", "react");
        if (death == null) death = BestClip(clips, source.name, "death", "die", "dead", "defeat", "dying");
        attackLookedFor = hurtLookedFor = deathLookedFor = true;

        // A model with just one animation (e.g. a flapping bird) plays it all the time
        if (idle == null && walk == null && run == null && clips.Count > 0) idle = clips[0];
    }

    /// <summary>
    /// Every animation a model can use, and which of them it would pick for idle/walk/run/jump/fall
    /// (used by Game Builder > Sounds to list a model's animations).
    /// </summary>
    public static List<AnimationClip> ClipsForModel(GameObject modelAsset, out Dictionary<AnimationClip, string> usedFor)
    {
        usedFor = new Dictionary<AnimationClip, string>();
        GameObject temp = new GameObject("temp") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            CustomModel probe = temp.AddComponent<CustomModel>();
            probe.model = modelAsset;
            probe.AutoFindClips();
            AnimationClip[] picked = { probe.idle, probe.walk, probe.run, probe.jump, probe.fall };
            for (int i = 0; i < picked.Length; i++)
            {
                if (picked[i] == null) continue;
                string slot = ((AnimSlot)i).ToString();
                usedFor[picked[i]] = usedFor.TryGetValue(picked[i], out string s) ? s + ", " + slot : slot;
            }
        }
        finally { DestroyImmediate(temp); }

        // All the clips in the model's file and in animation-only files beside it
        List<AnimationClip> clips = new List<AnimationClip>();
        string modelPath = AssetDatabase.GetAssetPath(modelAsset);
        if (string.IsNullOrEmpty(modelPath)) return clips;
        AddClipsFrom(modelPath, clips);
        string folder = System.IO.Path.GetDirectoryName(modelPath).Replace('\\', '/');
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == modelPath || System.IO.Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue;
            GameObject other = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (other != null && other.GetComponentInChildren<Renderer>(true) != null) continue;
            AddClipsFrom(path, clips);
        }
        foreach (AnimationClip c in usedFor.Keys) if (!clips.Contains(c)) clips.Add(c);

        // The ones it actually uses first
        Dictionary<AnimationClip, string> used = usedFor;
        return clips.OrderBy(c => used.ContainsKey(c) ? 0 : 1).ThenBy(c => c.name).ToList();
    }

    private static void AddClipsFrom(string path, List<AnimationClip> clips)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && !clips.Contains(clip)) clips.Add(clip);
        }
    }

    private static readonly string[] ActionWords = { "idle", "walk", "run", "jog", "jump", "fall", "air", "crawl", "swim", "glide", "flap" };
    private static readonly string[] AvoidWords = { "back", "left", "right", "strafe", "turn", "start", "stop", "end", "land", "attack", "death", "die", "hit" };

    /// <summary>
    /// Picks the best clip for one slot. Keywords are tried IN ORDER (e.g. walk before crawl),
    /// matching whole words in the clip name ("Jump_air" = jump + air, "HumanoidIdle" = humanoid + idle,
    /// "Walking" counts as walk). Among matches: prefer clips named after the model, then simple names.
    /// </summary>
    private static AnimationClip BestClip(List<AnimationClip> clips, string modelName, params string[] keywords)
    {
        List<string> modelWords = Words(modelName).Where(w => w.Length >= 3 && w != "model").ToList();

        foreach (string keyword in keywords)
        {
            AnimationClip best = null;
            int bestScore = int.MinValue;

            foreach (AnimationClip clip in clips)
            {
                List<string> words = Words(clip.name);
                if (!words.Any(w => w.StartsWith(keyword))) continue;

                int score = words.Count == 1 ? 30 : 0;                                   // "Walk" beats "Walk_Carry"
                if (words.Any(w => modelWords.Contains(w))) score += 60;                 // "Zombie_Walk" for a zombie
                foreach (string w in words)
                {
                    if (w.StartsWith(keyword)) continue;
                    if (ActionWords.Any(a => w.StartsWith(a)) && !keywords.Any(k => w.StartsWith(k))) score -= 25; // "IdleJumpUp" isn't a good idle
                    if (AvoidWords.Any(a => w.StartsWith(a))) score -= 30;                // "Walk_Backwards", "Jump_Land"
                    if (w.Length > 2 && !modelWords.Contains(w)) score -= 8;              // extra words like "Crouch_Idle"
                }
                score -= words.Count * 2;

                if (score > bestScore) { bestScore = score; best = clip; }
            }

            if (best != null) return best;
        }
        return null;
    }

    /// <summary>Splits "Zombie_Walk", "HumanoidIdleJumpUp" or "Fly-Glide 2" into lower-case words.</summary>
    private static List<string> Words(string name)
    {
        List<string> words = new List<string>();
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            bool newWord = !char.IsLetterOrDigit(c) || (char.IsUpper(c) && i > 0 && char.IsLower(name[i - 1]));
            if (newWord && current.Length > 0) { words.Add(current.ToString().ToLowerInvariant()); current.Clear(); }
            if (char.IsLetterOrDigit(c)) current.Append(c);
        }
        if (current.Length > 0) words.Add(current.ToString().ToLowerInvariant());
        return words;
    }
#endif
}
