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
    [Range(0.1f, 5f)] public float size = 1f;

    [Tooltip("Turn the model if it faces the wrong way.")]
    [Range(-180f, 180f)] public float turn = 0f;

    [Tooltip("Move the model up or down if it floats above or sinks into the ground.")]
    [Range(-3f, 3f)] public float raiseLower = 0f;

    [Tooltip("Plays when standing still.")] public AnimationClip idle;
    [Tooltip("Plays when moving.")] public AnimationClip walk;
    [Tooltip("Plays when moving fast (sprinting).")] public AnimationClip run;
    [Tooltip("Plays when going up in the air.")] public AnimationClip jump;
    [Tooltip("Plays when falling.")] public AnimationClip fall;

    [Tooltip("Speed up or slow down all animations.")]
    [Range(0.1f, 3f)] public float animationSpeed = 1f;

    // Saved results of fitting the model (so we don't re-measure every frame)
    [SerializeField, HideInInspector] private GameObject builtFrom;
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

    // ------------------------------------------------------------------ Building the model

    /// <summary>Re-creates the model if it changed, otherwise just re-applies the size/turn/height sliders.</summary>
    public void Refresh()
    {
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

        GameObject instance = CreateInstance(model);
        instance.name = ModelChildPrefix + " - " + model.name;

        // Measure the model at the world origin, unrotated and unscaled
        instance.transform.SetParent(null, false);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        Bounds modelBounds = MeasureRenderers(instance.transform, null, out bool hasRenderers);
        if (!hasRenderers) modelBounds = new Bounds(Vector3.zero, Vector3.one);

        // Turn off the model's own physics so it can't get in the player's way
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Transform parent = GetVisualParent();
        GetTargetArea(parent, out Vector3 targetBottomCentre, out float targetHeight);

        instance.transform.SetParent(parent, false);
        instance.layer = gameObject.layer;

        fitScale = targetHeight / Mathf.Max(modelBounds.size.y, 0.001f);
        modelAnchor = new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
        targetAnchor = targetBottomCentre;
        modelInstance = instance.transform;
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
    private void GetTargetArea(Transform parent, out Vector3 bottomCentre, out float height)
    {
#if HAS_KINEMATIC_CC
        CapsuleCollider capsule = GetComponent<KinematicCharacterMotor>() != null ? GetComponent<CapsuleCollider>() : null;
        if (capsule != null)
        {
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
            height = b.size.y;
            bottomCentre = new Vector3(b.center.x, b.min.y, b.center.z);
            return;
        }

        Bounds world = MeasureRenderers(transform, modelInstance, out bool found);
        if (found)
        {
            height = world.size.y / Mathf.Max(parent.lossyScale.y, 0.001f);
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

        Animator animator = modelInstance.GetComponent<Animator>();
        if (animator == null) animator = modelInstance.gameObject.AddComponent<Animator>();
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
        mixer = AnimationMixerPlayable.Create(graph, clipCount);

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
        }

        // Smoothly blend between animations
        float total = 0f;
        for (int i = 0; i < 5; i++)
        {
            weights[i] = Mathf.MoveTowards(weights[i], i == (int)currentSlot ? 1f : 0f, dt * 8f);
            total += weights[i];
        }
        if (total > 0f) for (int i = 0; i < 5; i++) weights[i] /= total;
        ApplyWeights();
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
            mixer.SetInputWeight(mixerInput[i], weights[i]);
            clipPlayables[i].SetSpeed(animationSpeed);
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
        idle = walk = run = jump = fall = null;
    }

    /// <summary>
    /// Looks for animation clips in the model file, and in other files in the same folder
    /// (handy for Mixamo, where every animation is its own file).
    /// </summary>
    public void AutoFindClips()
    {
        if (model == null) return;
        string modelPath = AssetDatabase.GetAssetPath(model);
        if (string.IsNullOrEmpty(modelPath)) return;

        string folder = System.IO.Path.GetDirectoryName(modelPath).Replace('\\', '/');
        Avatar modelAvatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
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

        if (idle == null) idle = BestClip(clips, model.name, "idle", "stand", "breath");
        if (walk == null) walk = BestClip(clips, model.name, "walk");
        if (run == null) run = BestClip(clips, model.name, "run", "sprint", "jog", "gallop");
        if (jump == null) jump = BestClip(clips, model.name, "jump");
        if (fall == null) fall = BestClip(clips, model.name, "fall", "inair", "in_air", "air");

        // A model with just one animation (e.g. a flapping bird) plays it all the time
        if (idle == null && walk == null && run == null && clips.Count > 0) idle = clips[0];
    }

    private static void AddClipsFrom(string path, List<AnimationClip> clips)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && !clips.Contains(clip)) clips.Add(clip);
        }
    }

    private static AnimationClip BestClip(List<AnimationClip> clips, string modelName, params string[] keywords)
    {
        AnimationClip best = null;
        int bestScore = 0;
        string[] avoid = { "back", "left", "right", "strafe", "turn", "start", "stop", "end", "land", "attack", "death", "die", "hit" };

        foreach (AnimationClip clip in clips)
        {
            string name = (clip.name + " " + System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(clip))).ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (!name.Contains(keyword)) continue;
                int score = clip.name.ToLowerInvariant() == keyword ? 100 : 50;
                // Prefer clips named after the model (e.g. "Zombie_Walk" for a zombie), then shorter, simpler names
                foreach (string word in modelName.ToLowerInvariant().Split(new[] { '-', '_', ' ', '.' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    if (word.Length >= 3 && word != "model" && clip.name.ToLowerInvariant().Contains(word)) score += 60;
                }
                score -= Mathf.Max(0, clip.name.Length - keyword.Length) / 3;
                foreach (string bad in avoid) if (name.Contains(bad)) score -= 30;
                // Other action words in the name (e.g. "IdleJumpUp" for idle) make it a worse match
                foreach (string other in new[] { "idle", "walk", "run", "jump", "fall", "air" })
                {
                    if (other != keyword && System.Array.IndexOf(keywords, other) < 0 && name.Contains(other)) score -= 20;
                }
                if (score > bestScore) { bestScore = score; best = clip; }
            }
        }
        return best;
    }
#endif
}
