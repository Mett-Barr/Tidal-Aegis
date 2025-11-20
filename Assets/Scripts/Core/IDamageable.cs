using UnityEngine;

namespace NavalCommand.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsDead();
        Team GetTeam();
    }
}
