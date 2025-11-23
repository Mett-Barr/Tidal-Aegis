using UnityEngine;
using UnityEditor;
using NavalCommand.Utils;
using NavalCommand.Data;

namespace NavalCommand.Editor
{
    public class FixShipsTool : EditorWindow
    {
        [MenuItem("Tools/Fix Invisible Ships")]
        public static void FixShips()
        {
            // 0. REGENERATE ALL CONTENT (Fixes Pink Hulls for everyone)
            Debug.Log("[FixShipsTool] Regenerating ALL ship assets to fix materials...");
            ContentGenerator.GenerateContent();

            string prefabPath = "Assets/_Project/Prefabs/Enemies/Ship_SuperFlagship.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"Could not find prefab at {prefabPath}");
                return;
            }

            // Instantiate to modify
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            
            try
            {
                // Ensure ShipBuilder exists
                ShipBuilder builder = instance.GetComponent<ShipBuilder>();
                if (builder == null)
                {
                    builder = instance.AddComponent<ShipBuilder>();
                    Debug.Log("Added missing ShipBuilder component.");
                }

                // Add Debugger
                if (instance.GetComponent<ShipDebugger>() == null)
                {
                    instance.AddComponent<ShipDebugger>();
                    Debug.Log("Added ShipDebugger component.");
                }

                // Configure
                builder.ShipClass = WeightClass.SuperHeavy;
                builder.BuildTrigger = false; // Reset trigger

                // Assign Default Weapon Stats (Crucial for firing)
                string statsPath = "Assets/_Project/Data/Weapons/Weapon_FlagshipGun_Basic.asset";
                WeaponStatsSO stats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
                if (stats != null)
                {
                    builder.DefaultWeaponStats = stats;
                    Debug.Log($"[FixShipsTool] Assigned DefaultWeaponStats: {stats.name}");
                }
                else
                {
                    Debug.LogError($"[FixShipsTool] Could not find WeaponStats at {statsPath}");
                }

                // Build (This generates and saves the mesh)
                builder.BuildShip();

                // ---------------------------------------------------------
                // NEW: Ensure Collider and Layer
                // ---------------------------------------------------------
                // ---------------------------------------------------------
                // NEW: Ensure Collider and Layer
                // ---------------------------------------------------------
                if (instance.GetComponent<Collider>() == null)
                {
                    var col = instance.AddComponent<BoxCollider>();
                    col.size = new Vector3(10, 5, 30); // Approximate size for SuperHeavy
                    Debug.Log("[FixShipsTool] Added missing BoxCollider.");
                }
                
                // Ensure Layer Exists (Editor Only Hack)
                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty layers = tagManager.FindProperty("layers");
                if (LayerMask.NameToLayer("Player") == -1)
                {
                    layers.GetArrayElementAtIndex(6).stringValue = "Player"; // Force Layer 6 to be Player
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[FixShipsTool] Created 'Player' layer at index 6.");
                }

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1) instance.layer = playerLayer;
                else Debug.LogWarning("[FixShipsTool] 'Player' layer still not found!");

                // ---------------------------------------------------------

                // CRITICAL: Find the mesh we just saved and assign it explicitly
                MeshFilter mf = instance.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Debug.Log($"[FixShipsTool] Mesh assigned: {mf.sharedMesh.name}");
                }
                else
                {
                    Debug.LogError("[FixShipsTool] Mesh NOT assigned after build!");
                }

                // Apply changes back to Prefab
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                DestroyImmediate(instance); // Fix: Destroy the temporary instance to prevent duplicates
                
                // ---------------------------------------------------------
                // NEW: Auto-bind to GameManager
                // ---------------------------------------------------------
                var gameManager = FindObjectOfType<NavalCommand.Core.GameManager>();
                if (gameManager != null)
                {
                    Undo.RecordObject(gameManager, "Bind Player Prefab");
                    
                    // 1. Assign Prefab
                    gameManager.PlayerPrefab = prefab; 
                    
                    // 2. Spawn or Replace in Scene
                    GameObject existingShip = GameObject.Find("Player Flagship");
                    if (existingShip == null) existingShip = GameObject.Find("Ship_SuperFlagship");

                    if (existingShip != null)
                    {
                        Debug.Log($"[FixShipsTool] Found existing ship '{existingShip.name}'. Updating it...");
                        
                        // Ensure it's named correctly
                        if (existingShip.name != "Player Flagship") existingShip.name = "Player Flagship";
                        
                        // Unity automatically updates the scene instance from the prefab changes we just applied.
                        // We do NOT revert overrides here to preserve the user's custom position/rotation.
                        
                        gameManager.PlayerFlagship = existingShip.GetComponent<NavalCommand.Entities.Units.FlagshipController>();
                    }
                    else
                    {
                        // Spawn new if missing
                        Vector3 spawnPos = Vector3.zero;
                        Quaternion spawnRot = Quaternion.identity;
                        
                        // Try to spawn where the old one was if GameManager had a ref
                        if (gameManager.PlayerFlagship != null && gameManager.PlayerFlagship.gameObject.scene.IsValid())
                        {
                             spawnPos = gameManager.PlayerFlagship.transform.position;
                             spawnRot = gameManager.PlayerFlagship.transform.rotation;
                        }

                        GameObject sceneInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        sceneInstance.name = "Player Flagship";
                        sceneInstance.transform.position = spawnPos;
                        sceneInstance.transform.rotation = spawnRot;
                        
                        gameManager.PlayerFlagship = sceneInstance.GetComponent<NavalCommand.Entities.Units.FlagshipController>();
                        Debug.Log($"[FixShipsTool] Spawned 'Player Flagship' at {spawnPos}.");
                    }

                    // 3. Select it for the user
                    if (gameManager.PlayerFlagship != null)
                    {
                        Selection.activeGameObject = gameManager.PlayerFlagship.gameObject;
                        EditorGUIUtility.PingObject(gameManager.PlayerFlagship.gameObject);
                    }
                    
                    EditorUtility.SetDirty(gameManager);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); // FORCE SAVE
                    Debug.Log($"<color=green>[FixShipsTool] SCENE UPDATED. You can now configure the ship in the Hierarchy!</color>");
                }
                else
                {
                    Debug.LogError("[FixShipsTool] Could not find GameManager in scene!");
                }

                // ---------------------------------------------------------
                // NEW: Auto-assign Physics Config
                // ---------------------------------------------------------
                var physicsSystem = FindObjectOfType<NavalCommand.Systems.WorldPhysicsSystem>();
                if (physicsSystem == null)
                {
                    GameObject go = new GameObject("WorldPhysicsSystem");
                    physicsSystem = go.AddComponent<NavalCommand.Systems.WorldPhysicsSystem>();
                    Debug.Log("[FixShipsTool] Created missing WorldPhysicsSystem.");
                }

                if (physicsSystem.Config == null)
                {
                    string configPath = "Assets/_Project/Data/PhysicsConfig.asset";
                    PhysicsConfigSO config = AssetDatabase.LoadAssetAtPath<PhysicsConfigSO>(configPath);
                    if (config != null)
                    {
                        physicsSystem.Config = config;
                        EditorUtility.SetDirty(physicsSystem);
                        Debug.Log($"[FixShipsTool] Assigned PhysicsConfig to WorldPhysicsSystem.");
                    }
                    else
                    {
                        Debug.LogError($"[FixShipsTool] Could not find PhysicsConfig at {configPath}");
                    }
                }


                Debug.Log("<color=green>SUCCESS: Super Flagship fixed! Mesh regenerated (URP/Standard) and bound to GameManager.</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to fix ship: {e.Message}");
            }
            finally
            {
                // Cleanup
                if (instance != null) DestroyImmediate(instance);
            }
        }
    }
}
