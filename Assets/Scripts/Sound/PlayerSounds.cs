using UnityEngine;

#if HAS_KINEMATIC_CC
using KinematicCharacterController;
using KinematicCharacterController.Examples;
#endif

/// <summary>
/// The player's footsteps (Run / Sprint) and Jump sound. Added to the player automatically by the SoundManager.
/// </summary>
public class PlayerSounds : MonoBehaviour
{
    private AudioSource steps;
    private float stepTimer;
    private SoundEntry playingEntry;

#if HAS_KINEMATIC_CC
    private KinematicCharacterMotor motor;
    private ExampleCharacterController controller;
#endif
    private PlayerLogic logic;
    private CustomModel model;

    private void Awake()
    {
        steps = gameObject.AddComponent<AudioSource>();
        steps.playOnAwake = false;
        steps.spatialBlend = 0f;

#if HAS_KINEMATIC_CC
        motor = GetComponent<KinematicCharacterMotor>();
        controller = GetComponent<ExampleCharacterController>();
        if (controller != null) controller.OnJumped += OnJumped;
#endif
        model = GetComponentInChildren<CustomModel>();
        logic = GetComponentInParent<PlayerLogic>();
        if (logic == null) logic = FindAnyObjectByType<PlayerLogic>();
    }

    private void OnDestroy()
    {
#if HAS_KINEMATIC_CC
        if (controller != null) controller.OnJumped -= OnJumped;
#endif
    }

    private void OnJumped()
    {
        // The model's own jump-animation sound (if it has one) plays instead
        if (model != null && model.HasOwnSound(CustomModel.AnimSlot.Jump)) return;
        SoundManager.Play(SoundEvent.Jump, transform.position, gameObject);
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return; // paused

        bool moving = false;
#if HAS_KINEMATIC_CC
        if (motor != null)
        {
            Vector3 flat = motor.BaseVelocity;
            flat.y = 0f;
            moving = motor.GroundingStatus.IsStableOnGround && flat.sqrMagnitude > 1f;
        }
#endif
        bool sprinting = moving && logic != null && logic.IsSprinting;

        // The model's own walk/run animation sounds (if it has them) replace the general footsteps
        if (moving && model != null && (model.HasOwnSound(CustomModel.AnimSlot.Walk) || model.HasOwnSound(CustomModel.AnimSlot.Run)))
            moving = false;

        // Sprint uses the Run sound (faster) if it has no sound of its own
        SoundEntry entry = null;
        if (moving)
        {
            entry = sprinting ? SoundManager.Find(SoundEvent.Sprint, gameObject) : null;
            if (entry == null) entry = SoundManager.Find(SoundEvent.Run, gameObject);
        }

        if (entry == null)
        {
            // Stopped: cut off a long running loop, let a short footstep finish
            if (steps.isPlaying && steps.clip != null && steps.clip.length > 1f) steps.Stop();
            stepTimer = 0f;
            playingEntry = null;
            return;
        }

        float gap = entry.repeatEvery;
        if (sprinting && entry.when == SoundEvent.Run) gap *= 0.7f;

        if (entry != playingEntry && steps.isPlaying && steps.clip != null && steps.clip.length > 1f) steps.Stop();
        playingEntry = entry;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f && !steps.isPlaying)
        {
            steps.clip = entry.clip;
            steps.volume = entry.volume * SoundManager.EffectsVolume;
            steps.pitch = entry.varyPitch ? Random.Range(0.9f, 1.1f) : 1f;
            steps.Play();
            stepTimer = gap;
        }
    }
}
