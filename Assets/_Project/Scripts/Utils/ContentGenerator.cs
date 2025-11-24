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
        // [MenuItem("NavalCommand/Rebuild World (Force Update)")]
        public static void RebuildAllContent()
        {
            Debug.Log($"Starting World Rebuild... [Pipeline: {UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in"}]");
            DeleteGeneratedMaterials(); // Force clean regeneration
            
            EnsureDirectories();
            GenerateProjectiles();
            GenerateWeaponStats();
            GeneratePhysicsConfig();
            GenerateBasicVFXPrefabs();
            GenerateVFXLibrary();
            GenerateShips();
            SetupSpawningSystem();
            SetupVFXManager();
            
            FixInvisibleShips();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("World Rebuild Complete!");
        }

        private static void DeleteGeneratedMaterials()
        {
            string dir = "Assets/_Project/Generated/Materials";
            if (Directory.Exists(dir))
            {
                // Delete all assets in the folder
                string[] files = Directory.GetFiles(dir, "*.mat");
                foreach (string file in files)
                {
                    AssetDatabase.DeleteAsset(file);
                }
                AssetDatabase.Refresh();
            }
        }

        public static void FixInvisibleShips()
        {
            // Logic from FixShipsTool
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
                }

                // Add Debugger
                if (instance.GetComponent<ShipDebugger>() == null)
                {
                    instance.AddComponent<ShipDebugger>();
                }

                // Configure
                builder.ShipClass = WeightClass.SuperHeavy;
                builder.BuildTrigger = false; // Reset trigger

                // Assign Default Weapon Stats
                string statsPath = "Assets/_Project/Data/Weapons/Weapon_FlagshipGun_Basic.asset";
                WeaponStatsSO stats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
                if (stats != null)
                {
                    builder.DefaultWeaponStats = stats;
                }

                // Build (This generates and saves the mesh)
                builder.BuildShip();

                // Ensure Collider
                if (instance.GetComponent<Collider>() == null)
                {
                    var col = instance.AddComponent<BoxCollider>();
                    col.size = new Vector3(10, 5, 30); // Approximate size for SuperHeavy
                }
                
                // Ensure Layer Exists (Editor Only Hack)
                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty layers = tagManager.FindProperty("layers");
                if (LayerMask.NameToLayer("Player") == -1)
                {
                    layers.GetArrayElementAtIndex(6).stringValue = "Player"; // Force Layer 6 to be Player
                    tagManager.ApplyModifiedProperties();
                }

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1) instance.layer = playerLayer;

                // Apply Changes
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Debug.Log("[ContentGenerator] Super Flagship Fixed & Rebuilt!");
            }
            finally
            {
                DestroyImmediate(instance);
            }
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

            // CRITICAL FIX: Add ProjectileBehavior
            var behavior = go.AddComponent<NavalCommand.Entities.Projectiles.ProjectileBehavior>();
            behavior.Speed = config.ProjectileSpeed;
            behavior.Damage = config.Damage;
            behavior.MovementLogicName = config.MovementLogicName;
            behavior.ImpactProfile = config.ImpactProfile;
            
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

            // VFX: Trail Renderer
            // Only add TrailRenderer if NOT a tracer
            if (!config.ProjectileStyle.Contains("Tracer"))
            {
                var trail = go.AddComponent<TrailRenderer>();
                
                // Create Persistent Material for Trail
                string trailMatPath = $"Assets/_Project/Generated/Materials/TrailMat_{config.ProjectileStyle}.mat";
                Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(trailMatPath);
                
                if (trailMat == null)
                {
                    Shader shader = FindBestShader();
                    trailMat = new Material(shader);
                    AssetDatabase.CreateAsset(trailMat, trailMatPath);
                }
                else
                {
                    // Always update shader to ensure it's valid
                    trailMat.shader = FindBestShader();
                    EditorUtility.SetDirty(trailMat);
                }

                trail.material = trailMat;
                trail.time = 0.5f;
                trail.minVertexDistance = 0.1f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;

                AnimationCurve widthCurve = new AnimationCurve();
                if (config.ProjectileStyle == "Missile")
                {
                    // Smoke Trail
                    widthCurve.AddKey(0, 0.5f);
                    widthCurve.AddKey(1, 0.1f);
                    trail.time = 1.5f;
                    trail.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                    trail.endColor = new Color(0.5f, 0.5f, 0.5f, 0f);
                }
                else if (config.ProjectileStyle == "Shell")
                {
                    // Heavy Shell Trail - Shortened
                    widthCurve.AddKey(0, 0.6f);
                    widthCurve.AddKey(1, 0f);
                    trail.time = 0.1f; // Reduced from 0.3f
                    trail.startColor = new Color(1f, 0.9f, 0.5f, 0.8f); // Heat color
                    trail.endColor = new Color(0.5f, 0.5f, 0.5f, 0f);   // Smoke
                }
                else if (config.ProjectileStyle == "Torpedo")
                {
                    // Bubbles
                    widthCurve.AddKey(0, 0.6f);
                    widthCurve.AddKey(1, 0.8f);
                    trail.time = 2.0f;
                    trail.startColor = new Color(1f, 1f, 1f, 0.5f);
                    trail.endColor = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    // Default
                    widthCurve.AddKey(0, 0.2f);
                    widthCurve.AddKey(1, 0f);
                }
                trail.widthCurve = widthCurve;
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private enum RenderPipelineType { BuiltIn, URP, HDRP }

        private static RenderPipelineType GetCurrentPipeline()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rp != null)
            {
                if (rp.GetType().Name.Contains("Universal")) return RenderPipelineType.URP;
                if (rp.GetType().Name.Contains("HighDefinition")) return RenderPipelineType.HDRP;
                return RenderPipelineType.URP; // Assume URP if unknown SRP
            }
            return RenderPipelineType.BuiltIn;
        }

        private static Shader FindBestShader()
        {
            var pipeline = GetCurrentPipeline();
            Shader s = null;

            if (pipeline == RenderPipelineType.URP)
            {
                s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit"); // Fallback to generic unlit
            }
            else if (pipeline == RenderPipelineType.HDRP)
            {
                s = Shader.Find("HDRP/Unlit");
            }
            
            // Fallbacks
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (s == null) s = Shader.Find("Sprites/Default"); // Ultimate fallback
            
            if (s == null) Debug.LogWarning($"ContentGenerator: Could not find suitable particle shader for {pipeline}!");
            
            return s;
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

                // Create new material with pipeline-aware shader finding
                var pipeline = GetCurrentPipeline();
                Shader shader = null;

                if (pipeline == RenderPipelineType.URP)
                {
                    // Revert to Lit for Models - Unlit might be causing visibility issues if not configured right
                    shader = Shader.Find("Universal Render Pipeline/Lit"); 
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                }
                else if (pipeline == RenderPipelineType.HDRP)
                {
                    shader = Shader.Find("HDRP/Lit");
                }

                // Fallbacks
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default"); // Ultimate fallback

                Debug.Log($"[ContentGenerator] Selected Model Shader: {shader?.name} for {pipeline}");
                
                mat = new Material(shader);
                mat.color = color;
                
                // URP/HDRP use _BaseColor
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

                if (style.Contains("Tracer"))
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                // Update existing material color just in case
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

                if (style.Contains("Tracer"))
                {
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2f);
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
                    // DEBUG: Increased size for visibility (was 0.08, 0.6, 0.08)
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.3f, 2.0f, 0.3f), Vector3.zero, new Vector3(90, 0, 0), mat);
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
            so.ImpactProfile = config.ImpactProfile;
            
            // CIWS should be near-instant/laser-like, so 0 gravity
            if (config.Type == WeaponType.CIWS)
            {
                so.SetBaseGravityMultiplier(0f);
            }
            else
            {
                so.SetBaseGravityMultiplier(1f);
            }
            
            so.SetBaseRotationSpeed(config.RotationSpeed);
            so.SetBaseSpread(config.Spread);
            
            // Set Firing Tolerance
            // Reverted to strict 5 degrees for all weapons per user request
            so.SetBaseFiringAngleTolerance(5f);

            // AimingMode removed from WeaponStatsSO in favor of component-based logic (TurretRotator)

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
                    
                    // CRITICAL: If Missile, point UP
                    if (weaponType == WeaponType.Missile)
                    {
                        firePoint.localRotation = Quaternion.Euler(-90, 0, 0);
                    }
                }

                WeaponController wc = weaponVisual.AddComponent<WeaponController>();
                string statsPath = $"Assets/_Project/Data/Weapons/{weaponStatsName}.asset";
                wc.WeaponStats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
                wc.FirePoint = firePoint;
                wc.OwnerTeam = NavalCommand.Core.Team.Enemy;

                // Configure TurretRotator based on Platform Type
                TurretRotator rotator = weaponVisual.AddComponent<TurretRotator>();
                rotator.Initialize(weaponVisual.transform, firePoint);
                
                if (weaponType == WeaponType.Missile)
                {
                    rotator.CanRotate = false;
                    rotator.IsVerticalLaunch = true;
                }
                else
                {
                    rotator.CanRotate = true;
                    rotator.IsVerticalLaunch = false;
                }
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

            // Mounts 12-13: Autocannons (Was CIWS, now moved to dedicated mounts)
            AttachWeapon(builder, shipRoot, 12, "Weapon_Autocannon_Basic", WeaponType.Autocannon);
            AttachWeapon(builder, shipRoot, 13, "Weapon_Autocannon_Basic", WeaponType.Autocannon);

            // Mounts 14-17: CIWS (Front and Back Coverage)
            AttachWeapon(builder, shipRoot, 14, "Weapon_CIWS_Basic", WeaponType.CIWS); // Front Port
            AttachWeapon(builder, shipRoot, 15, "Weapon_CIWS_Basic", WeaponType.CIWS); // Front Starboard
            AttachWeapon(builder, shipRoot, 16, "Weapon_CIWS_Basic", WeaponType.CIWS); // Rear Port
            AttachWeapon(builder, shipRoot, 17, "Weapon_CIWS_Basic", WeaponType.CIWS); // Rear Starboard

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

                // Configure TurretRotator based on Platform Type
                TurretRotator rotator = weaponVisual.AddComponent<TurretRotator>();
                rotator.Initialize(weaponVisual.transform, firePoint);
                
                if (type == WeaponType.Missile)
                {
                    rotator.CanRotate = false;
                    rotator.IsVerticalLaunch = true;
                }
                else
                {
                    rotator.CanRotate = true;
                    rotator.IsVerticalLaunch = false;
                }
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
            
            // Force Random Mode to ensure variety
            spawner.Mode = NavalCommand.Systems.SpawnMode.Random;
            spawner.SpecificPrefabName = "";

            EditorUtility.SetDirty(spawner);
            Debug.Log($"SpawningSystem configured with {prefabs.Count} enemy types.");
        }

        // [MenuItem("NavalCommand/Tools/Generate Empty Hulls")]
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
        // [MenuItem("NavalCommand/Tools/Generate HUD")]
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

        private static void GenerateVFXLibrary()
        {
            string path = "Assets/_Project/Data/VFX/DefaultVFXLibrary.asset";
            CreateDir("Assets/_Project/Data/VFX");
            
            NavalCommand.Systems.VFX.VFXLibrarySO lib = AssetDatabase.LoadAssetAtPath<NavalCommand.Systems.VFX.VFXLibrarySO>(path);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<NavalCommand.Systems.VFX.VFXLibrarySO>();
                AssetDatabase.CreateAsset(lib, path);
            }
            
            // We won't overwrite rules if they exist, to preserve user edits.
            if (lib.Rules == null) lib.Rules = new System.Collections.Generic.List<NavalCommand.Systems.VFX.VFXRule>();
            
            // Load Prefabs
            GameObject splashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/VFX_Splash_Water.prefab");
            GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/VFX_Explosion_Medium.prefab");
            GameObject sparkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/VFX_Sparks_Kinetic.prefab");
            GameObject smallExplosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/VFX/VFX_Explosion_Small.prefab");

            if (lib.Rules.Count == 0)
            {
                // Rule 1: Water Splash (Any Impact on Water)
                var splashRule = new NavalCommand.Systems.VFX.VFXRule();
                splashRule.AnyCategory = true;
                splashRule.AnySize = true;
                splashRule.Surface = NavalCommand.Systems.VFX.SurfaceType.Water;
                splashRule.Priority = 10;
                splashRule.VFXPrefab = splashPrefab;
                lib.Rules.Add(splashRule);
                
                // Rule 2: Explosion (Explosive on Any Surface)
                var explosionRule = new NavalCommand.Systems.VFX.VFXRule();
                explosionRule.Category = NavalCommand.Systems.VFX.ImpactCategory.Explosive;
                explosionRule.AnySize = true;
                explosionRule.AnySurface = true;
                explosionRule.Priority = 5;
                explosionRule.VFXPrefab = explosionPrefab;
                lib.Rules.Add(explosionRule);

                // Rule 3: Kinetic Sparks (Kinetic on Armor)
                var sparkRule = new NavalCommand.Systems.VFX.VFXRule();
                sparkRule.Category = NavalCommand.Systems.VFX.ImpactCategory.Kinetic;
                sparkRule.AnySize = true;
                sparkRule.Surface = NavalCommand.Systems.VFX.SurfaceType.Armor_Metal;
                sparkRule.Priority = 5;
                sparkRule.VFXPrefab = sparkPrefab;
                lib.Rules.Add(sparkRule);

                // Rule 4: CIWS Interception (Kinetic on Air)
                var interceptionRule = new NavalCommand.Systems.VFX.VFXRule();
                interceptionRule.Category = NavalCommand.Systems.VFX.ImpactCategory.Kinetic;
                interceptionRule.AnySize = true;
                interceptionRule.Surface = NavalCommand.Systems.VFX.SurfaceType.Air;
                interceptionRule.Priority = 10; // Higher priority than generic Kinetic
                interceptionRule.VFXPrefab = smallExplosionPrefab;
                lib.Rules.Add(interceptionRule);
            }
            else
            {
                // Update existing rules if they are missing prefabs (optional, but good for first run)
                foreach (var rule in lib.Rules)
                {
                    if (rule.VFXPrefab == null)
                    {
                        if (rule.Surface == NavalCommand.Systems.VFX.SurfaceType.Water) rule.VFXPrefab = splashPrefab;
                        else if (rule.Category == NavalCommand.Systems.VFX.ImpactCategory.Explosive) rule.VFXPrefab = explosionPrefab;
                        else if (rule.Category == NavalCommand.Systems.VFX.ImpactCategory.Kinetic && rule.Surface == NavalCommand.Systems.VFX.SurfaceType.Armor_Metal) rule.VFXPrefab = sparkPrefab;
                        else if (rule.Category == NavalCommand.Systems.VFX.ImpactCategory.Kinetic && rule.Surface == NavalCommand.Systems.VFX.SurfaceType.Air) rule.VFXPrefab = smallExplosionPrefab;
                    }
                }
            }
            
            EditorUtility.SetDirty(lib);
        }

        private static void GenerateBasicVFXPrefabs()
        {
            CreateDir("Assets/_Project/Prefabs/VFX");
            CreateDir("Assets/_Project/Generated/Materials");

            // 1. Water Splash
            CreateParticleVFX("VFX_Splash_Water", (go) => {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false; // CRITICAL: Disable looping for auto-despawn
                main.duration = 1f;
                main.startLifetime = 0.5f;
                main.startSpeed = 10f;
                main.startSize = 0.5f;
                main.startColor = new Color(0.8f, 0.9f, 1f, 0.8f);
                main.gravityModifier = 1f;
                
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });
                
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                shape.radius = 0.1f;
                
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = GetOrCreateVFXMaterial("VFX_Mat_Splash", new Color(0.8f, 0.9f, 1f, 0.8f));
            });

            // 2. Explosion Medium
            CreateParticleVFX("VFX_Explosion_Medium", (go) => {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false; // CRITICAL: Disable looping
                main.duration = 1f;
                main.startLifetime = 0.6f;
                main.startSpeed = 5f;
                main.startSize = 2f;
                main.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
                
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = GetOrCreateVFXMaterial("VFX_Mat_Explosion", new Color(1f, 0.5f, 0f, 1f));
            });

            // 3. Kinetic Sparks
            CreateParticleVFX("VFX_Sparks_Kinetic", (go) => {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false; // CRITICAL: Disable looping
                main.duration = 0.5f;
                main.startLifetime = 0.2f;
                main.startSpeed = 15f;
                main.startSize = 0.2f;
                main.startColor = Color.yellow;
                main.gravityModifier = 0.5f;
                
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });
                
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 45f;
                shape.radius = 0.1f;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = GetOrCreateVFXMaterial("VFX_Mat_Sparks", Color.yellow);
            });

            // 4. Small Explosion (CIWS Interception)
            CreateParticleVFX("VFX_Explosion_Small", (go) => {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false; // CRITICAL: Disable looping
                main.duration = 0.5f;
                main.startLifetime = 0.3f;
                main.startSpeed = 8f;
                main.startSize = 1f;
                main.startColor = new Color(1f, 0.8f, 0.4f, 1f); // Light Orange/Yellow
                
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });
                
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = GetOrCreateVFXMaterial("VFX_Mat_ExplosionSmall", new Color(1f, 0.8f, 0.4f, 1f));
            });
        }

        private static Material GetOrCreateVFXMaterial(string name, Color color)
        {
            string path = $"Assets/_Project/Generated/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // Smart Shader Selection (Aggressive)
            // We prioritize URP Particle shaders because they are most likely to work in a modern URP project.
            // We do NOT check currentRenderPipeline because it might be null in Editor/EditMode.
            
            Shader shader = null;
            
            // 1. Try URP Particles Unlit (Best for simple colored VFX)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            
            // 2. Try URP Particles Simple Lit
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            
            // 3. Try URP Lit (Generic)
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            
            // 4. Try Built-in Particles (Fallback)
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                
                // Set common properties
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color);
                
                // CRITICAL: URP Particles Unlit often needs a BaseMap even if just white
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);

                // Force Transparency for URP Lit/Unlit if possible (Mode 1 = Transparent)
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f); // Alpha
                
                // Enable Keywords
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHATEST_ON"); // Sometimes needed
                
                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"[ContentGenerator] Created material {name} with shader: {shader.name}");
            }
            else
            {
                mat.shader = shader;
                mat.color = color;
                
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color);
                
                // CRITICAL: URP Particles Unlit often needs a BaseMap even if just white
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);

                // Force Transparency
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0.0f);
                
                // Enable Keywords
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                
                EditorUtility.SetDirty(mat);
                Debug.Log($"[ContentGenerator] Updated material {name} with shader: {shader.name}");
            }
            return mat;
        }

        private static void CreateParticleVFX(string name, System.Action<GameObject> setup)
        {
            string path = $"Assets/_Project/Prefabs/VFX/{name}.prefab";
            GameObject go = new GameObject(name);
            try
            {
                setup(go);
                
                // Attach AutoDespawn
                go.AddComponent<NavalCommand.Systems.VFX.VFXAutoDespawn>();
                
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                DestroyImmediate(go);
            }
        }

        private static void SetupVFXManager()
        {
            NavalCommand.Systems.VFX.VFXManager manager = Object.FindObjectOfType<NavalCommand.Systems.VFX.VFXManager>();
            if (manager == null)
            {
                GameObject go = new GameObject("VFXManager");
                manager = go.AddComponent<NavalCommand.Systems.VFX.VFXManager>();
            }
            
            // Assign Library via SerializedObject to access private field
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty prop = so.FindProperty("_library");
            if (prop != null)
            {
                string path = "Assets/_Project/Data/VFX/DefaultVFXLibrary.asset";
                var lib = AssetDatabase.LoadAssetAtPath<NavalCommand.Systems.VFX.VFXLibrarySO>(path);
                prop.objectReferenceValue = lib;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
