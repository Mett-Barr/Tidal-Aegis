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
            Range = 150000f,
            Cooldown = 3f,
            Damage = 30f,
            ProjectileSpeed = 762f,
            RotationSpeed = 30f,
            Spread = 0.1f,
            
            ProjectileName = "Projectile_FlagshipGun",
            ProjectileColor = Color.yellow,
            ProjectileStyle = "Shell",
            MovementLogicName = "Ballistic",
            AimingLogicName = "Ballistic",
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Large)
        };

        public static readonly WeaponConfig Missile = new WeaponConfig("Weapon_Missile_Basic", "Missile Launcher", WeaponType.Missile, TargetCapability.Ship | TargetCapability.Aircraft)
        {
            Range = 120000f,
            Cooldown = 10f,
            Damage = 60f,
            ProjectileSpeed = 290f,
            RotationSpeed = 45f,
            Spread = 0f,

            ProjectileName = "Projectile_Missile",
            ProjectileColor = Color.red,
            ProjectileStyle = "Missile",
            MovementLogicName = "GuidedMissile",
            CruiseHeight = 15f,
            TerminalHomingDistance = 50f,
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
            Range = 10000f,
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
            Range = 2500f,
            Cooldown = 0.2f,
            Damage = 5f,
            ProjectileSpeed = 1100f,
            RotationSpeed = 120f,
            Spread = 0.8f,

            ProjectileName = "Projectile_Autocannon",
            ProjectileColor = new Color(1f, 0.5f, 0f),
            ProjectileStyle = "Tracer",
            MovementLogicName = "Ballistic",
            AimingLogicName = "Ballistic",
            ImpactProfile = new ImpactProfile(ImpactCategory.Kinetic, ImpactSize.Small)
        };

        public static readonly WeaponConfig CIWS = new WeaponConfig("Weapon_CIWS_Basic", "CIWS", WeaponType.CIWS, TargetCapability.Aircraft | TargetCapability.Missile)
        {
            Range = 1500f,
            Cooldown = 0.004f, // 15000 RPM
            Damage = 2f,
            ProjectileSpeed = 1100f,
            RotationSpeed = 115f,
            Spread = 0.3f,

            ProjectileName = "Projectile_CIWS",
            ProjectileColor = Color.white,
            ProjectileStyle = "Tracer_Small",
            MovementLogicName = "Ballistic",
            ImpactProfile = new ImpactProfile(ImpactCategory.Explosive, ImpactSize.Small),
            
            // Platform Settings
            GravityMultiplier = 1f, // Explicitly set
            FiringAngleTolerance = 2f, // Precise
            AimingLogicName = "Ballistic"
        };

        static WeaponRegistry()
        {
            AllWeapons.Add(FlagshipGun);
            AllWeapons.Add(Missile);
            AllWeapons.Add(Torpedo);
            AllWeapons.Add(Autocannon);
            AllWeapons.Add(CIWS);
        }
    }
}
