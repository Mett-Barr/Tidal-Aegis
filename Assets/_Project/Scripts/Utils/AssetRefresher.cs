using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    public class AssetRefresher
    {
        public static void RefreshProjectilePrefabs()
        {
            Debug.Log("[AssetRefresher] Refreshing projectile prefabs...");
            
            string[] projectileNames = new string[]
            {
                "Projectile_FlagshipGun",
                "Projectile_Missile",
                "Projectile_Torpedo",
                "Projectile_Autocannon",
                "Projectile_CIWS"
            };

            foreach (string name in projectileNames)
            {
                string path = $"Assets/_Project/Prefabs/Projectiles/{name}.prefab";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[AssetRefresher] Reimported {path}");
            }
            
            AssetDatabase.Refresh();
            Debug.Log("[AssetRefresher] Refresh complete!");
        }
    }
}
