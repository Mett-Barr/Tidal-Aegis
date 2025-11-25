using UnityEngine;
using UnityEditor;

namespace NavalCommand.Editor.Diagnostics
{
    /// <summary>
    /// Diagnostic tools for VFX shader issues.
    /// Helps identify why muzzle flash shows as pink cube.
    /// </summary>
    public static class VFXShaderDiagnostics
    {
        [MenuItem("Tools/Debug/VFX Diagnostics/1. Check Available Shaders")]
        public static void CheckShaders()
        {
            Debug.Log("=== [VFX DIAGNOSTICS] Checking Available Shaders ===");
            
            string[] shaderNames = new string[]
            {
                "Legacy Shaders/Particles/Additive",
                "Particles/Standard Unlit",
                "Universal Render Pipeline/Particles/Unlit",
                "Mobile/Particles/Additive",
                "Sprites/Default",
                "Unlit/Color"
            };
            
            foreach (var name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                string status = shader != null ? "✓ EXISTS" : "✗ NOT FOUND";
                Debug.Log($"  {status}: {name}");
            }
        }

        [MenuItem("Tools/Debug/VFX Diagnostics/2. Inspect MuzzleFlash Prefab")]
        public static void InspectMuzzleFlashPrefab()
        {
            Debug.Log("=== [VFX DIAGNOSTICS] Inspecting VFX_MuzzleFlash.prefab ===");
            
            string path = "Assets/_Project/Prefabs/VFX/Projectile/VFX_MuzzleFlash.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null)
            {
                Debug.LogError($"  ✗ Prefab NOT FOUND at: {path}");
                return;
            }
            
            Debug.Log($"  ✓ Prefab exists at: {path}");
            
            var ps = prefab.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                Debug.LogError("  ✗ No ParticleSystem component");
                return;
            }
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                Debug.LogError("  ✗ No ParticleSystemRenderer component");
                return;
            }
            
            Material mat = renderer.sharedMaterial;
            if (mat == null)
            {
                Debug.LogError("  ✗ Material is NULL");
                return;
            }
            
            Debug.Log($"  ✓ Material name: {mat.name}");
            
            if (mat.shader == null)
            {
                Debug.LogError("  ✗ Shader is NULL (This causes pink cube!)");
            }
            else
            {
                Debug.Log($"  ✓ Shader name: {mat.shader.name}");
                Debug.Log($"  ✓ Material color: {mat.color}");
            }
        }

        [MenuItem("Tools/Debug/VFX Diagnostics/3. List All VFX Prefabs")]
        public static void ListVFXPrefabs()
        {
            Debug.Log("=== [VFX DIAGNOSTICS] Listing VFX Prefabs ===");
            
            string path = "Assets/_Project/Prefabs/VFX/Projectile";
            
            if (!System.IO.Directory.Exists(path))
            {
                Debug.LogWarning($"  ✗ Directory NOT FOUND: {path}");
                return;
            }
            
            string[] prefabs = System.IO.Directory.GetFiles(path, "*.prefab");
            Debug.Log($"  Found {prefabs.Length} prefab(s):");
            
            foreach (var p in prefabs)
            {
                Debug.Log($"    - {System.IO.Path.GetFileName(p)}");
            }
        }

        [MenuItem("Tools/Debug/VFX Diagnostics/4. Test Material Creation")]
        public static void TestMaterialCreation()
        {
            Debug.Log("=== [VFX DIAGNOSTICS] Testing Material Creation ===");
            
            // Try to find shader
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            
            if (shader == null)
            {
                Debug.LogError("  ✗ Shader 'Legacy Shaders/Particles/Additive' NOT FOUND!");
                
                // Try fallback
                shader = Shader.Find("Mobile/Particles/Additive");
                if (shader != null)
                {
                    Debug.LogWarning("  ⚠ Using fallback: Mobile/Particles/Additive");
                }
                else
                {
                    Debug.LogError("  ✗ No fallback shader found either!");
                    return;
                }
            }
            else
            {
                Debug.Log($"  ✓ Shader found: {shader.name}");
            }
            
            // Create material
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.95f, 0.3f, 1f);
            
            // Save as asset
            string testPath = "Assets/_Project/Generated/Materials/TEST_DiagnosticMaterial.mat";
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(testPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogWarning($"  ⚠ Creating directory: {dir}");
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Generated");
                AssetDatabase.CreateFolder("Assets/_Project/Generated", "Materials");
            }
            
            AssetDatabase.CreateAsset(mat, testPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"  ✓ Test material created at: {testPath}");
            
            // Reload and verify
            Material reloaded = AssetDatabase.LoadAssetAtPath<Material>(testPath);
            
            if (reloaded == null)
            {
                Debug.LogError("  ✗ Failed to reload material!");
            }
            else if (reloaded.shader == null)
            {
                Debug.LogError("  ✗ Reloaded material has NULL shader!");
            }
            else
            {
                Debug.Log($"  ✓ Reloaded successfully");
                Debug.Log($"  ✓ Reloaded shader: {reloaded.shader.name}");
            }
        }

        [MenuItem("Tools/Debug/VFX Diagnostics/5. Run Full Diagnostic")]
        public static void RunFullDiagnostic()
        {
            Debug.Log("╔═══════════════════════════════════════════════════════════════╗");
            Debug.Log("║          VFX MUZZLE FLASH FULL DIAGNOSTIC                     ║");
            Debug.Log("╚═══════════════════════════════════════════════════════════════╝");
            Debug.Log("");
            
            CheckShaders();
            Debug.Log("");
            
            InspectMuzzleFlashPrefab();
            Debug.Log("");
            
            ListVFXPrefabs();
            Debug.Log("");
            
            TestMaterialCreation();
            Debug.Log("");
            
            Debug.Log("╔═══════════════════════════════════════════════════════════════╗");
            Debug.Log("║          DIAGNOSTIC COMPLETE                                  ║");
            Debug.Log("╚═══════════════════════════════════════════════════════════════╝");
        }
    }
}
