using UnityEngine;
using UnityEditor;
using NavalCommand.Data;
using System.IO;

namespace NavalCommand.Editor
{
    public class AssetGenerator
    {
        // [MenuItem("Naval Command/Generate Weapon Assets")]
        public static void Generate()
        {
            string weaponPath = "Assets/_Project/Data/Weapons";
            string warheadPath = "Assets/_Project/Data/Warheads";
            
            if (!Directory.Exists(weaponPath)) Directory.CreateDirectory(weaponPath);
            if (!Directory.Exists(warheadPath)) Directory.CreateDirectory(warheadPath);

            foreach (var legacyConfig in WeaponRegistry.AllWeapons)
            {
                // 1. Create WarheadConfigSO
                WarheadConfigSO warhead = ScriptableObject.CreateInstance<WarheadConfigSO>();
                warhead.Damage = legacyConfig.Damage;
                // Heuristic for Radius based on ImpactSize
                warhead.ExplosionRadius = GetRadiusFromSize(legacyConfig.ImpactProfile.Size);
                warhead.ImpactProfile = legacyConfig.ImpactProfile;
                
                string warheadName = $"Warhead_{legacyConfig.ID.Replace("Weapon_", "")}";
                string fullWarheadPath = $"{warheadPath}/{warheadName}.asset";
                
                // Check if exists to avoid overwriting if we run multiple times (optional, but good practice)
                // Actually, for migration we WANT to overwrite to ensure parity.
                AssetDatabase.CreateAsset(warhead, fullWarheadPath);

                // 2. Create WeaponConfigSO
                WeaponConfigSO weapon = ScriptableObject.CreateInstance<WeaponConfigSO>();
                weapon.WeaponName = legacyConfig.DisplayName;
                weapon.Type = legacyConfig.Type;
                weapon.TargetCapability = legacyConfig.TargetType;
                weapon.Range = legacyConfig.Range;
                weapon.Cooldown = legacyConfig.Cooldown;
                weapon.Spread = legacyConfig.Spread;
                
                // Find Prefab
                string prefabPath = $"Assets/_Project/Prefabs/Projectiles/{legacyConfig.ProjectileName}.prefab";
                weapon.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (weapon.ProjectilePrefab == null) Debug.LogError($"Could not find prefab at {prefabPath}");

                weapon.ProjectileSpeed = legacyConfig.ProjectileSpeed;
                weapon.RotationSpeed = legacyConfig.RotationSpeed;
                weapon.WarheadConfig = warhead;
                weapon.MovementLogicName = legacyConfig.MovementLogicName;
                weapon.CruiseHeight = legacyConfig.CruiseHeight;
                weapon.TerminalHomingDistance = legacyConfig.TerminalHomingDistance;
                weapon.VerticalLaunchHeight = legacyConfig.VerticalLaunchHeight;
                weapon.TurnRate = legacyConfig.TurnRate;
                weapon.ProjectileColor = legacyConfig.ProjectileColor;
                weapon.ProjectileStyle = legacyConfig.ProjectileStyle;

                string weaponAssetName = legacyConfig.ID;
                AssetDatabase.CreateAsset(weapon, $"{weaponPath}/{weaponAssetName}.asset");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Weapon Assets Generated!");
        }

        private static float GetRadiusFromSize(NavalCommand.Systems.VFX.ImpactSize size)
        {
            switch (size)
            {
                case NavalCommand.Systems.VFX.ImpactSize.Massive: return 50f;
                case NavalCommand.Systems.VFX.ImpactSize.Large: return 20f;
                case NavalCommand.Systems.VFX.ImpactSize.Medium: return 10f;
                default: return 0f;
            }
        }
    }
}
