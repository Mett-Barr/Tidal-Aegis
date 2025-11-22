using UnityEngine;
using NavalCommand.Data;

namespace NavalCommand.Core
{
    public static class BallisticsConfig
    {
        // Real-world speeds in m/s (Approximate)
        // Real-world speeds in m/s (Source: US Navy specs)
        // Real-world speeds in m/s (Source: US Navy specs)
        public const float RealSpeed_BattleshipGun = 762f; // 16"/50 caliber Mark 7 (approx 2500 fps)
        public const float RealSpeed_NavalGun = 823f;    // Mk 45 Mod 4 5-inch gun (approx 2700 fps)
        public const float RealSpeed_Missile = 290f;     // Harpoon Cruise Missile (High Subsonic, Mach 0.85)
        public const float RealSpeed_Torpedo = 28f;      // Mk 48 ADCAP (~55 knots)
        public const float RealSpeed_Autocannon = 1100f; // Mk 38 25mm (approx 1100 m/s)
        public const float RealSpeed_CIWS = 1100f;       // Phalanx CIWS (approx 1100 m/s)

        // Real-world ranges in meters
        public const float RealRange_BattleshipGun = 150000f; // ~150km (Exaggerated for gameplay to get 15km range)
        public const float RealRange_NavalGun = 24000f;      // ~24km
        public const float RealRange_Missile = 120000f;      // ~120km
        public const float RealRange_Torpedo = 10000f;       // ~10km
        public const float RealRange_Autocannon = 2500f;     // ~2.5km
        public const float RealRange_CIWS = 1500f;           // ~1.5km

        // Global Scaling
        // 1.0 = Realistic speeds
        public static float GlobalSpeedScale = 0.2f; 
        public static float GlobalRangeScale = 0.1f; // 1/10th scale for gameplay

        public static float GetSpeed(WeaponType type)
        {
            float baseSpeed = 0f;
            switch (type)
            {
                case WeaponType.FlagshipGun: baseSpeed = RealSpeed_BattleshipGun; break;
                case WeaponType.Missile: baseSpeed = RealSpeed_Missile; break;
                case WeaponType.Torpedo: baseSpeed = RealSpeed_Torpedo; break;
                case WeaponType.Autocannon: baseSpeed = RealSpeed_Autocannon; break;
                case WeaponType.CIWS: baseSpeed = RealSpeed_CIWS; break;
            }
            return baseSpeed * GlobalSpeedScale;
        }

        public static float GetRange(WeaponType type)
        {
            float baseRange = 0f;
            switch (type)
            {
                case WeaponType.FlagshipGun: baseRange = RealRange_BattleshipGun; break;
                case WeaponType.Missile: baseRange = RealRange_Missile; break;
                case WeaponType.Torpedo: baseRange = RealRange_Torpedo; break;
                case WeaponType.Autocannon: baseRange = RealRange_Autocannon; break;
                case WeaponType.CIWS: baseRange = RealRange_CIWS; break;
            }
            return baseRange * GlobalRangeScale;
        }

        public static float GetGravityMultiplier(WeaponType type)
        {
            // If we scale speed down, we might need to scale gravity down 
            // to maintain similar trajectory shapes, OR keep it 1.0 for "heavy" feel.
            // For now, let's keep it 1.0 for ballistic, 0 for powered.
            
            // Missiles and Torpedoes are powered/guided, so they don't use ballistic gravity
            if (type == WeaponType.Missile || type == WeaponType.Torpedo) return 0f;

            // For ballistic weapons, we need to ensure the projectile can physically reach the max range.
            // Max Ballistic Range formula: R = v^2 / g
            // Therefore, required gravity: g = v^2 / R
            
            float v = GetSpeed(type);
            float r = GetRange(type);
            
            if (r <= 1f) return 1f; // Avoid division by zero

            // Use 9.81f as standard gravity reference
            float g_req = (v * v) / r;
            float multiplier = g_req / 9.81f; 

            // We only need to REDUCE gravity if it's too strong (multiplier < 1). 
            // If we have enough speed (multiplier > 1), we stick to standard gravity (1.0) for realism.
            return Mathf.Min(multiplier, 1f);
        }
    }
}
