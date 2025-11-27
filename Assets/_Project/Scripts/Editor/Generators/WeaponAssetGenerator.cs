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
            so.Mode = config.Mode;  // NEW: Set firing mode
            so.ProjectileColor = config.ProjectileColor;  // NEW: Set beam/projectile color
            so.TargetType = config.TargetType;
            so.SetBaseRange(config.Range);
            so.SetBaseCooldown(config.Cooldown);
            so.SetBaseDamage(config.Damage);
            so.SetBaseProjectileSpeed(config.ProjectileSpeed);
            so.ImpactProfile = config.ImpactProfile;
            
            // Direct Mapping from Config (No logic!)
            so.SetBaseGravityMultiplier(config.GravityMultiplier);
            so.SetBaseRotationSpeed(config.RotationSpeed);
            so.SetBaseRotationAcceleration(config.RotationAcceleration);
            so.SetBaseSpread(config.Spread);
            so.SetBaseFiringAngleTolerance(config.FiringAngleTolerance);
            
            // Platform Settings
            so.SetBaseCanRotate(config.CanRotate);
            so.SetBaseIsVLS(config.IsVLS);
            so.SetBaseAimingLogicName(config.AimingLogicName);

            // Projectile Prefab (only for Projectile mode)
            if (config.Mode == FiringMode.Projectile)
            {
                string projPath = $"Assets/_Project/Prefabs/Projectiles/{config.ProjectileName}.prefab";
                so.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
            }
            else
            {
                so.ProjectilePrefab = null;  // No projectile for beam weapons
            }
            
            // MuzzleFlash now handled by VFXManager, no longer per-weapon

            so.TargetMask = ~0; // Default to Everything
            
            EditorUtility.SetDirty(so);
        }

        private static void GenerateWeapon(WeaponConfig config)
        {
            // 1. Generate Projectile Prefab (only for Projectile mode)
            if (config.Mode == FiringMode.Projectile)
            {
                CreateProjectilePrefab(config);
            }

            // 2. Create WeaponStats SO
            CreateWeaponStats(config);

            // 3. Generate Laser Cannon Prefab (Special Case)
            if (config.Type == WeaponType.LaserCIWS)
            {
                CreateLaserCannonPrefab(config);
            }
        }

        private static void CreateLaserCannonPrefab(WeaponConfig config)
        {
            string path = "Assets/_Project/Prefabs/Weapons/LaserCannon_Spherical.prefab";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Weapons"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Weapons");

            // 1. Root
            GameObject root = new GameObject("LaserCannon_Spherical");
            
            // 2. Components
            var weaponCtrl = root.AddComponent<NavalCommand.Entities.Components.WeaponController>();
            var rotator = root.AddComponent<NavalCommand.Entities.Components.TurretRotator>();

            // 3. Visual Hierarchy
            // Material (URP Support)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material grayMat = new Material(shader);
            grayMat.color = new Color(0.3f, 0.3f, 0.35f); // Dark Gray Metal
            if (grayMat.HasProperty("_BaseColor")) grayMat.SetColor("_BaseColor", grayMat.color);

            Material darkMat = new Material(shader);
            darkMat.color = new Color(0.1f, 0.1f, 0.15f); // Darker accents
            if (darkMat.HasProperty("_BaseColor")) darkMat.SetColor("_BaseColor", darkMat.color);

            Material lensMat = new Material(shader);
            lensMat.color = Color.cyan;
            if (lensMat.HasProperty("_BaseColor")) lensMat.SetColor("_BaseColor", lensMat.color);
            lensMat.EnableKeyword("_EMISSION");
            if (lensMat.HasProperty("_EmissionColor")) lensMat.SetColor("_EmissionColor", Color.cyan * 2f);
            lensMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            // Save Materials
            string matDir = "Assets/_Project/Generated/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            
            AssetDatabase.CreateAsset(grayMat, $"{matDir}/Mat_LaserCannon_Gray.mat");
            AssetDatabase.CreateAsset(darkMat, $"{matDir}/Mat_LaserCannon_Dark.mat");
            AssetDatabase.CreateAsset(lensMat, $"{matDir}/Mat_LaserCannon_Lens.mat");
            
            // --- Base ---
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform);
            baseObj.transform.localPosition = new Vector3(0, 0.1f, 0); // Height 2 * 0.1 = 0.2. Center at 0.1.
            baseObj.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f); // Wide flat cylinder
            baseObj.GetComponent<Renderer>().sharedMaterial = grayMat;
            GameObject.DestroyImmediate(baseObj.GetComponent<Collider>());

            // --- TurretBase (Azimuth/Yoke) ---
            GameObject turretBase = new GameObject("TurretBase");
            turretBase.transform.SetParent(root.transform);
            turretBase.transform.localPosition = new Vector3(0, 0.2f, 0); // Sit on top of base (0.2 height)
            turretBase.transform.localRotation = Quaternion.identity;

            // Yoke Crossbar
            GameObject yokeCross = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yokeCross.name = "Yoke_Crossbar";
            yokeCross.transform.SetParent(turretBase.transform);
            yokeCross.transform.localPosition = new Vector3(0, 0.2f, 0);
            yokeCross.transform.localScale = new Vector3(2.2f, 0.4f, 0.8f);
            yokeCross.GetComponent<Renderer>().sharedMaterial = grayMat;
            GameObject.DestroyImmediate(yokeCross.GetComponent<Collider>());

            // Yoke Arms
            CreateYokeArm(turretBase.transform, new Vector3(-0.9f, 1.0f, 0), grayMat);
            CreateYokeArm(turretBase.transform, new Vector3(0.9f, 1.0f, 0), grayMat);

            // --- TurretGun (Elevation/Sphere) ---
            GameObject turretGun = new GameObject("TurretGun");
            turretGun.transform.SetParent(turretBase.transform);
            turretGun.transform.localPosition = new Vector3(0, 1.2f, 0); // Center of arms
            turretGun.transform.localRotation = Quaternion.identity;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Turret_Sphere";
            sphere.transform.SetParent(turretGun.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 1.6f;
            sphere.GetComponent<Renderer>().sharedMaterial = grayMat;
            GameObject.DestroyImmediate(sphere.GetComponent<Collider>());

            // --- Lens/Muzzle ---
            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            muzzle.name = "Muzzle_Housing";
            muzzle.transform.SetParent(turretGun.transform);
            muzzle.transform.localRotation = Quaternion.Euler(90, 0, 0); // Face forward
            muzzle.transform.localPosition = new Vector3(0, 0, 0.6f);
            muzzle.transform.localScale = new Vector3(1.0f, 0.2f, 1.0f);
            muzzle.GetComponent<Renderer>().sharedMaterial = darkMat;
            GameObject.DestroyImmediate(muzzle.GetComponent<Collider>());

            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Lens";
            lens.transform.SetParent(muzzle.transform);
            lens.transform.localRotation = Quaternion.identity;
            lens.transform.localPosition = new Vector3(0, 1.0f, 0); // Flush with surface
            lens.transform.localScale = new Vector3(0.7f, 0.01f, 0.7f); // Very thin disk
            lens.GetComponent<Renderer>().sharedMaterial = lensMat;
            GameObject.DestroyImmediate(lens.GetComponent<Collider>());

            // --- FirePoint ---
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(turretGun.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 0.8f); // Exact surface (0.6 + 0.2)
            firePoint.transform.localRotation = Quaternion.identity;

            // 4. Configuration
            rotator.Initialize(turretBase.transform, turretGun.transform, firePoint.transform);
            rotator.MinPitch = -20f;
            rotator.MaxPitch = 85f;

            weaponCtrl.FirePoint = firePoint.transform;
            
            // Assign the generated WeaponStats
            string statsPath = $"Assets/_Project/Data/Weapons/{config.ID}.asset";
            weaponCtrl.WeaponStats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateYokeArm(Transform parent, Vector3 pos, Material mat)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Yoke_Arm";
            arm.transform.SetParent(parent);
            arm.transform.localPosition = pos;
            arm.transform.localScale = new Vector3(0.4f, 1.8f, 0.8f);
            arm.GetComponent<Renderer>().sharedMaterial = mat;
            GameObject.DestroyImmediate(arm.GetComponent<Collider>());
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
            
            // Assign VFX Type based on projectile style (unified pooling system)
            vfxCtrl.VFXType = GetVFXTypeForStyle(config.ProjectileStyle);

            // Visuals (Model only, no particles)
            CreateProjectileVisuals(root, config.ProjectileStyle, config.ProjectileColor);

            // Save
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }
        
        private static NavalCommand.VFX.VFXType GetVFXTypeForStyle(string style)
        {
            return style switch
            {
                "Missile" => NavalCommand.VFX.VFXType.MissileTrail,
                "Torpedo" => NavalCommand.VFX.VFXType.TorpedoBubbles,
                "Laser" => NavalCommand.VFX.VFXType.TracerGlow,      // NEW: Use TracerGlow for laser beam (cyan trail)
                "Tracer" => NavalCommand.VFX.VFXType.None,           // No VFX for Autocannon (visual feedback from tracer model)
                "Tracer_Small" => NavalCommand.VFX.VFXType.None,     // No VFX for CIWS (high fire rate)
                _ => NavalCommand.VFX.VFXType.None // Shell have no VFX
            };
        }

        private static void CreateProjectileVisuals(GameObject parent, string style, Color color)
        {
            // Material Generation
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            
            if (style.Contains("Tracer") || style == "Laser")  // UPDATED: Enable emission for Laser too
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

            // VFX is now spawned independently via VFXPrefab, no inline particles needed

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
                    
                    // NEW: Add Flame particle system as child (Unity best practice)
                    CreateFlameParticleSystem(parent, color);
                    // VFX handled by VFX_MissileTrail.prefab
                    break;
                case "Torpedo":
                    CreatePrimitive(model, PrimitiveType.Cylinder, new Vector3(0.4f, 2f, 0.4f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    CreatePrimitive(model, PrimitiveType.Sphere, new Vector3(0.4f, 0.4f, 0.4f), new Vector3(0, 0, 2f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.8f, 0.05f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    CreatePrimitive(model, PrimitiveType.Cube, new Vector3(0.05f, 0.8f, 0.3f), new Vector3(0, 0, -1.8f), Vector3.zero, mat);
                    // VFX handled by VFX_TorpedoBubbles.prefab
                    break;
                case "Tracer":
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.15f, 1f, 0.15f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    // VFX handled by VFX_TracerGlow.prefab
                    break;
                case "Tracer_Small":
                    // CIWS - Small, subtle tracers to avoid visual clutter from high fire rate
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.08f, 0.6f, 0.08f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    // No VFX for CIWS - the tracer bullets themselves provide visual feedback through high fire rate
                    break;
                case "Laser":  // NEW: Laser projectile (thin, glowing beam)
                    // Very thin, elongated capsule for laser beam appearance
                    CreatePrimitive(model, PrimitiveType.Capsule, new Vector3(0.05f, 2f, 0.05f), Vector3.zero, new Vector3(90, 0, 0), mat);
                    // VFX handled by VFX_TracerGlow.prefab (cyan trail)
                    break;
            }
        }

        /// <summary>
        /// Create Flame particle system for missile projectiles.
        /// Flame is attached as child to follow projectile lifecycle automatically.
        /// </summary>
        private static void CreateFlameParticleSystem(GameObject parent, Color baseColor)
        {
            GameObject flameObj = new GameObject("Flame");
            flameObj.transform.SetParent(parent.transform);
            flameObj.transform.localPosition = new Vector3(0, 0, -1.8f); // Missile rear
            flameObj.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = flameObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 2f; // Low initial speed, velocity over lifetime handles direction
            main.startSize = 0.6f;
            main.startColor = new Color(1f, 0.5f, 0f);
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 80;

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = 120f;

            // Cone shape (will be overridden by velocity)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;

            // CRITICAL: Use velocity over lifetime to make particles move backward
            // This ensures flame always points opposite to missile movement direction
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-15f); // Backward velocity (-Z)

            // Color over lifetime for flame effect
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0f), 0f),    // Bright yellow-orange
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),  // Orange-red
                    new GradientColorKey(new Color(0.5f, 0f, 0f), 1f)     // Dark red
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Create material
            Material flameMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            flameMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f));
            flameMat.SetColor("_Color", new Color(1f, 0.8f, 0.3f));
            flameMat.EnableKeyword("_ALPHABLEND_ON");
            if (flameMat.HasProperty("_Surface"))
                flameMat.SetFloat("_Surface", 1); // Transparent
            if (flameMat.HasProperty("_Blend"))
                flameMat.SetFloat("_Blend", 0); // Alpha blend

            string matPath = $"Assets/_Project/Generated/Materials/Mat_Flame_{parent.name}.mat";
            AssetDatabase.CreateAsset(flameMat, matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            flameMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (flameMat == null)
            {
                Debug.LogWarning($"[WeaponAssetGenerator] Failed to load flame material at {matPath}");
                return;
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = flameMat;

            // Initially disabled - will be enabled by ProjectileBehavior.Initialize()
            flameObj.SetActive(false);
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
