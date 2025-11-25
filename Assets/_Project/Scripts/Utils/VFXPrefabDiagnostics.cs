using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Diagnostic tool to inspect VFX Prefabs and their materials.
    /// </summary>
    public static class VFXPrefabDiagnostics
    {
        [MenuItem("Tools/Debug Tools/Diagnose VFX Prefabs")]
        public static void DiagnoseVFXPrefabs()
        {
            Debug.Log("=== VFX Prefab Diagnostics ===");

            string[] vfxPrefabs = new string[]
            {
                "Assets/_Project/Prefabs/VFX/Projectile/VFX_MissileTrail.prefab",
                "Assets/_Project/Prefabs/VFX/Projectile/VFX_TorpedoBubbles.prefab",
                "Assets/_Project/Prefabs/VFX/Projectile/VFX_TracerGlow.prefab"
            };

            foreach (string prefabPath in vfxPrefabs)
            {
                Debug.Log($"\n--- Checking: {prefabPath} ---");
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"❌ Prefab not found at {prefabPath}");
                    continue;
                }

                Debug.Log($"✓ Prefab loaded: {prefab.name}");

                // Check AutoFollowVFX component
                var autoFollow = prefab.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
                if (autoFollow == null)
                {
                    Debug.LogWarning($"⚠️ Missing AutoFollowVFX component!");
                }
                else
                {
                    Debug.Log($"✓ AutoFollowVFX component found");
                }

                // Check all particle systems
                var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
                Debug.Log($"Found {particleSystems.Length} particle systems");

                foreach (var ps in particleSystems)
                {
                    Debug.Log($"\n  Particle System: {ps.name}");
                    
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer == null)
                    {
                        Debug.LogError($"    ❌ No ParticleSystemRenderer!");
                        continue;
                    }

                    Material mat = renderer.sharedMaterial;
                    if (mat == null)
                    {
                        Debug.LogError($"    ❌ Material is NULL!");
                        continue;
                    }

                    Debug.Log($"    ✓ Material: {mat.name}");

                    Shader shader = mat.shader;
                    if (shader == null)
                    {
                        Debug.LogError($"    ❌ Shader is NULL!");
                    }
                    else
                    {
                        Debug.Log($"    ✓ Shader: {shader.name}");
                        
                        // Check if shader is valid
                        if (shader.name == "Hidden/InternalErrorShader")
                        {
                            Debug.LogError($"    ❌ SHADER ERROR: Using InternalErrorShader (pink)");
                        }
                    }

                    // Check textures
                    if (mat.HasProperty("_BaseMap"))
                    {
                        Texture baseTex = mat.GetTexture("_BaseMap");
                        Debug.Log($"    _BaseMap: {(baseTex != null ? baseTex.name : "NULL")}");
                    }
                    if (mat.HasProperty("_MainTex"))
                    {
                        Texture mainTex = mat.GetTexture("_MainTex");
                        Debug.Log($"    _MainTex: {(mainTex != null ? mainTex.name : "NULL")}");
                    }
                }
            }

            Debug.Log("\n=== VFX Prefab Diagnostics Complete ===");
        }
    }
}
