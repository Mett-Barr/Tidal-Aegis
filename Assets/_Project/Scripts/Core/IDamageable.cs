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
        Surface,
        Air,
        Missile
    }

    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsDead();
        Team GetTeam();
        UnitType GetUnitType();
    }
}
