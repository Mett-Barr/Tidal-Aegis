using UnityEngine;
using UnityEditor;
using NavalCommand.Editor.Generators;

namespace NavalCommand.Editor
{
    /// <summary>
    /// Automatically configures VFXManager with generated VFX Prefabs.
    /// </summary>
    public static class VFXManagerConfigurator
    {
        private const string VFX_PREFAB_PATH = "Assets/_Project/Prefabs/VFX/Projectile";
        
        /// <summary>
        /// Find VFXManager in scene and configure Trail VFX Prefab references.
        /// </summary>
        public static void ConfigureVFXManager()
        {
            // Find VFXManager in scene
            var vfxManager = GameObject.FindObjectOfType<NavalCommand.Systems.VFX.VFXManager>();
            
            if (vfxManager == null)
            {
                Debug.LogError("[VFXManagerConfigurator] VFXManager not found in scene!");
                return;
            }

            // Load VFX Prefabs
            string vfxPath = "Assets/_Project/Prefabs/VFX/Projectile";
            
            GameObject missileTrail = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_MissileTrail.prefab");
            GameObject torpedoBubbles = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_TorpedoBubbles.prefab");
            GameObject tracerGlow = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_TracerGlow.prefab");
            GameObject muzzleFlash = AssetDatabase.LoadAssetAtPath<GameObject>($"{vfxPath}/VFX_MuzzleFlash.prefab");

            // Use SerializedObject to assign private fields
            SerializedObject so = new SerializedObject(vfxManager);
            
            so.FindProperty("_missileTrailPrefab").objectReferenceValue = missileTrail;
            so.FindProperty("_torpedoBubblesPrefab").objectReferenceValue = torpedoBubbles;
            so.FindProperty("_tracerGlowPrefab").objectReferenceValue = tracerGlow;
            so.FindProperty("_muzzleFlashPrefab").objectReferenceValue = muzzleFlash;

            // Apply changes
            so.ApplyModifiedProperties();

            // Mark scene dirty (configuration persists for current session)
            EditorUtility.SetDirty(vfxManager);

            Debug.Log("[VFX_DEBUG] VFXManager configured:");
            Debug.Log($"  - MissileTrail: {(missileTrail != null ? "✓" : "✗")}");
            Debug.Log($"  - TorpedoBubbles: {(torpedoBubbles != null ? "✓" : "✗")}");
            Debug.Log($"  - TracerGlow: {(tracerGlow != null ? "✓" : "✗")}");
            Debug.Log($"  - MuzzleFlash: {(muzzleFlash != null ? "✓" : "✗")}");
        }
    }
}
