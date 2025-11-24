using UnityEngine;
using UnityEditor;
using NavalCommand.Core;
using NavalCommand.Systems;
using NavalCommand.UI;
using NavalCommand.Entities.Units;
using NavalCommand.Data;
using UnityEngine.EventSystems;

namespace NavalCommand.Editor.Tooling
{
    public static class HierarchyRestorer
    {
        public static void RestoreHierarchy()
        {
            Debug.Log("Starting Hierarchy Restoration...");

            // Ensure Data Integrity
            NavalCommand.Utils.ContentGenerator.RebuildAllContent(); // Regenerate all assets to fix 0-values

            RestorePoolManager(); // Infrastructure first
            RestoreWorldPhysicsSystem(); // Physics first
            RestoreSpatialGridSystem(); // Grid second
            RestoreGameManager();
            RestoreSpawningSystem();
            RestoreHUD();
            RestoreEventSystem();
            RestoreLighting();
            RestoreCamera();

            Debug.Log("Hierarchy Restoration Complete!");
        }

        private static void RestoreGameManager()
        {
            GameManager gm = Object.FindObjectOfType<GameManager>();
            if (gm == null)
            {
                GameObject go = new GameObject("GameManager");
                gm = go.AddComponent<GameManager>();
                Debug.Log("Created GameManager");
            }

            // Strict Player Ship Enforcement
            FlagshipController playerShip = null;
            
            // Find ALL flagships (including inactive) to prevent duplicates
            var allShips = Object.FindObjectsOfType<FlagshipController>(true);
            
            if (allShips.Length > 0)
            {
                // Prioritize the one named "Player_SuperFlagship"
                foreach (var ship in allShips)
                {
                    if (ship.name == "Player_SuperFlagship")
                    {
                        playerShip = ship;
                        break;
                    }
                }
                
                // If not found by name, take the first one
                if (playerShip == null) playerShip = allShips[0];
                
                // Destroy duplicates (User requested "Clear meaningless generation")
                foreach (var ship in allShips)
                {
                    if (ship != playerShip)
                    {
                        Debug.LogWarning($"[HierarchyRestorer] Destroying duplicate Flagship: {ship.name}");
                        Object.DestroyImmediate(ship.gameObject);
                    }
                }
            }

            // If still no ship, create one
            if (playerShip == null)
            {
                string prefabPath = "Assets/_Project/Prefabs/Enemies/Ship_SuperFlagship.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    GameObject shipInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    shipInstance.name = "Player_SuperFlagship";
                    shipInstance.transform.position = Vector3.zero;
                    shipInstance.transform.rotation = Quaternion.identity;
                    
                    playerShip = shipInstance.GetComponent<FlagshipController>();
                    gm.PlayerPrefab = prefab; // Also assign prefab for reference
                    Debug.Log("Instantiated Player_SuperFlagship");
                }
                else
                {
                    Debug.LogError($"Could not find Super Flagship prefab at {prefabPath}");
                }
            }

            // Final Assignment
            if (playerShip != null)
            {
                if (playerShip.gameObject.name != "Player_SuperFlagship")
                {
                    playerShip.gameObject.name = "Player_SuperFlagship";
                }
                
                gm.PlayerFlagship = playerShip;
                EditorUtility.SetDirty(gm);
                Debug.Log("Assigned Player_SuperFlagship to GameManager");
            }
        }

        private static void RestoreSpawningSystem()
        {
            SpawningSystem spawner = Object.FindObjectOfType<SpawningSystem>();
            if (spawner == null)
            {
                GameObject go = new GameObject("SpawningSystem");
                spawner = go.AddComponent<SpawningSystem>();
                Debug.Log("Created SpawningSystem");
            }

            // Configure Spawning System (Force Update)
            string[] enemyNames = new string[]
            {
                "Ship_Kamikaze",
                "Ship_Light_FlagshipGun",
                "Ship_Light_Missile",
                "Ship_Light_Torpedo",
                "Ship_Light_Autocannon",
                "Ship_Light_CIWS"
            };

            System.Collections.Generic.List<GameObject> prefabs = new System.Collections.Generic.List<GameObject>();
            foreach (string name in enemyNames)
            {
                string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"Could not find enemy prefab: {name}");
                }
            }

            spawner.EnemyPrefabs = prefabs.ToArray();
            spawner.Mode = SpawnMode.Random; // Force Random
            spawner.SpecificPrefabName = ""; // Clear specific name
            spawner.SpecificEnemyIndex = 0;
            
            EditorUtility.SetDirty(spawner);
            Debug.Log($"Configured SpawningSystem with {prefabs.Count} enemies (Mode: Random)");
        }

        private static void RestoreWorldPhysicsSystem()
        {
            WorldPhysicsSystem physics = Object.FindObjectOfType<WorldPhysicsSystem>();
            if (physics == null)
            {
                GameObject go = new GameObject("WorldPhysicsSystem");
                physics = go.AddComponent<WorldPhysicsSystem>();
                Debug.Log("Created WorldPhysicsSystem");
            }

            if (physics.Config == null)
            {
                string path = "Assets/_Project/Data/PhysicsConfig.asset";
                PhysicsConfigSO config = AssetDatabase.LoadAssetAtPath<PhysicsConfigSO>(path);
                if (config != null)
                {
                    physics.Config = config;
                    Debug.Log("Assigned PhysicsConfig to WorldPhysicsSystem");
                }
                else
                {
                    Debug.LogError($"Could not find PhysicsConfig at {path}");
                }
            }
        }

        private static void RestorePoolManager()
        {
            if (Object.FindObjectOfType<NavalCommand.Infrastructure.PoolManager>() == null)
            {
                GameObject go = new GameObject("PoolManager");
                go.AddComponent<NavalCommand.Infrastructure.PoolManager>();
                Debug.Log("Created PoolManager");
            }
        }

        private static void RestoreSpatialGridSystem()
        {
            if (Object.FindObjectOfType<SpatialGridSystem>() == null)
            {
                GameObject go = new GameObject("SpatialGridSystem");
                go.AddComponent<SpatialGridSystem>();
                Debug.Log("Created SpatialGridSystem");
            }
        }

        private static void RestoreHUD()
        {
            if (Object.FindObjectOfType<HUDManager>() == null)
            {
                // Try to load prefab first if it exists, otherwise create from scratch
                string prefabPath = "Assets/_Project/Prefabs/UI/HUD.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.name = "HUD";
                    Debug.Log("Restored HUD from Prefab");
                }
                else
                {
                    // Create basic structure
                    GameObject go = new GameObject("HUD");
                    var canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    go.AddComponent<UnityEngine.UI.CanvasScaler>();
                    go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    go.AddComponent<HUDManager>();
                    Debug.Log("Created HUD (Basic)");
                }
            }
        }

        private static void RestoreEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                Debug.Log("Created EventSystem");
            }
        }

        private static void RestoreLighting()
        {
            if (Object.FindObjectOfType<Light>() == null)
            {
                GameObject go = new GameObject("Directional Light");
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                go.transform.rotation = Quaternion.Euler(50, -30, 0);
                Debug.Log("Created Directional Light");
            }
        }

        private static void RestoreCamera()
        {
            Camera cam = Object.FindObjectOfType<Camera>();
            GameObject camObj;

            if (cam == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                Debug.Log("Created Main Camera");
            }
            else
            {
                camObj = cam.gameObject;
            }

            // Add and Configure CameraSystem
            CameraSystem camSys = camObj.GetComponent<CameraSystem>();
            if (camSys == null)
            {
                camSys = camObj.AddComponent<CameraSystem>();
            }

            // Configure Settings
            camSys.Angle = 60f;
            camSys.Distance = 250f;
            
            // Try to assign target immediately if possible
            GameManager gm = Object.FindObjectOfType<GameManager>();
            if (gm != null && gm.PlayerFlagship != null)
            {
                camSys.Target = gm.PlayerFlagship.transform;
                camSys.SnapToTarget(); // Force initial position
            }

            Debug.Log("Configured CameraSystem (Angle: 60, Dist: 250)");
        }
    }
}
