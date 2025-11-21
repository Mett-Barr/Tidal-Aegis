#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using NavalCommand.Data;
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
            GenerateShips();
            SetupSpawningSystem();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Basic Content Generated Successfully!");
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
            // Flagship Gun: Ballistic, Standard
            CreateProjectile("Projectile_FlagshipGun", Color.yellow, ProjectileType.Ballistic, 30f, 20f);
            
            // Missile: VLS -> Cruise (15m) -> Terminal
            CreateProjectile("Projectile_Missile", Color.red, ProjectileType.Homing, 15f, 50f, 
                cruiseHeight: 15f, terminalDist: 50f, vlsHeight: 20f, turnRate: 2f);
            
            // Torpedo: Underwater (-2m) -> Homing
            CreateProjectile("Projectile_Torpedo", Color.blue, ProjectileType.Homing, 10f, 80f,
                cruiseHeight: -2f, terminalDist: 30f, vlsHeight: 0f, turnRate: 1f);
            
            // Autocannon: Fast, Straight
            CreateProjectile("Projectile_Autocannon", new Color(1f, 0.5f, 0f), ProjectileType.Straight, 60f, 5f);
            
            // CIWS: Very Fast, Straight
            CreateProjectile("Projectile_CIWS", Color.white, ProjectileType.Straight, 80f, 2f);
        }

        private static void CreateProjectile(string name, Color color, ProjectileType type, float speed, float damage,
            float cruiseHeight = 0f, float terminalDist = 0f, float vlsHeight = 0f, float turnRate = 0f)
        {
            string path = $"Assets/_Project/Prefabs/Projectiles/{name}.prefab";
            
            // Always recreate to ensure updates
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // We want to update existing prefabs with new scripts/values
                // Simplest way is to delete and recreate for this generator tool
                AssetDatabase.DeleteAsset(path);
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = Vector3.one * 0.5f;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.sharedMaterial.color = color;

            // Physics
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = (type == ProjectileType.Ballistic || type == ProjectileType.Straight);
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Behavior
            var proj = go.AddComponent<ProjectileBehavior>();
            proj.BehaviorType = type;
            proj.Speed = speed;
            proj.Damage = damage;
            proj.GravityMultiplier = (type == ProjectileType.Ballistic || type == ProjectileType.Straight) ? 1f : 0f;
            
            // Advanced Settings
            proj.CruiseHeight = cruiseHeight;
            proj.TerminalHomingDistance = terminalDist;
            proj.VerticalLaunchHeight = vlsHeight;
            proj.TurnRate = turnRate;

            // Collider
            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private static void GenerateWeaponStats()
        {
            // Flagship Gun: Reliable, medium range
            CreateWeaponStats("Weapon_FlagshipGun_Basic", "Flagship Gun", WeaponType.FlagshipGun, 120f, 3f, 30f, "Projectile_FlagshipGun");
            
            // Missile: Long range, slow reload, high damage
            CreateWeaponStats("Weapon_Missile_Basic", "Missile Launcher", WeaponType.Missile, 300f, 10f, 60f, "Projectile_Missile");
            
            // Torpedo: Medium range, very slow reload, massive damage
            CreateWeaponStats("Weapon_Torpedo_Basic", "Torpedo Tube", WeaponType.Torpedo, 150f, 12f, 100f, "Projectile_Torpedo");
            
            // Autocannon: Short range, rapid fire, suppression
            CreateWeaponStats("Weapon_Autocannon_Basic", "Autocannon", WeaponType.Autocannon, 60f, 0.2f, 5f, "Projectile_Autocannon");
            
            // CIWS: Very short range, extreme fire rate, defense
            CreateWeaponStats("Weapon_CIWS_Basic", "CIWS", WeaponType.CIWS, 40f, 0.05f, 2f, "Projectile_CIWS");
        }

        private static void CreateWeaponStats(string name, string displayName, WeaponType type, float range, float cooldown, float damage, string projectileName)
        {
            string path = $"Assets/_Project/Data/Weapons/{name}.asset";
            WeaponStatsSO so = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(path);
            
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<WeaponStatsSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.DisplayName = displayName;
            so.Type = type;
            so.Range = range;
            so.Cooldown = cooldown;
            so.Damage = damage;
            
            string projPath = $"Assets/_Project/Prefabs/Projectiles/{projectileName}.prefab";
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
                    firePoint.localPosition = new Vector3(0, 1.5f, 1.5f);
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
