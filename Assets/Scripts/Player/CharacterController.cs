using UnityEngine;
using System.Collections.Generic;
using KinematicCharacterController.Examples;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CharacterController : MonoBehaviour
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
        // 1. Find and instantiate ExamplePlayer prefab as a child
        GameObject spawnedPlayerMesh = SpawnPrefabChild("ExampleCharacter t:Prefab", "Auto Setup Player Mesh");
        
        // 2. Find and instantiate ExampleCamera prefab as a child
        GameObject spawnedCameraMesh = SpawnPrefabChild("ExampleCamera t:Prefab", "Auto Setup Camera Mesh");

        if (spawnedPlayerMesh == null || spawnedCameraMesh == null)
        {
            Debug.LogError("❌ Character setup failed: Could not locate required prefabs in project.");
            return;
        }

        // 3. Ensure ExamplePlayer script is attached to this GameObject
        ExamplePlayer playerScript = GetComponent<ExamplePlayer>();
        if (playerScript == null)
        {
            playerScript = gameObject.AddComponent<ExamplePlayer>();
            Debug.Log($"✅ Attached 'ExamplePlayer' script to '{gameObject.name}'.");
        }

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

        // 7. Assign component references from the newly spawned children
        ExampleCharacterController characterComp = spawnedPlayerMesh.GetComponent<ExampleCharacterController>();
        ExampleCharacterCamera cameraComp = spawnedCameraMesh.GetComponent<ExampleCharacterCamera>();

        if (characterComp != null) playerScript.Character = characterComp;
        if (cameraComp != null) playerScript.CharacterCamera = cameraComp;

        // 8. Hide ExamplePlayer script from Inspector (PlayerLogic remains visible)
       // playerScript.hideFlags = HideFlags.HideInInspector;

        CacheExistingChildren();

        EditorUtility.SetDirty(playerScript);
        EditorUtility.SetDirty(logicScript);
        EditorUtility.SetDirty(healthScript);
        EditorUtility.SetDirty(fallDamageScript);
        EditorUtility.SetDirty(gameObject);

        Debug.Log("🧙‍♂️ Character created, wired up, PlayerLogic, Health, and FallDamage attached successfully!");
#endif
    }

#if UNITY_EDITOR
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