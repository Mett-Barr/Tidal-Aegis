using UnityEngine;

namespace NavalCommand.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public enum UnitType
    {
        Ship,       // Surface Ships
        Aircraft,   // Planes, Helicopters
        Missile,    // Guided Missiles
        Torpedo,    // Underwater Guided
        Shell       // Unguided Projectiles
    }

    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsDead();
        Team GetTeam();
        UnitType GetUnitType();
    }
}
