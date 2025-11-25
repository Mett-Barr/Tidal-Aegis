using UnityEngine;
using UnityEditor;

namespace NavalCommand.Editor.Diagnostics
{
    public static class CIWSVerifier
    {
        [MenuItem("Tools/Debug/Verify CIWS Hierarchy")]
        public static void Verify()
        {
            Debug.Log("=== Verifying CIWS Hierarchy ===");
            
            // Load Ship_Light_CIWS prefab
            string path = "Assets/_Project/Prefabs/Enemies/Ship_Light_CIWS.prefab";
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (ship == null)
            {
                Debug.LogError($"❌ Ship prefab not found at {path}. Did you run Rebuild World?");
                return;
            }

            // Find Weapon_CIWS
            Transform weapon = FindRecursive(ship.transform, "Weapon_CIWS");
            if (weapon == null)
            {
                Debug.LogError("❌ Weapon_CIWS not found in ship hierarchy!");
                return;
            }
            Debug.Log("✅ Found Weapon_CIWS");

            // Check TurretBase
            Transform baseObj = weapon.Find("TurretBase");
            if (baseObj == null)
            {
                Debug.LogError("❌ TurretBase not found under Weapon_CIWS!");
                return;
            }
            Debug.Log("✅ Found TurretBase (Yaw Axis)");

            // Check TurretGun
            Transform gunObj = baseObj.Find("TurretGun");
            if (gunObj == null)
            {
                Debug.LogError("❌ TurretGun not found under TurretBase!");
                return;
            }
            Debug.Log("✅ Found TurretGun (Pitch Axis)");

            // Check FirePoint
            Transform firePoint = gunObj.Find("FirePoint");
            if (firePoint == null)
            {
                Debug.LogError("❌ FirePoint not found under TurretGun!");
                return;
            }
            Debug.Log("✅ Found FirePoint under TurretGun");

            Debug.Log("=== Verification Passed! Hierarchy is correct. ===");
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
