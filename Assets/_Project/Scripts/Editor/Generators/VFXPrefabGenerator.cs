using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NavalCommand.Editor.Generators
{
    /// <summary>
    /// Generates VFX Prefabs for projectile trails and effects.
    /// </summary>
    public static class VFXPrefabGenerator
    {
        private const string VFX_PREFAB_PATH = "Assets/_Project/Prefabs/VFX/Projectile";
        private const string MATERIAL_PATH = "Assets/_Project/Generated/Materials";

        [MenuItem("Tools/Generate/VFX Prefabs")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            Debug.Log("[VFXPrefabGenerator] Starting VFX Prefab Generation...");

            // Missile: Flame + Smoke trail
            GenerateMissileTrailVFX();

            // Torpedo: Bubbles
            GenerateTorpedoBubblesVFX();

            // Tracer: Glow trail
            GenerateTracerGlowVFX();

            // NEW: Generate muzzle flash
            GenerateMuzzleFlashVFX();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VFXPrefabGenerator] All VFX Prefabs Generated!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/VFX"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "VFX");
            }
            if (!AssetDatabase.IsValidFolder(VFX_PREFAB_PATH))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/VFX", "Projectile");
            }
        }

        private static void GenerateMissileTrailVFX()
        {
            string path = $"{VFX_PREFAB_PATH}/VFX_MissileTrail.prefab";
            
            // Create materials - Uniform semi-transparent gray smoke
            Material flameMat = CreateParticleMaterial("Flame", new Color(1f, 0.5f, 0f)); // Orange flame
            Material trailMat = CreateTrailMaterial("MissileSmoke", 
                new Color(0.6f, 0.6f, 0.6f),   // Start: Medium gray (semi-transparent feel)
                new Color(0.5f, 0.5f, 0.5f));  // End: Slightly darker gray
            
            if (flameMat == null || trailMat == null)
            {
                Debug.LogError("[VFXPrefabGenerator] Failed to create materials for VFX_MissileTrail");
                return;
            }
            
            GameObject root = new GameObject("VFX_MissileTrail");

            // Add AutoFollowVFX component
            var autoFollow = root.AddComponent<NavalCommand.VFX.AutoFollowVFX>();
            autoFollow.AutoDestructDelay = 1.0f;

            // HYBRID SYSTEM: TrailRenderer (continuous) + ParticleSystems (dynamic)
            
            // 1. TrailRenderer - Faster fade (2.5s lifetime, fades in first 1s)
            CreateTrailRenderer(root, "SmokeTrail", trailMat, 2.5f, 0.3f, 1.2f);
            
            // NOTE: Flame particle system is now created as part of the Projectile Prefab
            // in WeaponAssetGenerator.CreateFlameParticleSystem()
            // This follows Unity best practices: continuous effects are children of the GameObject

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VFXPrefabGenerator] Created {path} (Uniform gray smoke, slow fade)");
        }

        private static void GenerateTorpedoBubblesVFX()
        {
            string path = $"{VFX_PREFAB_PATH}/VFX_TorpedoBubbles.prefab";
            
            // BUBBLES ONLY - No trail effect
            Material bubbleMat = CreateParticleMaterial("Bubbles", Color.white);
            
            if (bubbleMat == null)
            {
                Debug.LogError("[VFXPrefabGenerator] Failed to create materials for VFX_TorpedoBubbles");
                return;
            }
            
            GameObject root = new GameObject("VFX_TorpedoBubbles");

            var autoFollow = root.AddComponent<NavalCommand.VFX.AutoFollowVFX>();
            autoFollow.AutoDestructDelay = 1.0f;

            // ONLY bubbles, NO TrailRenderer (no smoke effect)
            GameObject bubblesObj = CreateParticleSystem(root, "Bubbles", 
                Color.white,     // Pure white
                0.6f,  // lifetime: short
                0.3f,  // size: small
                0.2f,  // speed: slow rise
                bubbleMat, 
                8f);   // emission: very sparse

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VFXPrefabGenerator] Created {path} (Bubbles only, no trail)");
        }

        private static void GenerateTracerGlowVFX()
        {
            string path = $"{VFX_PREFAB_PATH}/VFX_TracerGlow.prefab";
            
            // Create materials FIRST
            Material glowMat = CreateParticleMaterial("Glow", Color.yellow);
            Material trailMat = CreateTrailMaterial("TracerGlow", Color.yellow, new Color(1f, 0.8f, 0f));
            
            if (glowMat == null || trailMat == null)
            {
                Debug.LogError("[VFXPrefabGenerator] Failed to create materials for VFX_TracerGlow");
                return;
            }
            
            GameObject root = new GameObject("VFX_TracerGlow");

            var autoFollow = root.AddComponent<NavalCommand.VFX.AutoFollowVFX>();
            autoFollow.AutoDestructDelay = 1.5f;

            // HYBRID SYSTEM
            CreateTrailRenderer(root, "GlowTrail", trailMat, 1.0f, 0.15f, 0.6f);
            GameObject glowObj = CreateParticleSystem(root, "Glow", Color.yellow, 0.2f, 0.3f, 3f, glowMat, 10f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VFXPrefabGenerator] Created {path} (Hybrid: TrailRenderer + Particles)");
        }

        /// <summary>
        /// Generate yellow muzzle flash VFX for weapon firing.
        /// </summary>
        private static void GenerateMuzzleFlashVFX()
        {
            string path = $"{VFX_PREFAB_PATH}/VFX_MuzzleFlash.prefab";
            
            GameObject root = new GameObject("VFX_MuzzleFlash");
            
            // Particle System
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = 0.1f;
            main.startSpeed = 2f;
            main.startSize = 0.6f;
            main.startColor = new Color(1f, 0.95f, 0.3f);
            main.loop = false;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.Callback;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 4) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.3f, 0.3f, 0.01f);

            // CRITICAL FIX: Save material as Asset (not just in-memory)
            // Create material with proper shader
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                Debug.LogError("[VFXPrefabGenerator] Legacy Additive shader not found!");
                shader = Shader.Find("Mobile/Particles/Additive");
            }
            
            Material flashMat = new Material(shader);
            flashMat.color = new Color(1f, 0.95f, 0.3f, 1f);
            
            // Save material as asset (CRITICAL: prevents NULL material in prefab)
            string matPath = $"{MATERIAL_PATH}/Mat_MuzzleFlash.mat";
            
            // Ensure Materials directory exists
            if (!AssetDatabase.IsValidFolder(MATERIAL_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            
            // Delete old material if exists
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
            {
                AssetDatabase.DeleteAsset(matPath);
            }
            
            // Create and save material asset
            AssetDatabase.CreateAsset(flashMat, matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Reload material from AssetDatabase (CRITICAL: ensures proper reference)
            flashMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            if (flashMat == null)
            {
                Debug.LogError($"[VFXPrefabGenerator] Failed to load material from {matPath}");
                return;
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = flashMat;  // Now using asset reference, not in-memory object

            // Add auto-recycle
            var autoRecycle = root.AddComponent<NavalCommand.VFX.AutoRecycleVFX>();
            autoRecycle.RecycleDelay = 0.15f;
            autoRecycle.VFXTypeToRecycle = NavalCommand.VFX.VFXType.MuzzleFlash;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VFXPrefabGenerator] Created {path} (Yellow muzzle flash)");
        }

        private static GameObject CreateParticleSystem(GameObject parent, string name, Color color, float lifetime, float size, float speed, Material mat, float emissionRate = 20f)
        {
            GameObject psObj = new GameObject(name);
            psObj.transform.SetParent(parent.transform);
            psObj.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.loop = true;
            main.playOnAwake = false;
            
            // CRITICAL FIX: Force Local simulation space
            // Without this, particles stay in world coordinates even after GameObject is hidden!
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            // CRITICAL: For flame particles, limit max particles to prevent residue
            if (name.Contains("Flame") || name.Contains("Glow"))
            {
                main.maxParticles = 5; // Very few particles
            }

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = emissionRate;

            // Shape
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;

            // Color over Lifetime - Alpha fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            
            // Size over Lifetime - Expansion
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = AnimationCurve.Linear(0, 1, 1, 2);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer
            var renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = mat;

            return psObj;
        }

        private static Material CreateParticleMaterial(string name, Color color)
        {
            string matPath = $"{MATERIAL_PATH}/Mat_VFX_{name}.mat";
            
            // EXACT SAME LOGIC AS WeaponAssetGenerator.CreateProjectileVisuals
            
            // Create shader reference
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
            {
                Debug.LogWarning("[VFXPrefabGenerator] URP Particles/Unlit shader not found, using fallback");
                particleShader = Shader.Find("Particles/Standard Unlit");
            }
            
            Material mat = new Material(particleShader);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            
            // Assign white texture
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(MATERIAL_PATH))
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
                Debug.LogError($"[VFXPrefabGenerator] Failed to load material at {matPath}");
                return null;
            }
            
            return mat;
        }

        private static Material CreateTrailMaterial(string name, Color startColor, Color endColor)
        {
            string matPath = $"{MATERIAL_PATH}/Mat_Trail_{name}.mat";
            
            // CRITICAL: Use shader that supports vertex colors for gradient alpha
            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader == null)
            {
                Debug.LogWarning("[VFXPrefabGenerator] URP Particles/Unlit shader not found, using fallback");
                trailShader = Shader.Find("Particles/Standard Unlit");
            }
            
            Material mat = new Material(trailShader);
            
            // CRITICAL: Enable vertex color modulation
            if (mat.HasProperty("_BaseColorMap")) mat.SetTexture("_BaseColorMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            
            // CRITICAL: Set color mode to multiply with vertex color
            if (mat.HasProperty("_ColorMode"))
            {
                mat.SetFloat("_ColorMode", 0); // Multiply mode
            }
            
            // Enable transparency
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent
            }
            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0); // Alpha blend
            }
            
            // Set render queue for transparency
            mat.renderQueue = 3000;
            
            // Save as asset
            if (!AssetDatabase.IsValidFolder(MATERIAL_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Debug.LogError($"[VFXPrefabGenerator] Failed to load trail material at {matPath}");
                return null;
            }
            
            return mat;
        }

        /// <summary>
        /// Create TrailRenderer component with fade gradient.
        /// </summary>
        private static TrailRenderer CreateTrailRenderer(GameObject parent, string name, Material mat, float time, float startWidth, float endWidth)
        {
            TrailRenderer trail = parent.AddComponent<TrailRenderer>();
            
            trail.time = time;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.material = mat;
            trail.minVertexDistance = 0.1f;
            trail.autodestruct = false;
            trail.emitting = true;
            
            // Width expansion curve for dramatic smoke dissipation effect
            // Makes smoke expand significantly as it fades (newer = thin, older = very wide)
            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0.0f, 0.8f);  // Newest: 80% width (slightly thin)
            widthCurve.AddKey(0.3f, 1.5f);  // 30%: 150% width
            widthCurve.AddKey(0.6f, 3.0f);  // 60%: 300% width (expanding rapidly)
            widthCurve.AddKey(1.0f, 5.0f);  // Oldest: 500% width (dramatic dissipation)
            trail.widthCurve = widthCurve;
            
            // Faster fade: Alpha reaches 0 at 40% of trail lifetime
            // Remaining 60% is invisible grace period
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0.0f),
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1.0f)
                },
                new GradientAlphaKey[] {
                    // Fade completes in first 40% (1s out of 2.5s)
                    new GradientAlphaKey(0.75f, 0.0f),   // Position 0.0 (newest): visible
                    new GradientAlphaKey(0.5f, 0.15f),   // Position 0.15: fading
                    new GradientAlphaKey(0.2f, 0.3f),    // Position 0.3: mostly faded
                    new GradientAlphaKey(0.0f, 0.4f),    // Position 0.4: FADE COMPLETE (1s)
                    new GradientAlphaKey(0.0f, 1.0f)     // Position 0.4-1.0: invisible (1.5s grace)
                }
            );
            trail.colorGradient = gradient;
            
            return trail;
        }
    }
}
