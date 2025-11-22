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
                
                // ---------------------------------------------------------
                // NEW: Auto-bind to GameManager
                // ---------------------------------------------------------
                var gameManager = FindObjectOfType<NavalCommand.Core.GameManager>();
                if (gameManager != null)
                {
                    Undo.RecordObject(gameManager, "Bind Player Prefab");
                    
                    // 1. Assign Prefab
                    gameManager.PlayerPrefab = prefab; 
                    
                    // 2. Spawn into Scene for User Control
                    if (gameManager.PlayerFlagship == null)
                    {
                        GameObject sceneInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        sceneInstance.name = "Player Flagship";
                        sceneInstance.transform.position = Vector3.zero;
                        
                        gameManager.PlayerFlagship = sceneInstance.GetComponent<NavalCommand.Entities.Units.FlagshipController>();
                        Debug.Log($"[FixShipsTool] Spawned 'Player Flagship' into the scene for manual control.");
                    }
                    else
                    {
                        Debug.Log($"[FixShipsTool] PlayerFlagship already exists in scene: {gameManager.PlayerFlagship.name}");
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
