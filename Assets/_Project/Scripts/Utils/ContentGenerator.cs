#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using NavalCommand.Data;
using NavalCommand.Core; // Added for BallisticsConfig
using NavalCommand.Entities.Projectiles;
using NavalCommand.Entities.Components;
using NavalCommand.Entities.Units;
using System.IO;

namespace NavalCommand.Utils
{
    public class ContentGenerator : MonoBehaviour
    {
        [MenuItem("NavalCommand/Generate Basic Content")]
        public static void GenerateContent()
        {
            EnsureDirectories();
            GenerateProjectiles();
            GenerateWeaponStats();
            GeneratePhysicsConfig();
            GenerateShips();
            SetupSpawningSystem();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Basic Content Generated Successfully!");
        }

        private static void GeneratePhysicsConfig()
        {
            string path = "Assets/_Project/Data/PhysicsConfig.asset";
            PhysicsConfigSO config = AssetDatabase.LoadAssetAtPath<PhysicsConfigSO>(path);
            
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PhysicsConfigSO>();
                AssetDatabase.CreateAsset(config, path);
            }

            // Set Defaults
            config.GlobalSpeedScale = 0.05f;
            config.GlobalRangeScale = 1f;
            config.StandardGravity = 9.81f;
            
            EditorUtility.SetDirty(config);
        }

        private static void EnsureDirectories()
        {
            CreateDir("Assets/_Project/Prefabs/Projectiles");
            CreateDir("Assets/_Project/Prefabs/Enemies");
            CreateDir("Assets/_Project/Data/Weapons");
        }

        private static void CreateDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void GenerateProjectiles()
        {
            foreach (var config in WeaponRegistry.AllWeapons)
            {
                CreateProjectile(config);
            }
        }

        private static void CreateProjectile(WeaponConfig config)
        {
            string path = $"Assets/_Project/Prefabs/Projectiles/{config.ProjectileName}.prefab";
            
            // Always recreate to ensure updates
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            GameObject go = new GameObject(config.ProjectileName);
            
            // Create Model
            CreateProjectileModel(go, config.ProjectileStyle, config.ProjectileColor);

            // Physics
            // Physics
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false; // Logic handles gravity now
            rb.isKinematic = true; // Logic handles movement
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Behavior
            var proj = go.AddComponent<ProjectileBehavior>();
            proj.MovementLogicName = config.MovementLogicName;
            proj.Speed = config.ProjectileSpeed;
            proj.Damage = config.Damage;
            
            // Advanced Settings
            proj.CruiseHeight = config.CruiseHeight;
            proj.TerminalHomingDistance = config.TerminalHomingDistance;
            proj.VerticalLaunchHeight = config.VerticalLaunchHeight;
            proj.TurnRate = config.TurnRate;

            // Collider (Approximate based on style)
            if (config.ProjectileStyle.Contains("Tracer"))
            {
                var col = go.AddComponent<CapsuleCollider>();
                col.direction = 2; // Z-axis
                col.radius = 0.1f;
                col.height = 1f;
                col.isTrigger = true;
            }
            else
            {
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.5f, 0.5f, 2f);
                col.isTrigger = true;
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private static void CreateProjectileModel(GameObject parent, string style, Color color)
        {
            // 1. Create and Save Material
            string matPath = $"Assets/_Project/Generated/Materials/ProjectileMat_{style}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            if (mat == null)
            {
                // Ensure directory exists
                string dir = "Assets/_Project/Generated/Materials";
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                // Create new material
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard"); // Fallback

                mat.color = color;
                if (style.Contains("Tracer"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 2f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                // Update existing material color just in case
                mat.color = color;
                if (style.Contains("Tracer"))
                {
                    mat.SetColor("_EmissionColor", color * 2f);
                }
                EditorUtility.SetDirty(mat);
            }

            GameObject model = new GameObject("Model");
            model.transform.SetParent(parent.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            switch (style)
            {
                case "Shell": // Large Caliber Shell
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 0.8f, 0.4f), new Vector3(0, 0, 0), new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.38f, 0.4f, 0.4f), new Vector3(0, 0, 0.8f), Vector3.zero, mat); // Nose
                    break;

                case "Missile": // VLS Missile
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.3f, 1.5f, 0.3f), new Vector3(0, 0, 0), new Vector3(90, 0, 0), mat); // Body
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.28f, 0.5f, 0.28f), new Vector3(0, 0, 1.5f), new Vector3(90, 0, 0), mat); // Nose
                    // Fins
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(1.2f, 0.05f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 1.2f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    break;

                case "Torpedo": // Underwater Torpedo
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 2f, 0.4f), new Vector3(0, 0, 0), new Vector3(90, 0, 0), mat); // Body
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0, 0, 2f), Vector3.zero, mat); // Nose
                    // Propeller/Fins
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.8f, 0.05f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 0.8f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    break;

                case "Tracer": // Autocannon
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.15f, 1f, 0.15f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    break;

                case "Tracer_Small": // CIWS
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.08f, 0.6f, 0.08f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    break;
            }
        }

        private static void CreatePrimitive(GameObject parent, PrimitiveType type, Vector3 scale, Vector3 pos, Vector3 rot, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = pos;
            obj.transform.localRotation = Quaternion.Euler(rot);
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(obj.GetComponent<Collider>()); // Remove physics from visual parts
        }

        private static void GenerateWeaponStats()
        {
            foreach (var config in WeaponRegistry.AllWeapons)
            {
                CreateWeaponStats(config);
            }
        }

        private static void CreateWeaponStats(WeaponConfig config)
        {
            string path = $"Assets/_Project/Data/Weapons/{config.ID}.asset";
            WeaponStatsSO so = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(path);
            
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<WeaponStatsSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.DisplayName = config.DisplayName;
            so.Type = config.Type;
            so.TargetType = config.TargetType;
            so.SetBaseRange(config.Range);
            so.SetBaseCooldown(config.Cooldown);
            so.SetBaseDamage(config.Damage);
            so.SetBaseProjectileSpeed(config.ProjectileSpeed);
            so.SetBaseGravityMultiplier(1f); // Default to 1
            so.SetBaseRotationSpeed(config.RotationSpeed);
            so.SetBaseSpread(config.Spread);
            
            string projPath = $"Assets/_Project/Prefabs/Projectiles/{config.ProjectileName}.prefab";
            so.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
            
            // Default mask (Everything)
            so.TargetMask = ~0; 
            
            EditorUtility.SetDirty(so);
        }

        private static void GenerateShips()
        {
            // Create a temporary ShipBuilder to use its methods
            GameObject tempBuilderObj = new GameObject("TempBuilder");
            ShipBuilder builder = tempBuilderObj.AddComponent<ShipBuilder>();

            try
            {
                CreateModularShip(builder, "Ship_Light_FlagshipGun", "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);
                CreateModularShip(builder, "Ship_Light_Missile", "Weapon_Missile_Basic", WeaponType.Missile);
                CreateModularShip(builder, "Ship_Light_Torpedo", "Weapon_Torpedo_Basic", WeaponType.Torpedo);
                CreateModularShip(builder, "Ship_Light_Autocannon", "Weapon_Autocannon_Basic", WeaponType.Autocannon);
                CreateModularShip(builder, "Ship_Light_CIWS", "Weapon_CIWS_Basic", WeaponType.CIWS);
                CreateKamikazeShip(builder);
                CreateSuperFlagship(builder);
            }
            finally
            {
                DestroyImmediate(tempBuilderObj);
            }
        }

        private static void CreateModularShip(ShipBuilder builder, string name, string weaponStatsName, WeaponType weaponType)
        {
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            
            // Always recreate to ensure updates
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // Overwrite logic handled by SaveAsPrefabAsset
            }

            // 1. Create Hull (This is now the Root)
            GameObject shipRoot = builder.CreateHullModule(WeightClass.Light);
            shipRoot.name = name;
            shipRoot.transform.localPosition = Vector3.zero;
            shipRoot.transform.localRotation = Quaternion.identity;

            // 2. Find Mount Point (Light hull has MountPoint_1 at front)
            Transform mountPoint = shipRoot.transform.Find("MountPoint_1");
            if (mountPoint != null)
            {
                // 3. Create Weapon
                GameObject weaponVisual = builder.CreateWeaponModule(weaponType);
                weaponVisual.transform.SetParent(mountPoint);
                weaponVisual.transform.localPosition = Vector3.zero;
                weaponVisual.transform.localRotation = Quaternion.identity;

                // 4. Setup Weapon Logic
                Transform firePoint = weaponVisual.transform.Find("FirePoint");
                if (firePoint == null)
                {
                    firePoint = new GameObject("FirePoint").transform;
                    firePoint.SetParent(weaponVisual.transform);
                    // Move it further forward (z=4) and higher (y=2) to clear the hull collider
                    firePoint.localPosition = new Vector3(0, 2f, 4f); 
                }

                WeaponController wc = weaponVisual.AddComponent<WeaponController>();
                string statsPath = $"Assets/_Project/Data/Weapons/{weaponStatsName}.asset";
                wc.WeaponStats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
                wc.FirePoint = firePoint;
                wc.OwnerTeam = NavalCommand.Core.Team.Enemy;
            }
            else
            {
                Debug.LogError($"MountPoint_1 not found on Light Hull for {name}");
            }

            // 5. Add Unit Components to Root
            var rb = shipRoot.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var unit = shipRoot.AddComponent<EnemyUnit>();
            unit.UnitTeam = NavalCommand.Core.Team.Enemy;

            var col = shipRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 2f, 8f); // Approx size for Light Hull
            col.center = new Vector3(0, 1f, 0);

            // 6. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(shipRoot, path);
            DestroyImmediate(shipRoot);
        }

        private static void CreateKamikazeShip(ShipBuilder builder)
        {
            string name = "Ship_Kamikaze";
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            
            // Always recreate
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            // 1. Create Hull (Light)
            GameObject shipRoot = builder.CreateHullModule(WeightClass.Light);
            shipRoot.name = name;
            shipRoot.transform.localPosition = Vector3.zero;
            shipRoot.transform.localRotation = Quaternion.identity;

            // 2. Add Components
            var rb = shipRoot.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var unit = shipRoot.AddComponent<KamikazeController>();
            unit.UnitTeam = NavalCommand.Core.Team.Enemy;
            unit.MaxHP = 30f;

            var col = shipRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 2f, 8f);
            col.center = new Vector3(0, 1f, 0);

            // 3. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(shipRoot, path);
            DestroyImmediate(shipRoot);
        }

        private static void CreateSuperFlagship(ShipBuilder builder)
        {
            string name = "Ship_SuperFlagship";
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            
            // Always recreate
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            // 1. Create Hull (SuperHeavy)
            GameObject shipRoot = builder.CreateHullModule(WeightClass.SuperHeavy);
            shipRoot.name = name;
            shipRoot.transform.localPosition = Vector3.zero;
            shipRoot.transform.localRotation = Quaternion.identity;

            // 2. Attach Weapons
            // Mounts 1-3: Main Guns (FlagshipGun)
            AttachWeapon(builder, shipRoot, 1, "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);
            AttachWeapon(builder, shipRoot, 2, "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);
            AttachWeapon(builder, shipRoot, 3, "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);

            // Mounts 4-5: Secondary Guns (FlagshipGun)
            AttachWeapon(builder, shipRoot, 4, "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);
            AttachWeapon(builder, shipRoot, 5, "Weapon_FlagshipGun_Basic", WeaponType.FlagshipGun);

            // Mounts 6-7: Missiles
            AttachWeapon(builder, shipRoot, 6, "Weapon_Missile_Basic", WeaponType.Missile);
            AttachWeapon(builder, shipRoot, 7, "Weapon_Missile_Basic", WeaponType.Missile);

            // Mounts 8-9: Torpedoes
            AttachWeapon(builder, shipRoot, 8, "Weapon_Torpedo_Basic", WeaponType.Torpedo);
            AttachWeapon(builder, shipRoot, 9, "Weapon_Torpedo_Basic", WeaponType.Torpedo);

            // Mounts 10-11: Autocannons
            AttachWeapon(builder, shipRoot, 10, "Weapon_Autocannon_Basic", WeaponType.Autocannon);
            AttachWeapon(builder, shipRoot, 11, "Weapon_Autocannon_Basic", WeaponType.Autocannon);

            // Mounts 12-13: CIWS
            AttachWeapon(builder, shipRoot, 12, "Weapon_CIWS_Basic", WeaponType.CIWS);
            AttachWeapon(builder, shipRoot, 13, "Weapon_CIWS_Basic", WeaponType.CIWS);

            // 3. Add Components
            var rb = shipRoot.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Flagship Controller (Player)
            var unit = shipRoot.AddComponent<FlagshipController>();
            unit.MaxHP = 50000f; // Massive HP for Super Flagship
            // Note: FlagshipController usually handles input. 
            // If this is an enemy Super Flagship, we might need a different controller.
            // But the user asked for "Super Flagship", implying it might be for the player or a boss.
            // Given "SpawnEnemy" context, it might be an enemy.
            // But "Flagship" usually implies Player.
            // The prompt says "Implement a massive 'Super Flagship'... Update Scene: Find GameManager... ensure player uses this new prefab".
            // So it IS for the player.
            // FlagshipController is correct.

            var col = shipRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(10f, 5f, 80f); // Approx size for SuperHeavy
            col.center = new Vector3(0, 2.5f, 0);

            // 4. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(shipRoot, path);
            DestroyImmediate(shipRoot);
        }

        private static void AttachWeapon(ShipBuilder builder, GameObject shipRoot, int mountIndex, string weaponStatsName, WeaponType type)
        {
            Transform mountPoint = shipRoot.transform.Find($"MountPoint_{mountIndex}");
            if (mountPoint != null)
            {
                GameObject weaponVisual = builder.CreateWeaponModule(type);
                weaponVisual.transform.SetParent(mountPoint);
                weaponVisual.transform.localPosition = Vector3.zero;
                weaponVisual.transform.localRotation = Quaternion.identity;

                Transform firePoint = weaponVisual.transform.Find("FirePoint");
                if (firePoint == null)
                {
                    firePoint = new GameObject("FirePoint").transform;
                    firePoint.SetParent(weaponVisual.transform);
                    firePoint.localPosition = new Vector3(0, 1.5f, 1.5f);
                }

                WeaponController wc = weaponVisual.AddComponent<WeaponController>();
                string statsPath = $"Assets/_Project/Data/Weapons/{weaponStatsName}.asset";
                wc.WeaponStats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
                wc.FirePoint = firePoint;
                // Default to Player, but if spawned as enemy, this needs to change.
                // However, this prefab is used for BOTH Player and Enemy spawning.
                // The correct way is to set the Team when spawning or in the Controller.
                // But WeaponController reads OwnerTeam.
                // Let's set it to Player by default here, but ensure it gets updated by the Unit Controller.
                wc.OwnerTeam = NavalCommand.Core.Team.Player; 
            }
            else
            {
                Debug.LogWarning($"MountPoint_{mountIndex} not found on Super Flagship");
            }
        }

        private static void SetupSpawningSystem()
        {
            NavalCommand.Systems.SpawningSystem spawner = Object.FindObjectOfType<NavalCommand.Systems.SpawningSystem>();
            if (spawner == null)
            {
                Debug.LogWarning("SpawningSystem not found in scene. Skipping setup.");
                return;
            }

            string[] prefabNames = new string[]
            {
                "Ship_Kamikaze",
                "Ship_Light_FlagshipGun",
                "Ship_Light_Missile",
                "Ship_Light_Torpedo",
                "Ship_Light_Autocannon",
                "Ship_Light_CIWS"
                // "Ship_SuperFlagship" // Removed as per request: Do not spawn as enemy
            };

            System.Collections.Generic.List<GameObject> prefabs = new System.Collections.Generic.List<GameObject>();
            foreach (string name in prefabNames)
            {
                string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                else
                {
                    Debug.LogError($"Could not load enemy prefab at {path}");
                }
            }

            spawner.EnemyPrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(spawner);
            Debug.Log($"SpawningSystem configured with {prefabs.Count} enemy types.");
        }

        [MenuItem("NavalCommand/Generate Empty Hulls")]
        public static void GenerateEmptyHulls()
        {
            EnsureDirectories();
            
            GameObject tempBuilderObj = new GameObject("TempBuilder");
            ShipBuilder builder = tempBuilderObj.AddComponent<ShipBuilder>();

            try
            {
                foreach (WeightClass weight in System.Enum.GetValues(typeof(WeightClass)))
                {
                    CreateEmptyHull(builder, weight);
                }
            }
            finally
            {
                DestroyImmediate(tempBuilderObj);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Empty Hulls Generated Successfully!");
        }

        private static void CreateEmptyHull(ShipBuilder builder, WeightClass weight)
        {
            string name = $"Hull_Empty_{weight}";
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";

            // 1. Create Hull (Root)
            GameObject shipRoot = builder.CreateHullModule(weight);
            shipRoot.name = name;
            shipRoot.transform.localPosition = Vector3.zero;
            shipRoot.transform.localRotation = Quaternion.identity;

            // 2. Add Basic Components
            var rb = shipRoot.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = shipRoot.AddComponent<BoxCollider>();
            
            // Estimate size based on weight (Matches ShipBuilder new specs)
            float width = 4f;
            float length = 15f; // S Class
            if (weight == WeightClass.Medium) { length = 19f; } // M Class
            if (weight == WeightClass.Heavy) { length = 24f; } // L Class

            col.size = new Vector3(width, 2f, length);
            col.center = new Vector3(0, 1f, 0);

            // 3. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(shipRoot, path);
            DestroyImmediate(shipRoot);
        }
        [MenuItem("NavalCommand/Generate HUD")]
        public static void GenerateHUD()
        {
            // 1. Create Canvas
            GameObject canvasObj = GameObject.Find("DashboardCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("DashboardCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 2. Create Dashboard Panel (Bottom Left)
            GameObject panelObj = GameObject.Find("DashboardPanel");
            if (panelObj == null)
            {
                panelObj = new GameObject("DashboardPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                
                RectTransform rect = panelObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; // Bottom Left
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(20, 20);
                rect.sizeDelta = new Vector2(300, 150);

                Image bg = panelObj.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
            }

            // 3. Add DashboardUI Script
            NavalCommand.UI.DashboardUI ui = panelObj.GetComponent<NavalCommand.UI.DashboardUI>();
            if (ui == null) ui = panelObj.AddComponent<NavalCommand.UI.DashboardUI>();

            // 4. Create Text Elements
            ui.ThrottleText = CreateTextElement(panelObj, "ThrottleText", "THROTTLE: STOP", new Vector2(10, 110));
            ui.RudderText = CreateTextElement(panelObj, "RudderText", "RUDDER: CENTER", new Vector2(10, 70));
            ui.SpeedText = CreateTextElement(panelObj, "SpeedText", "0.0 kts", new Vector2(10, 30));

            Debug.Log("HUD Generated Successfully!");
        }

        private static Text CreateTextElement(GameObject parent, string name, string defaultText, Vector2 position)
        {
            Transform existing = parent.transform.Find(name);
            GameObject textObj;
            
            if (existing != null)
            {
                textObj = existing.gameObject;
            }
            else
            {
                textObj = new GameObject(name);
                textObj.transform.SetParent(parent.transform, false);
            }

            Text textComp = textObj.GetComponent<Text>();
            if (textComp == null) textComp = textObj.AddComponent<Text>();

            textComp.text = defaultText;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 24;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleLeft;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, 30);

            return textComp;
        }
    }
}
#endif
