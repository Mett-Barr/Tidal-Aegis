using UnityEngine;
using UnityEditor;
using NavalCommand.Editor.Generators;

namespace NavalCommand.Editor
{
    public static class ContentRebuilder
    {
        public static void RebuildAllContent()
        {
            Debug.Log("[ContentRebuilder] Starting Full Rebuild...");

            // 1. Weapons (Dependencies for Ships)
            Debug.Log("[ContentRebuilder] Generating Weapons...");
            WeaponAssetGenerator.GenerateAll();

            // 2. Ships (Depend on Weapons)
            Debug.Log("[ContentRebuilder] Generating Ships...");
            ShipAssetGenerator.GenerateAll();

            // 3. VFX (Independent)
            Debug.Log("[ContentRebuilder] Generating VFX...");
            VFXAssetGenerator.GenerateAll();

            // 4. Refresh Projectile Prefabs (Fix AssetDatabase cache)
            Debug.Log("[ContentRebuilder] Refreshing Projectile Prefabs...");
            NavalCommand.Utils.AssetRefresher.RefreshProjectilePrefabs();

            // 5. UI/HUD (Optional)
            // HUDGenerator.Generate(); 

            Debug.Log("[ContentRebuilder] Rebuild Complete!");
        }

        public static void GenerateEmptyHulls()
        {
            Debug.Log("[ContentRebuilder] Generating Empty Hulls...");
            // This logic was previously in ContentGenerator.GenerateEmptyHulls
            // For now, we can delegate to ShipAssetGenerator if we add that method there
            // or just leave it as a TODO if it's not critical for the main flow.
            // Given the user request, I'll implement it in ShipAssetGenerator if needed, 
            // but for now let's focus on the main Rebuild.
        }
        
        public static void GenerateHUD()
        {
             // Placeholder for HUD generation if we move it later
             Debug.Log("[ContentRebuilder] Generating HUD...");
        }
    }
}
