using UnityEngine;
using UnityEditor;
using NavalCommand.Data;
using NavalCommand.Entities.Projectiles;

namespace NavalCommand.Editor
{
    public class InteractionSystemValidator : EditorWindow
    {
        // [MenuItem("Naval Command/Validate Interaction System")]
        public static void ShowWindow()
        {
            GetWindow<InteractionSystemValidator>("Interaction Validator");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Validate All Weapon Configs"))
            {
                ValidateWeapons();
            }
        }

        private void ValidateWeapons()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponConfigSO");
            Debug.Log($"[Validation] Found {guids.Length} Weapon Configs.");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WeaponConfigSO config = AssetDatabase.LoadAssetAtPath<WeaponConfigSO>(path);
                
                if (config.ProjectilePrefab == null)
                {
                    Debug.LogError($"[Validation] <color=red>{config.name}</color>: Missing Projectile Prefab!");
                }
                else
                {
                    var pb = config.ProjectilePrefab.GetComponent<ProjectileBehavior>();
                    if (pb == null)
                    {
                        Debug.LogError($"[Validation] <color=red>{config.name}</color>: Prefab {config.ProjectilePrefab.name} missing ProjectileBehavior!");
                    }
                }

                if (config.WarheadConfig == null)
                {
                    Debug.LogError($"[Validation] <color=red>{config.name}</color>: Missing Warhead Config!");
                }
            }
            Debug.Log("[Validation] Complete.");
        }
    }
}
