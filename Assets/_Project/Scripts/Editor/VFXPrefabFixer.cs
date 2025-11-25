using UnityEngine;
using UnityEditor;

namespace NavalCommand.Editor
{
    /// <summary>
    /// Tool to manually fix VFX Prefab materials.
    /// </summary>
    public static class VFXPrefabFixer
    {
        private const string MATERIAL_PATH = "Assets/_Project/Generated/Materials";
        
        [MenuItem("Tools/Debug Tools/Fix VFX Prefab Materials")]
        public static void FixVFXPrefabMaterials()
        {
            Debug.Log("=== Fixing VFX Prefab Materials ===");
            
            FixPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_MissileTrail.prefab", 
                      new[] { ("Flame", Color.red), ("Smoke", Color.white) });
            
            FixPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_TorpedoBubbles.prefab", 
                      new[] { ("Bubbles", new Color(1f, 1f, 1f, 0.5f)) });
            
            FixPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_TracerGlow.prefab", 
                      new[] { ("Glow", Color.yellow) });
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("=== VFX Prefab Materials Fixed! ===");
        }
        
        private static void FixPrefab(string prefabPath, (string name, Color color)[] particles)
        {
            Debug.Log($"\nFixing: {prefabPath}");
            
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"❌ Prefab not found: {prefabPath}");
                return;
            }
            
            // Instantiate prefab in scene
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"❌ Failed to instantiate prefab");
                return;
            }
            
            bool modified = false;
            
            foreach (var (name, color) in particles)
            {
                // Find particle system
                Transform psTransform = instance.transform.Find(name);
                if (psTransform == null)
                {
                    Debug.LogWarning($"  ⚠️ Particle system '{name}' not found");
                    continue;
                }
                
                ParticleSystem ps = psTransform.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    Debug.LogWarning($"  ⚠️ No ParticleSystem component on '{name}'");
                    continue;
                }
                
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"  ⚠️ No ParticleSystemRenderer on '{name}'");
                    continue;
                }
                
                // Load material
                string matPath = $"{MATERIAL_PATH}/Mat_VFX_{name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                
                if (mat == null)
                {
                    Debug.LogError($"  ❌ Material not found: {matPath}");
                    continue;
                }
                
                // Assign material
                renderer.sharedMaterial = mat;
                modified = true;
                
                Debug.Log($"  ✓ Assigned material to '{name}': {mat.name} (Shader: {mat.shader.name})");
            }
            
            if (modified)
            {
                // Apply changes back to prefab
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                Debug.Log($"  ✓ Changes applied to prefab");
            }
            
            // Cleanup
            GameObject.DestroyImmediate(instance);
        }
    }
}
