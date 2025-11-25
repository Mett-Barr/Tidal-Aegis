using UnityEditor;
using System.IO;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Utility to clean VFX Prefabs before regeneration.
    /// </summary>
    public static class VFXCleaner
    {
        private const string VFX_PREFAB_PATH = "Assets/_Project/Prefabs/VFX/Projectile";
        
        public static void CleanVFXPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(VFX_PREFAB_PATH))
            {
                UnityEngine.Debug.Log($"[VFXCleaner] VFX Prefab folder doesn't exist: {VFX_PREFAB_PATH}");
                return;
            }
            
            UnityEngine.Debug.Log($"[VFXCleaner] Cleaning VFX Prefabs at {VFX_PREFAB_PATH}...");
            
            // Delete the entire VFX Projectile folder
            bool success = AssetDatabase.DeleteAsset(VFX_PREFAB_PATH);
            
            if (success)
            {
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[VFXCleaner] ✓ VFX Prefabs cleaned successfully!");
            }
            else
            {
                UnityEngine.Debug.LogError("[VFXCleaner] Failed to delete VFX Prefabs folder");
            }
        }
    }
}
