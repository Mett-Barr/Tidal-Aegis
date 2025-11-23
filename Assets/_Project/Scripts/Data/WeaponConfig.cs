using UnityEngine;
using NavalCommand.Entities.Projectiles;

namespace NavalCommand.Data
{
    public class WeaponConfig
    {
        public string ID;
        public string DisplayName;
        public WeaponType Type;
        public TargetCapability TargetType;
        public NavalCommand.Systems.VFX.ImpactProfile ImpactProfile;
        
        // Stats
        public float Range;
        public float Cooldown;
        public float Damage;
        public float ProjectileSpeed;
        public float RotationSpeed;
        public float Spread;

        // Projectile Visuals & Behavior
        public string ProjectileName;
        public Color ProjectileColor;
        public string ProjectileStyle; // "Shell", "Missile", "Torpedo", "Tracer", "Tracer_Small"
        public string MovementLogicName; // "Ballistic", "GuidedMissile", "Torpedo", "Linear"
        
        // Advanced Projectile Settings
        public float CruiseHeight = 0f;
        public float TerminalHomingDistance = 0f;
        public float VerticalLaunchHeight = 0f;
        public float TurnRate = 0f;

        public WeaponConfig(string id, string displayName, WeaponType type, TargetCapability targetType)
        {
            ID = id;
            DisplayName = displayName;
            Type = type;
            TargetType = targetType;
        }
    }
}
