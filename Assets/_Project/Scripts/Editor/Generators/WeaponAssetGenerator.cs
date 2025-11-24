using UnityEngine;
using UnityEditor;
using NavalCommand.Data;
using NavalCommand.Entities.Projectiles;
using UnityEngine.Rendering;

namespace NavalCommand.Editor.Generators
{
    public static class WeaponAssetGenerator
    {
        public static void GenerateAll()
        {
            EnsureDirectories();
            
            foreach (var config in WeaponRegistry.AllWeapons)
            {
                GenerateWeapon(config);
            }
            
            AssetDatabase.SaveAssets();
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Weapons"))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Weapons");
            
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Projectiles"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Projectiles");
        }

        private static void GenerateWeapon(WeaponConfig config)
        {
            // 1. Generate Projectile Prefab
            CreateProjectilePrefab(config);

            // 2. Generate WeaponStats SO
            CreateWeaponStats(config);
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
            
            // Direct Mapping from Config (No logic!)
            so.SetBaseGravityMultiplier(config.GravityMultiplier);
            so.SetBaseRotationSpeed(config.RotationSpeed);
            so.SetBaseSpread(config.Spread);
            so.SetBaseFiringAngleTolerance(config.FiringAngleTolerance);
            
            // Platform Settings
            so.SetBaseCanRotate(config.CanRotate);
            so.SetBaseIsVLS(config.IsVLS);
            so.SetBaseAimingLogicName(config.AimingLogicName);

            string projPath = $"Assets/_Project/Prefabs/Projectiles/{config.ProjectileName}.prefab";
            so.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
            
            so.TargetMask = ~0; // Default to Everything
            
            EditorUtility.SetDirty(so);
        }

        private static void CreateProjectilePrefab(WeaponConfig config)
        {
            string path = $"Assets/_Project/Prefabs/Projectiles/{config.ProjectileName}.prefab";
            
            // Create Root
            GameObject root = new GameObject(config.ProjectileName);
            
            // Add Components
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false; // Logic handles gravity
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 2f;
            col.direction = 2; // Z-axis
            col.isTrigger = true;

            var behavior = root.AddComponent<ProjectileBehavior>();
            // Note: Behavior is initialized at runtime, but we can set defaults if needed
            behavior.MovementLogicName = config.MovementLogicName;
            behavior.Speed = config.ProjectileSpeed;
            behavior.Damage = config.Damage;
            
            // Advanced Settings
            behavior.CruiseHeight = config.CruiseHeight;
            behavior.TerminalHomingDistance = config.TerminalHomingDistance;
            behavior.VerticalLaunchHeight = config.VerticalLaunchHeight;
            behavior.TurnRate = config.TurnRate;
            behavior.ImpactProfile = config.ImpactProfile;

            // Visuals
            CreateProjectileVisuals(root, config.ProjectileStyle, config.ProjectileColor);

            // Save
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateProjectileVisuals(GameObject parent, string style, Color color)
        {
            // Material Generation
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            
            if (style.Contains("Tracer"))
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Save Material (Optional, but good for debugging)
            string matPath = $"Assets/_Project/Generated/Materials/Mat_{parent.name}.mat";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            AssetDatabase.CreateAsset(mat, matPath);

            // Create Mesh
            GameObject model = new GameObject("Model");
            model.transform.SetParent(parent.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            switch (style)
            {
                case "Shell": 
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 0.8f, 0.4f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.38f, 0.4f, 0.4f), new Vector3(0, 0, 0.8f), Vector3.zero, mat);
                    break;
                case "Missile":
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.3f, 1.5f, 0.3f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.28f, 0.5f, 0.28f), new Vector3(0, 0, 1.5f), new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(1.2f, 0.05f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 1.2f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    break;
                case "Torpedo":
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 2f, 0.4f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0, 0, 2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.8f, 0.05f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 0.8f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    break;
                case "Tracer":
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.15f, 1f, 0.15f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    break;
                case "Tracer_Small":
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
            GameObject.DestroyImmediate(obj.GetComponent<Collider>());
        }
    }
}
