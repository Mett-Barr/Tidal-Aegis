using System.Collections.Generic;
using UnityEngine;
using NavalCommand.Systems.VFX;

namespace NavalCommand.Data
{
    public static class WeaponRegistry
    {
        public static readonly List<WeaponConfig> AllWeapons = new List<WeaponConfig>();

        public static readonly WeaponConfig FlagshipGun = new WeaponConfig("Weapon_FlagshipGun_Basic", "Flagship Gun", WeaponType.FlagshipGun, TargetCapability.Ship)
        {
            Role = WeaponRole.MainGun,   // NEW: Main battery role
            Range = 150000f,
            Cooldown = 3f,
            Damage = 30f,
            ProjectileSpeed = 762f,
            RotationSpeed = 15f, // "Game Feel" Heavy
            Spread = 0.4f, // ~0.4-0.6 degrees for large caliber
            FiringAngleTolerance = 1.0f, // Relaxed from 0.5 to allow SmoothDamp settling
            
            ProjectileName = "Projectile_FlagshipGun",
            ProjectileColor = Color.yellow,
            ProjectileStyle = "Shell",
            MovementLogicName = "Ballistic",
            AimingLogicName = "AdvancedPredictive",
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Large)
        };

        public static readonly WeaponConfig Missile = new WeaponConfig("Weapon_Missile_Basic", "Missile Launcher", WeaponType.Missile, TargetCapability.Ship | TargetCapability.Aircraft)
        {
            Role = WeaponRole.MissileLauncher,  // NEW: VLS launcher role
            Range = 120000f,
            Cooldown = 10f,
            Damage = 60f,
            ProjectileSpeed = 290f,
            RotationSpeed = 45f,
            Spread = 0f,

            ProjectileName = "Projectile_Missile",
            ProjectileColor = new Color(0.7f, 0.7f, 0.7f), // Gray to avoid confusion with orange flame
            ProjectileStyle = "Missile",
            MovementLogicName = "GuidedMissile",
            CruiseHeight = 15f,
            TerminalHomingDistance = 150f, // Optimized for short-range testing: 150m cruise + 150m terminal (90° @ 240°/s = 0.375s × 290m/s = 109m)
            VerticalLaunchHeight = 20f,
            TurnRate = 15f,
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Massive),
            
            // Platform Settings
            GravityMultiplier = 0f,
            CanRotate = false,
            IsVLS = true,
            FiringAngleTolerance = 180f, // VLS doesn't need to aim
            AimingLogicName = "Direct"
        };

        public static readonly WeaponConfig Torpedo = new WeaponConfig("Weapon_Torpedo_Basic", "Torpedo Tube", WeaponType.Torpedo, TargetCapability.Ship)
        {
            Role = WeaponRole.TorpedoLauncher,  // NEW: Torpedo launcher role
            Range = 100000f,
            Cooldown = 12f,
            Damage = 100f,
            ProjectileSpeed = 28f,
            RotationSpeed = 30f,
            Spread = 0f,

            ProjectileName = "Projectile_Torpedo",
            ProjectileColor = Color.blue,
            ProjectileStyle = "Torpedo",
            MovementLogicName = "Torpedo",
            CruiseHeight = -2f,
            TerminalHomingDistance = 30f,
            VerticalLaunchHeight = 0f,
            TurnRate = 1f,
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Massive),
            
            // Platform Settings
            GravityMultiplier = 0f,
            CanRotate = true,
            IsVLS = false,
            AimingLogicName = "Direct"
        };

        public static readonly WeaponConfig Autocannon = new WeaponConfig("Weapon_Autocannon_Basic", "Autocannon", WeaponType.Autocannon, TargetCapability.Ship | TargetCapability.Aircraft)
        {
            Role = WeaponRole.Secondary,  // NEW: Secondary battery role
            Range = 2500f,
            Cooldown = 0.2f,
            Damage = 5f,
            ProjectileSpeed = 1100f,
            RotationSpeed = 80f, // "Game Feel" Snappy
            Spread = 0.3f, // ~0.3 degrees
            FiringAngleTolerance = 1.0f, // Relaxed from 0.5

            ProjectileName = "Projectile_Autocannon",
            ProjectileColor = new Color(1f, 0.5f, 0f),
            ProjectileStyle = "Tracer",
            MovementLogicName = "Ballistic",
            AimingLogicName = "AdvancedPredictive",
            ImpactProfile = new ImpactProfile(ImpactCategory.Kinetic, ImpactSize.Small)
            
            // Platform Settings
            // GravityMultiplier default is 1.0
        };

        public static readonly WeaponConfig CIWS = new WeaponConfig("Weapon_CIWS_Basic", "CIWS", WeaponType.CIWS, TargetCapability.Aircraft | TargetCapability.Missile)
        {
            Role = WeaponRole.PointDefense,  // NEW: Point defense role
            Range = 1500f,
            Cooldown = 0.004f, // 15000 RPM
            Damage = 5f,  // 5 HP/hit (1-shot kill on 3HP missiles, reliable last-line defense)
            ProjectileSpeed = 1100f,
            RotationSpeed = 120f, // "Game Feel" Fast
            Spread = 0.1f, // ~0.1 degrees (Tight)

            ProjectileName = "Projectile_CIWS",
            ProjectileColor = new Color(0.85f, 0.65f, 0.35f), // Brass/Copper tracer color
            ProjectileStyle = "Tracer_Small",
            MovementLogicName = "Ballistic",
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Small),
            
            // Platform Settings
            GravityMultiplier = 1f, // Explicitly set
            FiringAngleTolerance = 3.0f, // Relaxed significantly for fast movers (was 0.1)
            AimingLogicName = "AdvancedPredictive"
        };

        // === LASER CIWS: Role-Kinematics-Payload Demonstration ===
        public static readonly WeaponConfig LaserCIWS = new WeaponConfig(
            "Weapon_LaserCIWS_Basic", 
            "Laser CIWS", 
            WeaponType.LaserCIWS, 
            TargetCapability.Aircraft | TargetCapability.Missile
        ) {
            // === NEW: Beam Mode ===
            Mode = FiringMode.Beam,  // Continuous beam, not projectile
            
            // === ROLE: Point Defense (Same as CIWS) ===
            Role = WeaponRole.PointDefense,
            Range = 2000f,                   // Beam max range
            RotationSpeed = 150f,            // Fast tracking
            FiringAngleTolerance = 2.0f,     // Can fire with slight misalignment
            
            // === PAYLOAD: Energy Damage (as DPS) ===
            ImpactProfile = new ImpactProfile(ImpactCategory.Energy, ImpactSize.Small),
            Damage = 1.5f,                   // 1.5 DPS (3 HP / 1.5 DPS = 2s kill, balanced with CIWS 2-shot)
            Cooldown = 0.1f,                 // Beam activation delay
            ProjectileColor = new Color(0f, 1f, 1f),  // Cyan beam
            
            // === KINEMATICS: N/A for Beam ===
            // MovementLogicName not used
            // GravityMultiplier not used
            // ProjectileSpeed not used
            ProjectileName = "Beam_Laser",   // Placeholder
            ProjectileStyle = "Laser",       // For future reference
            
            // === AIMING ===
            AimingLogicName = "Direct",      // Direct aim (raycast)
            CanRotate = true
        };

        static WeaponRegistry()
        {
            AllWeapons.Add(FlagshipGun);
            AllWeapons.Add(Missile);
            AllWeapons.Add(Torpedo);
            AllWeapons.Add(Autocannon);
            AllWeapons.Add(CIWS);
            AllWeapons.Add(LaserCIWS);  // NEW
        }
    }
}
