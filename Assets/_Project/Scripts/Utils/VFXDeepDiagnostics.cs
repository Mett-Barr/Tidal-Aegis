using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Deep diagnostic tool to compare VFX and Projectile prefab structures.
    /// </summary>
    public static class VFXDeepDiagnostics
    {
        [MenuItem("Tools/Debug Tools/Deep VFX vs Projectile Comparison")]
        public static void DeepComparison()
        {
            Debug.Log("=== DEEP VFX vs PROJECTILE COMPARISON ===\n");
            
            // Compare working projectile with broken VFX
            Debug.Log("--- WORKING PROJECTILE (Projectile_Missile) ---");
            InspectPrefabStructure("Assets/_Project/Prefabs/Projectiles/Projectile_Missile.prefab");
            
            Debug.Log("\n--- BROKEN VFX (VFX_MissileTrail) ---");
            InspectPrefabStructure("Assets/_Project/Prefabs/VFX/Projectile/VFX_MissileTrail.prefab");
            
            Debug.Log("\n=== COMPARISON COMPLETE ===");
        }
        
        private static void InspectPrefabStructure(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ Prefab not found: {prefabPath}");
                return;
            }
            
            Debug.Log($"✓ Prefab: {prefab.name}");
            Debug.Log($"  Active: {prefab.activeSelf}");
            Debug.Log($"  Children Count: {prefab.transform.childCount}");
            
            // List all children
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                Transform child = prefab.transform.GetChild(i);
                Debug.Log($"  └─ Child {i}: {child.name} (Active: {child.gameObject.activeSelf})");
                
                // Check components
                Component[] components = child.GetComponents<Component>();
                foreach (var comp in components)
                {
                    Debug.Log($"      └─ Component: {comp.GetType().Name}");
                    
                    // If it's a Renderer, check material
                    if (comp is Renderer renderer)
                    {
                        Debug.Log($"          └─ Material: {(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "NULL")}");
                        if (renderer.sharedMaterial != null)
                        {
                            Debug.Log($"          └─ Shader: {renderer.sharedMaterial.shader.name}");
                        }
                    }
                    
                    // If it's a ParticleSystemRenderer, check material
                    if (comp is ParticleSystemRenderer psRenderer)
                    {
                        Debug.Log($"          └─ ParticleSystem Material: {(psRenderer.sharedMaterial != null ? psRenderer.sharedMaterial.name : "NULL")}");
                        if (psRenderer.sharedMaterial != null)
                        {
                            Debug.Log($"          └─ Shader: {psRenderer.sharedMaterial.shader.name}");
                        }
                    }
                }
            }
            
            // Check root components
            Component[] rootComps = prefab.GetComponents<Component>();
            Debug.Log($"  Root Components:");
            foreach (var comp in rootComps)
            {
                Debug.Log($"    └─ {comp.GetType().Name}");
            }
        }
    }
}
