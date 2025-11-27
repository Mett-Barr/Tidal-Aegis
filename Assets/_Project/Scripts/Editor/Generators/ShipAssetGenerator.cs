using System.Linq;
using UnityEngine;
using UnityEditor;
using NavalCommand.Data;
using NavalCommand.Utils;
using NavalCommand.Entities.Components;
using NavalCommand.Entities.Units;
using NavalCommand.Core;

namespace NavalCommand.Editor.Generators
{
    public static class ShipAssetGenerator
    {
        public static void GenerateAll()
        {
            // Create a temporary ShipBuilder to use its methods
            GameObject tempBuilderObj = new GameObject("TempBuilder");
            ShipBuilder builder = tempBuilderObj.AddComponent<ShipBuilder>();

            // Load or Create Shared Hull Material
            Material hullMat = GetOrCreateHullMaterial();

            try
            {
                // Define Ships to Generate
                CreateModularShip(builder, "Ship_Light_Missile", "Weapon_Missile_Basic", WeaponType.Missile, hullMat);
                CreateModularShip(builder, "Ship_Light_Torpedo", "Weapon_Torpedo_Basic", WeaponType.Torpedo, hullMat);
                CreateModularShip(builder, "Ship_Light_Autocannon", "Weapon_Autocannon_Basic", WeaponType.Autocannon, hullMat);
                CreateModularShip(builder, "Ship_Light_CIWS", "Weapon_CIWS_Basic", WeaponType.CIWS, hullMat);
                CreateModularShip(builder, "Ship_Light_LaserCIWS", "Weapon_LaserCIWS_Basic", WeaponType.LaserCIWS, hullMat);  // NEW: Laser CIWS ship
                
                // Special Ships
                CreateKamikazeShip(builder, hullMat);
                CreateSuperFlagship(builder, hullMat);
            }
            finally
            {
                GameObject.DestroyImmediate(tempBuilderObj);
            }
        }

        private static Material GetOrCreateHullMaterial()
        {
            string matPath = "Assets/_Project/Generated/Materials/HullMat.mat";
            if (!System.IO.Directory.Exists("Assets/_Project/Generated/Materials"))
            {
                System.IO.Directory.CreateDirectory("Assets/_Project/Generated/Materials");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                // Try to find URP Lit shader, fallback to Standard
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                
                mat = new Material(shader);
                mat.color = Color.gray;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.gray);
                
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        private static void CreateModularShip(ShipBuilder builder, string name, string weaponStatsName, WeaponType weaponType, Material hullMat)
        {
            // 1. Create Hull
            GameObject shipRoot = builder.CreateHullModule(WeightClass.Light, hullMat);
            shipRoot.name = name;

            // 2. Attach Weapon
            string statsPath = $"Assets/_Project/Data/Weapons/{weaponStatsName}.asset";
            WeaponStatsSO stats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
            
            if (stats != null)
            {
                Transform mountPoint = shipRoot.transform.Find("MountPoint_1");
                if (mountPoint != null)
                {
                    AttachWeapon(builder, shipRoot, mountPoint, weaponType, stats, hullMat);
                }
            }
            // ... (rest of method)
            else
            {
                Debug.LogWarning($"[ShipAssetGenerator] Could not find stats for {name} at {statsPath}");
            }

            // 3. Add Unit Controller
            // Assuming modular ships are enemies for now
            // If we have a modular player ship, we'd need a flag.
            // Based on names, "Ship_Light_*" are enemies.
            EnemyUnit unit = shipRoot.AddComponent<EnemyUnit>();
            unit.MaxHP = 100f; // Default for Light
            unit.MoveSpeed = 7f;

            // Add Rigidbody (Required by BaseUnit)
            Rigidbody rb = shipRoot.GetComponent<Rigidbody>();
            if (rb == null) rb = shipRoot.AddComponent<Rigidbody>();
            rb.mass = 1000f;
            rb.drag = 1f;
            rb.angularDrag = 2f;
            rb.useGravity = false; // Floating
            rb.isKinematic = true; // Moved by script

            // 4. Save
            SaveShipPrefab(shipRoot, name);
        }

        private static void CreateKamikazeShip(ShipBuilder builder, Material hullMat)
        {
            GameObject shipRoot = builder.CreateHullModule(WeightClass.Light, hullMat);
            shipRoot.name = "Ship_Kamikaze";
            
            // Add Kamikaze logic
            KamikazeController kamikaze = shipRoot.AddComponent<KamikazeController>();
            kamikaze.MaxHP = 50f;

            // Add Rigidbody (Required by BaseUnit)
            Rigidbody rb = shipRoot.GetComponent<Rigidbody>();
            if (rb == null) rb = shipRoot.AddComponent<Rigidbody>();
            rb.mass = 500f;
            rb.drag = 1f;
            rb.angularDrag = 2f;
            rb.useGravity = false; // Floating
            rb.isKinematic = false; // CRITICAL: Must be false for Rb.velocity to work in KamikazeController
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better for high speed impacts
            
            SaveShipPrefab(shipRoot, "Ship_Kamikaze");
        }

        private static void CreateSuperFlagship(ShipBuilder builder, Material hullMat)
        {
            GameObject shipRoot = builder.CreateHullModule(WeightClass.SuperHeavy, hullMat);
            shipRoot.name = "Ship_SuperFlagship";
            
            // Load Weapons
            WeaponStatsSO mainGun = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>("Assets/_Project/Data/Weapons/Weapon_FlagshipGun_Basic.asset");
            WeaponStatsSO ciws = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>("Assets/_Project/Data/Weapons/Weapon_CIWS_Basic.asset");
            WeaponStatsSO laserCIWS = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>("Assets/_Project/Data/Weapons/Weapon_LaserCIWS_Basic.asset");  // NEW
            WeaponStatsSO auto = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>("Assets/_Project/Data/Weapons/Weapon_Autocannon_Basic.asset");
            WeaponStatsSO missile = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>("Assets/_Project/Data/Weapons/Weapon_Missile_Basic.asset");

            // Attach Weapons to all MountPoints
            // SuperHeavy has ~17 mounts.
            // 1, 2, 3: Centerline (Main Guns)
            // 4-13: Side Mounts (Autocannons/Secondary)
            // 14-15: Ballistic CIWS
            // 16-17: Laser CIWS (NEW)
            
            Transform[] children = shipRoot.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name.StartsWith("MountPoint_"))
                {
                    int index = int.Parse(child.name.Replace("MountPoint_", ""));
                    
                    if (index <= 3) // Main Guns
                    {
                        if (mainGun != null) AttachWeapon(builder, shipRoot, child, WeaponType.FlagshipGun, mainGun, hullMat);
                    }
                    else if (index >= 16) // Laser CIWS (Last 2)
                    {
                        if (laserCIWS != null) AttachWeapon(builder, shipRoot, child, WeaponType.LaserCIWS, laserCIWS, hullMat);
                    }
                    else if (index >= 14) // Ballistic CIWS (14-15)
                    {
                        if (ciws != null) AttachWeapon(builder, shipRoot, child, WeaponType.CIWS, ciws, hullMat);
                    }
                    else // Side Mounts (4-13)
                    {
                        // ARCHITECTURAL CHANGE: Mixed Loadout for "All-Rounder" Flagship
                        // 4-8: Autocannons (Close Defense / Surface)
                        // 9-13: Missiles (Long Range Strike)
                        if (index <= 8)
                        {
                             if (auto != null) AttachWeapon(builder, shipRoot, child, WeaponType.Autocannon, auto, hullMat);
                        }
                        else
                        {
                             if (missile != null) AttachWeapon(builder, shipRoot, child, WeaponType.Missile, missile, hullMat);
                        }
                    }
                }
            }

            // Add Player Controller
            FlagshipController controller = shipRoot.AddComponent<FlagshipController>();
            controller.MaxHP = 5000f; // Super Heavy HP
            controller.MaxSpeed = 15f;
            controller.Acceleration = 2f;
            controller.MaxTurnRate = 30f;
            
            // Add Rigidbody (Required by BaseUnit)
            Rigidbody rb = shipRoot.GetComponent<Rigidbody>();
            if (rb == null) rb = shipRoot.AddComponent<Rigidbody>();
            rb.mass = 50000f;
            rb.drag = 1f;
            rb.angularDrag = 2f;
            rb.useGravity = false; // Floating
            rb.isKinematic = true; // Moved by script? BaseUnit uses MovePosition, so Kinematic is safer or Non-Kinematic with constraints.
            // FlagshipController uses Rb.MovePosition, so Kinematic is usually preferred for "Kinematic Character Controller" style,
            // but for physics interactions (collisions), dynamic might be better.
            // Let's stick to Kinematic for now as per typical naval sims unless we have buoyancy.
            rb.isKinematic = true; 

            SaveShipPrefab(shipRoot, "Ship_SuperFlagship");
        }

        private static void AttachWeapon(ShipBuilder builder, GameObject shipRoot, Transform mountPoint, WeaponType type, WeaponStatsSO stats, Material hullMat)
        {
            GameObject weaponVisual = null;

            // 1. Create Visuals
            // NEW: Check for specific prefab for LaserCIWS
            if (type == WeaponType.LaserCIWS)
            {
                string prefabPath = "Assets/_Project/Prefabs/Weapons/LaserCannon_Spherical.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    weaponVisual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    Debug.LogWarning($"[ShipAssetGenerator] Laser Cannon prefab not found at {prefabPath}, falling back to generic module.");
                }
            }

            // Fallback to generic builder if no prefab loaded
            if (weaponVisual == null)
            {
                weaponVisual = builder.CreateWeaponModule(type, hullMat);
            }

            weaponVisual.transform.SetParent(mountPoint);
            weaponVisual.transform.localPosition = Vector3.zero;
            weaponVisual.transform.localRotation = Quaternion.identity;

            // 2. Setup FirePoint
            // Search recursively because FirePoint might be nested (e.g. inside TurretGun)
            Transform firePoint = weaponVisual.transform.Find("FirePoint"); // Try direct child first
            if (firePoint == null)
            {
                // Try finding deep
                var firePoints = weaponVisual.GetComponentsInChildren<Transform>().Where(t => t.name == "FirePoint").ToArray();
                if (firePoints.Length > 0) firePoint = firePoints[0];
            }

            if (firePoint == null)
            {
                firePoint = new GameObject("FirePoint").transform;
                firePoint.SetParent(weaponVisual.transform);
                // Default offset if not found
                firePoint.localPosition = new Vector3(0, 1.5f, 1.5f); 
            }

            // 3. Add Controller
            // CRITICAL: Check if component exists first (Prefab might have it)
            WeaponController wc = weaponVisual.GetComponent<WeaponController>();
            if (wc == null) wc = weaponVisual.AddComponent<WeaponController>();
            
            wc.FirePoint = firePoint;
            wc.WeaponStats = stats;
            wc.OwnerTeam = Team.Player; // Default, will be overwritten by UnitController
            
            // 4. Configure TurretRotator (New Logic)
            TurretRotator rotator = weaponVisual.GetComponent<TurretRotator>();
            if (rotator == null) rotator = weaponVisual.AddComponent<TurretRotator>();
            
            // Apply Platform Settings from SO
            rotator.CanRotate = stats.CanRotate;
            rotator.IsVerticalLaunch = stats.IsVLS;

            // CRITICAL FIX: Adjust Pitch Limits for close-in defense
            // Flagship deck is high, so we need significant depression to hit close targets (Kamikaze)
            if (type == WeaponType.Autocannon)
            {
                rotator.MinPitch = -45f; // Allow shooting down at steep angle
                rotator.MaxPitch = 89f;  // Allow shooting almost straight up
            }
            else if (type == WeaponType.CIWS || type == WeaponType.LaserCIWS) // NEW: Include LaserCIWS
            {
                rotator.MinPitch = -30f; // Phalanx usually has -25, giving it a bit more
                rotator.MaxPitch = 89f;  // Anti-missile needs high elevation
            }
            else
            {
                rotator.MinPitch = -10f; // Standard main gun limit
                rotator.MaxPitch = 60f;  // Main guns usually don't elevate that high
            }
        }

        private static void SaveShipPrefab(GameObject shipRoot, string name)
        {
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Enemies"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Enemies");
            }

            PrefabUtility.SaveAsPrefabAsset(shipRoot, path);
            GameObject.DestroyImmediate(shipRoot);
        }
    }
}
