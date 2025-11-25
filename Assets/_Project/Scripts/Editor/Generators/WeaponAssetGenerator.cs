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
            
            // Muzzle Flash
            string flashPath = $"Assets/_Project/Prefabs/Projectiles/MuzzleFlash_{config.ProjectileName}.prefab";
            so.MuzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(flashPath);

            so.TargetMask = ~0; // Default to Everything
            
            EditorUtility.SetDirty(so);
        }

        private static void GenerateWeapon(WeaponConfig config)
        {
            // 1. Generate Projectile Prefab
            CreateProjectilePrefab(config);

            // 2. Generate Muzzle Flash Prefab
            CreateMuzzleFlashPrefab(config);

            // 3. Generate WeaponStats SO
            CreateWeaponStats(config);
        }

        private static void CreateMuzzleFlashPrefab(WeaponConfig config)
        {
            string path = $"Assets/_Project/Prefabs/Projectiles/MuzzleFlash_{config.ProjectileName}.prefab";
            
            GameObject root = new GameObject($"MuzzleFlash_{config.ProjectileName}");
            
            // Add Particle System for Flash
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = 0.1f;
            main.startSpeed = 5f;
            main.startSize = 1.5f;
            main.startColor = config.ProjectileColor;
            main.loop = false;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.Destroy; // Auto destroy

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            // Add Light
            var lightObj = new GameObject("Light");
            lightObj.transform.SetParent(root.transform);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = config.ProjectileColor;
            light.range = 10f;
            light.intensity = 5f;
            
            // Auto-destroy light script? No, the root destroys itself via ParticleSystem stopAction.
            // But we need to make sure the light fades? 
            // For simplicity, just let it blink out when object is destroyed.

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
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
            behavior.MovementLogicName = config.MovementLogicName;
            behavior.Speed = config.ProjectileSpeed;
            behavior.Damage = config.Damage;
            
            // Advanced Settings
            behavior.CruiseHeight = config.CruiseHeight;
            behavior.TerminalHomingDistance = config.TerminalHomingDistance;
            behavior.VerticalLaunchHeight = config.VerticalLaunchHeight;
            behavior.TurnRate = config.TurnRate;
            behavior.ImpactProfile = config.ImpactProfile;

            // VFX Controller
            var vfxCtrl = root.AddComponent<ProjectileVFXController>();

            // Visuals & Particles
            CreateProjectileVisuals(root, vfxCtrl, config.ProjectileStyle, config.ProjectileColor);

            // Save
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateProjectileVisuals(GameObject parent, ProjectileVFXController vfxCtrl, string style, Color color)
        {
            // Material Generation
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            
            if (style.Contains("Tracer"))
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 10f); // HDR Glow
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Save Material
            string matPath = $"Assets/_Project/Generated/Materials/Mat_{parent.name}.mat";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            AssetDatabase.CreateAsset(mat, matPath);
            
            // CRITICAL FIX: Force save and refresh to ensure material is fully written to disk
            // before it's referenced by the prefab. Without this, some prefabs may have NULL material references.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // CRITICAL FIX 2: Reload the material from AssetDatabase to get the proper asset reference
            // Using the in-memory 'mat' object will cause NULL references when the prefab is saved.
            mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Debug.LogError($"[WeaponAssetGenerator] Failed to load material at {matPath}");
                return; // Cannot continue without material
            }

            // Create Mesh Model
            GameObject model = new GameObject("Model");
            model.transform.SetParent(parent.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Create VFX Root (Holder for Particles)
            GameObject vfxRoot = new GameObject("VFX_Root");
            vfxRoot.transform.SetParent(parent.transform);
            vfxRoot.transform.localPosition = Vector3.zero;
            vfxRoot.transform.localRotation = Quaternion.LookRotation(Vector3.back); // Point backwards for exhaust

            switch (style)
            {
                case "Shell": 
                    // Flagship Gun - Large, imposing shells
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.6f, 1.2f, 0.6f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.58f, 0.6f, 0.6f), new Vector3(0, 0, 1.2f), Vector3.zero, mat);
                    break;
                case "Missile":
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.3f, 1.5f, 0.3f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.28f, 0.5f, 0.28f), new Vector3(0, 0, 1.5f), new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(1.2f, 0.05f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 1.2f, 0.4f), new Vector3(0, 0, -1.2f), Vector3.zero, mat);
                    
                    // VFX: Flame + Smoke
                    CreateParticleSystem(vfxRoot, vfxCtrl.FlameParticles, "Flame", color, 0.1f, 0.5f, 10f);
                    CreateParticleSystem(vfxRoot, vfxCtrl.SmokeParticles, "Smoke", Color.white, 2.0f, 1.0f, 2f);
                    break;
                case "Torpedo":
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 2f, 0.4f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0, 0, 2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.8f, 0.05f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 0.8f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    
                    // VFX: Bubbles
                    CreateParticleSystem(vfxRoot, vfxCtrl.SmokeParticles, "Bubbles", new Color(1f, 1f, 1f, 0.5f), 1.5f, 0.8f, 1f);
                    break;
                case "Tracer":
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.15f, 1f, 0.15f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    
                    // VFX: Glow Trail
                    CreateParticleSystem(vfxRoot, vfxCtrl.FlameParticles, "Glow", color, 0.2f, 0.4f, 5f);
                    break;
                case "Tracer_Small":
                    // CIWS - Small, subtle tracers to avoid visual clutter from high fire rate
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.08f, 0.6f, 0.08f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    // No VFX for CIWS - the tracer bullets themselves provide visual feedback through high fire rate
                    break;
            }
        }

        private static void CreateParticleSystem(GameObject parent, System.Collections.Generic.List<ParticleSystem> list, string name, Color color, float lifetime, float size, float speed)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // CRITICAL for persistence
            main.playOnAwake = false; // Controlled by VFXController

            var emission = ps.emission;
            emission.rateOverTime = 50f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.1f;

            // Create and save material as asset (fixes pink rendering)
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
            {
                Debug.LogWarning("[WeaponAssetGenerator] URP Particles/Unlit shader not found, using fallback");
                particleShader = Shader.Find("Particles/Standard Unlit");
            }
            
            Material mat = new Material(particleShader);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            
            // CRITICAL: Assign white texture to prevent pink error
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            
            // Save material as asset
            string matPath = $"Assets/_Project/Generated/Materials/Mat_Particle_{parent.name}_{name}.mat";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Reload material from AssetDatabase to get proper asset reference
            mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            renderer.sharedMaterial = mat;

            list.Add(ps);
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
