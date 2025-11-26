using UnityEngine;
using NavalCommand.Entities.Projectiles;

namespace NavalCommand.Data
{
    public class WeaponConfig
    {
        public string ID;
        public string DisplayName;
        public WeaponType Type;                  // Legacy (for serialization/lookup)
        public WeaponRole Role;                  // NEW: Explicit tactical role
        public FiringMode Mode = FiringMode.Projectile;  // NEW: Projectile vs Beam
        public TargetCapability TargetType;
        public NavalCommand.Systems.VFX.ImpactProfile ImpactProfile;
        
        // Stats
        public float Range;
        public float Cooldown;
        public float Damage;
        public float ProjectileSpeed;
        public float RotationSpeed;
        public float RotationAcceleration; // New: deg/s^2
        public float Spread;

        // Projectile Visuals & Behavior
        public string ProjectileName;
        public Color ProjectileColor;
        public string ProjectileStyle; // "Shell", "Missile", "Torpedo", "Tracer", "Tracer_Small"
        public string MovementLogicName; // "Ballistic", "GuidedMissile", "Torpedo", "Linear"
        public string AimingLogicName;   // "Ballistic", "Direct", "Predictive"
        
        // Advanced Projectile Settings
        public float CruiseHeight = 0f;
        public float TerminalHomingDistance = 0f;
        public float VerticalLaunchHeight = 0f;
        public float TurnRate = 0f;

        // Platform & Aiming Settings
        public float GravityMultiplier = 1.0f; // Default 1.0 for Ballistic/Linear
        public bool CanRotate = true;          // Default true (Turrets)
        public bool IsVLS = false;             // Default false
        public float FiringAngleTolerance = 1.0f; // Default 1.0 degree (Tighter standard)

        public WeaponConfig(string id, string displayName, WeaponType type, TargetCapability targetType)
        {
            ID = id;
            DisplayName = displayName;
            Type = type;
            TargetType = targetType;
        }
    }
}
