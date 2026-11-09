using UnityEngine;
using System.Collections.Generic;

#if HAS_KINEMATIC_CC
using KinematicCharacterController.Examples;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlayerController : MonoBehaviour
{
    private HashSet<int> knownChildIDs = new HashSet<int>();

    private void OnEnable()
    {
        CacheExistingChildren();
    }

    private void Update()
    {
        if (Application.isPlaying) return;

        if (transform.childCount > knownChildIDs.Count)
        {
            foreach (Transform child in transform)
            {
                int id = child.gameObject.GetInstanceID();

                if (!knownChildIDs.Contains(id))
                {
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;

                    knownChildIDs.Add(id);
                }
            }
        }
        else if (transform.childCount < knownChildIDs.Count)
        {
            CacheExistingChildren();
        }
    }

    public void CreateCharacterMesh()
    {
#if UNITY_EDITOR
        // 1. Reuse the character if there already is one (just fix up anything missing), otherwise make one
        GameObject existingPlayer = FindChildStartingWith("ExampleCharacter");
        bool upgrading = existingPlayer != null;
        GameObject spawnedPlayerMesh = existingPlayer != null ? existingPlayer : SpawnPrefabChild("ExampleCharacter t:Prefab", "Auto Setup Player Mesh");

        // 2. Same for the camera
        GameObject existingCamera = FindChildStartingWith("ExampleCamera");
        GameObject spawnedCameraMesh = existingCamera != null ? existingCamera : SpawnPrefabChild("ExampleCamera t:Prefab", "Auto Setup Camera Mesh");

        if (upgrading)
        {
            Debug.Log("🔧 This level already has a character, so it was updated instead of making a second one.");
        }

        if (spawnedPlayerMesh == null)
        {
            Debug.LogError("❌ Character setup failed: Could not locate required player prefab in project.");
            return;
        }

#if HAS_KINEMATIC_CC
        // 3. Ensure ExamplePlayer script is attached to this GameObject
        ExamplePlayer playerScript = GetComponent<ExamplePlayer>();
        if (playerScript == null)
        {
            playerScript = gameObject.AddComponent<ExamplePlayer>();
            Debug.Log($"✅ Attached 'ExamplePlayer' script to '{gameObject.name}'.");
        }
#endif

        // 4. Ensure PlayerLogic script is attached to this GameObject (visible)
        PlayerLogic logicScript = GetComponent<PlayerLogic>();
        if (logicScript == null)
        {
            logicScript = gameObject.AddComponent<PlayerLogic>();
            Debug.Log($"✅ Attached 'PlayerLogic' script to '{gameObject.name}'.");
        }

        // 5. Ensure Health script is attached to the spawned player mesh
        Health healthScript = spawnedPlayerMesh.GetComponent<Health>();
        if (healthScript == null)
        {
            healthScript = spawnedPlayerMesh.AddComponent<Health>();
            Debug.Log($"✅ Attached 'Health' script to '{spawnedPlayerMesh.name}'.");
        }

        // 6. Ensure FallDamage script is attached to the spawned player mesh
        FallDamage fallDamageScript = spawnedPlayerMesh.GetComponent<FallDamage>();
        if (fallDamageScript == null)
        {
            fallDamageScript = spawnedPlayerMesh.AddComponent<FallDamage>();
            Debug.Log($"✅ Attached 'FallDamage' script to '{spawnedPlayerMesh.name}'.");
        }

        // 7. Add a model slot so students can swap the player's look (see PlayerController's 'Player Model')
        CustomModel playerModel = spawnedPlayerMesh.GetComponent<CustomModel>();
        if (playerModel == null)
        {
            playerModel = spawnedPlayerMesh.AddComponent<CustomModel>();
        }

        // New players get the default character from Game Settings (swap it any time)
        if (playerModel.model == null)
        {
            GameObject defaultModel = GameSettings.Load().defaultPlayerModel;
            if (defaultModel != null)
            {
                playerModel.model = defaultModel;
                playerModel.Refresh();
                Debug.Log($"🧓 Gave the player the default model '{defaultModel.name}'. Change it with 'Player Model' on PlayerController.");
            }
        }

#if HAS_KINEMATIC_CC
        // 8. Assign component references from the newly spawned children
        if (spawnedCameraMesh != null)
        {
            ExampleCharacterController characterComp = spawnedPlayerMesh.GetComponent<ExampleCharacterController>();
            ExampleCharacterCamera cameraComp = spawnedCameraMesh.GetComponent<ExampleCharacterCamera>();

            if (characterComp != null) playerScript.Character = characterComp;
            if (cameraComp != null) playerScript.CharacterCamera = cameraComp;
        }

        EditorUtility.SetDirty(playerScript);
#endif

        CacheExistingChildren();

        EditorUtility.SetDirty(logicScript);
        EditorUtility.SetDirty(healthScript);
        EditorUtility.SetDirty(fallDamageScript);
        EditorUtility.SetDirty(gameObject);

        Debug.Log("🧙‍♂️ Character created, wired up, PlayerLogic, Health, and FallDamage attached successfully!");
#endif
    }

#if UNITY_EDITOR
    private GameObject FindChildStartingWith(string prefix)
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith(prefix)) return child.gameObject;
        }
        return null;
    }

    private GameObject SpawnPrefabChild(string filter, string undoName)
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                Undo.RegisterCreatedObjectUndo(instance, undoName);
                return instance;
            }
        }
        Debug.LogWarning($"⚠️ Could not find asset with search: {filter}");
        return null;
    }
#endif

    private void CacheExistingChildren()
    {
        knownChildIDs.Clear();
        foreach (Transform child in transform)
        {
            knownChildIDs.Add(child.gameObject.GetInstanceID());
        }
    }
}