using System.Collections.Generic;
using UnityEngine;

/// <summary>Scatter settings for creatures and pickups (lives on the terrain).</summary>
[DisallowMultipleComponent]
public class CreatureScatter : MonoBehaviour
{
    public enum Kind { Wanderer, Patroller, Guard, Health, Stamina, Shield, SuperJump, SpeedBoost, BounceBall }

    [System.Serializable]
    public class Group
    {
        public string name = "Spiders";
        [Tooltip("Untick to leave this group out when you click Scatter All.")]
        public bool include = true;
        public Kind kind = Kind.Wanderer;
        [Range(1, 2000)] public int howMany = 10;
        [Tooltip("Random size (1 = normal size).")]
        public Vector2 sizeRange = new Vector2(0.8f, 1.3f);
        [Tooltip("Random speed for creatures that move.")]
        public Vector2 speedRange = new Vector2(2f, 5f);
        [Tooltip("How far wanderers roam, or how far patrollers walk (metres).")]
        public Vector2 roamRange = new Vector2(4f, 10f);
        [Tooltip("Ground ones float (or fly) this high above the ground, in metres. 0 = on the ground. Great for dragons and birds!")]
        public Vector2 flyHeightRange = Vector2.zero;
        [Tooltip("Pickups and bounce balls: run around (wander) instead of staying still. Catch them if you can!")]
        public bool pickupsWander = false;
        [Tooltip("3D models to pick from at random. Leave empty to use the normal look.")]
        public List<GameObject> models = new List<GameObject>();
        [Tooltip("Creatures: how many times the player must jump on its head to beat it. 0 = can't be beaten.")]
        [Range(0, 100)] public int jumpsToDefeat = 1;
        [Tooltip("Make them glow.")]
        public bool glow = false;

        [Header("Where can they go?")]
        public bool onGround = true;
        public bool onStillPlatforms = false;
        public bool onMovingPlatforms = false;
    }

    public List<Group> groups = new List<Group>();
    public ScatterPlace place = new ScatterPlace();

    private void Reset()
    {
        groups = new List<Group>
        {
            new Group { name = "Wandering creatures", kind = Kind.Wanderer, howMany = 10 },
            new Group { name = "Patrolling creatures", kind = Kind.Patroller, howMany = 5 },
            new Group { name = "Health pickups", kind = Kind.Health, howMany = 6, sizeRange = Vector2.one, onStillPlatforms = true, glow = true },
            new Group { name = "Super Jumps", kind = Kind.SuperJump, howMany = 3, sizeRange = Vector2.one, include = false, glow = true },
        };
    }
}
